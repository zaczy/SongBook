using DocumentFormat.OpenXml.Office2010.PowerPoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;

namespace Zaczy.SongBook.Data;

public class SongEntityTools
{

    /// <summary>
    /// Returns a theme-aware color blended with the provided categoryColor.
    /// If categoryColor is null/invalid, returns the theme base color.
    /// Blending is a simple interpolation in sRGB space.
    /// </summary>
    public static string? ThemeCategoryColor(string? categoryColor)
    {
        string baseColor = string.Empty;

        var themeName = ThemeValue();
        if (string.Equals(themeName, "Dark", StringComparison.OrdinalIgnoreCase))
            baseColor = "#333";
        else
            baseColor = "#EEE";

        string categoryNormalized = NormalizeToHex(categoryColor);
        // If no category color provided, just return base color
        if (string.IsNullOrWhiteSpace(categoryColor) || categoryNormalized.ToUpper() == "#FFFFFF" || categoryNormalized.ToUpper() == "#000000")
            return NormalizeToHex(baseColor);

        // Try parse both colors; if parsing fails for categoryColor, fall back to baseColor
        if (!TryParseHexColor(NormalizeToHex(baseColor), out byte bA, out byte bR, out byte bG, out byte bB))
            return NormalizeToHex(baseColor);

        if (!TryParseHexColor(categoryColor, out byte cA, out byte cR, out byte cG, out byte cB))
            return NormalizeToHex(baseColor);

        double ratio = 0.05;
        byte r = MixComponent(bR, cR, ratio);
        byte g = MixComponent(bG, cG, ratio);
        byte b = MixComponent(bB, cB, ratio);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Pomieszaj składowe dwóch kolorów (base i category) według podanej wagi (0.0 - 1.0).
    /// </summary>
    /// <param name="baseComp"></param>
    /// <param name="catComp"></param>
    /// <param name="weight"></param>
    /// <returns></returns>
    private static byte MixComponent(byte baseComp, byte catComp, double weight)
    {
        double blended = baseComp * (1.0 - weight) + catComp * weight;
        return (byte)Math.Round(Math.Clamp(blended, 0, 255));
    }

    /// <summary>
    /// Normalize shorthand (#EEE) to full hex (#RRGGBB) and ensure it starts with '#'.
    /// </summary>
    private static string NormalizeToHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return "#EEEEEE";

        var h = hex.Trim();
        if (!h.StartsWith("#")) h = "#" + h;

        if (h.Length == 4) // #RGB -> #RRGGBB
        {
            var r = h[1];
            var g = h[2];
            var b = h[3];
            return $"#{r}{r}{g}{g}{b}{b}";
        }

        if (h.Length == 7) // #RRGGBB
            return h;

        if (h.Length == 9) // #AARRGGBB -> drop alpha for UI background
            return $"#{h.Substring(3, 6)}";

        // fallback
        return "#EEEEEE";
    }

    /// <summary>
    /// Parses hex color. Accepts #RGB, #RRGGBB, #AARRGGBB and returns bytes (alpha, r, g, b).
    /// Returns false if parsing fails.
    /// </summary>
    private static bool TryParseHexColor(string hex, out byte a, out byte r, out byte g, out byte b)
    {
        a = 255; r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex)) return false;

        var h = hex.Trim();
        if (h.StartsWith("#")) h = h.Substring(1);

        try
        {
            if (h.Length == 3) // RGB
            {
                r = byte.Parse(new string(h[0], 2), NumberStyles.HexNumber);
                g = byte.Parse(new string(h[1], 2), NumberStyles.HexNumber);
                b = byte.Parse(new string(h[2], 2), NumberStyles.HexNumber);
                return true;
            }
            else if (h.Length == 6) // RRGGBB
            {
                r = byte.Parse(h.Substring(0, 2), NumberStyles.HexNumber);
                g = byte.Parse(h.Substring(2, 2), NumberStyles.HexNumber);
                b = byte.Parse(h.Substring(4, 2), NumberStyles.HexNumber);
                return true;
            }
            else if (h.Length == 8) // AARRGGBB
            {
                a = byte.Parse(h.Substring(0, 2), NumberStyles.HexNumber);
                r = byte.Parse(h.Substring(2, 2), NumberStyles.HexNumber);
                g = byte.Parse(h.Substring(4, 2), NumberStyles.HexNumber);
                b = byte.Parse(h.Substring(6, 2), NumberStyles.HexNumber);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    /// <summary>
    /// Pobierz aktualnie wybrany temat.
    /// Używa bezpośrednio MAUI API (AppInfo.RequestedTheme) zamiast refleksji.
    /// </summary>
    /// <returns></returns>
    private static string ThemeValue()
    {
        try
        {
            // AppInfo.RequestedTheme jest stabilnym API w .NET MAUI i działa niezależnie od wewnętrznej
            // lokalizacji typu Application — to prostsze i pewniejsze niż Type.GetType(...) + refleksja.
            var requested = AppInfo.RequestedTheme;
            return requested == AppTheme.Dark ? "Dark" : "Light";
        }
        catch
        {
            // fallback
            return "Light";
        }
    }

}

