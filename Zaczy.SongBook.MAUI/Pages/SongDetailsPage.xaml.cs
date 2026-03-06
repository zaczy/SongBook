using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DocumentFormat.OpenXml.Presentation;
using MauiIcons.Core;
using MauiIcons.Fluent;
using MauiIcons.FontAwesome;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Plugin.Maui.Audio;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Zaczy.Songbook.MAUI.Deezer;
using Zaczy.Songbook.MAUI.Pages;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Extensions;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.Deezer;
using Zaczy.SongBook.MAUI.Extensions;
using Zaczy.SongBook.MAUI.ViewModels;
using Timer = System.Timers.Timer;

namespace Zaczy.SongBook.MAUI.Pages;

public partial class SongDetailsPage : ContentPage
{
    private readonly Timer _hideControlsTimer;
    private readonly Timer _hideDeezerStatusTimer;
    private readonly UserViewModel _userViewModel;
    private readonly SongEntity _songEntity;
    private SongVisualization _visualization;
    private readonly string _autoScrollJs;
    private int _currentSongScrollSpeed;
    private bool _isSubscribed;
    private VisualizationCssOptions _visualizationCssOptions;
    private bool _previousKeepScreenOn;
    
    private readonly EventApi _eventApi;
    private readonly SongRepositoryLite _songRepository;
    private readonly Settings _settings;
    private readonly IAudioManager _audioManager; // Dodaj to
    
    private bool _suppressTopTouch;
    private IAudioPlayer? _deezerPlayer; // Zmieñ na IAudioPlayer
    private bool _isDeezerPlaying = false;

    public UserViewModel UserViewModel => _userViewModel;
    public SongEntity Song => _songEntity;
    
