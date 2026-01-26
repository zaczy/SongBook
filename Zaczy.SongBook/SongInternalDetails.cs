using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zaczy.SongBook.Chords;
using Zaczy.SongBook.Enums;
using Zaczy.SongBook.Extensions;

namespace Zaczy.SongBook;

public class SongInternalDetails
{
    public ChordsPosition ChordsPosition { get; set; }

    public HashSet<string> Chords { get; set; } = new();

    public int ChordLinesAboveLyricsCount { get; set; }
    public int ChordLinesAfterText { get; set; }

    Dictionary<string, string> SpecialChordsSuggestions { get; set; } = new();
    public void AddChords(IEnumerable<string> chords)
    {
        foreach (var chord in chords)
        {
            if(!Chords.Contains(chord))
                Chords.Add(chord);
        }
    }

    /// <summary>
    /// Analizuje piosenkę i zwraca szczegóły wewnętrzne dotyczące pozycji akordów oraz unikalnych akordów użytych w piosence.
    /// </summary>
    /// <param name="song"></param>
    /// <returns></returns>
    public static SongInternalDetails? AnalyseSong(Song song)
    {
        if(song == null || song.Lines == null || song.Lines.Count == 0)
        {
            return null;
        }

        var songInternalDetails = new SongInternalDetails();

        if (song?.Lines != null)
        {
            for (int i = 0; i < song.Lines.Count; i++)
            {
                string line = song.Lines[i];
                string? nextLine = i < song.Lines.Count - 1 ? song.Lines[i + 1] : string.Empty;

                if (Chord.IsChordLine(line) && !Chord.IsChordLine(nextLine))
                {
                    var chords = Chord.ExtractChordsFromLine(line);
                    songInternalDetails.AddChords(chords);
                    songInternalDetails.ChordLinesAboveLyricsCount++;
                }
                else if (Chord.ChordPartStart(line) > 1)
                {
                    var chords = Chord.ExtractChordsFromLine(line);
                    songInternalDetails.AddChords(chords);
                    songInternalDetails.ChordLinesAfterText++;
                }
            }
        }

        songInternalDetails.SpecialChordsSuggestions = ChordsSuggestions(song);

        return songInternalDetails;
    }

    public string? GetChordSuggestion(string chord)
    {
        if(SpecialChordsSuggestions.ContainsKey(chord))
        {
            return SpecialChordsSuggestions[chord];
        }
        return null;
    }

    /// <summary>
    /// Wydobywa sugestie wariantów akordów z właściwości ChordsVariations piosenki.
    /// </summary>
    /// <param name="song"></param>
    /// <returns></returns>
    public static Dictionary<string, string> ChordsSuggestions(Song? song)
    {
        if(song == null)
        {
            return new Dictionary<string, string>();
        }

        var suggestions = new Dictionary<string, string>();

        if(!string.IsNullOrEmpty(song?.ChordsVariations))
        { 
            song.ChordsVariations.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToList()
                .ForEach(line =>
                {
                    var parts = line.Split(new[] { ':', '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var chordName = parts[0].Trim() ?? String.Empty;
                        var chordDiagram = parts[1].Trim() ?? String.Empty;
                        if (!string.IsNullOrEmpty(chordName) && !string.IsNullOrEmpty(chordDiagram) && Chord.IsChord(chordName))
                        {
                            suggestions.Add(chordName, chordDiagram);
                        }
                    }
                });
        }

        return suggestions;
    }


    /// <summary>
    /// Rozpoznaje typ bloku linii tekstu piosenki.
    /// Zwraca Zwrotka dla: "zwr.", "zwrotka" lub pojedynczej liczby naturalnej (np. "1" lub "1.")
    /// Zwraca Refren dla: "ref.", "refren"
    /// W przeciwnym wypadku zwraca Inne.
    /// Dodatkowo obsługuje wieloczęściowe linie rozdzielone znakiem '|' np. "Zwrotka 1|Zwrotka 2".
    /// </summary>
    public static LyricLineBlockType RecognizeBlockType(List<string> lines, int i)
    {
        if(i>=lines.Count || i<0)
            return LyricLineBlockType.Inne;

        string line = lines[i];

        if (string.IsNullOrWhiteSpace(line))
            return LyricLineBlockType.Inne;

        var trimmed = (line?.Trim() ?? string.Empty);
        var normalized = trimmed?.NormalizeInlineWhitespace()?.ToLowerInvariant() ?? string.Empty;
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

        if (IsTabulaturaSegment(lines, i))
            return LyricLineBlockType.Tabulatura;

        if(normalized == "solo")
            return LyricLineBlockType.Solo;

        if (normalized == "wstęp")
            return LyricLineBlockType.Solo;

        if (normalized == "recytacja")
            return LyricLineBlockType.Recytacja;


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

    private static bool IsTabulaturaSegment(List<string> lines, int i)
    {
        if (lines == null || i < 0 || i >= lines.Count)
            return false;

        int tabuLines = TabulaturaSucceedingLines(lines, i);

        return tabuLines >= 3;
    }

    public static int TabulaturaSucceedingLines(List<string> lines, int i)
    {
        var tabStartRegex = new Regex(@"^[A-Ha-h][0-9~|-]*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        int tabuLines = 0;

        for (int struna = 0; struna < 6; struna++)
        {
            var line = i + struna < lines.Count ? lines[i + struna] : string.Empty;

            var trimmedStart = line.TrimStart();
            if (string.IsNullOrEmpty(trimmedStart))
                break;

            if (tabStartRegex.IsMatch(trimmedStart))
                tabuLines++;
            else
                break;
        }

        return tabuLines;
    }
}
