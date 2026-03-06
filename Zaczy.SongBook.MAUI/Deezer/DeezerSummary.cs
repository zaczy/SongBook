using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Extensions;
using Zaczy.SongBook.MAUI.Deezer;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.Songbook.MAUI.Deezer;

public class DeezerSummary
{

    public string? Title { get; set; }
    public string? Artist { get; set; }

    public string? DurationTxt { get; set; }

    public DeezerTrack? DeezerTrack { get; set; }

    /// <summary>
    /// Stwórz obiekt DeezerSummary na podstawie obiektu DeezerTrack.
    /// </summary>
    /// <param name="deezerTrack"></param>
    /// <returns></returns>
    public static DeezerSummary? CreateFromTrack(DeezerTrack? deezerTrack)
    {
        if (deezerTrack == null)
            return null;

        return new DeezerSummary()
        {
            Title = deezerTrack.SNG_TITLE,
            Artist = deezerTrack.ArtistNames,
            DurationTxt = deezerTrack.DURATION_FORMATTED,
            DeezerTrack = deezerTrack
        };
    }

    /// <summary>
    /// Pobierz informacje o utworze z Deezer na podstawie danych z SongEntity. 
    /// </summary>
    /// <param name="songEntity"></param>
    /// <param name="userViewModel"></param>
    /// <returns></returns>
    public static async Task<DeezerSummary?> CreateForSongEntity(SongEntity songEntity, UserViewModel userViewModel)
    {
        if (string.IsNullOrEmpty(userViewModel.DeezerArl))
        {
            return null;
        }

        DeezerService deezerService = new DeezerService(userViewModel.DeezerArl);

        userViewModel.DeezerStatusInfo = "Wysyłam zapytanie do Deezer";
        var searchResults = await deezerService.DeezerSearch($"{songEntity.Artist} {songEntity.Title}");
        if (searchResults?.results?.TRACK?.data?.Count() > 0)
        {
            userViewModel.DeezerStatusInfo = $"Zwróconych wyników: {searchResults.results.TRACK.data.Count()}";
            var track = searchResults.results.TRACK.data.Where(t => t.SNG_TITLE.ValueForComparisions() == songEntity.Title.ValueForComparisions() && t.ArtistNames.ValueForComparisions() == songEntity.Artist.ValueForComparisions()).FirstOrDefault();

            if (track != null)
            {
                userViewModel.DeezerStatusInfo = $"Znaleziono dopasowanie {track.ArtistNames} - {track.SNG_TITLE} {track.DURATION_FORMATTED}";
                return CreateFromTrack(track);
            }
            else
            {
                userViewModel.DeezerStatusInfo = "Nie znaleziono dopasowania w Deezer";
            }
        }
        else
        {
            userViewModel.DeezerStatusInfo = "Nie znaleziono utworu w Deezer";
        }


        return null;
    }
}
