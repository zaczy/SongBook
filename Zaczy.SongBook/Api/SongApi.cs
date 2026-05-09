using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Extensions;

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
    public async Task<List<SongCategory>> GetCategoriesListAsync(string? email)
    {
        var apiClient = new ApiClient(_baseUrl);

        string relativeUrl = $"/song-categories/byuser";

        if(!string.IsNullOrEmpty(email))
        {
            relativeUrl += $"?email={Uri.EscapeDataString(email)}";
        }

        var response = await apiClient.GetAsync<List<SongCategory>>(relativeUrl);
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

    /// <summary>
    /// Pobierz wszystkie piosenki z API i zapisz w lokalnej bazie danych
    /// </summary>
    /// <param name="songRepository">Repozytorium do zapisu piosenek</param>
    /// <returns></returns>
    public async Task<List<String>?> GetFromApi(SongRepository songRepository, bool checkForDiffsOnly=false)
    {
        var apiClient = new ApiClient(_baseUrl);

        List<string>? differenciesList = null;

        if (checkForDiffsOnly)
            differenciesList = new List<string>();

        // Pobierz wszystkie piosenki z API
        var response = await apiClient.GetAsync<List<SongEntity>>("/songs/all");
        
        if (response.IsSuccess && response.Data != null)
        {
            // Iteruj przez wszystkie pobrane piosenki
            foreach (var song in response.Data)
            {
                try
                {
                    // Sprawdź czy piosenka już istnieje w bazie danych
                    //var existingSong = await songRepository.SearchIdAsync(song.Id);
                    var existingSong = await songRepository.SearchOnlySongAsync(new Song(song));

                    if (existingSong != null)
                    {
                        List<SongDiffSpecification>? diffs = checkForDiffsOnly ? new List<SongDiffSpecification>() : null;

                        if(existingSong.HasSignigicantDifferences(song, new List<string> { "Id", "CreatedAt", "UpdatedAt" }, differenciesFound: diffs) )
                        {
                            if (!checkForDiffsOnly)
                            {
                                // Aktualizuj istniejącą piosenkę
                                song.ShallowCopyTo(existingSong, new List<string> { "Id", "CreatedAt", "UpdatedAt" });

                                await songRepository.UpdateAsync(existingSong);
                            }
                            else
                            {
                                string message = $"\"{existingSong.Title}\" różni się od wersji na serwerze (pola {string.Join(",", diffs?.Select(d=>d.FieldName)?.ToArray() ?? [] )})";
                                differenciesList!.Add(message);
                            }
                        }
                        else
                        {
                         //   System.Diagnostics.Debug.WriteLine($"GetFromApi: Song ID {song.Id} {song.Title}- no significant differences found");
                        }
                    }
                    else
                    {
                        // Dodaj nową piosenkę
                        var songModel = new Song(song);
                        await songRepository.AddAsync(songModel);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetFromApi: Error processing song ID {song.Id}: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"GetFromApi: Successfully synchronized {response.Data.Count} songs");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"GetFromApi: API error: {response.ErrorMessage} {response.ErrorDetails}");
        }

        return differenciesList;
    }
}
