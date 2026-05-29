namespace Zaczy.SongBook.MAUI;
public class Settings
{
    public string ApiBaseUrl { get; set; } = string.Empty;

    public string WebBaseUrl { get; set; } = string.Empty;

    public string? ApiToken { get; set; } = string.Empty;

    public int ListeningGroupCheckInterval { get; set; } = 10;
    public int ApiTimeout { get; set; } = 10000;
}