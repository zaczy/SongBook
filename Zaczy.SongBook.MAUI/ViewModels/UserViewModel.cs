using LiteDB;
using MauiIcons.Core;
using MauiIcons.Core.Base;
using MauiIcons.Fluent;
using MauiIcons.FontAwesome.Solid;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Zaczy.SongBook.Enums;

namespace Zaczy.SongBook.MAUI.ViewModels;

public class UserViewModel : INotifyPropertyChanged
{
    private readonly LiteDatabase _liteDb;
    private const int PrefsId = 1;
    private UserPreferences? _prefs;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserViewModel(LiteDatabase liteDb)
    {
        _liteDb = liteDb;
        Load();
    }

    /// <summary>
    /// PObierz ustawienia z zapisanych w LiteDb
    /// </summary>
    private void Load()
    {
        var col = _liteDb.GetCollection<UserPreferences>("user_prefs");
        _prefs = col.FindById(PrefsId) ?? new UserPreferences { Id = PrefsId };
    }

    /// <summary>
    /// Zapisz do LiteDb
    /// </summary>
    private void Save()
    {
        if (_prefs != null)
            {
            var col = _liteDb.GetCollection<UserPreferences>("user_prefs");
            col.Upsert(_prefs);
        }
    }

    /// <summary>
    /// Dopasuj wielkość czcionki
    /// </summary>
    public double FontSizeAdjustment
    {
        get => _prefs?.FontSizeAdjustment ?? 0;
        set
        {
            if (_prefs == null)
                return;

            if (_prefs != null)
            {
                _prefs.FontSizeAdjustment = value;
                Save();
                OnPropertyChanged();
            }
        }
    }

    public int? AutoScrollSpeed
    {
        get => _prefs?.AutoScrollSpeed;
        set
        {
            if (_prefs == null)
                return;

            if (_prefs.AutoScrollSpeed != value)
            {
                _prefs!.AutoScrollSpeed = value;
                Save();
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Sposób renderowania HTML
    /// </summary>
    public LyricsHtmlVersion LyricsHtmlVersion
    {
        get => _prefs?.LyricsHtmlVersion ?? LyricsHtmlVersion.RelativeHtml;

        set
        {
            if (_prefs == null)
                return;

            if (_prefs.LyricsHtmlVersion != value)
            {
                _prefs.LyricsHtmlVersion = value;
                Save();
                OnPropertyChanged(nameof(LyricsHtmlVersion));
            }
        }
    }

    private bool _scrollingInProgress;
    /// <summary>
    /// Czy jesteśmy w trakcie automatycznego przewijania
    /// </summary>
    public bool ScrollingInProgress 
    {
        get => _scrollingInProgress;
        set
        {
            if(_scrollingInProgress != value)
            {
                _scrollingInProgress = value;
                OnPropertyChanged(nameof(ScrollingInProgress));
                OnPropertyChanged(nameof(ToggleIcon));
                OnPropertyChanged(nameof(PlayIcon));
            }
        }
    }

    private bool _enablePinchGestures = true;
    /// <summary>
    /// Możliwość zmiany rozmiaru czcionki przez gesty
    /// </summary>
    public bool EnablePinchGestures
    {
        get { return _enablePinchGestures; }
        set 
        {
            if (_enablePinchGestures != value)
            {
                _enablePinchGestures = value;
                OnPropertyChanged(nameof(EnablePinchGestures));
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool ShowOnlyCustomChords
    {
        get { return _prefs?.ShowOnlyCustomChords ?? false; }
        set 
        {
            if (_prefs!= null && _prefs.ShowOnlyCustomChords != value)
            {
                _prefs.ShowOnlyCustomChords = value;
                Save();
                OnPropertyChanged(nameof(ShowOnlyCustomChords));
                OnPropertyChanged(nameof(ShowOnlyCustomChordsTxt));
            }
        }
    }

    /// <summary>
    /// Nie pokazuj tabulatur dla akordów
    /// </summary>
    public bool SkipTabulatures
    {
        get { return _prefs?.SkipTabulatures ?? false; }
        set 
        {
            if (_prefs!= null && _prefs.SkipTabulatures != value)
            {
                _prefs.SkipTabulatures = value;
                Save();
                OnPropertyChanged(nameof(SkipTabulatures));
                OnPropertyChanged(nameof(SkipTabulaturesTxt));
            }
        }
    }

    public string SkipTabulaturesTxt
    {
        get => _prefs?.SkipTabulatures == true ? "tabulatury są niepotrzebne - nie pokazuj ich" : "";
    }

    public string SkipLyricChordsTxt
    {
        get => _prefs?.SkipLyricChords == true ? "nie jestem gitarzystą - schowaj te dziwne literki" : "";
    }


    /// <summary>
    /// Nie pokazuj tabulatur dla akordów
    /// </summary>
    public bool SkipLyricChords
    {
        get { return _prefs?.SkipLyricChords ?? false; }
        set
        {
            if (_prefs != null && _prefs.SkipLyricChords != value)
            {
                _prefs.SkipLyricChords = value;
                Save();
                OnPropertyChanged(nameof(SkipLyricChords));
                OnPropertyChanged(nameof(SkipLyricChordsTxt));
            }
        }
    }


    public string ShowOnlyCustomChordsTxt
    {
        get => _prefs?.ShowOnlyCustomChords == true ? "wyświetlaj diagramy tylko dla niestandardowych przewrotów akordów wykorzystanych w piosence" : "pokazuj diagramy wszystkich chwytów";
    }


    public BaseIcon PlayIcon 
    {
        get
        {
            return  new BaseIcon() 
            {
                Icon = (ScrollingInProgress ? MauiIcons.FontAwesome.Solid.FontAwesomeSolidIcons.Pause : MauiIcons.FontAwesome.Solid.FontAwesomeSolidIcons.Play),
                IconSize = 15,
                IconColor = Microsoft.Maui.Graphics.Colors.DarkGray 
            };

        }
    }

    // BaseIcon instances for toggle (MVVM-friendly)
    private readonly BaseIcon _playToggle = new BaseIcon
    {
        Icon = FluentIcons.Play20,
        IconSize = 28,
        IconColor = Colors.White
    };

    private readonly BaseIcon _pauseToggle = new BaseIcon
    {
        Icon = FluentIcons.Pause20,
        IconSize = 28,
        IconColor = Colors.White
    };

    // Exposed icon that the view can bind to (mi:MauiIcon.Value)
    public BaseIcon ToggleIcon => ScrollingInProgress ? _pauseToggle : _playToggle;


    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}