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
    public bool IsAdmin { get; set; }
    public bool IsEditor { get; set; }

    public string? DeezerArl { get; set; } = "a15129c93bb2459ae6618cab267d52f11e7cf696abe76f4d91f633f8380141fa1ba9b1168284085ea5944266f4bd1a48249a1ecc5b6e947feb46987a6ef008b65090286bfef913d30ee852004513faf8dd98d73aea43c24b0cfe3b3191d253a4";


}
