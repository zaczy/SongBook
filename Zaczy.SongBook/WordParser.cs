using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        var body = doc.MainDocumentPart.Document.Body;

        foreach (var para in body.Elements<Paragraph>())
        {
            string trimmed = para.InnerText?.Trim();
            //string text = para.InnerText;
            string text = GetParagraphTextPreserveBreaks(para).TrimEnd('\r', '\n');
            int emptyLines = 0;
            if (string.IsNullOrEmpty(trimmed))
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

            var subLines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            string? style = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;

            foreach (var line in subLines)
            {
                AnalyzeLine(line, style);
            }
        }

        if (currentSong != null)
            songs.Add(currentSong);

        return songs;
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
                        songs.Add(currentSong);

                    currentSong = new Song
                    {
                        Title = text,
                        Lines = new List<string>(),
                        Source = "Śpiewnik RŻ"
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
}
