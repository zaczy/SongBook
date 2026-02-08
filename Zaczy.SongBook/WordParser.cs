using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Chords;

namespace Zaczy.SongBook;

public class WordParser
{
    List<Song> songs = new();
    Song? currentSong = null;

    /// <summary>
    /// Parsuj plik piosenki w formacie .docx
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public List<Song> ParseFile(string filename)
    {
        using var doc = WordprocessingDocument.Open(filename, false);
        var body = doc?.MainDocumentPart?.Document?.Body;

        if(body == null)
            throw new Exception("Nie można odczytać zawartości dokumentu.");

        bool startsWithTab = false;
        bool startsWithTabBefore = false;
        int previousParagraphSpacing = 0;
        foreach (var para in body.Elements<Paragraph>())
        {
            string? trimmed = para.InnerText?.Trim();
            //string text = para.InnerText;
            string text = GetParagraphTextPreserveBreaks(para).TrimEnd('\r', '\n');

            var subLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            string? style = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

            // returns spacing-before in twentieths of a point (20 => 1pt). 0 if not set.
            int spacingBefore = GetSpacingBeforeTwentieths(para, doc!.MainDocumentPart);
            bool hasSpacingBefore = spacingBefore > 4 && spacingBefore != 12*20 && spacingBefore != previousParagraphSpacing;
            if (hasSpacingBefore)
            {
                //System.Diagnostics.Debug.WriteLine($"Paragraph with spacing-before: {spacingBefore} (twentieths of a point)");
                if(currentSong?.LinesLazyLoad != null)
                    currentSong.LinesLazyLoad.Add(String.Empty);
            }
            previousParagraphSpacing = spacingBefore;

            foreach (var line in subLines)
            {
                int emptyLines = 0;
                if (string.IsNullOrEmpty(line))
                {
                    if (currentSong == null)
                        continue;
                    emptyLines++;
                    if (emptyLines >= 2)
                        continue;
                }
                else
                {
                    emptyLines = 0;
                }

                AnalyzeLine(line, style);
            }

            if(subLines.Length > 1 && currentSong != null)
            {
                currentSong.LinesLazyLoad!.Add(string.Empty);
            }

            startsWithTabBefore = startsWithTab;
        }

        if (currentSong != null)
        {
            this.FormatSong(currentSong);
            songs.Add(currentSong);
        }

        return songs;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="currentSong"></param>
    /// <returns></returns>
    private Song FormatSong(Song currentSong)
    {
        if (currentSong.Lines == null)
            return currentSong;

        // IV. Wyczyszczenie nadmiarowych pustych linii
        var lines = this.fixEmptyLines(currentSong.Lines!);

        bool startsWithTabLine = lines.Count>1 && lines[0].StartsWith("\t");

        // II. Dodanie dodatkowych pustych linii między refrenami a zwrotką
        lines = this.addEmptyLinesBetweenChorusesAndVerses(lines);

        // I. Wiodące znaki tabulacji
        lines = this.fixLeadingTabs(lines);

        // I. Wyliczenie długości największej części tekstowej przed akordami, aby wyrównać akordy do prawej strony
        int maxLength = 0;
        foreach (var line in lines)
        {
            var start = Chord.ChordPartStart(line.Replace("\t"," "));
            if(start > 2)
                maxLength = Math.Max(maxLength, start);
        }

        /// III. Wyrównanie akordów
        bool containsZwrotkaInfo = false;
        bool startsWithZwrotkaInfo = false;

        if (maxLength > 0)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Replace("\t"," ");
                if (line.StartsWith("zwr.") || line.StartsWith("ref."))
                {
                    containsZwrotkaInfo = true;
                    startsWithZwrotkaInfo = (i == 0);
                }

                var start = Chord.ChordPartStart(line);
                if (start > 2)
                {
                    var textPart = line.Substring(0, start);
                    var chordPart = line.Substring(start);
                    var spacesToAdd = maxLength - start+1;
                    if (spacesToAdd > 0)
                    {
                        textPart = textPart + new string(' ', spacesToAdd);
                        lines[i] = textPart + chordPart.Trim();
                    }
                }
            }
        }

