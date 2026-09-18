using DocumentFormat.OpenXml.InkML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook;

public class VisualizationCssOptions
{
    private HashSet<CssOption>? _customOptions;

    public HashSet<CssOption> CustomOptions
    {
        get
        {
            if (_customOptions == null)
                _customOptions = new HashSet<CssOption>();

            return _customOptions;
        }

        set
        {
            _customOptions = value;
        }
    }

    /// <summary>
    /// Dodaj dyrektywę
    /// </summary>
    /// <param name="cssClass"></param>
    /// <param name="cssProperty"></param>
    /// <param name="value"></param>
    /// <param name="context"></param>
    public void Add(string cssClass, string cssProperty, string value, string? context=null, int? priority=null)
    {
        
        bool shouldAdd = context == null || CustomOptions?.Any(o => o.CssClass == cssClass && o.CssProperty == cssProperty && o.Context == context && o.Value == value) != true;

        if (shouldAdd)
        {
            var existing = CustomOptions!.Where(o => o.CssClass == cssClass && o.CssProperty == cssProperty)?.FirstOrDefault();
            if (existing != null)
                CustomOptions!.Remove(existing);
            CustomOptions!.Add(new CssOption(cssClass, cssProperty, value) { Context = context, Priority = priority });
        }
    }

    /// <summary>
    /// Zwraca wartość z definicji (o ile została zdefiniowana)
    /// </summary>
    /// <param name="cssClass"></param>
    /// <param name="cssProperty"></param>
    /// <returns></returns>
    public string? CssValue(string cssClass, string cssProperty, string? predictedClassesAllowed=null)
    {
        List<CssOption>? list = this.CssValueList(cssClass, cssProperty, predictedClassesAllowed)?.ToList() ?? null;

        if (list != null)
        {
            return list.OrderByDescending(o => o.Priority).OrderByDescending(o=>o.LineNo).FirstOrDefault()?.Value; 
        }

        return null;
    }

    /// <summary>
    /// Zwraca wartość z definicji (o ile została zdefiniowana)
    /// </summary>
    /// <param name="cssClass"></param>
    /// <param name="cssProperty"></param>
    /// <returns></returns>
    public List<CssOption>? CssValueList(string cssClass, string cssProperty, string? predictedClassesAllowed = null)
    {
        if (predictedClassesAllowed != null)
        {
            var op = CustomOptions.Where(o => (o.CssClass == cssClass || (o.CssClass.Contains(cssClass) && o.CssClass.StartsWith(predictedClassesAllowed))) && o.CssProperty == cssProperty);

            return op?.ToList() ?? null;
        }
        else
        {

            var op = CustomOptions.Where(o => o.CssClass == cssClass && o.CssProperty == cssProperty);

            return op?.ToList() ?? null;
        }
    }


    /// <summary>
    /// Generuj kod css dla wybranej klasy
    /// </summary>
    /// <param name="cssClass"></param>
    /// <returns></returns>
    public string? GenerateCss(string cssClass)
    {
        string? css = null;
        string properties = String.Empty;

        foreach(var op in CustomOptions.Where(o => o.CssClass == cssClass))
        {
            if (!string.IsNullOrEmpty(op.CssProperty) && !string.IsNullOrEmpty(op.Value))
            {
                properties += $"{op.CssProperty}: {op.Value} !important; ";                
            }
        }

        if (!string.IsNullOrEmpty(properties))
            css = $"{cssClass} {{ {properties} }}";

        return css;
    }

    /// <summary>
    /// Generuj kod css dla wszystkich zdefiniowanych klas
    /// </summary>
    /// <returns></returns>
    public string? GenerateCss()
    {
        string css = string.Empty;

        var classes = CustomOptions.GroupBy(o => o.CssClass).Select(g => g.Key).ToList();

        foreach(string c in classes)
        {
            string? clsCss = this.GenerateCss(c);
            if (!string.IsNullOrEmpty(clsCss))
                css += $"{clsCss}\n";
        }

        return !string.IsNullOrEmpty(css) ? css : null;
    }

