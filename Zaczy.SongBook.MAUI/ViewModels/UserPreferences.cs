using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Enums;

namespace Zaczy.SongBook.MAUI.ViewModels;

public class UserPreferences
{
    public int Id { get; set; }
    public double FontSizeAdjustment { get; set; }
    public int? AutoScrollSpeed { get; set; }
    public LyricsHtmlVersion LyricsHtmlVersion { get; set; } = LyricsHtmlVersion.RelativeHtml;
    public bool ShowOnlyCustomChords { get; set; }
    public bool SkipTabulatures { get; set; } = true;
    public bool SkipLyricChords { get; set; }
    public bool LyricsDarkMode { get; set; }
    public bool MoveChordsToLyricsLine { get; set; }

    // New properties for authentication
    public string? UserEmail { get; set; }
    public string? UserToken { get; set; }
    public string? UserPicture { get; set; }
}
