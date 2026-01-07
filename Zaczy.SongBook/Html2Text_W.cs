using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zaczy.SongBook.Chords;
using static System.Net.Mime.MediaTypeNames;

namespace Zaczy.SongBook;

public class Html2Text_W : Html2Text
{
    public Html2Text_W(string htmlContent) : base(htmlContent)
    {
    }

    /// <summary>
    /// Pobiera tytuł utworu z tagu H1 (zawartość elementu strong)
    /// </summary>
    /// <returns>Tytuł utworu</returns>
    public string GetTitle()
    {
        if (string.IsNullOrEmpty(HtmlContent))
            return string.Empty;

        var match = Regex.Match(HtmlContent, @"<h1>\s*<strong>([^<]+)</strong>", RegexOptions.Singleline);
        
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    /// <summary>
    /// Pobiera nazwę artysty z tagu H1 (tekst po elemencie strong)
    /// </summary>
    /// <returns>Nazwa artysty</returns>
    public string GetArtist()
    {
        if (string.IsNullOrEmpty(HtmlContent))
            return string.Empty;

        var match = Regex.Match(HtmlContent, @"<h1>\s*<strong>[^<]+</strong>\s*([^<]+)\s*</h1>", RegexOptions.Singleline);
        
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    public string GetCapoInfo()
    {
        if (string.IsNullOrEmpty(HtmlContent))
            return string.Empty;

        var match = Regex.Match(HtmlContent, @"Kapodaster:\s*([^<]+)<br\s*/?>", RegexOptions.Singleline);
        
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    public string GetLyricsAuthor()
    {
        if (string.IsNullOrEmpty(HtmlContent))
            return string.Empty;

        var match = Regex.Match(HtmlContent, @"Tekst piosenki:\s*([^<]+)<br\s*/?>", RegexOptions.Singleline);

        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    public string GetMusicAuthor()
    {
        if (string.IsNullOrEmpty(HtmlContent))
            return string.Empty;

        var match = Regex.Match(HtmlContent, @"Muzyka:\s*([^<]+)<br\s*/?>", RegexOptions.Singleline);

        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }



    /// <summary>
    /// Konwertuje zawartość HTML na tekst z zachowaniem struktury akordów i tekstu
    /// </summary>
    /// <returns></returns>
    public new string ConvertToText()
    {
        if (string.IsNullOrEmpty(HtmlContent))
        {
            return string.Empty;
        }

        var result = new StringBuilder();
        
        var contentMatch = Regex.Match(HtmlContent, 
            //@"<div class=""interpretation-content"">(.*?)</div>\s*</div>", 
            @"<div class=""interpretation-content"">(.*?)</div>", 
            RegexOptions.Singleline);
        
        if (!contentMatch.Success)
            return string.Empty;

        var content = contentMatch.Groups[1].Value;
        
        var lines = Regex.Split(content, @"<br\s*/?>");

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                result.AppendLine();
                continue;
            }

            var sectionMatch = Regex.Match(trimmedLine, @"^<span class='text-muted'>([^<]+)</span>$");
            if (sectionMatch.Success)
            {
                result.AppendLine(sectionMatch.Groups[1].Value);
                continue;
            }

            if (!trimmedLine.Contains("annotated-lyrics") && 
                Regex.IsMatch(trimmedLine, @"<code[^>]*data-chord"))
            {
                if (!this.IsMixedLine(trimmedLine))
                {
                    var chords = ExtractChordsFromLine(trimmedLine);
                    result.AppendLine(chords);
                    continue;
                }
            }

            if (trimmedLine.Contains("annotated-lyrics"))
            {
                var (chordLine, textLine) = ProcessAnnotatedLyricsLine(trimmedLine);
                
                if (!string.IsNullOrWhiteSpace(chordLine))
                    result.AppendLine(chordLine);
                
                result.AppendLine(textLine);
                continue;
            }

            result.AppendLine(StripHtmlTags(trimmedLine));
        }

        return result.ToString().TrimEnd();
    }

    /// <summary>
    /// Sprawdza, czy linia zawiera zarówno tekst, jak i akordy (tekst przed akordami)
    /// </summary>
    /// <param name="trimmedLine">Linia HTML do sprawdzenia</param>
    /// <returns>True jeśli linia zawiera tekst przed akordami</returns>
    private bool IsMixedLine(string trimmedLine)
    {
        // Usuń tagi HTML i pobierz czysty tekst
        var plainText = StripHtmlTags(trimmedLine);
        
        if (string.IsNullOrWhiteSpace(plainText))
            return false;

        // Znajdź pozycję pierwszego akordu w oryginalnej linii HTML
        var firstChordMatch = Regex.Match(trimmedLine, @"<code[^>]*data-chord");
        
        if (!firstChordMatch.Success)
            return false;

        // Pobierz tekst przed pierwszym akordem
        var textBeforeChord = trimmedLine.Substring(0, firstChordMatch.Index);
        var plainTextBeforeChord = StripHtmlTags(textBeforeChord).Trim();

        // Jeśli jest jakikolwiek tekst przed pierwszym akordem, to jest to linia mieszana
        return !string.IsNullOrWhiteSpace(plainTextBeforeChord);
    }

    /// <summary>
    /// Wyodrębnia akordy z linii 
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private string ExtractChordsFromLine(string line)
    {
        var chords = new StringBuilder();
        var matches = Regex.Matches(line, @"<code[^>]*data-local='([^']*)'[^>]*>[^<]*</code>");
        
        foreach (Match match in matches)
        {
            var chord = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(chord))
            {
                if (chords.Length > 0)
                    chords.Append(' ');
                chords.Append(chord);
            }
        }
        
        return chords.ToString();
    }

    /// <summary>
    /// Przetwarza linię z annotated-lyrics, zwracając linię akordów i linię tekstu
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    private (string chordLine, string textLine) ProcessAnnotatedLyricsLine(string line)
    {
        var textBuilder = new StringBuilder();
        var chordBuilder = new StringBuilder();
        
        var innerContent = Regex.Replace(line, @"<span class='annotated-lyrics'>", "");
        innerContent = Regex.Replace(innerContent, @"</span>$", "");
        
        innerContent = Regex.Replace(innerContent, @"<span class='text-nowrap'>([^<]*(?:<code[^>]*>[^<]*</code>[^<]*)*)</span>", "$1");
        
        int currentTextPos = 0;
        int lastIndex = 0;
        
        var codePattern = @"<code[^>]*data-local='([^']*)'[^>]*>[^<]*</code>";
        var matches = Regex.Matches(innerContent, codePattern);
        
        foreach (Match match in matches)
        {
            var textBefore = innerContent.Substring(lastIndex, match.Index - lastIndex);
            textBefore = StripHtmlTags(textBefore);
            textBuilder.Append(textBefore);
            currentTextPos += textBefore.Length;
            
            var chord = match.Groups[1].Value.Trim();
            
            while (chordBuilder.Length < currentTextPos)
                chordBuilder.Append(' ');
            
            chordBuilder.Append(chord);
            
            lastIndex = match.Index + match.Length;
        }
        
        // Tekst po ostatnim akordzie
        var remainingText = innerContent.Substring(lastIndex);
        remainingText = StripHtmlTags(remainingText);
        textBuilder.Append(remainingText);
        
        return (chordBuilder.ToString().TrimEnd(), textBuilder.ToString().Trim());
    }

    /// <summary>
    /// Oczyść tekst z tagów HTML
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    private string StripHtmlTags(string html)
    {
        var text = Regex.Replace(html, @"<[^>]+>", "");
        return System.Net.WebUtility.HtmlDecode(text);
    }
}
