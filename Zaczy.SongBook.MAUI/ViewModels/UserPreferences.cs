using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Enums;
using Zaczy.SongBook.MAUI.Services;

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
    public bool? LyricsDarkMode { get; set; } = null;
    public bool MoveChordsToLyricsLine { get; set; }

    // New properties for authentication
    public string? UserEmail { get; set; }
    public string? UserToken { get; set; }
    public string? UserPicture { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsEditor { get; set; }
    public bool DeezerPlayerEnabled { get; set; }
    public string? DeezerArl { get; set; } = "d37ff63af9271ea7334e2bec30fc199f23c6d2df0dfcc954b6657aa935fb0e3a265e643cb199d3da66e9ae70ba2bfd75d7629aeb053cf5011453ab585e71b0c672c16a77d4cf9f61ce4e00eba30543c6fb95a8c2b0f119db6622bba1ad09f9fb";

    public bool ShowDiagnostics { get; set; } = false;
    public bool ScrollingStartCompensate { get; set; }
    public string? AppGuid { get; set; }

    public InstrumentType ChordsInstrument { get; set; } = InstrumentType.Guitar;
    public bool GroupModeActive { get; set; } = true;
    public PermissionsDecision BluetoothPermissionsDecision { get; set; }
    public bool BroadcastWeb { get; set; } = true;
    public bool BroadcastBluetooth { get; set; } = true;
    public bool EnableGroupListeningWhenDirector { get; set; } = true;

    public bool ExtendedApiLogging { get; set; } = false;
    public bool EnableEdgeToEdge { get; set; } = true;
    public string? TextFontFamily { get;  set; }
    public string? ChordFontFamily { get;  set; }
    public int? ChordFontSize { get;  set; }
    public string? ChordColor { get;  set; }
    public string? TextColor { get;  set; }
    public string? BackgroundColor { get;  set; }
    public bool SearchLyrics { get; set; }
    public bool SearchSongAuthors { get; set; } = true;
}
