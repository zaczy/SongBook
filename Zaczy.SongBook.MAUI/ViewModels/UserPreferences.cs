using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Enums;

namespace Zaczy.SongBook.MAUI.ViewModels;

public class UserPreferences
{
    
    public int? Id { get; set; }
    public double FontSizeAdjustment { get; set; } = 0;
    public int? AutoScrollSpeed { get; set; }

    public LyricsHtmlVersion LyricsHtmlVersion { get; set; } = Enums.LyricsHtmlVersion.RelativeHtml;

    public bool ShowOnlyCustomChords { get; set; } = false;

    public bool SkipTabulatures { get; set; } = false;
    public bool SkipLyricChords { get; set; } = false;
    public bool LyricsDarkMode { get; set; } = false;
    public bool MoveChordsToLyricsLine { get; set; } = false;

}
