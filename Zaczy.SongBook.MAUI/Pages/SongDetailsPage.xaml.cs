using Microsoft.Maui.Controls;
using Zaczy.SongBook.Data;
using System;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Pages
{
    public partial class SongDetailsPage : ContentPage
    {
        private readonly Timer _hideControlsTimer;
        private readonly UserViewModel _userViewModel;

        public SongDetailsPage(SongEntity songEntity, UserViewModel userViewModel)
        {
            InitializeComponent();

            _userViewModel = userViewModel;

            NavigationPage.SetHasNavigationBar(this, false);
            NavigationPage.SetHasBackButton(this, false);

            BindingContext = songEntity ?? new SongEntity();

            _hideControlsTimer = new Timer(3000) { AutoReset = false };
            _hideControlsTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() => ControlsPanel.IsVisible = false);
            };

            var visualization = new SongVisualization();

            if (songEntity != null)
            {
                var song = new Song(songEntity);
                string htmlDocument = visualization.LyricsHtml(song, Enums.LyricsHtmlVersion.RelativeHtml, skipHeaders: true);

                var autoScrollJs = @"
<script>
(function(){
  var rafId = null;
  var pos = 0;
  var speed = 50;
  var last = 0;

  // keep pos updated on manual scroll
  window.addEventListener('scroll', function(){
    pos = window.scrollY || window.pageYOffset || 0;
  }, { passive: true });

  // show controls when user touches near top (mobile)
  function maybeShowControls(evt){
    var y = (evt.touches && evt.touches[0] && evt.touches[0].clientY) || (evt.clientY || 0);
    if(y <= 120){ // threshold in px
      // navigate to custom scheme to notify native
      window.location.href = 'app://showControls';
    }
  }
  //window.addEventListener('touchstart', maybeShowControls, {passive:true});
  //window.addEventListener('mousedown', maybeShowControls, {passive:true}); // desktop/testing

  // base font size helpers
  window.getBaseFontSize = function(){
    var fs = window.getComputedStyle(document.documentElement).fontSize;
    return parseFloat(fs) || 17;
  };
  window.setBaseFontSize = function(px){
    px = Number(px) || 17;
    document.documentElement.style.fontSize = px + 'px';
    return px;
  };
  window.changeBaseFontSize = function(delta){
    var cur = window.getBaseFontSize();
    var next = Math.max(8, Math.min(72, cur + Number(delta)));
    window.setBaseFontSize(next);
    return next;
  };

  window.getScrollPosition = function(){
    return window.scrollY || window.pageYOffset || 0;
  };

  window.setScrollPosition = function(y){
    pos = Number(y) || 0;
    window.scrollTo(0, pos);
  };

  // startAutoScroll(pxPerSec, optionalStartY)
  window.startAutoScroll = function(pxPerSec, startY){
    speed = Number(pxPerSec) || 50;
    if (typeof startY === 'number') pos = startY;
    else pos = window.scrollY || window.pageYOffset || pos || 0;

    if(rafId) return;
    last = performance.now();

    // ensure initial position applied
    window.scrollTo(0, pos);

    function step(now){
      var dt = (now - last)/1000;
      last = now;
      pos += speed * dt;
      window.scrollTo(0, pos);
      if(window.innerHeight + pos >= document.body.scrollHeight - 1){
        window.stopAutoScroll();
        return;
      }
      rafId = requestAnimationFrame(step);
    }
    rafId = requestAnimationFrame(step);
  };

  window.stopAutoScroll = function(){
    if(rafId){ cancelAnimationFrame(rafId); rafId = null; }
  };

})();
</script>
";
                var insertAt = htmlDocument.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                if (insertAt >= 0)
                    htmlDocument = htmlDocument.Insert(insertAt, autoScrollJs);
                else
                    htmlDocument += autoScrollJs;

                LyricsWebView.Source = new HtmlWebViewSource { Html = htmlDocument };

                // intercept custom scheme navigations from JS
                LyricsWebView.Navigating += LyricsWebView_Navigating;
                LyricsWebView.Navigated += OnLyricsWebViewNavigated;
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

        // If you create pages through DI you can instead use:
        // public SongDetailsPage(SongEntity songEntity, UserViewModel userViewModel) { _userViewModel = userViewModel; ... }
        // and then you can remove the service-resolve fallback in the handler below.

        private async void OnLyricsWebViewNavigated(object? sender, WebNavigatedEventArgs e)
        {
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

                await LyricsWebView.EvaluateJavaScriptAsync($"setBaseFontSize({basePx});");
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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            LyricsWebView.Navigated -= OnLyricsWebViewNavigated;
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
                ShowControls();
            }
            catch (Exception) { /* handle or log if needed */ }
        }

        private async void OnStopScrollClicked(object sender, EventArgs e)
        {
            try
            {
                await LyricsWebView.EvaluateJavaScriptAsync("stopAutoScroll();");
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
    }
}