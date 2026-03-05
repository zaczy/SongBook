using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Maui.Data;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Pages;

public partial class CategoriesPage : ContentPage, INotifyPropertyChanged
{
    public ObservableCollection<SongCategoryEntity> Categories { get; } = new();

    private readonly SongCategoryRepositoryLite? _repo;
    private readonly SongRepositoryLite? _songRepo;
    private readonly SongListViewModel _songListViewModel;
    private bool _selectionChanged = false;

    // expose as public property (ICommand) so XAML binding via x:Reference can find it
    public System.Windows.Input.ICommand FetchCategoriesCommand { get; }

    public CategoriesPage(SongListViewModel vm)
    {
        FetchCategoriesCommand = new Command(async () => await RefreshFromApi());

        InitializeComponent();
        BindingContext = this;

        try
        {
            var mauiContext = Application.Current?.Handler?.MauiContext;
            var services = mauiContext?.Services;
            _repo = services?.GetService(typeof(SongCategoryRepositoryLite)) as SongCategoryRepositoryLite;
            _songRepo = services?.GetService(typeof(SongRepositoryLite)) as SongRepositoryLite;
            _songListViewModel = vm ?? throw new ArgumentNullException(nameof(vm));
        }
        catch
        {
            _repo = null;
            _songRepo = null;
            _songListViewModel = vm;
        }

        _ = LoadAsync();
    }

