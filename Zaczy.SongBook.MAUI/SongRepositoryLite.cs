using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.MAUI.Data
{
    public class SongRepositoryLite
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<SongEntity> _col;

        public SongRepositoryLite(LiteDatabase db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _col = _db.GetCollection<SongEntity>("songs");
            _col.EnsureIndex(x => x.Title);
            _col.EnsureIndex(x => x.Artist);
        }

        public Task<List<SongEntity>> GetAllAsync()
        {
            // LiteDB is synchronous by design; wrap to Task for async callers
            return Task.FromResult(_col.FindAll().ToList());
        }

        public Task<SongEntity>? GetByIdAsync(int id)
        {
            var result = Task.FromResult(_col.FindById(id));
            return result;
        }

        public Task AddAsync(SongEntity entity)
        {
            _col.Insert(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SongEntity entity)
        {
            _col.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _col.Delete(id);
            return Task.CompletedTask;
        }

        // Example of a search method used in your WPF code (adapt to your Song model)
        public Task<SongEntity>? SearchOnlySongAsync(string? title, string? artist)
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