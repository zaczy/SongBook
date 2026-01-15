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

namespace Zaczy.SongBook.MAUI.ViewModels
{
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
                ArtistFilter = string.Empty;
                await LoadSongsAsync();
            });

            // Command to import from API and refresh local DB/UI
            FetchCommand = new Command(async () => await FetchFromApiAndLoadAsync());
        }

        private readonly SongRepositoryLite _repo;

        private readonly string _apiBaseUrl;

        public ObservableCollection<SongEntity> Songs { get; } = new();

        private string? _titleFilter;
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

        private string? _artistFilter;
        public string? ArtistFilter
        {
            get => _artistFilter;
            set
            {
                if (_artistFilter != value)
                {
                    _artistFilter = value;
                    OnPropertyChanged(nameof(ArtistFilter));
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

        public async Task LoadSongsAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var all = await _repo.GetAllAsync();
                var query = all.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(TitleFilter))
                {
                    query = query.Where(s => !string.IsNullOrEmpty(s.Title) &&
                                              s.Title.Contains(TitleFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(ArtistFilter))
                {
                    query = query.Where(s => !string.IsNullOrEmpty(s.Artist) &&
                                              s.Artist.Contains(ArtistFilter, StringComparison.OrdinalIgnoreCase));
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
                    // nothing to import or request failed
                    return;
                }

                var remoteSongs = apiResponse.Data;

                foreach (var remote in remoteSongs)
                {
                    // Use title+artist exact match helper from repository
                    var existing = await _repo.SearchOnlySongAsync(remote.Title ?? string.Empty, remote.Artist ?? string.Empty);

                    if (existing != null)
                    {
                        existing.initFromSong(remote);
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _repo.UpdateAsync(existing);
                    }
                    else
                    {
                        var entity = new SongEntity();
                        entity.initFromSong(remote);
                        await _repo.AddAsync(entity);
                    }
                }

                // Reload UI collection from local DB (applies filters)
                await LoadSongsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}