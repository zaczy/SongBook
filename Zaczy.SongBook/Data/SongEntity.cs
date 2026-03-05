using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Zaczy.SongBook.Migrations;

namespace Zaczy.SongBook.Data;

[Table("songs")]
public partial class SongEntity : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    [Key]
    [Column("id")]
    public int Id { get; set; }

    private string? _title;
    [Column("title")]
    [MaxLength(255)]
    public string? Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    private string? _artist;
    [Column("artist")]
    [MaxLength(255)]
    public string? Artist
    {
        get => _artist;
        set
        {
            if (_artist != value)
            {
                _artist = value;
                OnPropertyChanged(nameof(Artist));
            }
        }
    }

    [Column("lyrics_author")]
    [JsonPropertyName("lyrics_author")]
    [MaxLength(255)]
    public string? LyricsAuthor { get; set; }

    [Column("music_author")]
    [JsonPropertyName("music_author")]
    [MaxLength(255)]
    public string? MusicAuthor { get; set; }

    [Column("capo")]
    [MaxLength(50)]
    public string? Capo { get; set; }

    [Column("lyrics")]
    public string? Lyrics { get; set; }

    [Column("comments")]
    [MaxLength(255)]
    public string? Comments { get; set; }

    [Column("chords_variations")]
    [JsonPropertyName("chords_variations")]
    [MaxLength(140)]
    public string? ChordsVariations { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("scrolling_delay")]
    [JsonPropertyName("scrolling_delay")]
    public int? ScrollingDelay { get; set; }

    [Column("song_duration")]
    [JsonPropertyName("song_duration")]
    public int? SongDuration { get; set; }

    private string? _songDurationTxt;
    [NotMapped]
    public string? SongDurationTxt
    {
        get
        {
            // Jeœli _songDurationTxt jest null, spróbuj wygenerowaæ z _songDuration
            if (_songDurationTxt == null && SongDuration.HasValue)
            {
                int totalSeconds = SongDuration.Value;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                _songDurationTxt = $"{minutes:D2}:{seconds:D2}";
            }
            return _songDurationTxt;
        }
        set
        {
            if (_songDurationTxt != value)
            {
                _songDurationTxt = value;

                // Jeœli wartoœæ jest w formacie mm:ss, przelicz na sekundy
                if (!string.IsNullOrWhiteSpace(value) && value.Contains(':'))
                {
                    var parts = value.Split(':');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int minutes) &&
                        int.TryParse(parts[1], out int seconds))
                    {
                        SongDuration = minutes * 60 + seconds;
                    }
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    SongDuration = null;
                }
            }
        }
    }


    [Column("spotify_link")]
    [JsonPropertyName("spotify_link")]
    [MaxLength(120)]
    public string? SpotifyLink { get; set; }

    [JsonIgnore]
    [NotMapped]
    public bool HasSpotifyLink
    {
        get => !string.IsNullOrEmpty(SpotifyLink);
    }

    [JsonPropertyName("more_info")]
    [Column("more_info")]
    [MaxLength(255)]
    public string? MoreInfo { get; set; }

    [JsonPropertyName("source")]
    [Column("source")]
    [MaxLength(40)]
    public string? Source { get; set; }

    private string? _categoryColor;
    [MaxLength(20)]
    [JsonPropertyName("category_color")]
    [Column("category_color")]
    public string? CategoryColor
    {
        get
        {
            return _categoryColor;
        }

        set => _categoryColor = value;
    }

    [NotMapped]
    public bool HasEditPrivileges { get; set; } = true;

    /// <summary>
    /// Inicjalizuje obiekt SongEntity na podstawie obiektu Song
    /// </summary>
    /// <param name="song"></param>
    public void initFromSong(Song song)
    {
        Title = song.Title;
        Artist = song.Artist;
        Capo = song.Capo;
        Lyrics = song.Lyrics;
        LyricsAuthor = song.LyricsAuthor;
        MusicAuthor = song.MusicAuthor;
        ChordsVariations = song.ChordsVariations;
        ScrollingDelay = song.ScrollingDelay;
        SongDuration = song.SongDuration;
        SpotifyLink = song.SpotifyLink;
        MoreInfo = song.MoreInfo;
        Source = song.Source;
        if (song?.ServerId != null)
            Id = song.ServerId.Value;
    }

    public override string ToString()
    {
        return $"{Title} ({Id})";
    }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<bool> HasUserEditPrivileges(string? email)
    {
        return !string.IsNullOrEmpty(email) && true;
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}