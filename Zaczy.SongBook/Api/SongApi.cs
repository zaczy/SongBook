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

        await SyncSelectedApiAsync(songs);
    }


    /// <summary>
    /// Wyślij wybrane piosenki do API, aby zsynchronizować dane. API powinno obsługiwać endpoint POST /songs/sync, który przyjmuje listę piosenek i aktualizuje bazę danych na serwerze.
    /// </summary>
    /// <param name="songList"></param>
    /// <returns></returns>
    public async Task SyncSelectedApiAsync(List<SongEntity> songList)
    {
        var apiClient = new ApiClient(_baseUrl);

        var request = new SongsAllRequest() { Songs = songList };

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

    public async Task<List<SongEntity>> GetNoCategoriesSongsListAsync(string? email)
    {
        var apiClient = new ApiClient(_baseUrl);

        string relativeUrl = $"/song-no-categories/byuser";

        if (!string.IsNullOrEmpty(email))
        {
            relativeUrl += $"?email={Uri.EscapeDataString(email)}";
        }

        var response = await apiClient.GetAsync<List<SongEntity>>(relativeUrl);
        if (response.IsSuccess && response.Data != null)
        {
            return response.Data;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"GetNoCategoriesListAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
            return new List<SongEntity>();
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
    /// Pobierz wszystkie piosenki z API porównaj z intniejącą bazą danych
    /// </summary>
    /// <param name="songRepository">Repozytorium do zapisu piosenek</param>
    /// <returns></returns>
    public async Task<List<SongComparisionResults>?> CompareWithApiAsync(SongRepository songRepository, bool checkLocalOnly=false)
    {
        var apiClient = new ApiClient(_baseUrl, 15000);

        List<SongComparisionResults>? differenciesList = new List<SongComparisionResults>();

        // Pobierz wszystkie piosenki z API
        var response = await apiClient.GetAsync<List<SongEntity>>("/songs/all");
        
        if (response.IsSuccess && response.Data != null)
        {
            // Iteruj przez wszystkie pobrane piosenki
            foreach (var song in response.Data)
            {
                try
                {
                    var existingSong = await songRepository.SearchOnlySongAsync(new Song(song));

                    if (existingSong != null)
                    {
                        List<SongDiffSpecification>? diffs = new List<SongDiffSpecification>();

                        if(existingSong.HasSignigicantDifferences(song, new List<string> { "Id", "CreatedAt", "UpdatedAt" }, differenciesFound: diffs) )
                        {
                            string message = $"\"{existingSong.Title}\" różni się od wersji na serwerze (pola {string.Join(",", diffs?.Select(d=>d.FieldName)?.ToArray() ?? [] )})";
                            differenciesList!.Add(new SongComparisionResults() { DiffSummary = message, SongTitle = existingSong.Title, DiffSpecification = diffs, BaseSongEntity = existingSong, ApiSong = song});
                        }
                    }
                    else
                    {
                        string message = $"\"{song.Title}\" - nowa piosenka)";
                        differenciesList!.Add(new SongComparisionResults() { DiffSummary = message, SongTitle = existingSong?.Title ?? song.Title, DiffSpecification = null, ApiSong = song });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetFromApi: Error processing song ID {song.Id}: {ex.Message}");
                }
            }

            // Piosenki istniejące lokalnie, których nie ma w API
            if (checkLocalOnly)
            {
                var localSongs = await songRepository.GetAllAsync();
                var apiTitles = response.Data
                    .Select(s => s.Title)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var localSong in localSongs)
                {
                    if (!apiTitles.Contains(localSong.Title ?? string.Empty))
                    {
                        string message = $"\"{localSong.Title}\" - istnieje tylko lokalnie";
                        differenciesList!.Add(new SongComparisionResults()
                        {
                            DiffSummary = message,
                            SongTitle = localSong.Title,
                            DiffSpecification = null,
                            BaseSongEntity = localSong,
                            ApiSong = null
                        });
                    }
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


    /// <summary>
    /// 
    /// </summary>
    /// <param name="songRepository"></param>
    /// <param name="songComparisionResults"></param>
    /// <returns></returns>
    public async Task CreateOrUpdateSongsAsync(SongRepository songRepository, List<SongComparisionResults>? songComparisionResults)
    {
        if(songComparisionResults != null)
        {
            foreach(var comparisionResult in songComparisionResults)
            {
                var song = comparisionResult.ApiSong;
                var existingSong = comparisionResult.BaseSongEntity;

                if (song == null)
                    continue;

                if (existingSong != null)
                {
                    // Aktualizuj istniejącą piosenkę
                    song.ShallowCopyTo(existingSong, new List<string> { "Id", "CreatedAt", "UpdatedAt" });

                    await songRepository.UpdateAsync(existingSong);
                }
                else
                {
                    // Dodaj nową piosenkę
                    var songModel = new Song(song);
                    await songRepository.AddAsync(songModel);

                }

            }
        }
    }
}
