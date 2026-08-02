using MahApps.Metro.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.WPF.Pages;


public enum SongSyncDirection
{
    ApiToLocal,
    LocalToApi
}
public partial class SongSyncWindow : MetroWindow
{
    private List<SelectableSongComparisionResult> _items = new();
    private ViewModel _viewModel;
    private SongSyncDirection _direction;

    public bool IsApiToLocal => _direction == SongSyncDirection.ApiToLocal;
    public bool IsLocalToApi => _direction == SongSyncDirection.LocalToApi;

    public List<SelectableSongComparisionResult> Items
    {
        get => _items;
    }

    public List<SongComparisionResults> SelectedResults =>
        _items.Where(i => i.IsSelected)
              .Select(i => i.Inner)
              .ToList();

    public SongSyncWindow(List<SongComparisionResults> songComparisionResults, ViewModel viewModel, SongSyncDirection direction)
    {
        _viewModel = viewModel;
        _direction = direction;

        InitializeComponent();

        _items = songComparisionResults
            .Select(r => new SelectableSongComparisionResult(r))
            .ToList();

        foreach (var item in _items)
            item.PropertyChanged += Item_PropertyChanged;

        //DataContext = _items;
        DataContext = this;

        CountLabel.Text = $"({_items.Count} pozycji)";
        UpdateSelectedCount();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableSongComparisionResult.IsSelected))
            UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        var count = _items.Count(i => i.IsSelected);
        SelectedCountLabel.Text = count > 0 ? $"Zaznaczono: {count}" : string.Empty;
        SyncSelectedButton.IsEnabled = count > 0;
        SyncUpSelectedButton.IsEnabled = count > 0;
    }

    /// <summary>
    /// Zaznaczy wszystkie
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.IsSelected = true;
    }

    /// <summary>
    /// Odznacz wszystkie
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.IsSelected = false;
    }

    /// <summary>
    /// Synchronizuj zaznaczone elementy
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SyncSelected_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;

        string message = String.Empty;
        foreach(var s in SelectedResults)
        {
            if (!string.IsNullOrEmpty(message))
                message += ", ";
            message += $"\"{s.SongTitle} (id: API {s.ApiSong?.Id}, local {s.BaseSongEntity?.Id})\"";
        }

        if (!string.IsNullOrEmpty(_viewModel.AppSettings.Settings.ApiBaseUrl) && !string.IsNullOrEmpty(_viewModel.AppSettings.ConnectionStrings.SongBookDb))
        {
            SongApi songApi = new SongApi(_viewModel.AppSettings.Settings.ApiBaseUrl);

            var factory = new SongBookDbContextFactory();
            var songRepository = new SongRepository(factory.CreateDbContext(_viewModel.AppSettings.ConnectionStrings.SongBookDb, _viewModel.AppSettings.Settings.DbProvider));

            await songApi.CreateOrUpdateSongsAsync(songRepository, SelectedResults);
        }

        Close();
    }

    /// <summary>
    /// Zamknięcie okna bez synchronizacji. DialogResult=false, więc można rozróżnić, czy użytkownik zamknął okno, czy kliknął "Synchronizuj".
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;

        Close();
    }

    private async void SyncUpSelected_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;

        if (!string.IsNullOrEmpty(_viewModel.AppSettings.Settings.ApiBaseUrl))
        {
            SongApi songApi = new SongApi(_viewModel.AppSettings.Settings.ApiBaseUrl);

            List<SongEntity> songList = new List<SongEntity>();
            foreach (var s in SelectedResults)
            {
                if (s.BaseSongEntity != null)
                {
                    songList.Add(s.BaseSongEntity);
                }
            }

            await songApi.SyncSelectedApiAsync(songList);
        }

        Close();
    }
}

/// <summary>
/// Wrapper dodający obsługę checkboxa do SongComparisionResults
/// </summary>
public class SelectableSongComparisionResult : INotifyPropertyChanged
{
    private bool _isSelected;

    public SongComparisionResults Inner { get; }

    public string? SongTitle => Inner.SongTitle;
    public string? DiffSummary => Inner.DiffSummary;
    public string FieldsSummary => Inner.FieldsSummary;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public SelectableSongComparisionResult(SongComparisionResults inner)
    {
        Inner = inner;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
