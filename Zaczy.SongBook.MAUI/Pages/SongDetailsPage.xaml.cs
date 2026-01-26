using MauiIcons.Core;
using MauiIcons.FontAwesome;
using MauiIcons.Fluent;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.MAUI.ViewModels;
using Timer = System.Timers.Timer;

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

        private bool _suppressTopTouch;

        // add fields near other private fields
        //private double _pinchScale;

        public SongDetailsPage(SongEntity songEntity, UserViewModel userViewModel)
        {
            _userViewModel = userViewModel;
            _songEntity = songEntity ?? new SongEntity();
            _songEntity.ScrollingDelay = 10;

            _visualizationCssOptions = new VisualizationCssOptions();
            _visualization = new SongVisualization() {  IncludeFontsAsBase64 = true, VisualizationCssOptions = _visualizationCssOptions };

            _ = new MauiIcon();
            _ = new MauiIcon() { Icon = MauiIcons.Fluent.FluentIcons.Search20, IconColor = Colors.Green };


            InitializeComponent();

            _visualizationCssOptions.Add(".lyrics-line", "font-family", "PoltawskiVariable");
            //_.visualizationCssOptions.Add(".lyrics-line", "font-size", "2em");
            _visualizationCssOptions.Add("pre", "font-weight", "600");

            if(DeviceInfo.Idiom == DeviceIdiom.Tablet)
            {
                _visualizationCssOptions.Add("body", "padding-top", "30px");
            }


            // prepare auto-scroll JS once
            _autoScrollJs = SongPreviewJavascript.JavascriptTxt();

            // append scroll-direction detector JS to the auto-scroll JS so it's injected into the page
            var detectScrollJs = @"
<script>
(function(){
  var lastY = window.pageYOffset || document.documentElement.scrollTop || 0;
  var threshold = 10;
  var debounceMs = 120;
  var debounceTimer = null;
  window.addEventListener('scroll', function(){
    var y = window.pageYOffset || document.documentElement.scrollTop || 0;
    var dy = y - lastY;
    lastY = y;
    if (debounceTimer) return;
    debounceTimer = setTimeout(function(){ debounceTimer = null; }, debounceMs);
    if (dy < -threshold) {
      // user scrolled up
      addToLog('app://showBack');

      try { window.location.href = 'app://showBack'; } catch(e){}
    } else if (dy > threshold) {
      // user scrolled down
      addToLog('app://hideBack');
      try { window.location.href = 'app://hideBack'; } catch(e){}
    }
  }, { passive: true });

  var pinchActive = false;
  var startDist = 0;
  var lastScale = 1;

  function dist(t0, t1){
    var dx = t0.pageX - t1.pageX;
    var dy = t0.pageY - t1.pageY;
    return Math.sqrt(dx*dx + dy*dy);
  }

  window.addEventListener('touchstart', function(e){
      addToLog('touchstart ' + (e.touches ? e.touches.length : 'brak e.touches'));
    if(e.touches && e.touches.length === 2){
      pinchActive = true;
      startDist = dist(e.touches[0], e.touches[1]) || 1;
      lastScale = 1;
    }
  }, { passive: true });

  window.addEventListener('touchmove', function(e){
      addToLog('touchmove ' + (e.touches ? e.touches.length : 'brak e.touches'));
    if(pinchActive && e.touches && e.touches.length === 2){
      var d = dist(e.touches[0], e.touches[1]);
      lastScale = d / startDist;
    }
  }, { passive: true });

  window.addEventListener('touchend', function(e){
    if(!pinchActive) return;
    addToLog('touchend');
    pinchActive = false;
    // thresholds chosen to avoid accidental triggers
    if(lastScale >= 1.15){
      try { window.location.href = 'app://pinchIn'; } catch(e){}
    } else if(lastScale <= 0.85){
      try { window.location.href = 'app://pinchOut'; } catch(e){}
    }
    lastScale = 1;
  }, { passive: true });
})();
</script>
";

            // merge detection snippets
            _autoScrollJs += detectScrollJs;

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

        /// <summary>
        /// Initialize fonts (copy packaged assets to a physical path WebView can access) then render HTML. 
        /// </summary>
        /// <returns></returns>
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

            await RegenerateHtmlAsync();
        }

        /// <summary>
        // Copy packaged font assets to AppData (if present) and register paths in SongVisualization.CssFontsPath.
        // SongVisualization will embed base64 if the file exists.
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
            catch(Exception ex)
            {
                string msg = ex.Message;

                // ignore for robustness; you may add logging here
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
                        MainThread.BeginInvokeOnMainThread(() => { ControlsPanel.IsVisible = false; ModifyTapAreaForBackButton(true); });
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

        private void ModifyTapAreaForBackButton(bool reset=false)
        {
            //var m = TopTouchArea.Margin;
            //TopTouchArea.Margin = new Thickness(reset ? 0 : 35, m.Top, m.Right, m.Bottom);
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
        // Start auto-scroll at ~50 px/sec (adjust as needed).
        // Before starting, query current scroll position so auto-scroll begins exactly where user left it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnStartScrollClicked(object sender, EventArgs e)
        {
            await this.StartScrollClicked();
        }

        /// <summary>
        // Start auto-scroll at ~50 px/sec (adjust as needed).
        // Before starting, query current scroll position so auto-scroll begins exactly where user left it.
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
            try
            {
                var posStr = await LyricsWebView.EvaluateJavaScriptAsync("window.getScrollPosition().toString();");
                if (double.TryParse(posStr?.Trim('"'), out var pos))
                {
                    await LyricsWebView.EvaluateJavaScriptAsync($"startAutoScroll({UserViewModel?.AutoScrollSpeed ?? 30}, {pos}, {_songEntity.ScrollingDelay});");
                }
                else
                {
                    await LyricsWebView.EvaluateJavaScriptAsync($"startAutoScroll({UserViewModel?.AutoScrollSpeed ?? 30}, 0, {_songEntity.ScrollingDelay});");
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
                _userViewModel.FontSizeAdjustment += 2;
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
                _userViewModel.FontSizeAdjustment -= 2;
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

        // new back handler
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

    }
}