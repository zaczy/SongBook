using LiteDB;
using MauiIcons.Core;
using MauiIcons.Core.Base;
using MauiIcons.Fluent;
using MauiIcons.FontAwesome.Solid;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Zaczy.Songbook.MAUI.Services;
using Zaczy.SongBook.Enums;
using Zaczy.SongBook.Extensions;

namespace Zaczy.SongBook.MAUI.ViewModels;

public class UserViewModel : INotifyPropertyChanged
{
    private readonly LiteDatabase _liteDb;
    //private readonly GoogleAuthService _authService;
    private const int PrefsId = 1;
    private UserPreferences? _prefs;
    private readonly Settings _settings;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserViewModel(LiteDatabase liteDb, IOptions<Settings> settings)
    {
        _liteDb = liteDb;
        //_authService = authService;
        _settings = settings.Value;

        Load();

        AuthenticateCommand = new Command(async () => await AuthenticateAsync());
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
        //get => _prefs?.SkipLyricChords == true ? "nie jestem gitarzystą - schowaj te dziwne literki" : "";
        get => "nie jestem gitarzystą - schowaj te dziwne literki";
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

    /// <summary>
    /// Tryb ciemny dla tekstu piosenek
    /// </summary>
    public bool LyricsDarkMode 
    { 
        get => _prefs?.LyricsDarkMode ?? false;
        set
        {
            if (_prefs != null && _prefs.LyricsDarkMode != value)
            {
                _prefs.LyricsDarkMode = value;
                Save();
                OnPropertyChanged(nameof(LyricsDarkMode));
            }
        }
    }

    /// Przenieś akordy na koniec linii z tekstem
    public bool MoveChordsToLyricsLine 
    {
        get => _prefs?.MoveChordsToLyricsLine ?? false;
        set
        {
            if (_prefs != null && _prefs.MoveChordsToLyricsLine != value)
            {
                _prefs.MoveChordsToLyricsLine = value;
                Save();
                OnPropertyChanged(nameof(MoveChordsToLyricsLine));
            }
        }
    }

    /// <summary>
    /// Email zalogowanego użytkownika
    /// </summary>
    public string? UserEmail
    {
        get => _prefs?.UserEmail;
        set
        {
            if (_prefs != null && _prefs.UserEmail != value)
            {
                _prefs.UserEmail = value;
                Save();
                OnPropertyChanged(nameof(UserEmail));
                OnPropertyChanged(nameof(IsUserAuthenticated));
            }
        }
    }

    /// <summary>
    /// Token autoryzacyjny Google
    /// </summary>
    public string? UserToken
    {
        get => _prefs?.UserToken;
        set
        {
            if (_prefs != null && _prefs.UserToken != value)
            {
                _prefs.UserToken = value;
                Save();
                OnPropertyChanged(nameof(UserToken));
                OnPropertyChanged(nameof(IsUserAuthenticated));
            }
        }
    }

    /// <summary>
    /// Awatar użytkownika (URL do zdjęcia z Google)
    /// </summary>
    public string? UserPicture
    {
        get => _prefs?.UserPicture;
        set
        {
            if (_prefs != null && _prefs.UserPicture != value)
            {
                _prefs.UserPicture = value;
                Save();
                OnPropertyChanged(nameof(UserPicture));
            }
        }
    }

    /// <summary>
    /// Czy użytkownik jest zalogowany
    /// </summary>
    public bool IsUserAuthenticated => !string.IsNullOrEmpty(UserEmail) && !string.IsNullOrEmpty(UserToken);


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

    public string ApplicationVersion => $"Wersja aplikacji {AppInfo.Current.VersionString}";

    public ICommand AuthenticateCommand { get; }

    private string? _loginInfo;
    public string? LoginInfo
    {
        get 
        { 
            return _loginInfo; 
        }
        set 
        {
            if (_loginInfo != value)
            {
                _loginInfo = value;
                OnPropertyChanged(nameof(LoginInfo));
            }
        }
    }

    /// <summary>
    /// Przeprowadź proces logowania lub wylogowania użytkownika. 
    /// </summary>
    /// <returns></returns>
    private async Task AuthenticateAsync()
    {
        if (IsUserAuthenticated)
        {
            // Wyloguj
            UserEmail = null;
            UserToken = null;
            return;
        }

        //var result = await _authService.AuthenticateAsync();
        var result = await WebAuthenticationBrowserClient.LoginWithGoogle(_settings);

        LoginInfo = result.ToJson();

        if (result != null)
        {
            UserEmail = result.Email;
            UserToken = result.AccessToken; // lub IdToken
            UserPicture = result.Picture;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}