using Maui.ColorPicker;
using Microsoft.Maui.Graphics.Text;
using System.Globalization;
using Zaczy.Songbook.MAUI.Helpers;
using Zaczy.SongBook;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.Db;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.SongBook.MAUI.Pages;

public partial class LayoutEditorPage : ContentPage
{
    private readonly UserViewModel _userViewModel;
    private readonly SongRepositoryLite _songRepository;

    private SongEntity? _previewSong;
    private SongVisualization _visualization;
    private VisualizationCssOptions _cssOptions;
    private bool _isLoading = true;
    private bool _isDuringControlsSync = false;

    private static readonly Random _rng = new();

    private static readonly string[] AvailableFonts = FontsHelper.FontsAvailable();

    private bool _textColorExpanded;
    private bool _chordColorExpanded;

    public LayoutEditorPage(UserViewModel userViewModel, SongRepositoryLite songRepository)
    {
        InitializeComponent();

        _userViewModel = userViewModel;
        _songRepository = songRepository;

        _cssOptions = new VisualizationCssOptions();
        _visualization = new SongVisualization { IncludeFontsAsBase64 = true };

        InitializeFontList();
    }

    protected override async void OnAppearing()
    {

        base.OnAppearing();

        await FontsHelper.EnsureFontsAvailableAsync(_visualization);
        await LoadRandomSongAsync();
        LoadCurrentSettingsIntoUi();

        _isLoading = false;
        await UpdatePreviewAsync();
    }

    private void InitializeFontList()
    {
        FontFamilyPicker.ItemsSource = AvailableFonts;
        FontFamilyPicker.SelectedIndex = 0;

        ChordFontFamilyPicker.ItemsSource = AvailableFonts;
        ChordFontFamilyPicker.SelectedIndex = FontFamilyPicker.SelectedIndex;
    }

    /// <summary>
    /// Za³aduj obecne wartoœci z UserViewModel do UI.
    /// </summary>
    private void LoadCurrentSettingsIntoUi()
    {
        VisualizationCssOptions visualizationCssOptions = new VisualizationCssOptions();
        LyricsVisualisationHelper.AdjustDarkModeCss(_userViewModel, visualizationCssOptions);

        var bodyColor = _userViewModel?.CustomLyricsCss?.TextColor ?? visualizationCssOptions.CssValue("body", "color");
        var chordsColor = _userViewModel?.CustomLyricsCss?.ChordColor ?? visualizationCssOptions.CssValue(".chords", "color");

        FontSizeSlider.Value = 17 + _userViewModel.FontSizeAdjustment;

        TextColorPicker.PickedColor = Color.FromArgb(bodyColor);
        TextColorEntry.Text = bodyColor;
        
        ChordColorEntry.Text = chordsColor;
        ChordColorPicker.PickedColor = Color.FromArgb(chordsColor);

        ChordFontSizeSlider.Value = 0;

        var bgColor = _userViewModel?.CustomLyricsCss?.BackgroundColor ?? visualizationCssOptions.CssValue("body", "background-color");
        BackgroundColorPicker.PickedColor = Color.FromArgb(bgColor);
        BackgroundColorEntry.Text = bgColor;

        SkipTabsCheck.IsChecked = _userViewModel?.SkipTabulatures ?? false;
        MoveChordsToLyricsLine.IsChecked = _userViewModel?.MoveChordsToLyricsLine ?? false;
        CustomChordsOnlyCheck.IsChecked = _userViewModel?.ShowOnlyCustomChords ?? false;

       if(_userViewModel?.CustomLyricsCss?.FontFamily != null)
        {
            var fontIndex = Array.IndexOf(AvailableFonts, _userViewModel.CustomLyricsCss.FontFamily);
            if (fontIndex >= 0)
                FontFamilyPicker.SelectedIndex = fontIndex;
        }
        if(_userViewModel?.CustomLyricsCss?.ChordFontFamily != null)
        {
            var chordFontIndex = Array.IndexOf(AvailableFonts, _userViewModel.CustomLyricsCss.ChordFontFamily);
            if (chordFontIndex >= 0)
                ChordFontFamilyPicker.SelectedIndex = chordFontIndex;
        }
    }

