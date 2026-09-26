using System.Globalization;

namespace Zaczy.SongBook;

public class CustomLyricsCss
{

    public string? FontFamily { get; set; }
    public int? FontSize { get; set; }
    public string? TextColor { get; set; }
    public string? ChordColor { get; set; }
    public string? BackgroundColor { get; set; }
  
    public string? ChordFontFamily { get; set; }
    public int? ChordFontSize { get; set; }

    public static string CreatePreviewCss(CustomLyricsCss? customLyricsCss)
    {
        if (customLyricsCss == null)
            return string.Empty;

        var cssOptions = new VisualizationCssOptions();
        cssOptions.ApplyCustomCss(customLyricsCss);

        // ApplyCustomCss targets .lyrics-line; support the Pre format as well.
        if (!string.IsNullOrEmpty(customLyricsCss.FontFamily))
            cssOptions.Add("pre", "font-family", customLyricsCss.FontFamily);

        if ((customLyricsCss?.FontSize ?? 0)> 0)
        {
            int size = customLyricsCss?.FontSize ?? 0;
            cssOptions.Add(
                "pre",
                "font-size",
                $"{size.ToString(CultureInfo.InvariantCulture)}px");
        }

        if (!string.IsNullOrEmpty(customLyricsCss?.TextColor))
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

}