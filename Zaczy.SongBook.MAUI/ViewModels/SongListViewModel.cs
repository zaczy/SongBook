using Microsoft.Extensions.Options;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.Api;
using Zaczy.SongBook;
using Zaczy.SongBook.MAUI.Pages;

namespace Zaczy.SongBook.MAUI.ViewModels;

public class SongListViewModel : INotifyPropertyChanged
{

    public SongListViewModel(SongRepositoryLite repo, IOptions<Settings> options)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _apiBaseUrl = options.Value.ApiBaseUrl;
        LoadCommand = new Command(async () => await LoadSongsAsync());
        FilterCommand = new Command(async () => await LoadSongsAsync());
        ClearCommand = new Command(async () =>
        {
            TitleFilter = string.Empty;
            PageFilter = string.Empty;
            await LoadSongsAsync();
        });
        PageCommand = new Command(async () => await LoadSongsByPageAsync());
        CategoriesCommand = new Command(async () => await LoadCategoriesPage());

        // Command to import from API and refresh local DB/UI
        FetchCommand = new Command(async () => await FetchFromApiAndLoadAsync());
    }

    private readonly SongRepositoryLite _repo;

    private readonly string _apiBaseUrl;

    public ObservableCollection<SongEntity> Songs { get; } = new();

    private string? _titleFilter;
    /// <summary>
    /// Tytu³ utworu
    /// </summary>
    public string? TitleFilter
    {
        get => _titleFilter;
        set
        {
            if (_titleFilter != value)
            {
                _titleFilter = value;
                OnPropertyChanged(nameof(TitleFilter));
            }
        }
    }

    private string? _pageFilter;
    /// <summary>
    /// Wykonawca
    /// </summary>
    public string? PageFilter
    {
        get => _pageFilter;
        set
        {
            if (_pageFilter != value)
            {
                _pageFilter = value;
                OnPropertyChanged(nameof(PageFilter));
            }
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy != value)
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }

    public ICommand LoadCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand FetchCommand { get; }
    public ICommand PageCommand { get; }
    public ICommand CategoriesCommand { get; }

    /// <summary>
    /// Pobierz listê utworów z bazy lokalnej (z uwzglêdnieniem filtrów)
    /// </summary>
    /// <returns></returns>
    public async Task LoadSongsAsync()
    {
        if (IsBusy) return;

        try
        {
            if(string.IsNullOrEmpty(TitleFilter) && !string.IsNullOrEmpty(PageFilter))
            {
                await this.LoadSongsByPageAsync();
                return;
            }

            IsBusy = true;

            var all = await _repo.GetAllAsync();
            var query = all.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TitleFilter))
            {
                query = query.Where(s => (!string.IsNullOrEmpty(s.Title) && s.Title.Contains(TitleFilter, StringComparison.OrdinalIgnoreCase))
                                        || (!string.IsNullOrEmpty(s.Artist) && s.Artist.Contains(TitleFilter, StringComparison.OrdinalIgnoreCase))
                                        );
            }

            var ordered = query.OrderBy(s => s.Title ?? string.Empty).ToList();

            Songs.Clear();
            foreach (var s in ordered)
                Songs.Add(s);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public async Task LoadSongsByPageAsync()
    {
        if(_pageFilter == null)
            return;

        int pageNumber = int.Parse(_pageFilter);

        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var all = await _repo.GetAllAsync();
            var query = all.AsEnumerable();
            query = query.Where(s => s.Id == pageNumber);
            var ordered = query.OrderBy(s => s.Title ?? string.Empty).ToList();
            Songs.Clear();
            foreach (var s in ordered)
                Songs.Add(s);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// IdŸ do utworu o konkretnym ID (podanym w PageFilter) - pobierz go z bazy i poka¿ na liœcie. Przydatne do testowania szybkiego dostêpu do konkretnego rekordu.
    /// </summary>
    /// <returns></returns>
    public async Task GoToSongByPageAsync()
    {
        if (_pageFilter == null)
            return;

        int pageNumber = int.Parse(_pageFilter);

        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var all = await _repo.GetAllAsync();
            var query = all.AsEnumerable();
            var song = query.Where(s => s.Id == pageNumber).FirstOrDefault();
        }
        finally
        {
            IsBusy = false;
        }
    }


    /// <summary>
    /// Fetches all songs from {_apiBaseUrl}/songs/all, persists them into local LiteDB
    /// (inserts new or updates existing by Title+Artist), then reloads the Songs collection.
    /// </summary>
    public async Task FetchFromApiAndLoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var apiClient = new ApiClient(_apiBaseUrl);

            var apiResponse = await apiClient.GetAsync<List<Song>>("songs/all");

            if (!apiResponse.IsSuccess || apiResponse.Data == null)
            {
                System.Diagnostics.Debug.WriteLine($"FetchFromApiAndLoadAsync: API error: {apiResponse.ErrorMessage} {apiResponse.ErrorDetails}");
                return;
            }

            var remoteSongs = apiResponse.Data;

            // For diagnostics: count before
            var before = await _repo.GetAllAsync();
            System.Diagnostics.Debug.WriteLine($"FetchFromApiAndLoadAsync: Before import count = {before.Count}");

            foreach (var remote in remoteSongs)
            {
                // case-insensitive search by title+artist
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var existing = await _repo.SearchOnlySongAsync(remote!.Title!, remote!.Artist!);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                if (existing != null)
                {
                    existing.initFromSong(remote!);
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _repo.UpdateAsync(existing);
                }
                else
                {
                    var entity = new SongEntity();
                    entity.initFromSong(remote!);
                    await _repo.AddAsync(entity);
                }
            }

            // Diagnostics: count after DB changes
            var after = await _repo.GetAllAsync();
            System.Diagnostics.Debug.WriteLine($"FetchFromApiAndLoadAsync: After import count = {after.Count}");

            // Ensure the ObservableCollection is updated on the main thread
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // reload UI collection from local DB (applies filters)
                await LoadSongsAsync();
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


    private async Task LoadCategoriesPage()
    {
        try
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Prefer the Page from the first Window (single-window apps).
                var page = Application.Current?.Windows?.FirstOrDefault()?.Page;

                // If no Page found, fall back to Shell navigation (common in MAUI Shell apps).
                var navigation = page?.Navigation ?? Shell.Current?.Navigation;

                if (navigation != null)
                {
                    await navigation.PushAsync(new CategoriesPage(this));
                    return;
                }

                // As a last resort try MainPage if available (kept as non-obsolete fallback).
                var fallbackPage = Application.Current?.MainPage;
                if (fallbackPage?.Navigation != null)
                {
                    await fallbackPage.Navigation.PushAsync(new CategoriesPage(this));
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadCategoriesPage error: {ex.Message}");
        }
    }

}