    private async Task LoadRandomSongAsync()
    {
        var songs = await _songRepository.GetAllAsync();
        if (songs == null || songs.Count == 0)
        {
            PreviewSongLabel.Text = "Brak piosenek w bazie";
            _previewSong = null;
            return;
        }

        _previewSong = songs[_rng.Next(songs.Count)];
        PreviewSongLabel.Text = $"Podgl¹d: {_previewSong.Title} — {_previewSong.Artist}";
    }

    private readonly SemaphoreSlim _previewRenderLock = new(1, 1);
    private int _previewRequestVersion;

    private SongEntity? _loadedPreviewSong;
    private string? _loadedPreviewStructure;
    private string? _previewStyleId;

    /// <summary>
    /// AKtualizuj
    /// </summary>
    /// <returns></returns>
    private async Task UpdatePreviewAsync()
    {
        if (_isLoading || _isDuringControlsSync || _previewSong == null)
            return;

        var requestVersion = ++_previewRequestVersion;

        // Coalesce rapid slider and picker events.
        await Task.Delay(40);

        if (requestVersion != _previewRequestVersion)
            return;

        SetPreviewLoading(true);

        await _previewRenderLock.WaitAsync();

        try
        {
            if (requestVersion != _previewRequestVersion ||
                _isLoading || _isDuringControlsSync || _previewSong == null)
                return;

            // All control and view-model access stays on the UI thread.
            var previewSong = _previewSong;
            var customLyricsCss = LyricsCssFromControls();
            var editableCss = CreatePreviewCss(customLyricsCss);

            var skipTabulatures = SkipTabsCheck.IsChecked;
            var moveChords = MoveChordsToLyricsLine.IsChecked == true;
            var customChordsOnly = CustomChordsOnlyCheck.IsChecked;
            var instrument = _userViewModel.ChordsInstrument;
            var htmlVersion = _userViewModel.LyricsHtmlVersion;

            // Only changes that affect document structure require rebuilding.
            var structure = System.Text.Json.JsonSerializer.Serialize(new
            {
                SkipTabulatures = skipTabulatures,
                MoveChordsToLyricsLine = moveChords,
                CustomChordsOnly = customChordsOnly,
                Instrument = instrument,
                HtmlVersion = htmlVersion,
                DarkMode = _userViewModel.LyricsDarkMode
            });

            var canUpdateCss =
                _previewStyleId != null &&
                ReferenceEquals(_loadedPreviewSong, previewSong) &&
                _loadedPreviewStructure == structure;

            if (canUpdateCss &&
                await TryUpdatePreviewCssAsync(_previewStyleId!, editableCss))
            {
                return;
            }

            // A new request may have arrived while JavaScript was executing.
            if (requestVersion != _previewRequestVersion)
                return;

            var cssOptions = new VisualizationCssOptions();
            var visualization = LyricsVisualisationHelper.CreateVisualizationOptions(
                _visualization, cssOptions, _userViewModel);

            visualization.CssFontsPath =
                new Dictionary<string, string>(_visualization.CssFontsPath);

            visualization.VisualizationOptions = new SongVisualizationOptions(cssOptions)
            {
                SkipTabulatures = skipTabulatures,
                MoveChordsToLyricsLine = moveChords,
                CustomChordsOnly = customChordsOnly,
                Instrument = instrument,
                ChordDiagramColor = customLyricsCss.TextColor
            };

            // Do not ApplyCustomCss here: editable rules belong to one style element.
            var song = new Song(previewSong);
            var styleId = $"layout-editor-style-{Guid.NewGuid():N}";

            var html = await Task.Run(() =>
            {
                var document = visualization.LyricsHtml(song, htmlVersion, true);

                if (string.IsNullOrWhiteSpace(document))
                    document = "<html><head></head><body></body></html>";

                return document.Replace(
                    "</head>",
                    $"<style id=\"{styleId}\">{editableCss}</style></head>",
                    StringComparison.OrdinalIgnoreCase);
            });

            if (requestVersion != _previewRequestVersion)
                return;

            _previewStyleId = null;
            await LoadPreviewHtmlAsync(html);

            // Record the document actually loaded. A queued request can now
            // update its CSS instead of generating the same document again.
            _loadedPreviewSong = previewSong;
            _loadedPreviewStructure = structure;
            _previewStyleId = styleId;
            _cssOptions = cssOptions;
            _visualization = visualization;
        }
        catch (Exception ex)
        {
            // Allow the next request to recover by loading a fresh document.
            _previewStyleId = null;
            System.Diagnostics.Debug.WriteLine($"Preview update failed: {ex}");
        }
        finally
        {
            SetPreviewLoading(false);
            _previewRenderLock.Release(); 
        }
    }

