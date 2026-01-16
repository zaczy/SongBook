using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using Zaczy.SongBook.Data;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using Zaczy.SongBook.MAUI.ViewModels;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zaczy.SongBook.MAUI.Pages
{
    public partial class SongDetailsPage : ContentPage
    {
        private readonly Timer _hideControlsTimer;
        private readonly UserViewModel _userViewModel;

        // store injected song entity and helpers so we can regenerate HTML later
        private readonly SongEntity _songEntity;
        private readonly SongVisualization _visualization;
        private readonly string _autoScrollJs;

        private bool _isSubscribed;
        private VisualizationCssOptions _visualizationCssOptions;

        // remember previous KeepScreenOn value so we restore it on exit
        private bool _previousKeepScreenOn;

        // Expose the UserViewModel as a public property so XAML can bind to it via x:Reference
        public UserViewModel UserViewModel => _userViewModel;

        public SongDetailsPage(SongEntity songEntity, UserViewModel userViewModel)
        {
            _userViewModel = userViewModel;
            _songEntity = songEntity ?? new SongEntity();
            _visualizationCssOptions = new VisualizationCssOptions();
            _visualization = new SongVisualization() {  IncludeFontsAsBase64 = true, VisualizationCssOptions = _visualizationCssOptions };

            InitializeComponent();

            _visualizationCssOptions.Add(".lyrics-line", "font-family", "PoltawskiVariable");
            _visualizationCssOptions.Add(".lyrics-line", "font-size", "2em");

            // prepare auto-scroll JS once
            _autoScrollJs = SongPreviewJavascript.JavascriptTxt();

            NavigationPage.SetHasNavigationBar(this, false);
            NavigationPage.SetHasBackButton(this, false);

            BindingContext = _songEntity;

            _hideControlsTimer = new Timer(3000) { AutoReset = false };
            _hideControlsTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() => ControlsPanel.IsVisible = false);
            };

            // ensure we react when the WebView finishes loading
            LyricsWebView.Navigating += LyricsWebView_Navigating;
            LyricsWebView.Navigated += OnLyricsWebViewNavigated;

            // initialize fonts and then generate initial HTML
            _ = InitializeAsync();
        }

        // Initialize fonts (copy packaged assets to a physical path WebView can access) then render HTML.
        private async Task InitializeAsync()
        {
            try
            {
                await EnsureFontsAvailableAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnsureFontsAvailableAsync failed: {ex.Message}");
            }

            // generate initial HTML after fonts are available
            await RegenerateHtmlAsync();
        }

        // Copy packaged font assets to AppData (if present) and register paths in SongVisualization.CssFontsPath.
        // SongVisualization will embed base64 if the file exists.
        private async Task EnsureFontsAvailableAsync()
        {
            // List of relative asset paths inside the app package (adjust to actual paths in your project)
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
                    // destination path in AppData (preserve subfolders)
                    var destPath = Path.Combine(appData, assetRelative.Replace('/', Path.DirectorySeparatorChar));
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir!);

                    // if missing, try to copy from app package (Resources/Raw or MauiAsset)
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
                            // asset not found in package — skip this font
                            System.Diagnostics.Debug.WriteLine($"Font asset not found in package: {assetRelative}");
                            continue;
                        }
                    }

                    // register for visualization — SongVisualization will embed base64 when it finds the file
                    if (_visualization.CssFontsPath == null)
                        _visualization.CssFontsPath = new Dictionary<string, string>();

                    // Use the physical file path; SongVisualization.GetFontBase64 will read it and embed
                    _visualization.CssFontsPath[fontKey] = destPath;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to prepare font {assetRelative}: {ex.Message}");
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // subscribe to user prefs changes when page is visible
            if (!_isSubscribed && _userViewModel != null)
            {
                _userViewModel.PropertyChanged += UserViewModel_PropertyChanged;
                _isSubscribed = true;
            }

            // keep screen on while this page is visible (Android)
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

            // always regenerate on appearing to catch changes made while page was not visible
            _ = RegenerateHtmlAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // restore previous KeepScreenOn value
            try
            {
                if (OperatingSystem.IsAndroid())
                    DeviceDisplay.KeepScreenOn = _previousKeepScreenOn;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore KeepScreenOn: {ex.Message}");
            }

            // unsubscribe WebView events to avoid duplicate handlers and leaks
            LyricsWebView.Navigated -= OnLyricsWebViewNavigated;
            LyricsWebView.Navigating -= LyricsWebView_Navigating;

            // unsubscribe to avoid leaks and to stop receiving events while page is not visible
            if (_isSubscribed && _userViewModel != null)
            {
                _userViewModel.PropertyChanged -= UserViewModel_PropertyChanged;
                _isSubscribed = false;
            }
        }

        private void UserViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e == null) return;

            // regenerate HTML when relevant user prefs change
            if (e.PropertyName == nameof(UserViewModel.LyricsHtmlVersion)
                || e.PropertyName == nameof(UserViewModel.FontSizeAdjustment))
            {
                MainThread.BeginInvokeOnMainThread(async () => await RegenerateHtmlAsync());
            }
        }

        /// <summary>
        /// Regenerates the HTML source for the WebView using current user preferences.
        /// Do not attempt to evaluate JS immediately — wait for Navigated (OnLyricsWebViewNavigated).
        /// </summary>
        private async Task RegenerateHtmlAsync()
        {
            try
            {
                var song = new Song(_songEntity);
                string htmlDocument = _visualization.LyricsHtml(song, _userViewModel.LyricsHtmlVersion, skipHeaders: true);

                var insertAt = htmlDocument.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                if (insertAt >= 0)
                    htmlDocument = htmlDocument.Insert(insertAt, _autoScrollJs);
                else
                    htmlDocument += _autoScrollJs;

                // set the HTML; wait for Navigated event to apply font-size via JS
                LyricsWebView.Source = new HtmlWebViewSource { Html = htmlDocument };
            }
            catch
            {
                // ignore for robustness; you may add logging here
            }
        }

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
                        MainThread.BeginInvokeOnMainThread(() => ControlsPanel.IsVisible = false);
                    }
                }
                catch { /* ignore malformed */ }
            }
        }

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

                // Evaluate JS and observe potential errors if JS function is missing.
                try
                {
                    await LyricsWebView.EvaluateJavaScriptAsync($"setBaseFontSize({basePx});");
                }
                catch (Exception ex)
                {
                    // swallow but optionally log; JS may not be ready or function missing
                    System.Diagnostics.Debug.WriteLine($"SetAbsoluteFontSize JS error: {ex.Message}");
                }
            }
            catch
            {
            }

        }

        private UserViewModel? ResolveUserViewModelFallback()
        {
            try
            {
                // safe fallback: get service provider from MAUI context
                var mauiContext = Application.Current?.Handler?.MauiContext;
                var services = mauiContext?.Services;
                return services?.GetService(typeof(UserViewModel)) as UserViewModel;
            }
            catch
            {
                return null;
            }
        }

        // Start auto-scroll at ~50 px/sec (adjust as needed).
        // Before starting, query current scroll position so auto-scroll begins exactly where user left it.
        private async void OnStartScrollClicked(object sender, EventArgs e)
        {
            try
            {
                var posStr = await LyricsWebView.EvaluateJavaScriptAsync("window.getScrollPosition().toString();");
                if (double.TryParse(posStr?.Trim('"'), out var pos))
                {
                    await LyricsWebView.EvaluateJavaScriptAsync($"startAutoScroll(50, {pos});");
                }
                else
                {
                    await LyricsWebView.EvaluateJavaScriptAsync("startAutoScroll(50);");
                }
                _userViewModel.ScrollingInProgress = true;
                ShowControls();
            }
            catch (Exception) { /* handle or log if needed */ }
        }

        private async void OnStopScrollClicked(object sender, EventArgs e)
        {
            try
            {
                await LyricsWebView.EvaluateJavaScriptAsync("stopAutoScroll();");
                _userViewModel.ScrollingInProgress = false;
                ShowControls();
            }
            catch (Exception) { /* handle or log if needed */ }
        }

        // Increase base HTML font size by ~2px
        private async void OnIncreaseFontClicked(object sender, EventArgs e)
        {
            try
            {
                _userViewModel.FontSizeAdjustment += 2;
                await LyricsWebView.EvaluateJavaScriptAsync("changeBaseFontSize(2);");
                // keep controls visible briefly
                ShowControls();
            }
            catch { /* ignore */ }
        }

        // Decrease base HTML font size by ~2px
        private async void OnDecreaseFontClicked(object sender, EventArgs e)
        {
            try
            {
                _userViewModel.FontSizeAdjustment -= 2;
                await LyricsWebView.EvaluateJavaScriptAsync("changeBaseFontSize(-2);");
                ShowControls();
            }
            catch { /* ignore */ }
        }

        // Handler for the transparent top touch area
        private void OnTopTouched(object sender, EventArgs e)
        {
            _ = ShowControlsAsync();
        }

        // Async show/hide helpers (used previously)
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
            catch (Exception)
            {
                // ignore/navigation failure
            }

            // Alternatywnie
            // await Navigation.PushAsync(serviceProvider.GetRequiredService<SettingsPage>());
        }

        private void OnScrollToggleClicked(object sender, EventArgs e)
        {
            if (_userViewModel.ScrollingInProgress == true)
                this.OnStopScrollClicked(sender, e);
            else
                this.OnStartScrollClicked(sender, e);

            OnPropertyChanged(nameof(UserViewModel));
        }
    }
}