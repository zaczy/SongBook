using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Chords;
using static System.Net.Mime.MediaTypeNames;

namespace Zaczy.SongBook;

public class SongVisualization
{
    public string MonoFontPath { get; set; } = string.Empty;

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

        if (!string.IsNullOrEmpty(MonoFontPath))
        {
            var fontBase64 = GetFontBase64(MonoFontPath);
            if (!string.IsNullOrEmpty(fontBase64))
            {
                sb.AppendLine($@"
                    @font-face {{
                        font-family: 'CustomFixedFont';
                        src: url(data:font/truetype;base64,{fontBase64}) format('truetype');
                        font-weight: normal;
                        font-style: normal;
                    }}");
            }
        }

        sb.AppendLine(@"
            pre {
                font-family: 'CustomFixedFont', Consolas, monospace;
                font-size: 14px;
                line-height: 1.4;
            }
            .chords { 
                color: magenta; 
                font-weight: bold; 
            }");

        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>{song.Title}</h1>");
        sb.AppendLine($"<h3>{song.Artist}</h3>");

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

                if(chordStart>0)
                {
                    lyrics += line.Substring(0, chordStart )
                        + "<span class=\"chords\">"
                        + line.Substring(chordStart)
                        + "</span>\n";
                }
                else
                    lyrics += line + "\n";
            }
        }

        sb.AppendLine($"<pre>{lyrics}</pre>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

}
