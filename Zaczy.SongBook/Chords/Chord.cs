using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Chords;

public class Chord
{
    public string? Name { get; set; }
    public string Fingering { get; set; } = string.Empty;

    // Skala chromatyczna (notacja polska z H)
    private static readonly string[] ChromaticScale = 
        ["C", "C#", /*"Cis", */"D", "D#", /*"Dis",*/ "E", "F", "F#", /*"Fis",*/ "G", "G#", /*"Gis",*/ "A", /*"As",*/ "B", "H"];
    
    // Alternatywne nazwy z bemolami
    private static readonly Dictionary<string, string> FlatToSharp = new()
    {
        ["Db"] = "C#", ["Eb"] = "D#", ["Fb"] = "E", ["Gb"] = "F#", 
        ["Ab"] = "G#", ["Bb"] = "A#", ["Hb"] = "A#", ["Cb"] = "H", 
        ["Cis"] = "C#", ["Dis"] = "D#", ["Fis"] = "F#", ["Gis"] = "G#", ["As"] = "Gis"
    };

    public static bool IsChord(string text)
    {
        var validChords = new HashSet<string>
        {
            "C", "Cm", "D", "Dm", "E", "Em", "F", "Fm", "G", "Gm", "A", "Am", "B", "Bm", "H", "Hm"
        };

        if (validChords.Contains(text) || validChords.Contains(text.ToUpper()))
        {
            return true;
        }

        // Check for chords with suffixes like 7, maj7, sus4, etc.
        var chordPattern = @"^[A-Ha-h](#|b)?(m|min|maj|dim|aug)?(2|4|5|6|7|9|11|13)?(sus2|sus4|add9|add11|maj7|min7|dim7|aug7|is|\+)?$";
        
        if (Regex.IsMatch(text, chordPattern))
        {
            return true;
        }
        else if (Regex.IsMatch(text.Replace("is",""), chordPattern))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Transponuje pojedynczy akord o wskazaną liczbę półtonów
    /// </summary>
    /// <param name="chord">Akord do transpozycji</param>
    /// <param name="semitones">Liczba półtonów (dodatnia = w górę, ujemna = w dół)</param>
    /// <returns>Transponowany akord</returns>
    public static string Transpose(string chord, int semitones)
    {
        if (string.IsNullOrEmpty(chord) || !IsChord(chord))
            return chord;

        bool HashIsIsMode = chord.Contains("is");

        // Wyodrębnij podstawę akordu i sufiks
        var match = Regex.Match(chord.Replace("is","#"), @"^([A-Ha-h](#|b|is)?)(.*)$");
        if (!match.Success)
            return chord;

        var root = match.Groups[1].Value;
        var suffix = match.Groups[3].Value;
        
        // Zachowaj informację czy była mała litera (notacja polska)
        bool isLowerCase = char.IsLower(root[0]);
        root = char.ToUpper(root[0]) + root.Substring(1);

        // Zamień bemole na krzyżyki
        if (FlatToSharp.TryGetValue(root, out var sharpEquivalent))
        {
            root = sharpEquivalent;
        }

        // Znajdź indeks w skali chromatycznej
        int index = Array.IndexOf(ChromaticScale, root);
        if (index == -1)
            return chord;

        // Oblicz nowy indeks
        int newIndex = ((index + semitones) % 12 + 12) % 12;
        var newRoot = ChromaticScale[newIndex];

        // Przywróć małą literę jeśli była
        if (isLowerCase)
        {
            newRoot = newRoot.ToLower();
        }

        return newRoot + (HashIsIsMode ? suffix.Replace("#", "is") : suffix);

    }

    /// <summary>
    /// Transponuje wszystkie akordy w linii
    /// </summary>
    /// <param name="line">Linia z akordami</param>
    /// <param name="semitones">Liczba półtonów</param>
    /// <returns>Linia z transponowanymi akordami</returns>
    public static string TransposeLine(string line, int semitones)
    {
        if (string.IsNullOrEmpty(line))
            return line;

        var result = new StringBuilder();
        var tokens = Regex.Split(line, @"(\s+)");

        foreach (var token in tokens)
        {
            if (IsChord(token))
            {
                result.Append(Transpose(token, semitones));
            }
            else
            {
                result.Append(token);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Czy linia składa się wyłącznie z akordów
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static bool IsChordLine(string line)
    {
        var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return false;
        foreach (var token in tokens)
        {
            if (!IsChord(token))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Czy linia od pozycji charNo zawiera wyłącznie akordy
    /// </summary>
    /// <param name="part">Linia tekstu</param>
    /// <param name="charNo">Pozycja początkowa</param>
    /// <returns>True jeśli część linii od charNo zawiera wyłącznie akordy</returns>
    public static bool IsChordLinePart(string part, int charNo)
    {
        if (string.IsNullOrEmpty(part) || charNo < 0 || charNo >= part.Length)
            return false;

        var substring = part.Substring(charNo);
        return IsChordLine(substring);
    }

    /// <summary>
    /// Zwraca pozycję w linii, od której występują wyłącznie akordy
    /// </summary>
    /// <param name="text">Linia tekstu</param>
    /// <returns>Pozycja początkowa akordów lub -1 jeśli nie znaleziono</returns>
    public static int ChordPartStart(string text)
    {
        if (string.IsNullOrEmpty(text))
            return -1;

        // Jeśli cała linia to akordy, zwróć 0
        if (IsChordLine(text))
            return 0;

        // Szukaj pozycji, od której zaczyna się część z akordami
        for (int i = 1; i < text.Length; i++)
        {
            // Sprawdź tylko pozycje po spacji lub tabulatorze (granice słów)
            if (text[i - 1] == ' ' || text[i - 1] == '\t')
            {
                if (IsChordLinePart(text, i))
                    return i;
            }
        }

        return -1;
    }
}
