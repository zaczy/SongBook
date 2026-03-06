namespace Zaczy.SongBook.MAUI.Deezer;

public class DeezerContributors
{
    public List<string>? main_artist { get; set; }
    public List<string>? composer { get; set; }
    public List<string>? author { get; set; }
    public List<string>? featuring { get; set; }

    public string? Performers()
    {
        if (main_artist?.Count > 0)
        {
            return string.Join(", ", main_artist);
        }

        return null;
    }
}