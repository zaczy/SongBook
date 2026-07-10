using Android.Bluetooth;
using Android.Bluetooth.LE;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.MAUI.Services;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Services;

public partial class BluetoothGroupService
{
    private const string BleLocalName = "SongBook";

    private BluetoothLeAdvertiser? _advertiser;
    private SongBookAdvertiseCallback? _advertiseCallback;

    public partial Task StartAdvertisingAsync(int songId, string rolePostfix, CancellationToken ct)
    {
        try
        {
            var manager = Android.App.Application.Context
                .GetSystemService(Android.Content.Context.BluetoothService) as BluetoothManager;

            var adapter = manager?.Adapter;

            _advertiser = adapter?.BluetoothLeAdvertiser;

            if (_advertiser == null)
            {
                if (_userViewModel.ExtendedApiLogging)
                    _ = _eventApi.SendEventAsync("BLE", "Bluetooth LE Advertiser is null.");
                return Task.CompletedTask;
            }

            var songIdBytes = Encoding.ASCII.GetBytes($"{songId};{rolePostfix}");

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.LowLatency)!
                .SetTxPowerLevel(AdvertiseTx.PowerHigh)!
                .SetConnectable(false)!
                .SetTimeout(60_000)!
                .Build();

            var advertiseData = new AdvertiseData.Builder()
                .AddServiceUuid(Android.OS.ParcelUuid.FromString(ServiceUuid.ToString()))!
                .AddManufacturerData(0x1234, songIdBytes)!
                .SetIncludeDeviceName(false)!
                .Build();

            var scanResponse = new AdvertiseData.Builder()
                .SetIncludeDeviceName(false)!
                .AddServiceData(
                    Android.OS.ParcelUuid.FromString(ServiceUuid.ToString()),
                    System.Text.Encoding.UTF8.GetBytes(BleLocalName))!
                .Build();

            _advertiseCallback = new SongBookAdvertiseCallback(_eventApi, _userViewModel);
            _advertiser.StartAdvertising(settings, advertiseData, scanResponse, _advertiseCallback);
        }
        catch (Exception ex)
        {
            _ = _eventApi.SendEventAsync("BLE", $"Exception while starting BLE Advertising: {ex}");
            System.Diagnostics.Debug.WriteLine($"Exception while starting BLE Advertising: {ex}");
        }

        return Task.CompletedTask;
    }

    public partial Task StopAdvertisingAsync()
    {
        if (_advertiseCallback != null)
            _advertiser?.StopAdvertising(_advertiseCallback);

        _advertiseCallback = null;
        return Task.CompletedTask;
    }

    private class SongBookAdvertiseCallback : AdvertiseCallback
    {
        private readonly EventApi _eventApi;
        private readonly UserViewModel _userViewModel;

        public SongBookAdvertiseCallback(EventApi eventApi, UserViewModel userViewModel) : base()
        {
            _eventApi = eventApi;
            _userViewModel = userViewModel;
        }

        public override void OnStartSuccess(AdvertiseSettings? settingsInEffect)
        {
            if (_userViewModel.ExtendedApiLogging)
                _ = _eventApi.SendEventAsync("BLE", "BLE Advertising started.");
            System.Diagnostics.Debug.WriteLine("BLE Advertising started.");
        }

        public override void OnStartFailure(AdvertiseFailure errorCode)
        {
            if (_userViewModel.ExtendedApiLogging)
                _ = _eventApi.SendEventAsync("BLE", $"BLE Advertising failed: {errorCode}");
            System.Diagnostics.Debug.WriteLine($"BLE Advertising failed: {errorCode}");
        }
    }
}