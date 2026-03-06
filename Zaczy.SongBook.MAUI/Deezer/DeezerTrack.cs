using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI.Deezer;
public class DeezerTrack
{
    public string? SNG_ID { get; set; }
    public string? ALB_TITLE { get; set; }
    public string? ART_NAME { get; set; }

    public string? ALB_PICTURE { get; set; }

    public int? LYRICS_ID { get; set; }
    public DateTime? PHYSICAL_RELEASE_DATE { get; set; }
    public DateTime? DIGITAL_RELEASE_DATE { get; set; }

    public int? DURATION { get; set; }

    public string? DURATION_FORMATTED
    {
        get
        {
            if (DURATION == null)
                return null;
            TimeSpan time = TimeSpan.FromSeconds(DURATION.Value);
            return time.ToString(@"mm\:ss");
        }
    }


    public string? SNG_TITLE { get; set; }


    public object? SNG_CONTRIBUTORS { get; set; }

    public List<DeezerArtist>? ARTISTS { get; set; } = new List<DeezerArtist>();

    public string ArtistNames
    {
        get
        {
            if (ARTISTS == null || ARTISTS.Count == 0)
                return string.Empty;
            return string.Join(", ", ARTISTS.OrderBy(a => a.ARTISTS_SONGS_ORDER).Select(a => a.ART_NAME));
        }
    }

    public string? ALB_COVER
    {
        get
        {
            if (!string.IsNullOrEmpty(ALB_PICTURE))
            {
                return ImageUrl(ALB_PICTURE);
            }
            return null;
        }
    }

    public string Url 
    { 
        get
        {
            if(!string.IsNullOrEmpty(SNG_ID))
            {
                return $"https://www.deezer.com/pl/track/{SNG_ID}";
            }

            return string.Empty;
        }
    }

    public static string ImageUrl(string hash)
    {
        return $"https://e-cdns-images.dzcdn.net/images/cover/{hash}/500x500-000000-80-0-0.jpg";
    }

    public override string ToString()
    {
        return $"{ArtistNames} - {SNG_TITLE} ({DURATION_FORMATTED})";
    }

}
