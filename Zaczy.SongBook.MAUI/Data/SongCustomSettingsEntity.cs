using LiteDB;
using System;

namespace Zaczy.SongBook.MAUI.Data;

/// <summary>
/// Lokalne ustawienia u¿ytkownika dla konkretnej piosenki (przechowywane w LiteDB).
/// Powi¹zane z SongEntity przez SongId.
/// </summary>
public class SongCustomSettingsEntity
{
    /// <summary>
    /// Klucz lokalny — zgodny z SongEntity.Id
    /// </summary>
    [BsonId]
    public int SongId { get; set; }

    /// <summary>
    /// OpóŸnienie startu przewijania w sekundach
    /// </summary>
    public int? ScrollingDelay { get; set; }

    /// <summary>
    /// Funkcja startu przewijania (linear / log / quad / cubic)
    /// </summary>
    public string? ScrollingStartFunction { get; set; }

    /// <summary>
    /// Lokalna korekta tonacji (liczba pó³tonów wzglêdem orygina³u)
    /// </summary>
    public int ToneAdjustment { get; set; } = 0;

    /// <summary>
    /// Notatki u¿ytkownika do piosenki
    /// </summary>
    public string? UserNotes { get; set; }

    /// <summary>
    /// Czy piosenka jest w ulubionych lokalnie
    /// </summary>
    public bool IsFavorite { get; set; } = false;

    public float? ScrollingSpeedFactor { get; set; } = 1;

    /// <summary>
    /// Data ostatniej modyfikacji ustawieñ
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}