using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    [MaxLength(255)]
    public string? LyricsAuthor { get; set; }

    [Column("music_author")]
    [MaxLength(255)]
    public string? MusicAuthor { get; set; }

    [Column("capo")]
    [MaxLength(50)]
    public string? Capo { get; set; }

    [Column("lyrics")]
    public string? Lyrics { get; set; }

    [Column("comments")]
    public string? Comments { get; set; }

    [Column("chords_variations")]
    public string? ChordsVariations { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public void initFromSong(Song song)
    {
        Title = song.Title;
        Artist = song.Artist;
        Capo = song.Capo;
        Lyrics = song.Lyrics;
        LyricsAuthor = song.LyricsAuthor;
        MusicAuthor = song.MusicAuthor;
        ChordsVariations = song.ChordsVariations;
    }
}