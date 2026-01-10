namespace Zaczy.SongBook.WPF;

public class AppSettings
{
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public SettingsSection Settings { get; set; } = new();
}

public class ConnectionStrings
{
    public string? SongBookDb { get; set; }
}

public class SettingsSection
{
    public string? ApiBaseUrl { get; set; }
}