    private static string CreatePreviewCss(CustomLyricsCss customLyricsCss)
    {
        var cssOptions = new VisualizationCssOptions();
        cssOptions.ApplyCustomCss(customLyricsCss);

        // ApplyCustomCss targets .lyrics-line; support the Pre format as well.
        if (!string.IsNullOrEmpty(customLyricsCss.FontFamily))
            cssOptions.Add("pre", "font-family", customLyricsCss.FontFamily);

        if (customLyricsCss.FontSize > 0)
        {
            cssOptions.Add(
                "pre",
                "font-size",
                $"{customLyricsCss.FontSize.ToString(CultureInfo.InvariantCulture)}px");
        }

        if (!string.IsNullOrEmpty(customLyricsCss.TextColor))
        {
            cssOptions.Add("pre", "color", customLyricsCss.TextColor);

            // ToSvgHorizontal embeds presentation attributes.
            // Override diagram geometry without recoloring finger-number text.
            cssOptions.Add(
                ".chord-list svg line",
                "stroke",
                customLyricsCss.TextColor);

            cssOptions.Add(
                ".chord-list svg rect, .chord-list svg circle",
                "fill",
                customLyricsCss.TextColor);
        }

        return cssOptions.GenerateCss() ?? string.Empty;
    }

    private async Task<bool> TryUpdatePreviewCssAsync(string styleId, string css)
    {
        // JSON encoding safely embeds strings in JavaScript.
        var styleIdJson = System.Text.Json.JsonSerializer.Serialize(styleId);
        var cssJson = System.Text.Json.JsonSerializer.Serialize(css);

        var script = $$"""
        (function () {
            const style = document.getElementById({{styleIdJson}});

            if (!style)
                return 0;

            const css = {{cssJson}};

            if (style.textContent !== css)
                style.textContent = css;

            return 1;
        })();
        """;

        var result = await PreviewWebView.EvaluateJavaScriptAsync(script);
        return result?.Trim().Trim('"') == "1";
    }

