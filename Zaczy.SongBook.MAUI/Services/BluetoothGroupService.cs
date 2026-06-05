using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Services;

public partial class BluetoothGroupService : IBluetoothGroupService
{
    public static readonly Guid ServiceUuid = new("12345678-1234-1234-1234-1234567890AB");

    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;
    private Action<int>? _onSongIdReceived;
    private readonly EventApi _eventApi;
    private CancellationTokenSource? _scanCts;
    private readonly UserViewModel _userViewModel;

    public BluetoothGroupService(EventApi eventApi, UserViewModel userViewModel)
    {
        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;

        // NIE ustawiaj ScanTimeout = 0 — Plugin.BLE traktuje to jako "skanuj przez 0ms"
        // int.MaxValue = praktycznie nieskoñczone skanowanie (~24 dni)
        _adapter.ScanTimeout = 600_000;

        _adapter.DeviceAdvertised += OnDeviceAdvertised;
        _adapter.DeviceDiscovered += OnDeviceAdvertised;
        _eventApi = eventApi;
        _userViewModel = userViewModel;
    }

    /// <inheritdoc/>
    public Task StartScanningAsync(Action<int> onSongIdReceived, CancellationToken ct = default)
    {
        _onSongIdReceived = onSongIdReceived;

        if(_userViewModel.ExtendedApiLogging)
            _ = _eventApi.SendEventAsync("BLE_SCAN", $"BLE State: {_ble.State}, starting scan...");

        if (_ble.State != BluetoothState.On)
        {
            if (_userViewModel.ExtendedApiLogging)
                    _ = _eventApi.SendEventAsync("BLE_SCAN", $"BLE not ready, state={_ble.State}");
            return Task.CompletedTask;
        }

        // Po³¹cz zewnêtrzny token z wewnêtrznym — StopScanningAsync anuluje _scanCts
        _scanCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _scanCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                if (_userViewModel.ExtendedApiLogging)
                    _ = _eventApi.SendEventAsync("BLE_SCAN", "Scan loop started.");

                // Pêtla: po ka¿dym zakoñczeniu skanu (timeout) startuj ponownie
                // dopóki token nie jest anulowany
                while (!token.IsCancellationRequested)
                {
                    if (_userViewModel.ExtendedApiLogging)
                        _ = _eventApi.SendEventAsync("BLE_SCAN", "Starting scan iteration...");

                    await _adapter.StartScanningForDevicesAsync(
                        serviceUuids: null,
                        //serviceUuids: [ServiceUuid],
                        allowDuplicatesKey: true,
                        cancellationToken: token
                        
                        );

                    if (!token.IsCancellationRequested)
                    {
                        if (_userViewModel.ExtendedApiLogging)
                            _ = _eventApi.SendEventAsync("BLE_SCAN", "Scan iteration ended, restarting in 500ms...");
                        await Task.Delay(500, token); // krótka przerwa przed kolejn¹ iteracj¹
                    }
                }

                if (_userViewModel.ExtendedApiLogging)
                    _ = _eventApi.SendEventAsync("BLE_SCAN", "Scan loop exited.");
            }
            catch (OperationCanceledException)
            {
                if (_userViewModel.ExtendedApiLogging)
                    _ = _eventApi.SendEventAsync("BLE_SCAN", "Scan cancelled.");
            }
            catch (Exception ex)
            {
                _ = _eventApi.SendEventAsync("BLE_SCAN", $"Scan error: {ex.Message}");
            }
        }, token);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopScanningAsync()
    {
        _adapter.DeviceAdvertised -= OnDeviceAdvertised;
        _adapter.DeviceDiscovered -= OnDeviceAdvertised;
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = null;
        await _adapter.StopScanningForDevicesAsync();
        _onSongIdReceived = null;
    }

    private void OnDeviceAdvertised(object? sender, DeviceEventArgs e)
    {
        var records = e.Device.AdvertisementRecords;

        if (_userViewModel.ExtendedApiLogging)
            _ = _eventApi.SendEventAsync("BLE_SCAN", $"Device: {e.Device.Name ?? "N/A"} [{e.Device.Id}], records: {records?.Count ?? -1}");

        if (records == null) return;

        foreach (var record in records)
        {
            if (_userViewModel.ExtendedApiLogging)
                _ = _eventApi.SendEventAsync("BLE_SCAN", $"  type={record.Type} len={record.Data?.Length ?? -1} hex={ToHex(record.Data)}");

            if (record.Type == Plugin.BLE.Abstractions.AdvertisementRecordType.ManufacturerSpecificData
                && record.Data?.Length >= 6)
            {
                var companyId = BitConverter.ToUInt16(record.Data, 0);
                var songId    = BitConverter.ToInt32(record.Data, 2);

                if (_userViewModel.ExtendedApiLogging)
                    _ = _eventApi.SendEventAsync("BLE_SCAN", $"  ManufData companyId=0x{companyId:X4} songId={songId}");

                if (companyId == 0x1234 && songId > 0)
                {
                    if (_userViewModel.ExtendedApiLogging)
                        _ = _eventApi.SendEventAsync("BLE_SCAN", $"  >>> SongBook songId={songId} received!");
                    _onSongIdReceived?.Invoke(songId);
                    return;
                }
            }
        }
    }

    private static string ToHex(byte[]? data)
        => data == null ? "null" : BitConverter.ToString(data).Replace("-", "");

    // Advertising jest platform-specific — patrz BluetoothGroupService.android.cs
    public partial Task StartAdvertisingAsync(int songId, CancellationToken ct = default);
    public partial Task StopAdvertisingAsync();

#if !ANDROID
    public partial Task StartAdvertisingAsync(int songId, CancellationToken ct)
        => Task.CompletedTask;

    public partial Task StopAdvertisingAsync()
        => Task.CompletedTask;
#endif
}