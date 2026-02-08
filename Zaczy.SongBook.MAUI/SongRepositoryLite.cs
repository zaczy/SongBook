using LiteDB;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.MAUI.Data
{
    public class SongRepositoryLite
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<SongEntity> _col;
        private readonly string _apiBaseUrl;

        public SongRepositoryLite(LiteDatabase db, IOptions<Zaczy.SongBook.MAUI.Settings> options)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _col = _db.GetCollection<SongEntity>("songs");
            _col.EnsureIndex(x => x.Title);
            _col.EnsureIndex(x => x.Artist);
            _apiBaseUrl = options.Value.ApiBaseUrl;
        }

        /// <summary>
        /// Za³aduj wszystkie piosenki z bazy danych. LiteDB jest synchroniczne, wiêc metoda jest opakowana w Task dla zgodnoœci z asynchronicznym API.
        /// </summary>
        /// <returns></returns>
        public Task<List<SongEntity>> GetAllAsync()
        {
            // LiteDB is synchronous by design; wrap to Task for async callers
            return Task.FromResult(_col.FindAll().ToList());
        }

        /// <summary>
        /// PObierz wskazan¹ piosenkê
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<SongEntity>? GetByIdAsync(int id)
        {
            var result = Task.FromResult(_col.FindById(id));
            return result;
        }

        /// <summary>
        /// Dodaj now¹ piosenkê do bazy danych. LiteDB jest synchroniczne, wiêc metoda jest opakowana w Task dla zgodnoœci z asynchronicznym API.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task AddAsync(SongEntity entity)
        {
            _col.Insert(entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Aktualizuj istniej¹c¹ piosenkê w bazie danych. 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task UpdateAsync(SongEntity entity)
        {
            _col.Update(entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Usuñ piosenkê o podanym ID z bazy danych.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task DeleteAsync(int id)
        {
            _col.Delete(id);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Usuñ wszystkie piosenki z bazy danych.
        /// </summary>
        /// <returns></returns>
        public Task DeleteAllAsync()
        {
            _col.DeleteAll();
            return Task.CompletedTask;
        }

        public async Task FetchCategorySongsFromApiAsync(int categoryId, SongCategoryEntity? songCategoryEntity=null)
        {
            var songApi = new SongApi(_apiBaseUrl);

            var categorySongs = await songApi.GetCategorySongsAsync(categoryId);
            if (categorySongs?.Songs != null)
            {
                foreach (var song in categorySongs.Songs)
                {
                    if (song?.Title != null)
                    {
                        string title = song?.Title ?? String.Empty;
                        string artist = song?.Artist ?? string.Empty; // Handle null artist as empty string for comparison
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        var existing = await SearchOnlySongAsync(title, artist);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        if (existing == null)
                        {
                            var entity = new SongEntity();
                            if(song?.ServerId != null)
                                entity.Id = song.ServerId.Value;
                            entity.initFromSong(song!);
                            entity.CategoryColor = SongEntityTools.ThemeCategoryColor(songCategoryEntity?.CategoryColor ?? string.Empty);
                            await AddAsync(entity);
                        }
                    }
                }
            }
        }


        // Example of a search method used in your WPF code (adapt to your Song model)
        public Task<SongEntity>? SearchOnlySongAsync(string title, string artist)
        {
            // use case-insensitive comparison to avoid duplicates differing only by case/whitespace
            var t = (title ?? string.Empty).Trim();
            var a = (artist ?? string.Empty).Trim();

            var found = _col.FindOne(x =>
                (x.Title ?? string.Empty).Trim().Equals(t, StringComparison.OrdinalIgnoreCase)
                && (x.Artist ?? string.Empty).Trim().Equals(a, StringComparison.OrdinalIgnoreCase)
            );

            return Task.FromResult(found);
        }

        // Simple seeding helper (call at app startup)
        public void SeedIfEmpty()
        {
            if (_col.Count() > 0) return;

            var defaultSongs = new SongEntity[]
            {
            };

            _col.InsertBulk(defaultSongs);
        }
    }
}