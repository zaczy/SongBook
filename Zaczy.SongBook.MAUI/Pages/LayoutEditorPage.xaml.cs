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

#pragma warning disable CA1416
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

    private readonly LayoutEditorChanges _controlChanges;

    private Dictionary<string, object?> ReadControlValues()
    {
        return new Dictionary<string, object?>
        {
            [nameof(FontFamilyPicker)] = FontFamilyPicker.SelectedItem?.ToString(),
            [nameof(FontSizeSlider)] = FontSizeSlider.Value,
            [nameof(ChordFontFamilyPicker)] = ChordFontFamilyPicker.SelectedItem?.ToString(),
            [nameof(ChordFontSizeSlider)] = ChordFontSizeSlider.Value,

            [nameof(TextColorPicker)] = ReadColorValue(TextColorPicker.PickedColor),
            [nameof(TextColorEntry)] = TextColorEntry.Text,
            [nameof(ChordColorPicker)] = ReadColorValue(ChordColorPicker.PickedColor),
            [nameof(ChordColorEntry)] = ChordColorEntry.Text,
            [nameof(BackgroundColorPicker)] = ReadColorValue(BackgroundColorPicker.PickedColor),
            [nameof(BackgroundColorEntry)] = BackgroundColorEntry.Text,

            [nameof(SkipTabsCheck)] = SkipTabsCheck.IsChecked,
            [nameof(MoveChordsToLyricsLine)] = MoveChordsToLyricsLine.IsChecked,
            [nameof(HideChordsCheck)] = HideChordsCheck.IsChecked
        };
    }


    public LayoutEditorPage(UserViewModel userViewModel, SongRepositoryLite songRepository)
    {
        InitializeComponent();

        _userViewModel = userViewModel;
        _songRepository = songRepository;

        _cssOptions = new VisualizationCssOptions();
        _visualization = new SongVisualization { IncludeFontsAsBase64 = true };

        InitializeFontList();

        _controlChanges = new LayoutEditorChanges(ReadControlValues);

    }

    protected override async void OnAppearing()
    {

        base.OnAppearing();

        await FontsHelper.EnsureFontsAvailableAsync(_visualization);
        await LoadRandomSongAsync();
        LoadCurrentSettingsIntoUi();

        _controlChanges.CaptureInitialValues();

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
    /// Załaduj obecne wartości z UserViewModel do UI.
    /// </summary>
    private void LoadCurrentSettingsIntoUi()
    {
        VisualizationCssOptions visualizationCssOptions = new VisualizationCssOptions();
        LyricsVisualisationHelper.AdjustDarkModeCss(_userViewModel, visualizationCssOptions);

        var bodyColor = _userViewModel?.CustomLyricsCss?.TextColor ?? visualizationCssOptions.CssValue("body", "color");
        var chordsColor = _userViewModel?.CustomLyricsCss?.ChordColor ?? visualizationCssOptions.CssValue(".chords", "color");

        FontSizeSlider.Value = _userViewModel?.FontSizeAdjustment ?? 0;

        TextColorPicker.PickedColor = Color.FromArgb(bodyColor);
        TextColorEntry.Text = bodyColor;
        
        ChordColorEntry.Text = chordsColor;
        ChordColorPicker.PickedColor = Color.FromArgb(chordsColor);

        ChordFontSizeSlider.Value = _userViewModel?.CustomLyricsCss?.ChordFontSize ?? 0;

        var bgColor = _userViewModel?.CustomLyricsCss?.BackgroundColor ?? visualizationCssOptions.CssValue("body", "background-color");
        BackgroundColorPicker.PickedColor = Color.FromArgb(bgColor);
        BackgroundColorEntry.Text = bgColor;

        SkipTabsCheck.IsChecked = _userViewModel?.SkipTabulatures ?? false;
        MoveChordsToLyricsLine.IsChecked = _userViewModel?.MoveChordsToLyricsLine ?? false;
        HideChordsCheck.IsChecked = _userViewModel?.SkipLyricChords ?? false;

       if(_userViewModel?.CustomLyricsCss?.FontFamily != null)
        {
            var fontIndex = Array.IndexOf(AvailableFonts, _userViewModel.CustomLyricsCss.FontFamily);
            if (fontIndex >= 0)
                FontFamilyPicker.SelectedIndex = fontIndex;
        }
        else
            FontFamilyPicker.SelectedIndex = 0;


        if (_userViewModel?.CustomLyricsCss?.ChordFontFamily != null)
        {
            var chordFontIndex = Array.IndexOf(AvailableFonts, _userViewModel.CustomLyricsCss.ChordFontFamily);
            if (chordFontIndex >= 0)
                ChordFontFamilyPicker.SelectedIndex = chordFontIndex;
        }
        else
            ChordFontFamilyPicker.SelectedIndex = 0;
    }

    /// <summary>
    /// Załaduj losową piosenkę z bazy danych i ustaw ją jako podgląd.
    /// </summary>
    /// <returns></returns>
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
        PreviewSongLabel.Text = $"Podgląd: {_previewSong.Title} — {_previewSong.Artist}";
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
            var customLyricsCss = LyricsCssFromControls(false);
            var editableCss = CustomLyricsCss.CreatePreviewCss(customLyricsCss);

            var skipTabulatures = SkipTabsCheck.IsChecked;
            var moveChords = MoveChordsToLyricsLine.IsChecked == true;
            var hideChords = HideChordsCheck.IsChecked;
            var instrument = _userViewModel.ChordsInstrument;
            var htmlVersion = _userViewModel.LyricsHtmlVersion;

            // Only changes that affect document structure require rebuilding.
            var structure = System.Text.Json.JsonSerializer.Serialize(new
            {
                SkipTabulatures = skipTabulatures,
                MoveChordsToLyricsLine = moveChords,
                HideChords = hideChords,
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
                SkipLyricChords = hideChords,
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
    /// Zainicjuj obiekt CustomLyricsCss na podstawie aktualnych ustawień w UI.
    /// </summary>
    /// <returns></returns>
    private CustomLyricsCss LyricsCssFromControls(bool changedValuesOnly)
    {
        var selectedFont = FontFamilyPicker.SelectedItem?.ToString() ?? AvailableFonts[0];
        //var fontSize = ((int)FontSizeSlider.Value).ToString(CultureInfo.InvariantCulture) + "px";
        var chordsSelectedFont = ChordFontFamilyPicker.SelectedItem?.ToString();

        var textColor = TextColorPicker.PickedColor.ToHex();
        var chordColor = ChordColorPicker.PickedColor.ToHex();

        return new Zaczy.SongBook.CustomLyricsCss
        {
            FontFamily = !changedValuesOnly || hasControlBeenChanged(nameof(FontFamilyPicker)) ? selectedFont : null,
            FontSize = !changedValuesOnly || hasControlBeenChanged(nameof(FontSizeSlider)) ? 17 + (int)FontSizeSlider!.Value : null,
            TextColor = !changedValuesOnly || hasControlBeenChanged(nameof(TextColorPicker)) ? textColor : null,
            ChordColor = !changedValuesOnly || hasControlBeenChanged(nameof(ChordColorPicker)) ? chordColor : null,
            ChordFontFamily = !changedValuesOnly || hasControlBeenChanged(nameof(ChordFontFamilyPicker)) ? chordsSelectedFont : null,
            ChordFontSize = !changedValuesOnly || hasControlBeenChanged(nameof(ChordFontSizeSlider)) ? (int)ChordFontSizeSlider.Value : null,
            BackgroundColor = !changedValuesOnly || hasControlBeenChanged(nameof(BackgroundColorPicker)) ? BackgroundColorPicker.PickedColor.ToHex() : null    
        };
    }

    private bool hasControlBeenChanged(string controlName)
    {
        var controls = _controlChanges.Controls;

        return controls[controlName].IsChanged;

    }

    /// <summary>
    /// Standaryzacja zapisu kolorów #XXXXXX
    /// </summary>
    /// <param name="candidate"></param>
    /// <param name="fallback"></param>
    /// <returns></returns>
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

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    /// <summary>
    /// Zmiana koloru tekstu - picker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void TextColorPicker_PickedColorChanged(object sender, PickedColorChangedEventArgs e)
    {
        TextColorEntry.Text = e.NewPickedColorValue.ToHex();
        TextColorEntry.TextColor = e.NewPickedColorValue;
        BackgroundColorEntry.TextColor = TextColorEntry.TextColor;

        await UpdatePreviewAsync();
    }

    /// <summary>
    /// Zmiana koloru chwytów - picker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void ChordColorPicker_PickedColorChanged(object sender, PickedColorChangedEventArgs e)
    {
        ChordColorEntry.Text = e.NewPickedColorValue.ToHex();
        ChordColorEntry.TextColor = e.NewPickedColorValue;
        await UpdatePreviewAsync();
    }

    /// <summary>
    /// Zmiana tła - picker
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void BackgroundColorPicker_PickedColorChanged(object sender, PickedColorChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"BackgroundColorPicker_PickedColorChanged {e.NewPickedColorValue.ToHex()} ({_isLoading} {_isDuringControlsSync})");

        BackgroundColorEntry.Text = e.NewPickedColorValue.ToHex();
        BackgroundColorEntry.BackgroundColor = e.NewPickedColorValue;

        await UpdatePreviewAsync();
    }

    /// <summary>
    /// Zmiana koloru przez edycję kodu hex
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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

    /// <summary>
    /// Włączanie/wyłączanie sekcji 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnFontSectionClicked(object sender, EventArgs e)
    {
        ToggleSection(FontSectionContent, FontSectionHeader, "Czcionka");
    }

    private void OnColorsSectionClicked(object sender, EventArgs e)
    {
        ToggleSection(ColorsSectionContent, ColorsSectionHeader, "Kolory");
    }

    private void OnPresentationSectionClicked(object sender, EventArgs e)
    {
        ToggleSection(
            PresentationSectionContent,
            PresentationSectionHeader,
            "Prezentacja");
    }

    private static void ToggleSection(
        VisualElement content,
        Button header,
        string title)
    {
        var isExpanded = !content.IsVisible;

        content.IsVisible = isExpanded;
        header.Text = $"{title}  {(isExpanded ? "▾" : "▸")}";

        SemanticProperties.SetHint(
            header,
            isExpanded ? "Zwiń sekcję" : "Rozwiń sekcję");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    private static object? ReadColorValue(Color? color)
    {
        if (color == null)
            return null;

        // Capture channel values rather than retaining a Color reference.
        return (color.Red, color.Green, color.Blue, color.Alpha);
    }

    private sealed record ControlValueChange(
        object? InitialValue,
        object? CurrentValue)
    {
        public bool IsChanged => !Equals(InitialValue, CurrentValue);
    }

    /// <summary>
    /// Zerowanie ustawień
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnResetClicked(object sender, EventArgs e)
    {
        _userViewModel.CustomLyricsCss = new CustomLyricsCss();

        _userViewModel.FontSizeAdjustment = 0;
        _userViewModel.MoveChordsToLyricsLine = false;
        _userViewModel.ShowOnlyCustomChords = false;
        _userViewModel.SkipTabulatures = true;

        LoadCurrentSettingsIntoUi();

        await UpdatePreviewAsync();
    }

    /// <summary>
    /// Zapisz ustawienia na stałe
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        _userViewModel.FontSizeAdjustment = (int)FontSizeSlider.Value;
        _userViewModel.MoveChordsToLyricsLine = MoveChordsToLyricsLine?.IsChecked == true;
        _userViewModel.SkipLyricChords = HideChordsCheck?.IsChecked == true;
        _userViewModel.SkipTabulatures = SkipTabsCheck.IsChecked == true;

        var controls = _controlChanges.Controls;
        var changedControlNames = controls
            .Where(item => item.Value.IsChanged)
            .Select(item => item.Key)
            .ToList();

        _userViewModel.CustomLyricsCss = LyricsCssFromControls(true);

        if(_userViewModel?.CustomLyricsCss?.FontSize != null)
            _userViewModel.CustomLyricsCss.FontSize = null;

        await Navigation.PopAsync();
    }

    private void ControlChangedExamples()
    {
            bool anythingChanged = _controlChanges.HasChanges;

            // Read once to obtain a consistent snapshot for multiple checks.
            var controls = _controlChanges.Controls;

            bool fontChanged = controls[nameof(FontFamilyPicker)].IsChanged;
            bool backgroundChanged = controls[nameof(BackgroundColorPicker)].IsChanged;

            var originalSize = controls[nameof(FontSizeSlider)].InitialValue;
            var currentSize = controls[nameof(FontSizeSlider)].CurrentValue;

            var changedControlNames = controls
                .Where(item => item.Value.IsChanged)
                .Select(item => item.Key)
                .ToArray();
    }

    private sealed class LayoutEditorChanges
    {
        private readonly Func<Dictionary<string, object?>> _readValues;
        private Dictionary<string, object?>? _initialValues;

        public LayoutEditorChanges(Func<Dictionary<string, object?>> readValues)
        {
            _readValues = readValues;
        }

        public bool HasChanges => Controls.Values.Any(change => change.IsChanged);

        public IReadOnlyDictionary<string, ControlValueChange> Controls
        {
            get
            {
                var currentValues = _readValues();
                var initialValues = _initialValues ?? currentValues;

                var changes = currentValues.ToDictionary(
                    item => item.Key,
                    item => new ControlValueChange(
                        initialValues[item.Key],
                        item.Value));

                return new System.Collections.ObjectModel.ReadOnlyDictionary<
                    string, ControlValueChange>(changes);
            }
        }

        public void CaptureInitialValues()
        {
            _initialValues = _readValues();
        }
    }


}

#pragma warning restore CA1416