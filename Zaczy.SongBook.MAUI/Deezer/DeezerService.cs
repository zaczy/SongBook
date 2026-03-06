using DeezNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI.Deezer;
public class DeezerService
{
    private string _arl;

    public DeezerService(string arl)
    {
        _arl = arl;
    }

    /// <summary>
    /// Pobierz informacje o playliście
    /// </summary>
    /// <param name="playlistUrl"></param>
    /// <returns></returns>
    public async Task<DeezerPlaylistResults?> GetPlaylist(string playlistUrl)
    {

        playlistUrl = await TransformUrl(playlistUrl);

        string playlistId = playlistUrl.TrimEnd('/').Split('/').Last();

        if(playlistId.Contains("?"))
        {
            playlistId = playlistId.Split('?')[0];
        }

        if (long.TryParse(playlistId, out long id))
        {
            var client = new DeezerClient();
            await client.SetARL(_arl);

            var response = await client.GWApi.GetPlaylistTracks(id);
            var json = response.Root.ToString();

            var searchResults = Newtonsoft.Json.JsonConvert.DeserializeObject<DeezerPlaylistResponse>(json);
            return searchResults?.results;

        }

        return null;
    }

    /// <summary>
    /// Wyszukaj utwory na Deezerze na podstawie zapytania. Wyniki będą zawierać informacje o znalezionych utworach, takie jak tytuł, wykonawca, album itp.
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<DeezerSearchResults?> DeezerSearch(string query, int limit=30)
    {
        // Inicjalizacja klienta
        var client = new DeezerClient();
        await client.SetARL(_arl);

        var results = await client.GWApi.Search(query, limit);
        var json = results.Root.ToString();

        var searchResults = Newtonsoft.Json.JsonConvert.DeserializeObject<DeezerSearchResults>(json);

        return searchResults;
    }

    /// <summary>
    /// Pobierz binarne dane utworu z Deezer na podstawie adresu URL. Dane te mogą być następnie zapisane jako plik MP3 lub przetwarzane w inny sposób.
    /// </summary>
    /// <param name="deezerUrl"></param>
    /// <returns></returns>
    public async Task<byte[]?> DeezerDownloadBytes(string deezerUrl)
    {
        var client = new DeezerClient();

        try
        {
            if (DeezerURL.TryParse(deezerUrl, out DeezerURL deezer))
            {
                if (!string.IsNullOrEmpty(deezerUrl))
                {
                    var id = deezer.Id;
                    var entityType = deezer.EntityType;

                    if (entityType == DeezNET.Data.EntityType.Track)
                    {
                        await client.SetARL(_arl);
                        //trackData = await client.GWApi.GetTrack(id);

                        var track = await this.GetDeezerTrackMetadataAsync(deezerUrl);

                        if (track != null)
                        {
                            var trackBytes = await client.Downloader.GetRawTrackBytes(id, DeezNET.Data.Bitrate.MP3_320);
                            return trackBytes;

                        }
                    }
                }
            }
        }
        catch //(Exception ex)
        {

        }

        return null;
    }

    /// <summary>
    /// Pobierz strumień utworu z Deezer na podstawie adresu URL. 
    /// </summary>
    /// <param name="deezerUrl"></param>
    /// <returns></returns>
    public async Task<Stream?> DeezerDownloadStream(string deezerUrl)
    {
        var client = new DeezerClient();

        try
        {
            if (DeezerURL.TryParse(deezerUrl, out DeezerURL deezer))
            {
                if (!string.IsNullOrEmpty(deezerUrl))
                {
                    var id = deezer.Id;
                    var entityType = deezer.EntityType;

                    if (entityType == DeezNET.Data.EntityType.Track)
                    {
                        await client.SetARL(_arl);
                        var track = await this.GetDeezerTrackMetadataAsync(deezerUrl);

                        if (track != null)
                        {
                            var stream = await client.Downloader.GetRawTrackStream(id, DeezNET.Data.Bitrate.MP3_320);
                            return stream;

                        }
                    }
                }
            }
        }
        catch //(Exception ex)
        {

        }

        return null;
    }