    private const int DefaultPriority = 3;

    /// <summary>
    /// Parsuje style CSS zawarte w podanym HTML-u (bloki &lt;style&gt;...&lt;/style&gt;)
    /// i tworzy wpisy w VisualizationCssOptions dla każdej pary właściwość: wartość.
    /// Każdemu wpisowi ustawiany jest Context = "Line {n}" (numer linii w źródłowym HTML)
    /// oraz Priority zależny od dopasowania reguły do <paramref name="mediaSize"/>.
    /// </summary>
    /// <param name="html">Kod HTML zawierający jeden lub więcej bloków &lt;style&gt;.</param>
    /// <param name="mediaSize">Szerokość ekranu w px do dopasowania reguł @media; -1 oznacza brak filtrowania.</param>
    public static VisualizationCssOptions FromHtml(string html, int mediaSize)
    {
        var result = new VisualizationCssOptions();
        if (string.IsNullOrWhiteSpace(html))
            return result;

        // 1) Wyciągnij zawartość wszystkich bloków <style>...</style> zachowując offsety w oryginale
        var styleBlocks = System.Text.RegularExpressions.Regex.Matches(
            html,
            @"<style\b[^>]*>(?<css>.*?)</style>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.Singleline);

        if (styleBlocks.Count > 0)
        {
            foreach (System.Text.RegularExpressions.Match m in styleBlocks)
            {
                var cssRaw = m.Groups["css"].Value;
                var cssOffset = m.Groups["css"].Index; // offset w oryginalnym HTML

                // Usuń komentarze zachowując znaki nowej linii, żeby nie zaburzać numerów wierszy
                var css = RemoveCssCommentsPreservingLines(cssRaw);
                ParseCssBlock(css, result, mediaSize, html, cssOffset, DefaultPriority);
            }
        }
        else
        {
            // Brak <style> - traktujemy wejście jako czysty CSS (offset = 0)
            var css = RemoveCssCommentsPreservingLines(html);
            ParseCssBlock(css, result, mediaSize, html, 0, DefaultPriority);
        }

        return result;
    }

    /// <summary>
    /// Zachowana zgodność wsteczna - bez filtrowania po szerokości ekranu.
    /// </summary>
    public static VisualizationCssOptions FromHtml(string html)
        => FromHtml(html, mediaSize: -1);

    /// <summary>
    /// Przetwarza rekurencyjnie fragment CSS - obsługuje zwykłe reguły i @media / @supports.
    /// </summary>
    /// <param name="css">Zawartość CSS do sparsowania.</param>
    /// <param name="target">Docelowa kolekcja opcji.</param>
    /// <param name="mediaSize">Szerokość ekranu docelowa (-1 = brak filtrowania).</param>
    /// <param name="originalHtml">Oryginalny HTML do liczenia numerów linii.</param>
    /// <param name="offsetInOriginal">Offset początku aktualnego bloku css w originalHtml.</param>
    /// <param name="currentPriority">Aktualny priorytet reguł (dziedziczony przez @media).</param>
    /// <param name="parentDefinitionMediaSize"></param>
    private static void ParseCssBlock(
        string css,
        VisualizationCssOptions target,
        int mediaSize,
        string originalHtml,
        int offsetInOriginal,
        int currentPriority,
        int? parentDefinitionMediaSize = null)
    {
        int i = 0;
        while (i < css.Length)
        {
            int braceOpen = css.IndexOf('{', i);
            if (braceOpen < 0) break;

            var selector = css.Substring(i, braceOpen - i).Trim();

            // Zapamiętaj offset selektora względem oryginalnego HTML (do numeru linii)
            int selectorOffsetInOriginal = offsetInOriginal + i;

            // Znajdź pasującą klamrę zamykającą
            int depth = 1;
            int j = braceOpen + 1;
            while (j < css.Length && depth > 0)
            {
                if (css[j] == '{') depth++;
                else if (css[j] == '}') depth--;
                if (depth == 0) break;
                j++;
            }
            if (j >= css.Length) break;

            var body = css.Substring(braceOpen + 1, j - braceOpen - 1);
            int bodyOffsetInOriginal = offsetInOriginal + braceOpen + 1;

            var trimmedSelector = selector.TrimStart();

            if (trimmedSelector.StartsWith("@media", StringComparison.OrdinalIgnoreCase))
            {
                var mediaCondition = trimmedSelector.Substring("@media".Length).Trim();

                bool matches = mediaSize < 0 || MediaQueryMatches(mediaCondition, mediaSize);
                if (matches)
                {
                    int mediaPriority = mediaSize < 0
                        ? currentPriority
                        : CalculateMediaPriority(mediaCondition, mediaSize, currentPriority);

                    // Wyznacz reprezentatywną szerokość z warunku @media
                    int? definitionMediaSize = ExtractDefinitionMediaSize(mediaCondition);

                    ParseCssBlock(body, target, mediaSize, originalHtml, bodyOffsetInOriginal, mediaPriority, definitionMediaSize);
                }
            }
            else if (trimmedSelector.StartsWith("@supports", StringComparison.OrdinalIgnoreCase))
            {
                ParseCssBlock(body, target, mediaSize, originalHtml, bodyOffsetInOriginal, currentPriority, parentDefinitionMediaSize);
            }
            else if (trimmedSelector.StartsWith("@"))
            {
                // @keyframes, @font-face - pomijamy
            }
            else if (!string.IsNullOrEmpty(selector))
            {
                ParseDeclarations(target, selector, body, originalHtml, bodyOffsetInOriginal, currentPriority, parentDefinitionMediaSize);
            }

            i = j + 1;
        }
    }

    /// <summary>
    /// Wyznacza priorytet dla reguł zagnieżdżonych w @media.
    /// Zasada: domyślny priorytet = 3, dopasowany @media ma priorytet tym wyższy,
    /// im mniejsza odległość między mediaSize a najbliższą granicą (min-width/max-width) reguły.
    /// </summary>
    private static int CalculateMediaPriority(string mediaCondition, int mediaSize, int basePriority)
    {
        // Zbierz wszystkie liczbowe punkty odniesienia (min-width, max-width, width)
        var featureRegex = new System.Text.RegularExpressions.Regex(
            @"\(\s*(?<name>[a-zA-Z\-]+)\s*:\s*(?<value>[^)]+?)\s*\)");

        int? closestDistance = null;

        foreach (System.Text.RegularExpressions.Match m in featureRegex.Matches(mediaCondition))
        {
            var name = m.Groups["name"].Value.ToLowerInvariant();
            if (name != "min-width" && name != "max-width" && name != "width")
                continue;

            if (!TryParsePixels(m.Groups["value"].Value.Trim(), out var px))
                continue;

            int distance = Math.Abs(mediaSize - px);
            if (closestDistance == null || distance < closestDistance)
                closestDistance = distance;
        }

        if (closestDistance == null)
            return basePriority + 1; // pasujący @media bez width - jednak coś ponad default

        // Im mniejszy dystans, tym wyższy priorytet.
        // Nadajemy bonus proporcjonalny do bliskości. Skala: <=50px = +10, <=100 = +8, <=200 = +6, <=500 = +4, <=1000 = +2, więcej = +1
        int bonus = closestDistance switch
        {
            <= 50 => 10,
            <= 100 => 8,
            <= 200 => 6,
            <= 500 => 4,
            <= 1000 => 2,
            _ => 1
        };

        return basePriority + bonus;
    }

    /// <summary>
    /// Usuwa komentarze CSS /* ... */ zamieniając je na spacje, tak aby zachować pozycje znaków nowej linii.
    /// Dzięki temu numery wierszy odpowiadają oryginałowi.
    /// </summary>
    private static string RemoveCssCommentsPreservingLines(string css)
    {
        if (string.IsNullOrEmpty(css)) return css;

        return System.Text.RegularExpressions.Regex.Replace(
            css,
            @"/\*.*?\*/",
            match =>
            {
                var sb = new System.Text.StringBuilder(match.Value.Length);
                foreach (var c in match.Value)
                    sb.Append(c == '\n' ? '\n' : ' ');
                return sb.ToString();
            },
            System.Text.RegularExpressions.RegexOptions.Singleline);
    }

    /// <summary>
    /// Zwraca numer linii (1-based) dla podanego offsetu w tekście.
    /// </summary>
    private static int GetLineNumber(string text, int offset)
    {
        if (offset <= 0) return 1;
        if (offset > text.Length) offset = text.Length;

        int line = 1;
        for (int k = 0; k < offset; k++)
        {
            if (text[k] == '\n') line++;
        }
        return line;
    }

    private static void ParseDeclarations(
        VisualizationCssOptions target,
        string selectorRaw,
        string body,
        string originalHtml,
        int bodyOffsetInOriginal,
        int priority,
        int? definitionMediaSize)
    {
        var selectors = selectorRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " "))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        int localPos = 0;
        while (localPos < body.Length)
        {
            int semicolon = body.IndexOf(';', localPos);
            int end = semicolon >= 0 ? semicolon : body.Length;

            var decl = body.Substring(localPos, end - localPos);
            int declOffsetInOriginal = bodyOffsetInOriginal + localPos;

            var colon = decl.IndexOf(':');
            if (colon > 0)
            {
                var prop = decl.Substring(0, colon).Trim();
                var val = decl.Substring(colon + 1).Trim();

                val = System.Text.RegularExpressions.Regex.Replace(
                    val, @"\s*!important\s*$", string.Empty,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (!string.IsNullOrEmpty(prop) && !string.IsNullOrEmpty(val))
                {
                    int lineNo = GetLineNumber(originalHtml, declOffsetInOriginal);

                    foreach (var sel in selectors)
                    {
                        target.CustomOptions.Add(new CssOption(sel, prop, val)
                        {
                            Priority = priority,
                            LineNo = lineNo,
                            DefinitionMediaSize = definitionMediaSize
                        });
                    }
                }
            }

            localPos = end + 1;
        }
    }

