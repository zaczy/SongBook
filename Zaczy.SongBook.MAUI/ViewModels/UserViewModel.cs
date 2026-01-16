using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiteDB;
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
                OnPropertyChanged(nameof(ScrollingText));
            }
        }
    }

    public string ScrollingText
    {
        get => ScrollingInProgress ? "■ Zatrzymaj" : "Przewijanie ▶";
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}