    private async Task LoadPreviewHtmlAsync(string html)
    {
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void OnNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result == WebNavigationResult.Success)
            {
                completion.TrySetResult(true);
            }
            else
            {
                completion.TrySetException(
                    new InvalidOperationException(
                        $"Preview navigation failed: {e.Result}"));
            }
        }

        PreviewWebView.Navigated += OnNavigated;

        try
        {
            PreviewWebView.Source = new HtmlWebViewSource { Html = html };
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
        finally
        {
            PreviewWebView.Navigated -= OnNavigated;
        }
    }

    /// <summary>
    /// Zainicjuj obiekt CustomLyricsCss na podstawie aktualnych ustawieñ w UI.
    /// </summary>
    /// <returns></returns>
    private CustomLyricsCss LyricsCssFromControls()
    {
        var selectedFont = FontFamilyPicker.SelectedItem?.ToString() ?? AvailableFonts[0];
        var fontSize = ((int)FontSizeSlider.Value).ToString(CultureInfo.InvariantCulture) + "px";
        var chordsSelectedFont = ChordFontFamilyPicker.SelectedItem?.ToString();

        var textColor = TextColorPicker.PickedColor.ToHex();
        var chordColor = ChordColorPicker.PickedColor.ToHex();

        return new Zaczy.SongBook.CustomLyricsCss
        {
            FontFamily = selectedFont,
            FontSize = (int)FontSizeSlider.Value,
            TextColor = textColor,
            ChordColor = chordColor,
            ChordFontFamily = chordsSelectedFont,
            ChordFontSize = (int)ChordFontSizeSlider.Value,
            BackgroundColor = BackgroundColorPicker.PickedColor.ToHex()
        };
    }

    private static string SanitizeColor(string? candidate, string fallback)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return fallback;
        var v = candidate.Trim();
        if (v.Length == 7 && v.StartsWith('#') &&
            v[1..].All(c => "0123456789abcdefABCDEF".IndexOf(c) >= 0))
            return v;
        return fallback;
    }

    private void SetPreviewLoading(bool isLoading)
    {
        PreviewLoadingOverlay.IsVisible = isLoading;
        PreviewLoadingIndicator.IsRunning = isLoading;
    }

    // --- Handlery zdarzeñ UI ---
    private async void OnOptionChanged(object sender, EventArgs e) => await UpdatePreviewAsync();
    private async void OnSliderChanged(object sender, ValueChangedEventArgs e) => await UpdatePreviewAsync();
    private async void OnCheckChanged(object sender, CheckedChangedEventArgs e) => await UpdatePreviewAsync();
    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        await UpdatePreviewAsync();
    }

    private async void OnRandomSongClicked(object sender, EventArgs e)
    {
        await LoadRandomSongAsync();
        await UpdatePreviewAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // TODO: zapisz wybrane wartoœci np. do UserViewModel/Preferences
        // Preferences.Set("Layout.FontFamily", FontFamilyPicker.SelectedItem?.ToString());
        // Preferences.Set("Layout.FontSize", (int)FontSizeSlider.Value);
        // Preferences.Set("Layout.TextColor", SanitizeColor(TextColorEntry.Text, "#000000"));
        // Preferences.Set("Layout.ChordColor", SanitizeColor(ChordColorEntry.Text, "#0055AA"));

        //_userViewModel.FontSizeAdjustment = (int)FontSizeSlider.Value - 17;

        await DisplayAlert("Zapisano", "Ustawienia layoutu zosta³y zapisane jako domyœlne.", "OK");
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    
    private async void TextColorPicker_PickedColorChanged(object sender, PickedColorChangedEventArgs e)
    {
        TextColorEntry.Text = e.NewPickedColorValue.ToHex();
        TextColorEntry.TextColor = e.NewPickedColorValue;
        BackgroundColorEntry.TextColor = TextColorEntry.TextColor;

        await UpdatePreviewAsync();
    }

    private async void ChordColorPicker_PickedColorChanged(object sender, PickedColorChangedEventArgs e)
    {
        ChordColorEntry.Text = e.NewPickedColorValue.ToHex();
        ChordColorEntry.TextColor = e.NewPickedColorValue;
        await UpdatePreviewAsync();
    }

    private async void BackgroundColorPicker_PickedColorChanged(object sender, PickedColorChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"BackgroundColorPicker_PickedColorChanged {e.NewPickedColorValue.ToHex()} ({_isLoading} {_isDuringControlsSync})");

        BackgroundColorEntry.Text = e.NewPickedColorValue.ToHex();
        BackgroundColorEntry.BackgroundColor = e.NewPickedColorValue;

        await UpdatePreviewAsync();
    }

    private async void ColorEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"ColorEntry_TextChanged {e.NewTextValue} ({_isLoading} {_isDuringControlsSync})");

        if (_isLoading) return;
        if (_isDuringControlsSync) return;

        var newText = e.NewTextValue;

        if (!string.IsNullOrWhiteSpace(SanitizeColor(newText, "")))
        {
            _isDuringControlsSync = true;
            if (sender == TextColorEntry)
            {
                var color = Color.FromArgb(newText);
                TextColorPicker.PickedColor = color;
                BackgroundColorEntry.TextColor = color;
            }
            else if (sender == ChordColorEntry)
            {
                var color = Color.FromArgb(newText);
                ChordColorPicker.PickedColor = color;
            }
            else if (sender == BackgroundColorEntry)
            {
                System.Diagnostics.Debug.WriteLine($"sender == BackgroundColorEntry ({_isLoading} {_isDuringControlsSync})");

                var color = Color.FromArgb(newText);
                BackgroundColorPicker.PickedColor = color;
            }
            _isDuringControlsSync = false;
            await UpdatePreviewAsync();
        }
    }


}