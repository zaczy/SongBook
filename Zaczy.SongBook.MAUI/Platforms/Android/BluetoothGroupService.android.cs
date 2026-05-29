using Android.Bluetooth;
using Android.Bluetooth.LE;
using System;
using System.Threading;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.MAUI.Services;

namespace Zaczy.SongBook.MAUI.Services;

public partial class BluetoothGroupService
{
    private const string BleLocalName = "SongBook";

    private BluetoothLeAdvertiser? _advertiser;
    private SongBookAdvertiseCallback? _advertiseCallback;

    public partial Task StartAdvertisingAsync(int songId, CancellationToken ct)
    {
        try
        {
            //_ = _eventApi.SendEventAsync("BLE", $"Wejœcie do StartAdvertisingAsync, song ID {songId}");

            var manager = Android.App.Application.Context
                .GetSystemService(Android.Content.Context.BluetoothService) as BluetoothManager;

            var adapter = manager?.Adapter;

            _advertiser = adapter?.BluetoothLeAdvertiser;

            if (_advertiser == null)
            {
                _ = _eventApi.SendEventAsync("BLE", "Bluetooth LE Advertiser is null.");
                return Task.CompletedTask;
            }

            var songIdBytes = BitConverter.GetBytes(songId);

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.LowLatency)!
                .SetTxPowerLevel(AdvertiseTx.PowerHigh)!
                .SetConnectable(false)!
                .SetTimeout(60_000)!
                .Build();

            //_ = _eventApi.SendEventAsync("BLE", $"Bluetooth LE Advertiser {songId}/3");


            // G³ówny pakiet advertisement — UUID serwisu + dane piosenki
            var advertiseData = new AdvertiseData.Builder()
                .AddServiceUuid(Android.OS.ParcelUuid.FromString(ServiceUuid.ToString()))!
                .AddManufacturerData(0x1234, songIdBytes)!
                .SetIncludeDeviceName(false)!   // nazwa NIE w g³ównym pakiecie (ograniczony rozmiar)
                .Build();

            //_ = _eventApi.SendEventAsync("BLE", $"Bluetooth LE Advertiser {songId}/4");

            // Scan response — tu umieszczamy czyteln¹ nazwê (widoczna w BLE Scanner)
            var scanResponse = new AdvertiseData.Builder()
                .SetIncludeDeviceName(false)!   // nie u¿ywamy nazwy adaptera
                //.SetIncludeDeviceName(true)!   // nie u¿ywamy nazwy adaptera
                .AddServiceData(                // zakoduj nazwê jako Service Data
                    Android.OS.ParcelUuid.FromString(ServiceUuid.ToString()),
                    System.Text.Encoding.UTF8.GetBytes(BleLocalName))!
                .Build();

            //_ = _eventApi.SendEventAsync("BLE", $"Build finished, starting BLE Advertising for song ID {songId}");


            _advertiseCallback = new SongBookAdvertiseCallback(_eventApi);
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

        public SongBookAdvertiseCallback(EventApi eventApi) : base()
        {
            _eventApi = eventApi;
        }

        public override void OnStartSuccess(AdvertiseSettings? settingsInEffect)
        {
            _ = _eventApi.SendEventAsync("BLE", "BLE Advertising started.");
            System.Diagnostics.Debug.WriteLine("BLE Advertising started.");
        }

        public override void OnStartFailure(AdvertiseFailure errorCode)
        {
            _ = _eventApi.SendEventAsync("BLE", $"BLE Advertising failed: {errorCode}");
            System.Diagnostics.Debug.WriteLine($"BLE Advertising failed: {errorCode}");
        }
    }
}