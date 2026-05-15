using LiteDB;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zaczy.SongBook.MAUI.Data;

namespace Zaczy.SongBook.MAUI.Db;

/// <summary>
/// Repozytorium lokalnych ustawieñ u¿ytkownika dla piosenek (LiteDB).
/// </summary>
public class SongCustomSettingsRepositoryLite
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<SongCustomSettingsEntity> _col;

    public SongCustomSettingsRepositoryLite(LiteDatabase db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _col = _db.GetCollection<SongCustomSettingsEntity>("song_custom_settings");
        _col.EnsureIndex(x => x.SongId, unique: true);
        _col.EnsureIndex(x => x.IsFavorite);
    }

    /// <summary>
    /// Pobierz ustawienia dla wskazanej piosenki. Zwraca null jeœli brak lokalnych ustawieñ.
    /// </summary>
    public Task<SongCustomSettingsEntity?> GetBySongIdAsync(int songId)
    {
        var result = _col.FindOne(x => x.SongId == songId);
        return Task.FromResult<SongCustomSettingsEntity?>(result);
    }

    /// <summary>
    /// Pobierz wszystkie lokalne ustawienia piosenek.
    /// </summary>
    public Task<List<SongCustomSettingsEntity>> GetAllAsync()
    {
        return Task.FromResult(_col.FindAll().ToList());
    }

    /// <summary>
    /// Pobierz ulubione piosenki (IsFavorite == true).
    /// </summary>
    public Task<List<SongCustomSettingsEntity>> GetFavoritesAsync()
    {
        return Task.FromResult(_col.Find(x => x.IsFavorite).ToList());
    }

    /// <summary>
    /// Zapisz lub zaktualizuj ustawienia dla piosenki (Upsert).
    /// Jeœli ustawienia dla danego SongId nie istniej¹ — tworzy nowy rekord.
    /// </summary>
    public Task UpsertAsync(SongCustomSettingsEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        entity.UpdatedAt = DateTime.UtcNow;
        _col.Upsert(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Zaktualizuj istniej¹ce ustawienia. Nie tworzy nowego rekordu jeœli brak.
    /// </summary>
    public Task UpdateAsync(SongCustomSettingsEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        entity.UpdatedAt = DateTime.UtcNow;
        _col.Update(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Usuñ lokalne ustawienia dla wskazanej piosenki.
    /// </summary>
    public Task DeleteAsync(int songId)
    {
        _col.DeleteMany(x => x.SongId == songId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Usuñ wszystkie lokalne ustawienia piosenek.
    /// </summary>
    public Task DeleteAllAsync()
    {
        _col.DeleteAll();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Prze³¹cz flagê ulubionej dla wskazanej piosenki.
    /// Tworzy rekord jeœli jeszcze nie istnieje.
    /// </summary>
    public async Task ToggleFavoriteAsync(int songId)
    {
        var existing = await GetBySongIdAsync(songId);
        if (existing == null)
        {
            existing = new SongCustomSettingsEntity { SongId = songId, IsFavorite = true };
        }
        else
        {
            existing.IsFavorite = !existing.IsFavorite;
        }

        await UpsertAsync(existing);
    }

    /// <summary>
    /// Pobierz lub utwórz domyœlny rekord ustawieñ dla piosenki.
    /// </summary>
    public async Task<SongCustomSettingsEntity> GetOrCreateAsync(int songId)
    {
        var existing = await GetBySongIdAsync(songId);
        if (existing != null)
            return existing;

        var created = new SongCustomSettingsEntity { SongId = songId };
        await UpsertAsync(created);
        return created;
    }

    /// <summary>
    /// Aktualizuj ustawienia dla piosenki, wykonuj¹c podan¹ akcjê na istniej¹cym rekordzie.
    /// </summary>
    /// <param name="songId"></param>
    /// <param name="updateAction"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task UpdateSettingsForSongAsync(int songId, Action<SongCustomSettingsEntity> updateAction)
    {
        if (updateAction == null) throw new ArgumentNullException(nameof(updateAction));
     
        var settings = await GetOrCreateAsync(songId);
        updateAction(settings);
        await UpsertAsync(settings);
    }

    /// <summary>
    /// Aktualizuj prêdkoœæ przewijania dla wskazanej piosenki.
    /// </summary>
    /// <param name="songId"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public Task UpdateScrollFactorAsync(int songId, float factor)
    {
        return UpdateSettingsForSongAsync(songId, s => s.ScrollingSpeedFactor = factor);
    }
}