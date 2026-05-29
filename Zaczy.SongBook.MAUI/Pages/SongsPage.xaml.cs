using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using MauiIcons.Core;
using Microsoft.Extensions.Options;
using Microsoft.Maui.Controls;
using Plugin.Maui.Audio;
using System;
using Zaczy.SongBook;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.Db;
using Zaczy.SongBook.MAUI.Extensions;
using Zaczy.SongBook.MAUI.Services;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Pages;

public partial class SongsPage : ContentPage
{
    private readonly SongListViewModel _songListViewModel;
    private readonly UserViewModel _userViewModel;
    private readonly EventApi _eventApi;
    private readonly Settings _settings;
    private readonly IAudioManager _audioManager;
    private readonly SongCustomSettingsRepositoryLite _customSettingsRepository;
    private readonly SingingGroupRepositoryLite _singingGroupRepositoryLite;
    private readonly ListenersGroupBroadcastService _listenersGroupBroadcastService;

    //private SingingGroupEntity? _currentlySelectedSingingGroup;

    public SongListViewModel SongListViewModel => _songListViewModel;
    public UserViewModel UserViewModel => _userViewModel;
    //IBluetoothGroupService? _bluetoothGroupService;

    public SongsPage(
        SongListViewModel vm,
        UserViewModel viewModel,
        EventApi eventApi,
        IOptions<Settings> settings,
        IAudioManager audioManager,
        SongCustomSettingsRepositoryLite customSettingsRepository,
        SingingGroupRepositoryLite singingGroupRepositoryLite,
        //IBluetoothGroupService? bluetoothGroupService 
        ListenersGroupBroadcastService listenersGroupBroadcastService
        )
    {
        _ = new MauiIcon() { Icon = MauiIcons.Fluent.FluentIcons.BluetoothSearching20, IconColor = Colors.Green };
        _ = new MauiIcon() { Icon = MauiIcons.FontAwesome.Solid.FontAwesomeSolidIcons.ArrowRotateLeft, IconColor = Colors.Green };
        _ = new MauiIcon() { Icon = MauiIcons.Fluent.Filled.FluentFilledIcons.Settings20Filled, IconColor = Colors.Green };

        InitializeComponent();

        _songListViewModel = vm ?? throw new ArgumentNullException(nameof(vm));
        _userViewModel = viewModel;
        BindingContext = _songListViewModel;
        _eventApi = eventApi;
        _settings = settings.Value;
        _audioManager = audioManager;
        _customSettingsRepository = customSettingsRepository ?? throw new ArgumentNullException(nameof(customSettingsRepository));
        _singingGroupRepositoryLite = singingGroupRepositoryLite ?? throw new ArgumentNullException(nameof(singingGroupRepositoryLite));
        //_bluetoothGroupService = bluetoothGroupService;
        _listenersGroupBroadcastService = listenersGroupBroadcastService ?? throw new ArgumentNullException(nameof(listenersGroupBroadcastService));

        // register to receive updates
        WeakReferenceMessenger.Default.Register<SongsPage, ValueChangedMessage<SongEntity>>(this, (page, message) =>
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var updatedSong = message.Value;
                    var existing = _songListViewModel.Songs.FirstOrDefault(s => s.Id == updatedSong.Id);
                    if (existing != null)
                    {
                        var idx = _songListViewModel.Songs.IndexOf(existing);
                        if (idx >= 0)
                        {
                            // replace item to force UI refresh
                            _songListViewModel.Songs[idx] = updatedSong;
                        }
                        else
                        {
                            // update properties in place
                            existing.Title = updatedSong.Title;
                            existing.Artist = updatedSong.Artist;
                            existing.Capo = updatedSong.Capo;
                            existing.Lyrics = updatedSong.Lyrics;
                            existing.UpdatedAt = updatedSong.UpdatedAt;
                        }
                    }
                    else
                    {
                        // not present — add and keep ordering
                        _songListViewModel.Songs.Add(updatedSong);
                        var sorted = _songListViewModel.Songs.OrderBy(s => s.Title ?? string.Empty).ToList();
                        _songListViewModel.Songs.Clear();
                        foreach (var s in sorted)
                            _songListViewModel.Songs.Add(s);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SongUpdated handler error: {ex.Message}");
            }
        });

