using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI.Services;

public interface IBluetoothGroupService
{
    /// <summary>
    /// Rozg³oœ songId przez BLE (rola Dyrygenta).
    /// </summary>
    Task StartAdvertisingAsync(int songId, CancellationToken ct = default);

    /// <summary>
    /// Zatrzymaj rozg³aszanie.
    /// </summary>
    Task StopAdvertisingAsync();

    /// <summary>
    /// Rozpocznij nas³uchiwanie na songId od Dyrygenta.
    /// </summary>
    Task StartScanningAsync(Action<int> onSongIdReceived, CancellationToken ct = default);

    /// <summary>
    /// Zatrzymaj skanowanie.
    /// </summary>
    Task StopScanningAsync();
}