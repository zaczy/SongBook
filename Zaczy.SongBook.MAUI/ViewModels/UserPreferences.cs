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
    public bool LyricsDarkMode { get; set; }
    public bool MoveChordsToLyricsLine { get; set; }

    // New properties for authentication
    public string? UserEmail { get; set; }
    public string? UserToken { get; set; }
    public string? UserPicture { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsEditor { get; set; }
    public bool DeezerPlayerEnabled { get; set; }
    public string? DeezerArl { get; set; } = "864acf7079aa1364f72ab423edf6a7fc5050d6a56de9ef24b0b5a982c9bbd967a8c02478e1464713f2d985f9cee7bcc69df25e8ee3078f54037f03acc9fbdc335ae587338b97d01d704896713ae9d15884e082c3041ade62c65055e9ebb4ad84";

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
}
