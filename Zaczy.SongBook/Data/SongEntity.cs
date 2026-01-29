using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Zaczy.SongBook.Data;

[Table("songs")]
public class SongEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [MaxLength(255)]
    public string? Title { get; set; }

    [Column("artist")]
    [MaxLength(255)]
    public string? Artist { get; set; }

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
    public int ScrollingDelay { get; set; }

    [Column("scrolling_tempo")]
    [JsonPropertyName("scrolling_tempo")]
    public int? ScrollingTempo { get; set; }

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
    public string? MoreInfo { get; internal set; }

    [JsonPropertyName("source")]
    [Column("source")]
    [MaxLength(40)]
    public string? Source { get; set; }

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
        ScrollingTempo = song.ScrollingTempo;
        SpotifyLink = song.SpotifyLink;
        MoreInfo = song.MoreInfo;
        Source = song.Source;
    }

    public override string ToString()
    {
        return $"{Title} ({Id})";
    }
}