    /// <summary>
    /// Pobierz metadane utworu z Deezer na podstawie adresu URL
    /// </summary>
    /// <param name="deezerUrl"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<DeezerTrack?> GetDeezerTrackMetadataAsync(string deezerUrl)
    {
        if (string.IsNullOrEmpty(deezerUrl))
            throw new Exception("Nie podano adresu Deezer!");

        try
        {
            var client = new DeezerClient();

            deezerUrl = await DeezerService.TransformUrl(deezerUrl);

            if (DeezerURL.TryParse(deezerUrl, out DeezerURL deezer))
            {
                var id = deezer.Id;
                var entityType = deezer.EntityType;

                if (entityType == DeezNET.Data.EntityType.Track)
                {
                    await client.SetARL(_arl);
                    var trackData = await client.GWApi.GetTrack(id);

                    var json = trackData.Root.ToString();
                    var track = Newtonsoft.Json.JsonConvert.DeserializeObject<DeezerTrackDataResults>(json);
                    //if (track?.results != null)
                    //{
                    //    _viewModel.RSOTools.AddLog(new LogInfo($"Wykonawca: {track.results.ArtistNames}") { IsFirstLine = true });
                    //    _viewModel.RSOTools.AddLog(new LogInfo($"Tytuł: {track.results.SNG_TITLE}") { Thumbnail = track.results.ALB_COVER });
                    //    _viewModel.RSOTools.AddLog(new LogInfo($"Premiera: {track.results.DIGITAL_RELEASE_DATE ?? track.results.PHYSICAL_RELEASE_DATE}"));
                    //    _viewModel.RSOTools.AddLog(new LogInfo($"Czas trwania: {track.results.DURATION_FORMATTED}"));
                    //}

                    return track?.results;
                }
            }
            else
            {
                throw new Exception("Nieprawidłowy adres Deezer!");
            }
        }
        catch
        {
        }
        finally
        {
        }

        return null;
    }


    /// <summary>
    /// Przekształć url
    /// </summary>
    /// <param name="deezerUrl"></param>
    /// <returns></returns>
    public static async Task<string> TransformUrl(string deezerUrl)
    {
        try
        {
            string unshort = await SafeUnshorten(deezerUrl);
            if (!string.IsNullOrEmpty(unshort))
            {
                var dict = ExtractQueryParameters(unshort);
                if (dict.TryGetValue("dest", out var dest))
                    deezerUrl = dest;
            }
        }
        catch
        {

        }
    
        return deezerUrl;
    }

    /// <summary>
    /// Pobiera docelowy URL z przekierowania, ale bez faktycznego podążania za nim (co jest ważne, bo Deezer może blokować boty, które próbują śledzić przekierowania)
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static async Task<string> SafeUnshorten(string url)
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler);

        // Kluczowe: Udajemy przeglądarkę, inaczej Deezer nie zwróci Location
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        try
        {
            var response = await client.GetAsync(url);
            if (response.Headers.Location != null)
            {
                return response.Headers.Location.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }

        return url; // Zwróć oryginał, jeśli nie udało się przekierować
    }

    /// <summary>
    /// Pobierz wartość parametru z odpowiedzi
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static Dictionary<string, string> ExtractQueryParameters(string url)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(url))
            return result;

        int q = url.IndexOf('?');
        if (q < 0)
            return result;

        string query = url.Substring(q + 1);
        int hash = query.IndexOf('#');
        if (hash >= 0)
            query = query.Substring(0, hash);

        foreach (var part in query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split(new[] { '=' }, 2);
            string key = Uri.UnescapeDataString(kv[0]);
            string value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;

            // Some services double-encode the inner URL; try decode a second time when it looks encoded
            if (value.Contains("%"))
            {
                try { value = Uri.UnescapeDataString(value); } catch { /* ignore decode errors */ }
            }

            result[key] = value;
        }

        return result;
    }

}