    private DeezerSummary? _deezerSummary;
        public DeezerSummary? DeezerSummary
    {
        get => _deezerSummary;
    }

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="songEntity"></param>
    /// <param name="userViewModel"></param>
    /// <param name="eventApi"></param>
    /// <param name="songRepository"></param>
    /// <param name="settings"></param>
    /// <param name="audioManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SongDetailsPage(
        SongEntity songEntity, 
        UserViewModel userViewModel, 
        EventApi eventApi, 
        SongRepositoryLite songRepository, 
        Settings settings,
        IAudioManager audioManager) // Dodaj parametr
    {
        _userViewModel = userViewModel;
        _songEntity = songEntity ?? new SongEntity();
        _songEntity.ScrollingDelay = 10;
        _eventApi = eventApi;
        _songRepository = songRepository ?? throw new ArgumentNullException(nameof(songRepository));
        _settings = settings;
        _audioManager = audioManager; // Przypisz

        _songEntity.HasEditPrivileges = _userViewModel.IsEditor || _userViewModel.IsAdmin || 
            _songEntity.HasUserEditPrivileges(_userViewModel.UserEmail).Result || true;

        _visualizationCssOptions = new VisualizationCssOptions();
        _visualization = this.CreateVisualizationOptions();

        _ = new MauiIcon() { Icon = MauiIcons.FontAwesome.Solid.FontAwesomeSolidIcons.LockOpen, IconColor = Colors.Green };
        _ = new MauiIcon() { Icon = MauiIcons.Fluent.FluentIcons.Checkmark24, IconColor = Colors.Green };
        
        InitializeComponent();
        
        _visualizationCssOptions.Add(".lyrics-line", "font-family", "PoltawskiVariable");
        _visualizationCssOptions.Add("pre", "font-weight", "600");

        if(DeviceInfo.Idiom == DeviceIdiom.Tablet)
        {
            _visualizationCssOptions.Add("body", "padding-top", "30px");
        }

        // prepare auto-scroll JS once
        _autoScrollJs = SongPreviewJavascript.JavascriptTxt();


        NavigationPage.SetHasNavigationBar(this, false);
        NavigationPage.SetHasBackButton(this, false);

        // set the page BindingContext to the SongEntity (UI labels use data:SongEntity)
        BindingContext = _songEntity;

        _hideControlsTimer = new Timer(3000) { AutoReset = false };
        _hideControlsTimer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() => ControlsPanel.IsVisible = false);
        };

        _hideDeezerStatusTimer = new Timer(5000) { AutoReset = false };
        _hideDeezerStatusTimer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() => DeezerStatus.IsVisible = false);
        };

        // ensure we react when the WebView finishes loading
        LyricsWebView.Navigating += LyricsWebView_Navigating;
        LyricsWebView.Navigated += OnLyricsWebViewNavigated;

        // initialize fonts and then generate initial HTML
        _ = InitializeAsync();
        }

    /// <summary>
    /// Dopasuj css dla trybu ciemnego
    /// </summary>
    /// <param name="visualizationCssOptions"></param>
    private void AdjustDarkModeCss(VisualizationCssOptions visualizationCssOptions)
    {
        if (_userViewModel.LyricsDarkMode)
        {
            var lyricsDarkBg = Application.Current?.Resources["LyricsDarkBackground"] as Color;
            var lyricsDarkText = Application.Current?.Resources["LyricsDarkText"] as Color;
            var lyricsDarkChords = Application.Current?.Resources["LyricsDarkChords"] as Color;

            _visualizationCssOptions.Add("body", "background-color", lyricsDarkBg?.ToHex() ?? "#222", "dark");
            _visualizationCssOptions.Add("body", "color", lyricsDarkText?.ToHex() ?? "#BBB", "dark");

            _visualizationCssOptions.Add(".chord-diagram", "background-color", lyricsDarkBg?.ToHex() ?? "#1e1e1e", "dark");
            _visualizationCssOptions.Add(".chord-diagram .fret", "background-color", lyricsDarkText?.ToHex() ?? "#1e1e1e", "dark");

            _visualizationCssOptions.Add(".chords", "color", lyricsDarkChords?.ToHex() ?? "#2c2c2c", "dark");
            _visualizationCssOptions.Add(".chords2", "color", lyricsDarkChords?.ToHex() ?? "#2c2c2c", "dark");

            _visualizationCssOptions.Add(".block-refren", "border-left", "15px solid #262626", "dark");
        }
        else
            _visualizationCssOptions?.CustomOptions?.RemoveWhere(opt => opt.Context == "dark");
    }

    /// <summary>
    /// Initialize fonts (copy packaged assets to a physical path WebView can access) then render HTML. 
    /// </summary>
    /// <returns></returns>
    private async Task InitializeAsync()
    {
        try
        {
            await EnsureFontsAvailableAsync();
            
            _ = InitializeDeezerSummaryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EnsureFontsAvailableAsync failed: {ex.Message}");
        }
    }

    private async Task InitializeDeezerSummaryAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_userViewModel?.DeezerArl))
            {
                await ShowDeezerControlsAsync();
                _deezerSummary = await DeezerSummary.CreateForSongEntity(_songEntity, _userViewModel);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeezerSummary creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Copy packaged font assets to AppData (if present) and register paths in SongVisualization.CssFontsPath.
    /// SongVisualization will embed base64 if the file exists.
    /// </summary>
    /// <returns></returns>
    private async Task EnsureFontsAvailableAsync()
    {
        var fontAssets = new Dictionary<string, string?>
        {
            { "InconsolataVariable", "assets/css/Inconsolata/Inconsolata-VariableFont_wdth,wght.ttf" },
            //{ "RobotoVariable",  "css/Roboto/Roboto-VariableFont_wdth,wght.ttf" },
            { "PoltawskiVariable", "assets/css/Poltawski_Nowy/PoltawskiNowy-VariableFont_wght.ttf" }
        };

        var appData = FileSystem.AppDataDirectory;

        foreach (var kv in fontAssets)
        {
            var fontKey = kv.Key;
            var assetRelative = kv.Value;
            if (string.IsNullOrEmpty(assetRelative))
                continue;

            try
            {
                var destPath = Path.Combine(appData, assetRelative.Replace('/', Path.DirectorySeparatorChar));
                var destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir!);

                if (!File.Exists(destPath))
                {
                    try
                    {
                        using var stream = await FileSystem.OpenAppPackageFileAsync(assetRelative);
                        using var outFs = File.Create(destPath);
                        await stream.CopyToAsync(outFs);
                    }
                    catch (Exception)
                    {
                        System.Diagnostics.Debug.WriteLine($"Font asset not found in package: {assetRelative}");
                        continue;
                    }
                }

                if (_visualization.CssFontsPath == null)
                    _visualization.CssFontsPath = new Dictionary<string, string>();

                _visualization.CssFontsPath[fontKey] = destPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to prepare font {assetRelative}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Zdarzenie wywo³ywane, gdy strona pojawia siê na ekranie.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            LyricsWebView.Navigating -= LyricsWebView_Navigating;
            LyricsWebView.Navigated -= OnLyricsWebViewNavigated;
        }
        catch { }

        LyricsWebView.Navigating += LyricsWebView_Navigating;
        LyricsWebView.Navigated += OnLyricsWebViewNavigated;

        if (!_isSubscribed && _userViewModel != null)
        {
            _userViewModel.PropertyChanged += UserViewModel_PropertyChanged;
            _isSubscribed = true;
        }

        try
        {
            _previousKeepScreenOn = DeviceDisplay.KeepScreenOn;
            if (OperatingSystem.IsAndroid())
                DeviceDisplay.KeepScreenOn = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set KeepScreenOn: {ex.Message}");
        }

        _ = RegenerateHtmlAsync();
    }

    /// <summary>
    /// Zdarzenie wywo³ywane, gdy strona znika z ekranu.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Zatrzymaj i zwolnij odtwarzacz Deezer
        if (_deezerPlayer != null)
        {
            _deezerPlayer.Stop();
            _deezerPlayer.Dispose();
            _deezerPlayer = null;
            _isDeezerPlaying = false;
        }

        //CleanupDeezerTempFile();

        try
        {
            if (OperatingSystem.IsAndroid())
                DeviceDisplay.KeepScreenOn = _previousKeepScreenOn;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to restore KeepScreenOn: {ex.Message}");
        }

        try
        {
            _userViewModel.ScrollingInProgress = false;
            _ = LyricsWebView.EvaluateJavaScriptAsync("stopAutoScroll();");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to stop auto-scroll: {ex.Message}");
        }

        LyricsWebView.Navigated -= OnLyricsWebViewNavigated;
        LyricsWebView.Navigating -= LyricsWebView_Navigating;

        if (_isSubscribed && _userViewModel != null)
        {
            _userViewModel.PropertyChanged -= UserViewModel_PropertyChanged;
            _isSubscribed = false;
        }

        WeakReferenceMessenger.Default.Unregister<ValueChangedMessage<SongEntity>>(this);
    }

    private void UserViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e == null) return;

        if (e.PropertyName == nameof(UserViewModel.LyricsHtmlVersion)
            || e.PropertyName == nameof(UserViewModel.FontSizeAdjustment))
        {
            MainThread.BeginInvokeOnMainThread(async () => await RegenerateHtmlAsync());
        }
        //else if (e.PropertyName == "EnablePinchGestures")
        //{
        //    MainThread.BeginInvokeOnMainThread(SetPinchOverlayFromViewModel);
        //}
    }

    /// <summary>
    /// Regenerates the HTML source for the WebView using current user preferences.
    /// Do not attempt to evaluate JS immediately — wait for Navigated (OnLyricsWebViewNavigated).
    /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously        
    private async Task RegenerateHtmlAsync()
    {
        try
        {
            if (_visualization?.VisualizationOptions != null)
            {
                _visualization.VisualizationOptions.CustomChordsOnly = UserViewModel?.ShowOnlyCustomChords ?? false;
            }

            _visualization = this.CreateVisualizationOptions();

            var song = new Song(_songEntity);
            string htmlDocument = _visualization!.LyricsHtml(song, _userViewModel.LyricsHtmlVersion, skipHeaders: true);

            var insertAt = htmlDocument.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (insertAt >= 0)
                htmlDocument = htmlDocument.Insert(insertAt, _autoScrollJs);
            else
                htmlDocument += _autoScrollJs;

            // set the HTML; wait for Navigated event to apply font-size via JS
            LyricsWebView.Source = new HtmlWebViewSource { Html = htmlDocument };
        }
        catch(Exception ex)
        {
            string msg = ex.Message;

        }
    }
