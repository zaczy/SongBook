using Microsoft.Extensions.Options;
using Plugin.Maui.Audio;
using System;
using System.Buffers.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.Db;
using Zaczy.SongBook.MAUI.Extensions;
using Zaczy.SongBook.MAUI.Pages;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Services;

public enum PermissionsDecision
{ 
    Granted,
    Denied,
    Unknown
}

public class ListenersGroupBroadcastService
{
    private BluetoothGroupService? _bluetoothGroupService;
    private readonly SingingGroupRepositoryLite _singingGroupRepositoryLite;
    private readonly EventApi _eventApi;
    private readonly UserViewModel _userViewModel;
    private readonly SongCustomSettingsRepositoryLite _customSettingsRepository;
    private readonly SongListViewModel _songListViewModel;
    private readonly IAudioManager _audioManager;
    private readonly Settings _settings;
    private SingingGroupEntity? _currentlySelectedSingingGroup;
    private CancellationTokenSource? _pollingCts;
    private bool _amIDirector = false;

    public ListenersGroupBroadcastService(
        SingingGroupRepositoryLite singingGroupRepositoryLite,
        EventApi eventApi,
        UserViewModel userViewModel,
        SongCustomSettingsRepositoryLite songCustomSettingsRepositoryLite,
        SongListViewModel songListViewModel,
        IAudioManager audioManager,
        IOptions<Settings> settings)
    {
        _singingGroupRepositoryLite = singingGroupRepositoryLite;
        _eventApi = eventApi;
        _userViewModel = userViewModel;
        _customSettingsRepository = songCustomSettingsRepositoryLite;
        _songListViewModel = songListViewModel;
        _audioManager = audioManager;
        _settings = settings?.Value ?? new Settings();
    }
    public bool AmILeader
    {
        get => _amIDirector;
        set
        {
            if (_amIDirector != value)
            {
                _amIDirector = value;
            }
        }
    }

    public SingingGroupEntity? CurrentlySelectedSingingGroup
    {
        get => _currentlySelectedSingingGroup;
        set => _currentlySelectedSingingGroup = value;
    }

    public CancellationTokenSource? PollingCts
    {
        get => _pollingCts;
        set => _pollingCts = value;
    }

    public BluetoothGroupService? BluetoothGroupService
    {
        get => _bluetoothGroupService;
        set => _bluetoothGroupService = value;
    }

    public PermissionsDecision BtPermissionsDecision
    {
        get => _userViewModel.BluetoothPermissionsDecision;
        set => _userViewModel.BluetoothPermissionsDecision = value;
    }

    /// <summary>
    /// Wywoływane gdy odebrano propozycję zmiany piosenki dla Dyrygenta (zamiast automatycznej nawigacji).
    /// </summary>
    public Action<SongEntity>? OnSongProposedForDirector { get; set; }

    /// <summary>
    /// Zwraca aktywną INavigation z aktywnego okna.
    /// </summary>
    private static INavigation? GetNavigation()
        => Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;