        if(containsZwrotkaInfo && !startsWithZwrotkaInfo)
        {
            lines.Insert(0, startsWithTabLine ? "ref." : "zwr.");
        }

        currentSong.Lines = lines;

        return currentSong;
    }

    /// <summary>
    /// Operacje na zwrotkach i refrenach - dodanie pustej linii przed zwrotką, dodanie "zwr." lub "ref." przed zwrotką lub refrenem
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    private List<string> addEmptyLinesBetweenChorusesAndVerses(List<string> lines)
    {
        var listAfter = new List<string>();
        string previousLine = string.Empty;
        bool prevStartsWithTab = false;

        foreach (var line in lines)
        {
            if (line.Trim() == string.Empty)
            {
                listAfter.Add(line);
                previousLine = line;
                continue;
            }

            bool startsWithTab = line.StartsWith("\t"); 

            if(startsWithTab != prevStartsWithTab)
            {
                if(!string.IsNullOrEmpty(previousLine.Trim()))
                {
                    listAfter.Add(string.Empty);
                }
                listAfter.Add(!startsWithTab ? "zwr." : "\tref.");
            }

            previousLine = line.Trim();
            if(line.Trim() != string.Empty)
                prevStartsWithTab = startsWithTab;

            listAfter.Add(line);
        }

        return listAfter;
    }

    /// <summary>
    /// Popraw wiodące tabulatury
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    private List<string> fixLeadingTabs(List<string> lines)
    {
        var newList = new List<string>();

        foreach (var line in lines)
        {
            string text = line;
            if (text.StartsWith("\t"))
            {
                string trimmed = text.TrimStart('\t');
                for (int i = 0; i < text.TakeWhile(c => c == '\t').Count(); i++)
                {
                    trimmed = "    " + trimmed;
                }
                text = trimmed;
            }

            newList.Add(text);
        }

        return newList;
    }

    /// <summary>
    /// Usuwa wielokrotne puste linie
    /// </summary>
    /// <param name="lines"></param>
    /// <returns></returns>
    private List<string> fixEmptyLines(List<string> lines)
    {
        var linesAfter = new List<string>();
        bool firstLine = true;
        int emptyLines = 0;
        foreach (var line in lines!)
        {
            if (firstLine && string.IsNullOrEmpty(line.Trim()))
            {
                continue;
            }

            firstLine = false;

            if (string.IsNullOrEmpty(line.Trim()))
            {
                emptyLines++;
                if (emptyLines >= 2)
                    continue;
            }
            else
            {
                emptyLines = 0;
            }

            linesAfter.Add(line);
        }

        for(int i= linesAfter.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrEmpty(linesAfter[i].Trim()))
            {
                linesAfter.RemoveAt(i);
            }
            else
            {
                break;
            }
        }


        return linesAfter;
    }

    /// <summary>
    /// Autorzy piosenki i inne metadane ze stylem PiosenkaAutorzy
    /// </summary>
    /// <param name="line"></param>
    /// <param name="song"></param>
    private void ParseMetadataLine(string line, Song song)
    {
        if (line.ToLower().StartsWith("sł. i muz."))
        {
            song.MusicAuthor = line.Replace("sł. i muz.", "").Replace(":","").Trim();
            song.LyricsAuthor = line.Replace("sł. i muz.", "").Replace(":","").Trim();
        }

        else if (line.ToLower().StartsWith("muzyka:") || line.ToLower().StartsWith("muz"))
            song.MusicAuthor = line.Replace("muz.", "").Replace("muzyka:", "").Replace(":", "").Trim();
        else if (line.ToLower().StartsWith("słowa") || line.ToLower().StartsWith("sł."))
            song.LyricsAuthor = line.Replace("sł.", "").Replace("słowa:", "").Replace(":", "").Trim();
        else if (line.ToLower().StartsWith("wykonawca"))
            song.Artist = line.Replace("wykonawca:", "").Replace(":", "").Trim();
        else if (line.ToLower().StartsWith("tłum."))
            song.MoreInfo = line.Replace("tłum.", "").Replace(":", "").Trim();
        else
            song.Artist = line;
    }

    /// <summary>
    /// W zależności od stylu akapitu analizuje linię tekstu i przypisuje do odpowiednich właściwości piosenki
    /// </summary>
    /// <param name="text"></param>
    /// <param name="style"></param>
    private void AnalyzeLine(string text, string? style)
    {
        if (!string.IsNullOrEmpty(text))
        {
            switch (style)
            {
                case "Nagwek2": // np. Tytuł piosenki
                    if (currentSong != null)
                    {
                        songs.Add(this.FormatSong(currentSong));
                    }

                    currentSong = new Song
                    {
                        Title = text,
                        Lines = new List<string>(),
                        Source = "Śpiewnik RŻ",
                        ScrollingDelay = 0
                    };
                    break;

                case "PiosenkaAutorzy": // np. Autor
                    if (currentSong != null)
                    {
                        if (text.Contains(","))
                        {
                            var parts = text.Split(',');
                            foreach (var p in parts)
                            {
                                ParseMetadataLine(p.Trim(), currentSong);
                            }
                        }
                        else
                            ParseMetadataLine(text, currentSong);
                    }
                    break;

                case "Normal": // Treść piosenki
                default:
                    currentSong!.LinesLazyLoad!.Add(text);
                    break;
            }
        }
        else
            currentSong!.LinesLazyLoad!.Add(string.Empty);
    }

    /// <summary>
    /// Treść paragrafu z zachowaniem podziałów linii (Break) i tabulatorów (TabChar)
    /// </summary>
    /// <param name="paragraph"></param>
    /// <returns></returns>
    private static string GetParagraphTextPreserveBreaks(Paragraph paragraph)
    {
        var sb = new StringBuilder();

        foreach (var run in paragraph.Elements<Run>())
        {
            foreach (var element in run.ChildElements)
            {
                switch (element)
                {
                    case Text t:
                        sb.Append(t.Text);
                        break;

                    case Break:
                        sb.AppendLine(); // albo '\n'
                        break;

                    case TabChar:
                        sb.Append('\t');
                        break;
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns effective spacing-before value (twentieths of a point) for a paragraph:
    /// - first checks paragraph properties (<w:spacing w:before="..."/>)
    /// - if not present checks paragraph style and follows BasedOn chain
    /// </summary>
    private static int GetSpacingBeforeTwentieths(Paragraph paragraph, MainDocumentPart? mainPart)
    {
        // Check direct paragraph property
        var beforeVal = paragraph.ParagraphProperties?.SpacingBetweenLines?.Before?.Value;
        if (!string.IsNullOrEmpty(beforeVal) && int.TryParse(beforeVal, out var v))
            return v;

        // If not set on paragraph, try to resolve from paragraph style (and basedOn chain)
        try
        {
            if (mainPart?.StyleDefinitionsPart?.Styles == null)
                return 0;

            var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            if (string.IsNullOrEmpty(styleId))
                return 0;

            var styles = mainPart.StyleDefinitionsPart.Styles.Elements<Style>();
            Style? style = styles.FirstOrDefault(s => s.StyleId?.Value == styleId);
            // walk basedOn chain
            while (style != null)
            {
                var styleBefore = style.StyleParagraphProperties?.SpacingBetweenLines?.Before?.Value;
                if (!string.IsNullOrEmpty(styleBefore) && int.TryParse(styleBefore, out var sv))
                    return sv;

                var basedOnId = style.BasedOn?.Val?.Value;
                if (string.IsNullOrEmpty(basedOnId))
                    break;
                style = styles.FirstOrDefault(s => s.StyleId?.Value == basedOnId);
            }
        }
        catch
        {
            // ignore and return 0 if anything fails
        }

        return 0;
    }
}
