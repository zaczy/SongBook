namespace Zaczy.SongBook.MAUI.Deezer;
public class DeezerAlbum
{
    public string? ALB_ID { get; set; }
    public string? ALB_TITLE { get; set; }
    public string? ALB_PICTURE { get; set; }
    public List<DeezerArtist>? ARTISTS { get ; set; }

    public DateTime? PHYSICAL_RELEASE_DATE { get; set; }
    public DateTime? ORIGINAL_RELEASE_DATE { get; set; }
    public string? ArtistNames
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
                return DeezerTrack.ImageUrl(ALB_PICTURE);
            }
            return null;
        }
    }
}