    /// <summary>
    /// Wyświetla alert na aktywnej stronie.
    /// </summary>
    private static Task DisplayAlert(string title, string message, string cancel)
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        return page != null
            ? page.DisplayAlert(title, message, cancel)
            : Task.CompletedTask;
    }

    /// <summary>
    /// Zapytaj o uprawnienia Bluetooth.
    /// </summary>
    public async Task<bool> RequestBluetoothPermissionsAsync()
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var status = await Permissions.RequestAsync<Permissions.Bluetooth>();
                if (status != PermissionStatus.Granted)
                    return false;
            }
            else
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RequestBluetoothPermissionsAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Zainicjuj klasę Bluetooth i uruchom skanowanie jeśli jesteśmy Słuchaczem.
    /// </summary>
    public async Task RunBluetoothPollingAsync(bool requeryForPermissions=false)
    {
        if (requeryForPermissions || _userViewModel.BluetoothPermissionsDecision == PermissionsDecision.Unknown)
        {
            var granted = await RequestBluetoothPermissionsAsync();
            if (!granted)
            {
                _userViewModel.BluetoothPermissionsDecision = PermissionsDecision.Denied;
                await DisplayAlert("Bluetooth", "Brak uprawnień do Bluetooth. Synchronizacja grupowa przez BLE nie będzie dostępna.", "OK");
                return;
            }
            else
            {
                _userViewModel.BluetoothPermissionsDecision = PermissionsDecision.Granted;
            }

        }

        try
        {
            _bluetoothGroupService = new BluetoothGroupService(_eventApi, _userViewModel);

            _currentlySelectedSingingGroup = await _singingGroupRepositoryLite.GetSelectedAsync();
            if (_currentlySelectedSingingGroup?.SelectedRole == SingingGroupRole.Artysta 
                || (_currentlySelectedSingingGroup?.SelectedRole == SingingGroupRole.Dyrygent && _userViewModel.EnableGroupListeningWhenDirector && !_userViewModel.ScrollingInProgress)
                )
            {
                _pollingCts ??= new CancellationTokenSource();
                await _bluetoothGroupService.StartScanningAsync(
                    onSongIdReceived: OnBluetoothSongIdReceived,
                    ct: _pollingCts.Token);
            }
        }
        catch (Exception ex)
        {
            _ = ex.SaveExceptionToFileAsync("InitBluetooth", eventApi: _eventApi);
        }
    }

    /// <summary>
    /// Zatrzymaj skanowanie BLE.
    /// </summary>
    public async Task StopAsync()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;

        if (_bluetoothGroupService != null)
            await _bluetoothGroupService.StopScanningAsync();
    }

    /// <summary>
    /// Wywoływane gdy BLE advertisement zawiera nowe songId od Dyrygenta.
    /// </summary>
    private void OnBluetoothSongIdReceived(int songId, string role)
    {
        if (songId == 0) return;
        if (_userViewModel.RejectedLeaderProposals.Contains(songId)) return;

        if (_userViewModel.ExtendedApiLogging)
            _ = _eventApi.SendEventAsync("BLE_RECV", $"Received songId via BLE: {songId}, role: {role}");

        if (_userViewModel.RejectedLeaderProposals.Contains(songId) || _userViewModel.PendingProposedSongs?.Any(se=>se.Id == songId) == true)
            return;

        this.NotifyUser(songId, role);
    }

    /// <summary>
    /// Wyświetl powiadomienie
    /// </summary>
    /// <param name="songId"></param>
    /// <param name="role"></param>
    private void NotifyUser(int songId, string role)
    {
        var ail = AmILeader;

        if (!ail && role == "A")
            return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                //if (CurrentlySelectedSingingGroup == null)
                //    CurrentlySelectedSingingGroup = await _singingGroupRepositoryLite.GetSelectedAsync();

                var navigation = GetNavigation();
                if (navigation == null) return;

                Boolean amILeaderLocal = AmILeader;
                var repo = _songListViewModel.Repo;
                if (repo == null) return;

                var song = await repo.GetByIdAsync(songId);
                if (song == null) return;

                var newPage = new SongDetailsPage(
                    song, _userViewModel, _eventApi,
                    repo, _settings, _audioManager,
                    _customSettingsRepository, _singingGroupRepositoryLite, this)
                {
                    GroupSongId = song.Id
                };

                var onSongProposedForDirectorLocal = OnSongProposedForDirector;

                var currentPage = navigation.NavigationStack.LastOrDefault();
                if (currentPage is SongDetailsPage sdp)
                {
                    if (sdp.GroupSongId == song.Id || sdp.Song.Id == song.Id)
                        return;

                    // Dyrygent: przekaż propozycję do aktywnej strony przez callback
                    if (onSongProposedForDirectorLocal != null && amILeaderLocal)
                        onSongProposedForDirectorLocal(song);
                    else
                    {
                        navigation.InsertPageBefore(newPage, currentPage);
                        await navigation.PopAsync(animated: false);
                    }
                }
                else
                {
                    // Dyrygent: przekaż propozycję do aktywnej strony przez callback
                    if (onSongProposedForDirectorLocal != null && amILeaderLocal)
                        onSongProposedForDirectorLocal(song);
                    else
                    {
                        await navigation.PushAsync(newPage);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnBluetoothSongIdReceived error: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Rozpocznij polling statusu grupy. Pobieraj aktualnie wybraną grupę, jeśli jest ustawiona i interwał > 0, uruchom pętlę pollingu.
    /// </summary>
    public void StartGroupPolling()
    {
        CurrentlySelectedSingingGroup = _singingGroupRepositoryLite.GetSelectedAsync().Result;

        if (CurrentlySelectedSingingGroup == null)
            return;

        StopGroupPolling();
        PollingCts = new CancellationTokenSource();

        if (_settings.ListeningGroupCheckInterval > 0)
            _ = RunGroupPollingAsync(PollingCts.Token);

        // Inicjuj BLE zawsze gdy jest aktywna grupa — niezależnie od interwału HTTP pollingu
        // Dyrygent potrzebuje BluetoothGroupService do wysyłania, Artysta do odbierania
        _ = RunBluetoothPollingAsync();
    }

    /// <summary>
    /// Zatrzymaj polling statusu grupy. Anuluj token i zatrzymaj skanowanie BLE jeśli jest aktywne.
    /// </summary>
    internal void StopGroupPolling()
    {
        PollingCts?.Cancel();
        PollingCts?.Dispose();
        PollingCts = null;

        if (BluetoothGroupService != null)
            _ = BluetoothGroupService.StopScanningAsync();
    }

    /// <summary>
    /// Pętla pollingu — co <see cref="Settings.ListeningGroupCheckInterval"/> sekund sprawdza status grupy
    /// i przechodzi na SongDetailsPage jeśli CurrentSongId jest ustawiony.
    /// </summary>
    private async Task RunGroupPollingAsync(CancellationToken ct)
    {
        if (CurrentlySelectedSingingGroup == null || CurrentlySelectedSingingGroup.IsLocalOnly)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.ListeningGroupCheckInterval));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await CheckGroupStatusInternetAsync();
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Sprawdż status grupy śpiewającej — jeśli jest ustawiony CurrentSongId, pobierz piosenkę i przejdź na stronę szczegółów.
    /// </summary>
    /// <returns></returns>
    private async Task CheckGroupStatusInternetAsync()
    {
        if (CurrentlySelectedSingingGroup == null || (_userViewModel?.BroadcastWeb != true))
            return;

        if (CurrentlySelectedSingingGroup.SelectedRole == SingingGroupRole.Dyrygent
            && (_userViewModel.EnableGroupListeningWhenDirector != true || _userViewModel.ScrollingInProgress))
            return;

        try
        {

            var singingGroupApi = new SingingGroupApi(_settings.ApiBaseUrl);
            var status = await singingGroupApi.GetServerGroupsStatusAsync(groupId: CurrentlySelectedSingingGroup.Id, ignoreOlderThanSeconds: 5 * 60);

            //var groupSongId = await SingingGroupApi.CurrentSongForListenersGroupAsync(
            //    _settings.ApiBaseUrl, CurrentlySelectedSingingGroup.Id);

            var groupSongId = status?.CurrentSongId;
            var groupSongRole = status?.CurrentSongUserRole ?? "Unknown";

            if (groupSongId == null || _userViewModel.RejectedLeaderProposals.Contains((int)groupSongId))
                return;

            var repo = _songListViewModel.Repo;
            if (repo == null) return;

            //var song = await repo.GetByIdAsync((int)groupSongId);
            //if (song == null) return;

            this.NotifyUser((int)groupSongId, groupSongRole);

            //await MainThread.InvokeOnMainThreadAsync(async () =>
            //{
            //    if (CurrentlySelectedSingingGroup.SelectedRole == SingingGroupRole.Dyrygent
            //        && OnSongProposedForDirector != null)
            //    {
            //        // Dyrygent: przekaż propozycję do aktywnej strony przez callback
            //        OnSongProposedForDirector(song);
            //    }
            //    else
            //    {
            //        // Artysta: nawiguj bezpośrednio
            //        var navigation = GetNavigation();
            //        if (navigation == null) return;

            //        var newPage = new SongDetailsPage(
            //            song!, _userViewModel, _eventApi,
            //            _songListViewModel.Repo,
            //            _settings, _audioManager, _customSettingsRepository, _singingGroupRepositoryLite,
            //            this)
            //        {
            //            GroupSongId = song!.Id
            //        };

            //        var currentPage = navigation.NavigationStack.LastOrDefault();
            //        if (currentPage is SongDetailsPage)
            //        {
            //            navigation.InsertPageBefore(newPage, currentPage);
            //            await navigation.PopAsync(animated: false);
            //        }
            //        else
            //        {
            //            await navigation.PushAsync(newPage);
            //        }
            //    }
            //});
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CheckGroupStatusAsync error: {ex.Message}");
        }
    }
   
}