        _ = this.GetUserDataFromServer();
    }

    /// <summary>
    /// Pobierz dane użytkownika z serwera (czy jest adminem, edytorem, arl do Deezer) i zaktualizuj ViewModel.
    /// </summary>
    /// <returns></returns>
    private async Task GetUserDataFromServer()
    {

        if (!string.IsNullOrEmpty(_userViewModel.UserEmail))
        {
            var userApi = new UserApi(_settings.ApiBaseUrl);
            var user = await userApi.GetUserByEmailAsync(_userViewModel.UserEmail);
            if(user != null)
            {
                _userViewModel.IsAdmin = user.IsAdmin;
                _userViewModel.IsEditor = user.IsEditor;
                _userViewModel.DeezerArl = user.DeezerArl;
            }
        }
    }

    /// <summary>
    /// Wykonywane, kiedy strona staje się widoczna. Jeśli lista piosenek jest pusta, ładuje je z bazy danych. 
    /// Dzięki temu dane są odświeżane przy każdym wejściu na stronę, ale tylko jeśli jest to potrzebne (np. po dodaniu nowej piosenki).
    /// Jeśli dane są już załadowane, nie wykonuje ponownie operacji ładowania.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        RefreshListeningGroupsProperties();

        if (_songListViewModel.Songs.Count == 0)
            await _songListViewModel.LoadSongsAsync();

        StartGroupPolling();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopGroupPolling();
    }

    /// <summary>
    /// Uruchom polling statusu grupy śpiewającej.
    /// </summary>
    private void StartGroupPolling()
    {

        _listenersGroupBroadcastService.StartGroupPolling();
    }

    private void StopGroupPolling()
    {
        _listenersGroupBroadcastService.StopGroupPolling();
    }


    // Handler wired from XAML TapGestureRecognizer. CommandParameter is the SongEntity.
    private async void OnItemTapped(object sender, EventArgs e)
    {
        // sender is the TapGestureRecognizer; get CommandParameter
        if (sender is TapGestureRecognizer tg && tg.CommandParameter is SongEntity song)
        {
            // push details page
            await Navigation.PushAsync(new SongDetailsPage(song, _userViewModel, _eventApi, _songListViewModel.Repo, _settings, _audioManager, _customSettingsRepository, _singingGroupRepositoryLite, _listenersGroupBroadcastService));
        }
        else
        {
            // fallback: get BindingContext from parent element (safer in some templates)
            if (sender is Element el && el.BindingContext is SongEntity ctxSong)
            {
                await Navigation.PushAsync(new SongDetailsPage(ctxSong, _userViewModel, _eventApi, _songListViewModel.Repo, _settings, _audioManager, _customSettingsRepository, _singingGroupRepositoryLite, _listenersGroupBroadcastService));
            }
        }
    }

    /// <summary>
    /// Fitrowanie po tytule i odświeżenie listy. Command jest w ViewModelu, więc sprawdzamy jego dostępność i wykonujemy.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnTitleFilterCompleted(object sender, EventArgs e)
    {
        if (BindingContext is SongListViewModel vm && vm.FilterCommand != null && vm.FilterCommand.CanExecute(null))
        {
            vm.FilterCommand.Execute(null);
        }
    }

    /// <summary>
    /// Filtrowanie po stronie serwera (paginacja) i odświeżenie listy. Command jest w ViewModelu, więc sprawdzamy jego dostępność i wykonujemy.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPageFilterCompleted(object sender, EventArgs e)
    {
        if (BindingContext is SongListViewModel vm && vm.PageCommand != null && vm.PageCommand.CanExecute(null))
        {
            vm.PageCommand.Execute(null);
        }
    }

    private void OnItemTapped(object sender, TappedEventArgs e)
    {

    }

    private void RefreshListeningGroupsProperties()
    {
        _listenersGroupBroadcastService.CurrentlySelectedSingingGroup =  _singingGroupRepositoryLite.GetSelectedAsync().Result;

        _songListViewModel.IsGroupListener = _listenersGroupBroadcastService.CurrentlySelectedSingingGroup?.SelectedRole == SingingGroupRole.Artysta;
        _songListViewModel.IsGroupLeader = _listenersGroupBroadcastService.CurrentlySelectedSingingGroup?.SelectedRole == SingingGroupRole.Dyrygent;

        _listenersGroupBroadcastService.AmILeader = _songListViewModel.IsGroupLeader;
    }

    private void OnBluetoothButtonClicked(object sender, EventArgs e)
    {
        if(_listenersGroupBroadcastService != null)
            _ = _listenersGroupBroadcastService.RunBluetoothPollingAsync();   
    }

    private async void GroupsButton_Clicked(object sender, EventArgs e)
    {
        await _songListViewModel.LoadOtherPage(new SingingGroupsPage(_listenersGroupBroadcastService));
    }
}