using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Chords;
using Zaczy.SongBook.Extensions;
using static System.Net.Mime.MediaTypeNames;

namespace Zaczy.SongBook;

public class SongVisualization
{
    public Dictionary<string, string> CssFontsPath { get; set; } = new Dictionary<string, string>();

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

    public string LyricsHtml(Song song)
    {
        if (song.Lines == null || song.Lines.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        sb.AppendLine($"<html><head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine("<style>");
        sb.AppendLine("html { font-size: 17px; }");

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
        sb.AppendLine(".lyrics-line.annotated .chords { position: relative; top: -1.5em; }");
        sb.AppendLine(@".lyrics-line.annotated .chords2 {  color: #b62610; 
    font-weight: 700; 
    display: inline-block;
    position: absolute;
    transform: translateY(-1.15em);
    white-space: nowrap; }");
        sb.AppendLine(".lyrics-line { position: relative; font-family: PoltawskiVariable_beta; font-weight: 500; display: inline-block; }");
        sb.AppendLine(".lyrics-line.annotated { height: 1.2em; margin-top: 1.5em; }");

        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>{song.Title} <span class=\"artist\">{song.Artist}</span></h1>");
        //sb.AppendLine($"<h3>{song.Artist}</h3>");

        string lyrics = string.Empty;
        foreach (var line in song.Lines)
        {
            if (Chord.IsChordLine(line))
            {
                lyrics += $"<span class=\"chords\">{line}</span>\n";
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
                    lyrics += line + "\n";
            }
        }
        sb.AppendLine($"<pre>{lyrics}</pre>");

        sb.AppendLine($"<h2>{song.Title} <span class=\"artist\">Variable Font Version</span></h2>");
        sb.AppendLine(TransformToVariableFontVersion(song));

        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    public string TransformToVariableFontVersion(Song song)
    {
        string lyrics = string.Empty;

        if (song?.Lines == null)
            return string.Empty;

        for (int i = 0; i < song.Lines.Count; i++)
        {
            string line = song.Lines[i];

            if (Chord.IsChordLine(line))
            {
                string lyricsLine = string.Empty;
                string? nextLine = i < song.Lines.Count - 1 ? song.Lines[i + 1] : null;
                if (!string.IsNullOrEmpty(nextLine) && !Chord.IsChordLine(nextLine))
                {
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
                    lyricsLine += (!string.IsNullOrEmpty(chordPart) ? $@"<span class=""chords2"">{clearChordForHtml(chordPart)}</span>" : "") + clearLyricsForHtml(lyricPart);

                    prevChordPosition = chordPosition + 1;

                    while (chordPosition != -1)
                    {
                        chordPosition = nextChordPosition(line, prevChordPosition);
                        if (chordPosition != -1)
                        {
                            chordPart = line.Substring(prevChordPosition > 0 ? prevChordPosition - 1 : 0, chordPosition - prevChordPosition + 1);
                            lyricPart = nextLine.SubstringSafe(prevChordPosition > 0 ? prevChordPosition - 1 : 0, chordPosition - prevChordPosition + 1);

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

                    lyrics += $@"<span class=""lyrics-line annotated"">{lyricsLine}</span><br />";

                    i++;
                    continue;
                }
            }

            if(Chord.IsChordLine(line))
                lyrics += $@"<span class=""lyrics-line""><span class=""chords"">{clearChordForHtml(line)}</span></span><br/>";
            else
                lyrics += $@"<span class=""lyrics-line"">{clearLyricsForHtml(line)}</span><br/>";
        }

        return lyrics;
    }

    private string clearChordForHtml(string input)
    {
        string ret = input.Trim();
        
        ret = System.Net.WebUtility.HtmlEncode(ret);

        return ret;
    }

    private string clearLyricsForHtml(string input)
    {
        string ret = System.Net.WebUtility.HtmlEncode(input);

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

}