    /// <summary>
    /// Ocenia media query typu "screen and (min-width: 768px) and (max-width: 1024px)".
    /// Obsługiwane feature: min-width, max-width, width (w pikselach).
    /// Obsługiwane operatory logiczne: "and" (AND), "," (OR).
    /// </summary>
    private static bool MediaQueryMatches(string mediaCondition, int mediaSize)
    {
        if (string.IsNullOrWhiteSpace(mediaCondition))
            return true;

        // Lista alternatyw rozdzielona przecinkami
        var alternatives = mediaCondition.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var alt in alternatives)
        {
            if (EvaluateSingleQuery(alt.Trim(), mediaSize))
                return true;
        }
        return false;
    }

    private static bool EvaluateSingleQuery(string query, int mediaSize)
    {
        // Rozbij na warunki "and"
        var parts = System.Text.RegularExpressions.Regex.Split(
            query, @"\s+and\s+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var raw in parts)
        {
            var part = raw.Trim();
            if (part.Length == 0) continue;

            if (part.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
                return false;

            if (part.Equals("print", StringComparison.OrdinalIgnoreCase))
                return false;

            if (part.Equals("screen", StringComparison.OrdinalIgnoreCase)
                || part.Equals("all", StringComparison.OrdinalIgnoreCase)
                || part.Equals("only screen", StringComparison.OrdinalIgnoreCase)
                || part.Equals("only", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Feature w nawiasach: (min-width: 768px)
            var featureMatch = System.Text.RegularExpressions.Regex.Match(
                part,
                @"^\(\s*(?<name>[a-zA-Z\-]+)\s*:\s*(?<value>[^)]+?)\s*\)$");

            if (!featureMatch.Success)
                continue;

            var name = featureMatch.Groups["name"].Value.ToLowerInvariant();
            var valueStr = featureMatch.Groups["value"].Value.Trim();
            if (!TryParsePixels(valueStr, out var pxValue))
                continue;

            switch (name)
            {
                case "min-width":
                    if (mediaSize < pxValue) return false;
                    break;
                case "max-width":
                    if (mediaSize > pxValue) return false;
                    break;
                case "width":
                    if (mediaSize != pxValue) return false;
                    break;
            }
        }
        return true;
    }

    private static bool TryParsePixels(string value, out int px)
    {
        px = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var m = System.Text.RegularExpressions.Regex.Match(
            value, @"^(?<num>\d+(\.\d+)?)\s*(?<unit>px|em|rem)?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return false;

        if (!double.TryParse(m.Groups["num"].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var num))
            return false;

        var unit = m.Groups["unit"].Value.ToLowerInvariant();
        switch (unit)
        {
            case "":
            case "px":
                px = (int)num;
                return true;
            case "em":
            case "rem":
                px = (int)(num * 16); // przybliżenie 1em = 16px
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Wyznacza reprezentatywną szerokość ekranu z warunku @media.
    /// Preferuje min-width, następnie max-width, a na końcu width.
    /// Zwraca null, jeśli warunek nie zawiera cechy szerokości.
    /// </summary>
    private static int? ExtractDefinitionMediaSize(string mediaCondition)
    {
        if (string.IsNullOrWhiteSpace(mediaCondition))
            return null;

        var featureRegex = new System.Text.RegularExpressions.Regex(
            @"\(\s*(?<name>[a-zA-Z\-]+)\s*:\s*(?<value>[^)]+?)\s*\)");

        int? minWidth = null, maxWidth = null, width = null;

        foreach (System.Text.RegularExpressions.Match m in featureRegex.Matches(mediaCondition))
        {
            var name = m.Groups["name"].Value.ToLowerInvariant();
            if (!TryParsePixels(m.Groups["value"].Value.Trim(), out var px))
                continue;

            switch (name)
            {
                case "min-width": minWidth ??= px; break;
                case "max-width": maxWidth ??= px; break;
                case "width": width ??= px; break;
            }
        }

        return minWidth ?? maxWidth ?? width;
    }

    /// <summary>
    /// Dodaj do wizualizacji niestandardowy CSS dla tekstu piosenki
    /// </summary>
    /// <param name="customLyricsCss"></param>
    public void ApplyCustomCss(CustomLyricsCss customLyricsCss)
    {
        if(customLyricsCss?.FontFamily != null)
            this.Add(".lyrics-line", "font-family", customLyricsCss.FontFamily);
        
        if((customLyricsCss?.FontSize ?? 0) > 0)
            this.Add(".lyrics-line", "font-size", $"{customLyricsCss.FontSize}px");
        
        if(customLyricsCss?.TextColor != null)
            this.Add(".lyrics-line", "color", customLyricsCss.TextColor);

        if (customLyricsCss?.ChordColor != null)
        {
            this.Add(".chords", "color", customLyricsCss.ChordColor);
            this.Add(".chords2", "color", customLyricsCss.ChordColor);
        }

        if (customLyricsCss?.ChordFontFamily != null)
        {
            this.Add(".chords2", "font-family", customLyricsCss.ChordFontFamily);
            this.Add(".chords", "font-family", customLyricsCss.ChordFontFamily);
        }

        if ((customLyricsCss?.ChordFontSize ?? 0) != 0)
        {
            var size = 17 + customLyricsCss!.ChordFontSize;
            this.Add(".chords2", "font-size", $"{size}px");
            this.Add(".chords", "font-size", $"{size}px");

        }

        if (customLyricsCss?.BackgroundColor != null)
        {
            this.Add("body", "background-color", customLyricsCss!.BackgroundColor);
        }
    }

}
