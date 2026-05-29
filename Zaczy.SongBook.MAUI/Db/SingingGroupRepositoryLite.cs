using LiteDB;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Db;

/// <summary>
/// Lightweight LiteDB repository for SingingGroupEntity.
/// </summary>
public class SingingGroupRepositoryLite
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<SingingGroupEntity> _col;
    private readonly string _apiBaseUrl;

    public bool HasGroups => _col.Count() > 0;

    /// <summary>
    /// Konstrutor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="options"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SingingGroupRepositoryLite(LiteDatabase db, IOptions<Settings> options)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _col = _db.GetCollection<SingingGroupEntity>("singing_groups");
        _col.EnsureIndex(x => x.GroupName);
        _apiBaseUrl = options.Value.ApiBaseUrl;
    }

    public Task<List<SingingGroupEntity>> GetAllAsync()
    {
        return Task.FromResult(_col.FindAll().OrderBy(g => g.GroupName).ToList());
    }

    /// <summary>
    /// Zwraca grupę o podanym ID lub null, jeśli nie istnieje w lokalnej bazie danych. 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<SingingGroupEntity?> GetByIdAsync(int id)
    {
        return Task.FromResult<SingingGroupEntity?>(_col.FindById(id));
    }

    /// <summary>
    /// Aktualizuj rekord
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public Task UpsertAsync(SingingGroupEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        entity.UpdatedAt = DateTime.UtcNow;
        _col.Upsert(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Usuń grupę o podanym ID z lokalnej bazy danych. Nie wpływa to na dane na serwerze API.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task DeleteAsync(int id)
    {
        _col.Delete(id);
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync()
    {
        _col.DeleteAll();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pobierz listę grup śpiewających z API i zaktualizuj lokalną bazę danych. Istniejące grupy zostaną nadpisane, a nowe dodane.
    /// </summary>
    /// <returns></returns>
    public async Task LoadGroupsFromApiAsync()
    {
        var api = new SingingGroupApi(_apiBaseUrl);

        // Grupa urządzenia w pobliżu
        var btGroup = new SingingGroupEntity()
        {
            Id = 9999,
            GroupName = "Urządzenia w pobliżu",
            Description = "Łączność poprzez Bluetooth ᛒ",
            ValidUntil = null,
            LastSetDate = null,
            Creator = null,
            Leader = null,
            CurrentSongId = null,
            IsLocalOnly = true
        };

        _col.Upsert(btGroup);


        var groups = await api.GetServerGroupsAsync();

        if (groups.Count > 0)
        {
            var entities = groups
                .Where(g => g.ValidUntil > DateTime.Now || g.ValidUntil == null)
                .Select(g => new SingingGroupEntity
            {
                Id = g.Id,
                GroupName = g.GroupName,
                Description = g.Description,
                ValidUntil = g.ValidUntil,
                LastSetDate = g.LastSetDate,
                Creator = g.Creator,
                Leader = g.Leader,
                CurrentSongId = g.CurrentSongId,
                UpdatedAt = DateTime.UtcNow
            });

            foreach (var entity in entities)
            {
                _col.Upsert(entity);
            }
        }
    }

    /// <summary>
    /// Znajdź po nazwie
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <returns></returns>
    public Task<List<SingingGroupEntity>> SearchByNameAsync(string searchTerm)
    {
        var results = _col.Find(g => g.GroupName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                          .OrderBy(g => g.GroupName)
                          .ToList();
        return Task.FromResult(results);
    }

    /// <summary>
    /// Aktualnie wybrana grupa śpiewająca 
    /// </summary>
    /// <returns></returns>
    public Task<SingingGroupEntity?> GetSelectedAsync()
    {
        var results = _col.Find(g => g.IsSelected && (g.ValidUntil > DateTime.Now || g.ValidUntil == null)).FirstOrDefault();
        return Task.FromResult(results);
    }


    /// <summary>
    /// Jeśli lokalna baza danych jest pusta, pobierz grupy z API i zaktualizuj bazę. Jeśli baza nie jest pusta, nic nie rób.
    /// </summary>
    /// <returns></returns>
    public async Task SeedIfEmptyAsync()
    {
        if (_col.Count() > 0) return;
        await LoadGroupsFromApiAsync();
    }

    public async Task<bool> AmILeaderAsync()
    {
        var selectedGroup = await GetSelectedAsync();
        if (selectedGroup == null) return false;
        
        return selectedGroup?.SelectedRole == SingingGroupRole.Dyrygent;
    }

}