#pragma warning restore CS1998 

    /// <summary>
    /// Poka¿ kontrolki i zresetuj timer ukrywania.
    /// </summary>
    private void ShowControls()
    {
        ControlsPanel.IsVisible = true;
        _hideControlsTimer.Stop();
        _hideControlsTimer.Start();
    }

    private void LyricsWebView_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrEmpty(e?.Url))
            return;

        // intercept our custom app:// scheme
        if (e.Url.StartsWith("app://", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            try
            {
                var uri = new Uri(e.Url);
                var command = uri.Host?.ToLowerInvariant();
                if (command == "showcontrols")
                {
                    MainThread.BeginInvokeOnMainThread(ShowControls);
                }
                else if (command == "hidecontrols")
                {
                    MainThread.BeginInvokeOnMainThread(() => { ControlsPanel.IsVisible = false; });
                }
                //else if (command == "showback")
                //{
                //    MainThread.BeginInvokeOnMainThread(() => { BackButton.IsVisible = true; ModifyTapAreaForBackButton(); } );
                //}
                //else if (command == "hideback")
                //{
                //    MainThread.BeginInvokeOnMainThread(() => { BackButton.IsVisible = false; ModifyTapAreaForBackButton(true); });
                //}
                else if (command == "pinchin" || command == "pinchout")
                {
                    // Only react when user allows pinch gestures
                    bool enabled = false;
                    try
                    {
                        var prop = _userViewModel?.GetType().GetProperty("EnablePinchGestures");
                        if (prop != null && prop.PropertyType == typeof(bool))
                            enabled = (bool)prop.GetValue(_userViewModel)!;
                    }
                    catch { /* ignore reflection problems */ }

                    if (enabled)
                    {
                        if (command == "pinchin")
                            MainThread.BeginInvokeOnMainThread(() => OnIncreaseFontClicked(this, EventArgs.Empty));
                        else
                            MainThread.BeginInvokeOnMainThread(() => OnDecreaseFontClicked(this, EventArgs.Empty));
                    }
                }
            }
            catch { /* ignore malformed */ }
        }
    }

    /// <summary>
    /// Nawigacja
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnLyricsWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        // Apply font size after navigation completes.
        await SetAbsoluteFontSize();
    }

    /// <summary>
    /// Ustaw wielkoœæ czcionki
    /// </summary>
    /// <returns></returns>
    private async Task SetAbsoluteFontSize()
    {
        try
        {
            var userVm = _userViewModel ?? ResolveUserViewModelFallback();
            if (userVm == null)
                return;

            var basePx = 17.0 + userVm.FontSizeAdjustment;

            try
            {
                await LyricsWebView.EvaluateJavaScriptAsync($"setBaseFontSize({basePx});");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetAbsoluteFontSize JS error: {ex.Message}");
            }
        }
        catch
        {
        }

    }

    /// <summary>
    /// Alternatywny odczyt userViewModel
    /// </summary>
    /// <returns></returns>
    private UserViewModel? ResolveUserViewModelFallback()
    {
        try
        {
            var mauiContext = Application.Current?.Handler?.MauiContext;
            var services = mauiContext?.Services;
            return services?.GetService(typeof(UserViewModel)) as UserViewModel;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Start auto-scroll at ~50 px/sec (adjust as needed).
    /// Before starting, query current scroll position so auto-scroll begins exactly where user left it.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnStartScrollClicked(object sender, EventArgs e)
    {
        await this.StartScrollClicked();
    }

    /// <summary>
    /// Start auto-scroll at ~50 px/sec (adjust as needed).
    /// Before starting, query current scroll position so auto-scroll begins exactly where user left it.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnStartScrollPanelClicked(object sender, EventArgs e)
    {
        await this.StartScrollClicked();
        ShowControls();
    }

    /// <summary>
    /// Start auto-scroll at user-defined speed from current position.
    /// </summary>
    /// <returns></returns>
    private async Task StartScrollClicked()
    {
        if (!string.IsNullOrEmpty(_deezerSummary?.DurationTxt))
        {
            _songEntity.SongDurationTxt = _deezerSummary?.DurationTxt;
        }

        int songDuration = _songEntity?.SongDuration ?? 3 * 60; 

        try
        {
            var posStr = await LyricsWebView.EvaluateJavaScriptAsync("window.getScrollPosition().toString();");
            var remainingLyricsInfo = await GetRemainingScrollInfoAsync(); 
            if (double.TryParse(posStr?.Trim('"'), out var pos))
            {
                if (pos == 0)
                {
                    if (remainingLyricsInfo != null)
                        _currentSongScrollSpeed = (int)(remainingLyricsInfo.RemainingPx / songDuration);
                }
                else
                {
                    var topLineInfo = await GetTopVisibleLineExampleAsync();
                    if(topLineInfo?.TotalLines > 0 && remainingLyricsInfo != null)
                    {
                        // estimate total document height based on current scrollTop and remainingPercent
                        var estimatedDocHeight = (topLineInfo.ScrollTop * 100) / (100 - remainingLyricsInfo.RemainingPercent);
                        var estimatedRemainingPx = estimatedDocHeight - (topLineInfo.ScrollTop + (estimatedDocHeight * (100 - remainingLyricsInfo.RemainingPercent) / 100));
                        _currentSongScrollSpeed = (int)(estimatedRemainingPx! / songDuration);
                    }
                }

                await LyricsWebView.EvaluateJavaScriptAsync($"startAutoScroll({_currentSongScrollSpeed}, {pos}, {_songEntity!.ScrollingDelay});");
            }
            else
            {
                _currentSongScrollSpeed = remainingLyricsInfo != null ? (int)(remainingLyricsInfo.RemainingPx / songDuration) : 30;
                await LyricsWebView.EvaluateJavaScriptAsync($"startAutoScroll({_currentSongScrollSpeed}, 0, {_songEntity!.ScrollingDelay});");
            }
            _userViewModel.ScrollingInProgress = true;

            

        }
        catch (Exception) { /* handle or log if needed */ }

    }


    private async void OnStopScrollPanelClicked(object sender, EventArgs e)
    {
        try
        {
            await this.StopScrollClicked();
            ShowControls();
        }
        catch (Exception) { /* handle or log if needed */ }
    }

    private async void OnStopScrollClicked(object sender, EventArgs e)
    {
        try
        {
            await this.StopScrollClicked();
        }
        catch (Exception) { /* handle or log if needed */ }
    }

    private async Task StopScrollClicked()
    {
        try
        {
            await LyricsWebView.EvaluateJavaScriptAsync("stopAutoScroll();");
            _userViewModel.ScrollingInProgress = false;
        }
        catch (Exception) { /* handle or log if needed */ }
    }


    /// <summary>
    /// Zwiêksz bazowy rozmiar czcionki HTML o 1px
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnIncreaseFontClicked(object sender, EventArgs e)
    {
        try
        {
            _userViewModel.FontSizeAdjustment += 1;
            var basePx = 17.0 + _userViewModel.FontSizeAdjustment;
            await LyricsWebView.EvaluateJavaScriptAsync($"setBaseFontSize({basePx});");
            // keep controls visible briefly
            ShowControls();
        }
        catch { /* ignore */ }
    }

    // Decrease base HTML font size by 1px
    private async void OnDecreaseFontClicked(object sender, EventArgs e)
    {
        try
        {
            _userViewModel.FontSizeAdjustment -= 1;
            var basePx = 17.0 + _userViewModel.FontSizeAdjustment;
            await LyricsWebView.EvaluateJavaScriptAsync($"setBaseFontSize({basePx});");
            ShowControls();
        }
        catch { /* ignore */ }
    }

    // Handler for the transparent top touch area
    private void OnTopTouched(object sender, EventArgs e)
    {
        // ignore this tap if it was caused by the BackButton click
        if (_suppressTopTouch)
        {
            _suppressTopTouch = false;
            return;
        }

        _ = ShowControlsAsync();
    }

    /// <summary>
    /// Poka¿ kontrolki z animacj¹
    /// </summary>
    /// <returns></returns>
    private async Task ShowControlsAsync()
    {
        try
        {
            ControlsPanel.IsVisible = true;
            ControlsPanel.Opacity = 0;
            await ControlsPanel.FadeTo(1, 200);
            _hideControlsTimer.Stop();
            _hideControlsTimer.Start();
        }
        catch { /* ignore animation errors */ }
    }

    private async Task ShowDeezerControlsAsync()
    {
        try
        {
            DeezerStatus.IsVisible = true;
            DeezerStatus.Opacity = 0;
            await DeezerStatus.FadeTo(1, 200);
            _hideDeezerStatusTimer.Stop();
            _hideDeezerStatusTimer.Start();
        }
        catch { /* ignore animation errors */ }
    }


    private async void OnResetFontClicked(object sender, EventArgs e)
    {
        _userViewModel.FontSizeAdjustment = 0;
        await this.SetAbsoluteFontSize();

    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        try
        {
            var mauiContext = Application.Current?.Handler?.MauiContext;
            var services = mauiContext?.Services;
            if (services == null)
                return;

            // resolve SettingsPage from DI (registered in MauiProgram)
            var page = services.GetService(typeof(SettingsPage)) as Page;
            if (page == null)
                return;

            await Navigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            await ex.SaveExceptionToFileAsync(" settings_page_navigation", eventApi: _eventApi);
        }

    }

    private void OnScrollToggleClicked(object sender, EventArgs e)
    {
        if (_userViewModel.ScrollingInProgress == true)
            this.OnStopScrollClicked(sender, e);
        else
            this.OnStartScrollClicked(sender, e);

        OnPropertyChanged(nameof(UserViewModel));
    }

    private void OnScrollTogglePanelClicked(object sender, EventArgs e)
    {
        if (_userViewModel.ScrollingInProgress == true)
            this.OnStopScrollPanelClicked(sender, e);
        else
            this.OnStartScrollPanelClicked(sender, e);

        OnPropertyChanged(nameof(UserViewModel));
    }

    /// <summary>
    /// Wróæ do poprzedniej strony
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnBackClicked(object sender, EventArgs e)
    {
        try
        {
            // Prevent the TopTouchArea tap handler from reacting to the same touch.
            _suppressTopTouch = true;

            // perform navigation
            if (Navigation.NavigationStack.Count > 1)
                await Navigation.PopAsync();
            else
                await Navigation.PopModalAsync();
        }
        catch { /* ignore navigation errors */ }
        finally
        {
            // restore listening after a short delay so normal taps work again
            _ = Task.Run(async () =>
            {
                await Task.Delay(350);
                _suppressTopTouch = false;
            });
        }
    }

    /// <summary>
    /// Odtwórz muzê
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnPlayMusicClicked(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_songEntity.SpotifyLink))
            {
                var id = SpotifyExtensions.ExtractSpotifyTrackId(_songEntity.SpotifyLink);

                var uri = DeviceInfo.Platform == DevicePlatform.Android ? $"spotify:track:{id}" : $"{_songEntity.SpotifyLink}";

                await Launcher.OpenAsync(uri);
            }
        }
        catch { }
    }

    /// <summary>
    /// Stwórz instancjê SongVisualization z aktualnymi opcjami (tryb ciemny, pokazywanie tylko custom chordów itp).
    /// </summary>
    /// <returns></returns>
    private SongVisualization CreateVisualizationOptions()
    {
        var fontsPath = _visualization?.CssFontsPath;

        this.AdjustDarkModeCss(_visualizationCssOptions);

        var visualization = new SongVisualization()
        {
            IncludeFontsAsBase64 = true,
            VisualizationOptions = new SongVisualizationOptions(_visualizationCssOptions)
            {
                CustomChordsOnly = UserViewModel.ShowOnlyCustomChords,
                SkipLyricChords = UserViewModel.SkipLyricChords,
                SkipTabulatures = UserViewModel.SkipTabulatures,
                MoveChordsToLyricsLine = UserViewModel.MoveChordsToLyricsLine
            }
        };

        if(_userViewModel.LyricsDarkMode == true)
        {
            var lyricsDarkText = Application.Current?.Resources["LyricsDarkText"] as Color;
            visualization.VisualizationOptions.ChordDiagramColor = lyricsDarkText?.ToHex();
        }

        if(fontsPath != null)
            visualization.CssFontsPath = fontsPath;

        return visualization;
    }

    private async Task<TopLineResult?> GetTopVisibleLineExampleAsync()
    {
        try
        {
            var json = await LyricsWebView.EvaluateJavaScriptAsync("getTopLine();");
            if (string.IsNullOrEmpty(json))
                return null;

            json = json.Trim('"'); // EvaluateJavaScriptAsync czêsto zwraca wynik w cudzys³owach
            json = json.Replace("\\\"", "\"");
            json = json.Replace("\\\\", "\\");

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var doc = System.Text.Json.JsonSerializer.Deserialize<TopLineResult>(json, options);

            return doc;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"getTopLine JS error: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Call the injected JS and return remaining scroll info.
    /// </summary>
    private async Task<RemainingScrollResult?> GetRemainingScrollInfoAsync()
    {
        try
        {
            // Ensure function exists; EvaluateJavaScriptAsync often returns quoted string, possibly escaped
            var raw = await LyricsWebView.EvaluateJavaScriptAsync("getRemainingScrollInfo();");
            if (string.IsNullOrEmpty(raw))
                return null;

            // Normalize the returned string: Remove surrounding quotes and unescape if necessary
            // Typical return form from EvaluateJavaScriptAsync: "\"{...}\""
            string json = raw.Trim();
            if (json.Length >= 2 && json[0] == '\"' && json[json.Length - 1] == '\"')
            {
                json = json.Substring(1, json.Length - 2);
                json = json.Replace("\\\"", "\"");
                json = json.Replace("\\\\", "\\");
            }
            else 
            {
                json = json.Replace("\\\"", "\"");
                json = json.Replace("\\\\", "\\");
            }

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = System.Text.Json.JsonSerializer.Deserialize<RemainingScrollResult>(json, options);
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetRemainingScrollInfoAsync error: {ex.Message}");
            return null;
        }
    }

    private class RemainingScrollResult
    {
        public int RemainingPx { get; set; }
        public int RemainingPercent { get; set; }   // 0..100
        public int RemainingLines { get; set; }     // -1 = unknown/not-applicable
        public int DocHeight { get; set; }
        public int Viewport { get; set; }
        public int ScrollTop { get; set; }
        public string? Error { get; set; }
    }

    private class TopLineResult
    {
        public int Index { get; set; }
        public string? Text { get; set; }
        public int? LineHeight { get; set; }
        public int? Top { get; set; }
        public int? ScrollTop { get; set; }

        public int? TotalLines { get; set; }
    }

    /// <summary>
    /// Song edit
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnSongEditClicked(object sender, EventArgs e)
    {
        try
        {
            var editPage = new SongWebEditPage(_songEntity, _userViewModel,  _settings, _songRepository);
            await Navigation.PushAsync(editPage);
        }
        catch (Exception ex)
        {
            await ex.SaveExceptionToFileAsync(" song_edit_navigation", eventApi: _eventApi);
        }
    }

    private async void OnPlayDeezerClicked(object sender, EventArgs e)
    {
        try
        {
            if (_isDeezerPlaying && _deezerPlayer != null)
            {
                // Zatrzymaj odtwarzanie
                _deezerPlayer.Stop();
                _deezerPlayer.Dispose();
                _deezerPlayer = null;
                _isDeezerPlaying = false;
                //CleanupDeezerTempFile();
                return;
            }

            if (!string.IsNullOrEmpty(_userViewModel?.DeezerArl))
            {
                await ShowDeezerControlsAsync();

                if (_deezerSummary == null)
                    _deezerSummary = await DeezerSummary.CreateForSongEntity(_songEntity, _userViewModel);

                if (_deezerSummary?.DeezerTrack != null)
                {
                    var deezerService = new DeezerService(_userViewModel.DeezerArl);

                    var trackBytes = await deezerService.DeezerDownloadBytes(_deezerSummary.DeezerTrack.Url);
                    _userViewModel.DeezerStatusInfo = $"Pobrano {trackBytes?.Length ?? 0} B.";

                    if (trackBytes != null && trackBytes.Length > 0)
                    {
                        var memoryStream = new MemoryStream(trackBytes);

                        _deezerPlayer = _audioManager.CreatePlayer(memoryStream);

                        // Opcjonalnie: obs³u¿ zdarzenie zakoñczenia
                        _deezerPlayer.PlaybackEnded += (s, args) =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                _isDeezerPlaying = false;
                                _deezerPlayer?.Dispose();
                                _deezerPlayer = null;
                                //CleanupDeezerTempFile();
                                _userViewModel.DeezerStatusInfo = $"Odtwarzanie zakoñczone";
                            });
                        };

                        _userViewModel.DeezerStatusInfo = $"Odtwarzam";
                        _deezerPlayer.Play();
                        _isDeezerPlaying = true;
                    }
                    else
                    {
                        _userViewModel.DeezerStatusInfo = "Nie uda³o siê pobraæ utworu z Deezer";
                    }
                }
                else
                {
                    _userViewModel.DeezerStatusInfo = "Nie znaleziono utworu w Deezer";
                }
            }
            else
            {
                _userViewModel!.DeezerStatusInfo = "Brak tokenu Deezer ARL w ustawieniach";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("B³¹d", $"Wyst¹pi³ problem: {ex.Message}", "OK");
            await ex.SaveExceptionToFileAsync("deezer_playback", eventApi: _eventApi);
        }
    }

    private void CleanupDeezerTempFile_OLD()
    {
        try
        {
            var tempFile = Path.Combine(FileSystem.CacheDirectory, $"deezer_temp_{_songEntity.Id}.mp3");
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to cleanup temp file: {ex.Message}");
        }
    }
}