    /// <summary>
    /// PObierz kategorie z lokalnej bazy, jeœli pusta to zainicjuj danymi z API. Aktualizuje ObservableCollection dla UI.
    /// Po za³adowaniu przywraca automatyczne zaznaczenie kafelków, dla których w encjach `IsSelected == true`.
    /// </summary>
    /// <returns></returns>
    private async Task LoadAsync()
    {
        try
        {
            if (_repo == null)
            {
                await DisplayAlert("Error", "Category repository not available (DI not configured).", "OK");
                return;
            }

            // Seed defaults if empty
            if (!_repo.HasCategories)
                await this.RefreshFromApi();

            var list = await _repo.GetAllAsync();
            Categories.Clear();
            foreach (var c in list)
                Categories.Add(c);

            // Restore selection for items that were persisted with IsSelected = true.
            // Update CollectionView.SelectedItems on the UI thread to avoid threading issues.
            var toSelect = Categories.Where(c => c.IsSelected).ToList();
            if (toSelect.Count > 0)
            {
                await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        CategoriesCollection.SelectedItems.Clear();
                        foreach (var item in toSelect)
                        {
                            if (!CategoriesCollection.SelectedItems.Contains(item))
                                CategoriesCollection.SelectedItems.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Restore selection error: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load categories: {ex.Message}", "OK");
        }

        OnPropertyChanged(nameof(IsEmptyCategories));
        OnPropertyChanged(nameof(HasCategories));


    }

    /// <summary>
    /// Pobierz kategorie z API
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await this.RefreshFromApi();
    }

    private async Task RefreshFromApi()
    {
        try
        {
            IsBusy = true;

            if (_repo == null)
            {
                await DisplayAlert("Error", "Repository not available.", "OK");
                return;
            }

            await _repo.DeleteAllAsync();
            await _repo.LoadCategoriesFromApiAsync();

            await LoadAsync();

            IsBusy = false;

            OnPropertyChanged(nameof(IsEmptyCategories));
            OnPropertyChanged(nameof(HasCategories));
        }
        catch (Exception ex)
        {
            IsBusy = false;
            await DisplayAlert("Houston, mamy problem!", $"B³¹d odœwie¿ania listy kategorii: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Dodaj kategoriê
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (_repo == null)
        {
            await DisplayAlert("Error", "Repository not available.", "OK");
            return;
        }

        string name = await DisplayPromptAsync("Nowa kategoria", "Nazwa kategorii:");
        if (string.IsNullOrWhiteSpace(name)) return;

        var entity = new SongCategoryEntity { Name = name.Trim(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await _repo.AddAsync(entity);
        await LoadAsync();
    }

    /// <summary>
    /// Usuñ kategoriê z urz¹dzenia
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (CategoriesCollection.SelectedItem is not SongCategoryEntity selected)
        {
            await DisplayAlert("Uwaga", "Wybierz kategoriê do usuniêcia.", "OK");
            return;
        }

        bool ok = await DisplayAlert("Usuñ", $"Usun¹æ kategoriê '{selected.Name}'?", "Tak", "Nie");
        if (!ok) return;

        if (_repo != null)
        {
            await _repo.DeleteAsync(selected.Id);
            await LoadAsync();
        }
    }

    /// <summary>
    /// Wywo³ane w czasie dotkniêcia kafelka kategorii
    /// </summary>
    private async void OnTileTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (sender is not Border border) return;
            if (border.BindingContext is not SongCategoryEntity entity) return;

            entity.IsSelected = !entity.IsSelected;

            if(_repo != null)
            {
                await _repo.UpdateAsync(entity);
            }

            var selectedItems = CategoriesCollection.SelectedItems;
            var prev = selectedItems.Cast<object>().ToList();

            if (entity.IsSelected)
            {
                if (!selectedItems.Contains(entity))
                    selectedItems.Add(entity);
            }
            else
            {
                if (selectedItems.Contains(entity))
                    selectedItems.Remove(entity);
            }

            var current = selectedItems.Cast<object>().ToList();

            _selectionChanged = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnTileTapped error: {ex.Message}");
        }
    }

    /// <summary>
    /// Kod wywo³ywany przy wyjœciu ze strony.
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if(_selectionChanged)
            await FetchSelectedCategoriesSongsAsync();
    }

    /// <summary>
    /// Pobierz piosenki dla ka¿dej wybranej kategorii. Ka¿da kategoria jest pobierana osobno, aby unikn¹æ problemów z du¿ymi zapytaniami. B³êdy s¹ logowane, ale nie przerywaj¹ ca³ego procesu.
    /// Je¿eli nie wybrano ¿adnej kategorii, zapytaj u¿ytkownika, czy na pewno chce usun¹æ wszystkie piosenki z bazy danych.
    /// </summary>
    /// <returns></returns>
    private async Task FetchSelectedCategoriesSongsAsync()
    {
        try
        {
            if (_songRepo == null) return;

            var selectedCategories = Categories.Where(c => c.IsSelected).ToList();
            if (!selectedCategories.Any())
            {
                // Ask user to confirm deleting all songs
                bool confirm = await DisplayAlert(
                    "Brak wybranych kategorii",
                    "Nie wybrano ¿adnej kategorii. Czy na pewno chcesz usun¹æ wszystkie piosenki z bazy danych?",
                    "Tak, usuñ wszystko",
                    "Anuluj");

                if (!confirm)
                {
                    return;
                }
            }

            _songListViewModel.IsBusy = true;
            await _songRepo.DeleteAllAsync();

            if (selectedCategories?.Count > 0)
            {
                foreach (var cat in selectedCategories)
                {
                    try
                    {
                        await _songRepo.FetchCategorySongsFromApiAsync(cat.Id, cat);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"FetchCategorySongsFromApiAsync for category {cat.Id} failed: {ex.Message}");
                    }
                }
            }

            _songListViewModel.IsBusy = false;
            await _songListViewModel.LoadSongsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FetchSelectedCategoriesSongsAsync error: {ex.Message}");
        }
    }

    private void CategoriesCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //_selectionChanged = true;
    }

    public bool IsEmptyCategories => Categories == null || Categories.Count == 0;
    public bool HasCategories => Categories != null && Categories.Count > 0;

    //public event PropertyChangedEventHandler? PropertyChanged;
    //protected virtual void OnPropertyChanged(string propertyName) =>
    //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}