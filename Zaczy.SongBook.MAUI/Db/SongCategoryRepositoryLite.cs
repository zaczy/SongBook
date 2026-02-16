using LiteDB;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.Maui.Data
{
    /// <summary>
    /// Lightweight LiteDB repository for SongCategoryEntity.
    /// </summary>
    public class SongCategoryRepositoryLite
    {
        private readonly LiteDatabase _db;
        private readonly ILiteCollection<SongCategoryEntity> _col;
        private readonly string _apiBaseUrl;

        public bool HasCategories => _col.Count() > 0;
        public SongCategoryRepositoryLite(LiteDatabase db, IOptions<Zaczy.SongBook.MAUI.Settings> options)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _col = _db.GetCollection<SongCategoryEntity>("categories");
            _col.EnsureIndex(x => x.Name);
            _apiBaseUrl = options.Value.ApiBaseUrl;
        }

        public Task<List<SongCategoryEntity>> GetAllAsync()
        {
            return Task.FromResult(_col.FindAll().OrderBy(c => c.Name).ToList());
        }

        public async Task<SongCategoryEntity?> GetByIdAsync(int id)
        {
            return await Task.FromResult(_col.FindById(id));
        }

        /// <summary>
        /// Dodaj now¹ kategoriê do bazy danych.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Task AddAsync(SongCategoryEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (entity.CreatedAt == default) entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt;
            _col.Insert(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SongCategoryEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            entity.UpdatedAt = DateTime.UtcNow;
            _col.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            _col.Delete(id);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Usuwa wszystkie rekordy z kolekcji kategorii.
        /// </summary>
        public Task DeleteAllAsync()
        {
            // LiteDB provides DeleteAll for collections; use it to remove all documents.
            _col.DeleteAll();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Create a couple of sample categories if the collection is empty.
        /// </summary>
        /// 
        public async Task SeedIfEmpty()
        {
            if (_col.Count() > 0) return;

            await LoadCategoriesFromApiAsync();
        }

        /// <summary>
        /// Pobierz kategorie z API
        /// </summary>
        /// <returns></returns>
        public async Task LoadCategoriesFromApiAsync()
        {
            var songApi = new SongApi(_apiBaseUrl);

            var response = await songApi.GetCategoriesListAsync();

            if (response.Count > 0)
            {
                var entities = response.Select(c => new SongCategoryEntity
                {
                    Id = c.Id,
                    Name = c.Name!,
                    SongsCount = c.SongsCount,
                    SymbolImage = c.SymbolImage,
                    Description = c.Description,
                    CategoryColor = c.CategoryColor,
                    CategoryColorThemed = SongEntityTools.ThemeCategoryColor(c.CategoryColor),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToArray();

                foreach (var entity in entities)
                {
                    if(_col.FindById(entity.Id) == null)
                        _col.Insert(entity);
                }

                //_col.InsertBulk(entities);
                return;
            }

        }

    }
}