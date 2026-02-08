using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.Api;

public class SongApi
{
    string _baseUrl;

    public SongApi(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Wyślij wszystkie piosenki do API, aby zsynchronizować dane. API powinno obsługiwać endpoint POST /songs/sync, który przyjmuje listę piosenek i aktualizuje bazę danych na serwerze.
    /// </summary>
    /// <param name="songRepository"></param>
    /// <returns></returns>
    public async Task SyncApi(SongRepository songRepository)
    {
        var apiClient = new ApiClient(_baseUrl);

        var songs = await songRepository.GetAllAsync();

        var request = new SongsAllRequest() { Songs = songs };

        var response = await apiClient.PostAsync("/songs/sync", request);
    }

    /// <summary>
    /// Pobierz listę kategorii
    /// </summary>
    /// <returns></returns>
    public async Task<List<SongCategory>> GetCategoriesListAsync()
    {
        var apiClient = new ApiClient(_baseUrl);
        var response = await apiClient.GetAsync<List<SongCategory>>("/song-categories");
        if (response.IsSuccess && response.Data != null)
        {
            return response.Data;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"GetCategoriesAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
            return new List<SongCategory>();
        }
    }

    /// <summary>
    /// Pobierz piosenki ze wskazanej kategorii
    /// </summary>
    /// <param name="categoryId"></param>
    /// <returns></returns>
    public async Task<SongCategory> GetCategorySongsAsync(int categoryId)
    {
        var apiClient = new ApiClient(_baseUrl);
        var response = await apiClient.GetAsync<SongCategory>($"/song-categories/{categoryId}");
        if (response.IsSuccess && response.Data != null)
        {
            return response.Data;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"GetCategoriesAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
            return new SongCategory();
        }
    }


}
