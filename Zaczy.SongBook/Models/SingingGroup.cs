using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Zaczy.SongBook.Models;

public class SingingGroup
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Nazwa grupy słuchaczy
    /// </summary>
    [Required]
    [MaxLength(255)]
    [JsonPropertyName("group_name")]
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Opis grupy
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Data ważności grupy
    /// </summary>
    [JsonPropertyName("valid_until")]
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Data ostatniego spotkania / zestawu piosenek
    /// </summary>
    [JsonPropertyName("last_set_date")]
    public DateTime? LastSetDate { get; set; }

    /// <summary>
    /// Email twórcy grupy (songbook_users.email)
    /// </summary>
    [EmailAddress]
    [MaxLength(100)]
    [JsonPropertyName("creator")]
    public string? Creator { get; set; }

    /// <summary>
    /// Email lidera grupy (songbook_users.email)
    /// </summary>
    [EmailAddress]
    [MaxLength(100)]
    [JsonPropertyName("leader")]
    public string? Leader { get; set; }

    [EmailAddress]
    [MaxLength(400)]
    [JsonPropertyName("leader_guid")]
    public string? LeaderGuid { get; set; }


    /// <summary>
    /// Aktualnie odtwarzana piosenka (songs.id)
    /// </summary>
    [JsonPropertyName("current_song_id")]
    public int? CurrentSongId { get; set; }

    [JsonPropertyName("current_song")]
    public Song? CurrentSong { get; set; }
}
