using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Chords;
using Zaczy.SongBook.Enums;
using Zaczy.SongBook.Extensions;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace Zaczy.SongBook;

public class SongVisualization
{
    public Dictionary<string, string> CssFontsPath { get; set; } = new Dictionary<string, string>();

    private Dictionary<LyricLineBlockType, int>? _blockTypeCounters;
    
    
    /// <summary>
    /// Ładuje czcionkę z pliku i konwertuje do Base64
    /// </summary>
    private static string GetFontBase64(string fontPath)
    {
        if (File.Exists(fontPath))
        {
            var fontBytes = File.ReadAllBytes(fontPath);
            return Convert.ToBase64String(fontBytes);
        }

        return string.Empty;
    }

    /// <summary>
    /// Tekst piosenki sformatowany jako HTML
    /// </summary>
    /// <param name="song"></param>
    /// <param name="version"></param>
    /// <param name="skipHeaders"></param>
    /// <returns></returns>
    public string LyricsHtml(Song song, LyricsHtmlVersion version = LyricsHtmlVersion.Pre, bool skipHeaders=false)
    {
        if (song.Lines == null || song.Lines.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        sb.AppendLine($"<html><head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<style>");
        sb.AppendLine("html {  }");
        sb.AppendLine("body { padding-bottom: 8em; }");

        if (CssFontsPath?.Count > 0)
        {
            foreach (var font in CssFontsPath)
            {
                var fontBase64 = GetFontBase64(font.Value);
                if (!string.IsNullOrEmpty(fontBase64))
                {
                    sb.AppendLine($@"
                    @font-face {{
                        font-family: '{font.Key}';
                        src: url(data:font/truetype;base64,{fontBase64}) format('truetype');
                        font-weight: normal;
                        font-style: normal;
                    }}");
                }
                sb.AppendLine($@"
                    @font-face {{
                        font-family: '{font.Key}_beta';
                        src: url('{font.Value}') format('truetype');
                        font-weight: normal;
                        font-style: normal;
                    }}");
            }
        }

        sb.AppendLine(@"
            pre {
                font-family: 'CustomFixedFont_beta', Roboto, Consolas, monospace;
                line-height: 1em;
                font-size: 1.1em;
                white-space: pre-wrap;
                word-wrap: break-word;
                font-weight: 500;
                font-stretch: 93%;
            }
            .chords { 
                color: #b62610; 
                font-weight: 700; 
            }");

        sb.AppendLine("H1 { font-family: RobotoVariable_beta; font-stretch: 100%; color: #b62610; }");
        sb.AppendLine("H1 .artist { font-family: RobotoVariable_beta; color: #CCC; font-size: 0.6em; }");
        sb.AppendLine("H2 { font-family: PoltawskiVariable_beta; font-weight: 600; }");
        sb.AppendLine("H2 .artist { color: #CCC; font-size: 0.6em; }");

        sb.AppendLine(".chord-line-block { display: inline-block; position: relative; }");


        sb.AppendLine(".lyrics-line { position: relative; font-family: PoltawskiVariable_beta; font-weight: 500; display: inline-block; }");
        sb.AppendLine(".lyrics-line.annotated { height: 1.2em; margin-top: 0.8em; }");
        //sb.AppendLine(".lyrics-line.annotated .chords { position: relative; top: -1.5em; }");
        sb.AppendLine(@".lyrics-line.annotated .chords2 {  color: #b62610; font-weight: 700; display: inline-block; position: absolute; transform: translateY(-1.0em); white-space: nowrap; font-size: 0.9em; }");

        sb.AppendLine(".block-zwrotka { margin-left: 30px; }");
        // sb.AppendLine(".block-zwrotka .block-header { font-size: 0.8em; background: #ccc; color: #FFF; text-align: right; position: absolute; display: inline-block; transform: translateX(-1.3em) translateY(0.2em); }");
        //sb.AppendLine(".block-zwrotka .block-header { font-size: 0.8em; color: #eee; color: #FFF; text-align: right; position: absolute; display: inline-block; transform: translateX(-1.8em) translateY(0.2em); padding: 2px; padding-left: 7px;padding-right: 5px; }");
        sb.AppendLine(".block-zwrotka .block-header { font-size: 0.7em; color: #CCC; text-align: right; position: absolute; display: inline-block; transform: translateX(-1.8em) translateY(-0.3em); padding: 2px; padding-left: 7px; padding-right: 5px; }");

        sb.AppendLine(".block-refren { margin-left: 70px; border-left: 15px solid #F0F0F0; padding-left: 10px;}");
        //sb.AppendLine(".block-refren .block-header { font-size: 0.6em; color: #CCC; position: absolute; display: inline-block; transform: translateX(-4.1em) translateY(0.5em); }");
        sb.AppendLine(".block-refren .block-header { display: none; }");

        sb.AppendLine(".capo-info { color: #AAA; font-size: 0.8em; margin-bottom: 10px; }");

        sb.AppendLine("@media (max-width: 576px) {");
        sb.AppendLine(".block-zwrotka { margin-left: 5px; }");
        //sb.AppendLine(".block-zwrotka .block-header { font-size: 0.8em; color: #CCC; position: absolute; display: inline-block; transform: translateX(-0.3em) translateY(0.2em); }");
        sb.AppendLine(".block-zwrotka .block-header { font-size: 0.7em; color: #CCC; text-align: right; position: absolute; display: inline-block; transform: translateX(-1.5em) translateY(-0.3em); padding: 2px; padding-left: 7px; padding-right: 5px; }");
        
        
        sb.AppendLine(".block-refren { margin-left: 10px; border-left: 8px solid #F0F0F0; padding-left: 10px; }");
        sb.AppendLine(".block-refren .block-header { display: none; }");
        sb.AppendLine("}");

        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        if(!skipHeaders)
            sb.AppendLine($"<h1>{song.Title} <span class=\"artist\">{song.Artist}</span></h1>");
        
        if(!string.IsNullOrEmpty(song.Capo))
        sb.AppendLine($@"<div class=""capo-info"">Kapodaster: {song.Capo}</div>");

        LyricLineBlockType currentBlockType = LyricLineBlockType.Inne;

        if (version == LyricsHtmlVersion.Pre)
        {
            string lyrics = string.Empty;
            foreach (var line in song.Lines)
            {
                var blockType = RecognizeBlockType(line);

                if(blockType != LyricLineBlockType.Inne)
                {
                    currentBlockType = blockType;
                    continue;
                }

                string spacesBefore = string.Empty;

                if (currentBlockType != LyricLineBlockType.Inne)
                {
                    switch (currentBlockType)
                    {
                        case LyricLineBlockType.Refren:
                            spacesBefore = spacesBefore.PadLeft(5, ' ');
                            break;

                        case LyricLineBlockType.Zwrotka:
                            //spacesBefore = spacesBefore.PadLeft(1, '@');
                            break;
                    }
                }

                if (Chord.IsChordLine(line))
                {
                    lyrics += $"<span class=\"chords\">{spacesBefore}{line}</span>\n";
                }
                else
                {
                    int chordStart = Chord.ChordPartStart(line);

                    if (chordStart > 0)
                    {
                        lyrics += line.Substring(0, chordStart)
                            + "<span class=\"chords\">"
                            + line.Substring(chordStart)
                            + "</span>\n";
                    }
                    else
                        lyrics += spacesBefore+line + "\n";
                }
            }
            sb.AppendLine($"<pre>{lyrics}</pre>");
        }
        else if (version == LyricsHtmlVersion.RelativeHtml)
        {
            sb.AppendLine(TransformToVariableFontVersion(song));
        }

        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    /// <summary>
    /// Wersja dla czcionek zmiennych - akordy nad tekstem
    /// </summary>
    /// <param name="song"></param>
    /// <returns></returns>
    public string TransformToVariableFontVersion(Song song)
    {
        string lyrics = string.Empty;

        if (song?.Lines == null)
            return string.Empty;

        LyricLineBlockType currentBlockType = LyricLineBlockType.Inne;
        LyricLineBlockType? previousBlockType = null;

        bool? firstBlockLine = null;
        bool? firstBlockLyricsLine = null;
        int emptyLineCounter = 0;


        for (int i = 0; i < song.Lines.Count; i++)
        {
            string line = song.Lines[i];
            string? next = i < song.Lines.Count - 1 ? song.Lines[i + 1] : null;


            if (string.IsNullOrWhiteSpace(line))
            {
                emptyLineCounter++;
                if(emptyLineCounter >=2)
                {
                    if (currentBlockType != LyricLineBlockType.Inne)
                        lyrics += "</div>";
                    currentBlockType = LyricLineBlockType.Inne;
                    previousBlockType = null;
                    firstBlockLine = null;
                    firstBlockLyricsLine = null;
                }
                lyrics += "<br/>";
                continue;
            }
            else
            {
                emptyLineCounter = 0;
            }

            var blockType = RecognizeBlockType(line);

            if (blockType != LyricLineBlockType.Inne)
            {
                if(currentBlockType == blockType)
                {
                    lyrics += "</div>";
                }
                currentBlockType = blockType;
                firstBlockLine = true;
                increaseBlockTypeCounter(blockType);
                continue;
            }

            if(firstBlockLine == true)
            {
                if(previousBlockType != null && previousBlockType != currentBlockType)
                {
                    lyrics += "</div>";
                }
                lyrics += $@"<div class=""block-{currentBlockType}"">";

                firstBlockLine = false;
                firstBlockLyricsLine = true;
            }

            string headerPrefix = string.Empty;

            if (Chord.IsChordLine(line))
            {
                string lyricsLine = string.Empty;
                string? nextLine = i < song.Lines.Count - 1 ? song.Lines[i + 1] : null;
                if (!string.IsNullOrEmpty(nextLine) && !Chord.IsChordLine(nextLine))
                {
                    headerPrefix = string.Empty;
                    if (firstBlockLyricsLine == true)
                    {
                        headerPrefix = $@"<div class=""block-header"">{this.blockHeaderText(currentBlockType)}</div>";
                        firstBlockLyricsLine = false;
                    }

                    if (looksLikeLyricsToShort(line, nextLine, 50))
                    {
                        lyricsLine += (!string.IsNullOrEmpty(line) ? $@"<span class=""chords2"">{clearChordForHtml(line)}</span>" : "") + clearLyricsForHtml(nextLine);
                        lyrics += headerPrefix + $@"<span class=""lyrics-line annotated"">{lyricsLine}</span><br/>";

                        i++;
                        continue;
                    }

                    int chordPosition = 0;
                    string nextLineAfter = string.Empty;
                    string lineAfter = string.Empty;
                    int prevChordPosition = 0;

                    string chordPart = string.Empty;
                    string lyricPart = string.Empty;

                    chordPosition = nextChordPosition(line, prevChordPosition);

                    chordPart = line.SubstringSafe(0, chordPosition == -1 ? line.Length : chordPosition);
                    lyricPart = nextLine.SubstringSafe(0, chordPosition == -1 ? nextLine.Length : chordPosition);

                    lineAfter += chordPart + "|";
                    nextLineAfter += lyricPart + "|";

                    // build inline sequence: chord span(s) interleaved with lyric text
                    lyricsLine += (!string.IsNullOrEmpty(chordPart) ? $@"<span class=""chords2"">{clearChordForHtml(chordPart)}</span>" : "") + headerPrefix + clearLyricsForHtml(lyricPart);

                    prevChordPosition = chordPosition + 1;

                    while (chordPosition != -1)
                    {
                        chordPosition = nextChordPosition(line, prevChordPosition);
                        if (chordPosition != -1)
                        {
                            chordPart = line.Substring(prevChordPosition > 0 ? prevChordPosition - 1 : 0, chordPosition - prevChordPosition + 1);
                            lyricPart = nextLine.SubstringSafe(prevChordPosition > 0 ? prevChordPosition - 1 : 0, chordPosition - prevChordPosition + 1);

                            //if(chordPart.Length > lyricPart.Length)
                            //    lyricPart = lyricPart + string.Concat(Enumerable.Repeat("&nbsp;", chordPart.Length-lyricPart.Length));

                            if(string.IsNullOrWhiteSpace(lyricPart))
                            {
                                chordPart = line.SubstringSafe(prevChordPosition > 0 ? prevChordPosition - 1 : 0, line.Length);
                                chordPosition = -1;
                            }

                            prevChordPosition = chordPosition + 1;
                        }
                        else
                        {
                            lyricPart = nextLine.SubstringSafe(prevChordPosition > 0 ? prevChordPosition - 1 : 0, nextLine.Length - prevChordPosition + 1);
                            chordPart = line.SubstringSafe(prevChordPosition > 0 ? prevChordPosition - 1 : 0, line.Length - prevChordPosition + 1);
                        }

                        lineAfter += chordPart + "|";
                        nextLineAfter += lyricPart + "|";

                        lyricsLine += (!string.IsNullOrEmpty(chordPart) ? $@"<span class=""chords2"">{clearChordForHtml(chordPart)}</span>" : "") + clearLyricsForHtml(lyricPart);
                    }

                    lyrics += $@"<span class=""lyrics-line annotated"">{lyricsLine}</span><br/>";

                    i++;
                    previousBlockType = currentBlockType;
                    continue;
                }
            }

            if (Chord.IsChordLine(line))
                lyrics += $@"<span class=""lyrics-line""><span class=""chords"">{clearChordForHtml(line)}</span></span><br/>";
            else
            {
                string? nextLine = i < song.Lines.Count - 1 ? song.Lines[i + 1] : null;
                blockType = RecognizeBlockType(nextLine!);

                int chordStart = Chord.ChordPartStart(line);

                if (chordStart > 0)
                {
                    lyrics += headerPrefix + $@"<span class=""lyrics-line"">{clearLyricsForHtml(line.Substring(0,chordStart))}<span class=""chords"">&nbsp;{clearChordForHtml(line.Substring(chordStart))}</span></span><br/>";

                }
                else if (!string.IsNullOrEmpty(line) || blockType == currentBlockType)
                    lyrics += headerPrefix + $@"<span class=""lyrics-line"">{clearLyricsForHtml(line)}</span><br/>";
            }

            previousBlockType = currentBlockType;

        }

        if (currentBlockType != LyricLineBlockType.Inne)
            lyrics += "</div>";

        if(lyrics.Contains("<br/><br/></div>"))
        {
            lyrics = lyrics.Replace("<br/><br/></div>", "</div><br/><br/>");
        }
        if (lyrics.Contains("<br/></div>"))
        {
            lyrics = lyrics.Replace("<br/></div>", "</div><br/>");
        }
        //if (lyrics.Contains("<br/><br/><div"))
        //{
        //    lyrics = lyrics.Replace("<br/><br/><div", "<br/><div");
        //}

        return lyrics;
    }

    /// <summary>
    /// Tekst nagłówka bloku linii tekstu piosenki
    /// </summary>
    /// <param name="blockType"></param>
    /// <returns></returns>
    private string blockHeaderText(LyricLineBlockType blockType)
    {
        switch (blockType)
        {
            case LyricLineBlockType.Zwrotka:
                if (_blockTypeCounters?.ContainsKey(blockType) == true)
                    return $"{_blockTypeCounters[blockType]}";
                break;

            case LyricLineBlockType.Refren:
                return "refren";
            default:
                return string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Linia tekstu zbyt krótka w stosunku do linii akordów
    /// </summary>
    /// <param name="chordLine"></param>
    /// <param name="lyricLine"></param>
    /// <returns></returns>
    private bool looksLikeLyricsToShort(string chordLine, string lyricLine, int acceptedPercent)
    {
        if (string.IsNullOrEmpty(lyricLine))
            return true;

        chordLine = chordLine.Trim().NormalizeInlineWhitespace();
        lyricLine = lyricLine.Trim().NormalizeInlineWhitespace();

        double percent = 100 * ((double)chordLine.Length / (double)lyricLine.Length);
        if (percent > acceptedPercent)
            return true;

        return false;
    }

    /// <summary>
    /// Wyczyść tekst akordu z niebezpiecznych znaków HTML
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private string clearChordForHtml(string input)
    {
        string ret = input.Trim();
        
        ret = System.Net.WebUtility.HtmlEncode(ret);

        return ret;
    }

    /// <summary>
    /// Wyczyść tekst linii z niebezpiecznych znaków HTML
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private string clearLyricsForHtml(string input)
    {
        string ret = input; // System.Net.WebUtility.HtmlEncode(input);

        if (string.IsNullOrWhiteSpace(ret))
            ret = "&nbsp;";

        return ret;
    }

    /// <summary>
    /// Pozycja następnego akordu w linii
    /// </summary>
    /// <param name="line"></param>
    /// <param name="prevChordStart"></param>
    /// <returns></returns>
    private int nextChordPosition(string line, int prevChordStart)
    {
        if (prevChordStart >= line.Length - 1)
            return -1;

        if (line[prevChordStart] != ' ' && line[prevChordStart] != '\t')
        {
            while (prevChordStart < line.Length && line[prevChordStart] != ' ' && line[prevChordStart] != '\t')
            {
                prevChordStart++;
            }
        }

        for (int i = prevChordStart; i < line.Length; i++)
        {
            if (line[i] == ' ' || line[i] == '\t')
            {
            }
            else
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Rozpoznaje typ bloku linii tekstu piosenki.
    /// Zwraca Zwrotka dla: "zwr.", "zwrotka" lub pojedynczej liczby naturalnej (np. "1" lub "1.")
    /// Zwraca Refren dla: "ref.", "refren"
    /// W przeciwnym wypadku zwraca Inne.
    /// Dodatkowo obsługuje wieloczęściowe linie rozdzielone znakiem '|' np. "Zwrotka 1|Zwrotka 2".
    /// </summary>
    public LyricLineBlockType RecognizeBlockType(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return LyricLineBlockType.Inne;

        var trimmed = line.Trim();
        var normalized = trimmed.NormalizeInlineWhitespace().ToLowerInvariant();
        normalized = normalized.Replace(".", "").Replace(";", "").Replace(":", "");

        // handle pipe-separated sequences like "zwrotka 1|zwrotka 2"
        if (normalized.Contains('|'))
        {
            var parts = normalized.Split('|').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            if (parts.Length > 0)
            {
                if (parts.All(IsZwrotkaSegment))
                    return LyricLineBlockType.Zwrotka;
                if (parts.All(IsRefrenSegment))
                    return LyricLineBlockType.Refren;
            }
        }

        if (IsZwrotkaSegment(normalized))
            return LyricLineBlockType.Zwrotka;

        if (IsRefrenSegment(normalized))
            return LyricLineBlockType.Refren;

        // single natural number, allow optional trailing dot (e.g. "1" or "1.")
        if (Regex.IsMatch(normalized, @"^\d+\.?$"))
            return LyricLineBlockType.Zwrotka;

        return LyricLineBlockType.Inne;

        static bool IsZwrotkaSegment(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (s == "zwr" || s == "zwrotka") return true;
            if (Regex.IsMatch(s, @"^zwrotka\s*\d+\.?$")) return true;
            if (Regex.IsMatch(s, @"^\d+\s*zwrotka\s*\.?$")) return true;
            if (Regex.IsMatch(s, @"^\d+\.?$")) return true; 
            return false;
        }

        static bool IsRefrenSegment(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (s == "ref" || s == "refren") return true;
            if (Regex.IsMatch(s, @"^refren\s*\d+\.?$")) return true; // "refren 1"
            return false;
        }
    }

    /// <summary>
    /// Zwiększ licznik zwrotek
    /// </summary>
    /// <param name="blockType"></param>
    private void increaseBlockTypeCounter(LyricLineBlockType blockType)
    {
        if (_blockTypeCounters == null)
            _blockTypeCounters = new Dictionary<LyricLineBlockType, int>();
     
        if (!_blockTypeCounters.ContainsKey(blockType))
            _blockTypeCounters[blockType] = 0;
        
        _blockTypeCounters[blockType]++;
    }

}
