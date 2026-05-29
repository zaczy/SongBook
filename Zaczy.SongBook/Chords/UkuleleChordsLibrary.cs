using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Chords;

public class UkuleleChordsLibrary : IChordsLibrary
{
    private int _stringsCount = 4;

    public static Dictionary<string, string?> ChordsDict = new Dictionary<string, string?>
    {
        { "C", "0003" },
        { "D", "2220" },
        { "E", "1402" },
        { "F", "2010" }
    };

    /// <summary>
    /// Akord na podstawie zapisu tekstowego (np. x02220)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="definition"></param>
    /// <returns></returns>
    public ChordBase? ChordByAscii(string name, string definition)
    {
        int barFret = -1;
        if (definition?.StartsWith("#") == true && definition?.Length == _stringsCount+2)
        {
            char c = definition[1];
            barFret = c >= 'a' ? c - 'a' + 10 : c - '0';
            definition = definition.Substring(2, _stringsCount);
        }

        if (string.IsNullOrEmpty(definition) || definition.Length != _stringsCount)
            return null;

        var predefined = ChordsDict.FirstOrDefault(x => x.Value == definition && x.Key == name);

        if (!string.IsNullOrEmpty(predefined.Key))
            return StandardChord(predefined.Key, predefined.Value);

        GuitarChord chord = new GuitarChord(name);
        if (barFret > 0)
        {
            chord.Tones.Add(new GuitarChordTone(fret: barFret) { Crossbar = true });
        }

        for (int i = 0; i < definition.Length; i++)
        {
            char c = definition[i];
            int stringNumber = _stringsCount - i;
            if (c == 'x' || c == 'X')
            {
                chord.MutedStrings.Add(stringNumber);
            }
            else if (c == '0')
            {
                if (barFret <= 0)
                    chord.OpenStrings.Add(stringNumber);
            }
            else if (char.IsAsciiHexDigit(c))
            {
                int fret = c >= 'a' ? c - 'a' + 10 : c - '0';
                //int fret = int.Parse(c.ToString());
                chord.Tones.Add(new GuitarChordTone(stringNumber, finger: 0, fret: fret));
            }
            else
            {
                // Invalid character
                return null;
            }
        }

        return chord;
    }

    /// <summary>
    /// Chwyt predefiniowany
    /// </summary>
    /// <param name="name"></param>
    /// <param name="variation"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ChordBase? StandardChord(string name, string? variation = null)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        UkuleleChord? chord = null;

        switch (name)
        {
            case "C":
                chord = new UkuleleChord(name, openStrings: new HashSet<int> { 4, 3, 3 }, mutedStrings: new HashSet<int> {  });
                chord.Tones.Add(new GuitarChordTone(1, 3, 3));  // struna, palec, próg
                break;

            case "c":
                chord = new UkuleleChord("c") { MutedStrings = new HashSet<int> { 4 }, OpenStrings = new HashSet<int> { } };
                chord.Tones.Add(new GuitarChordTone(3));  // struna , palec , próg 
                break;

            case "C9":
                chord = new UkuleleChord("C9") { MutedStrings = new HashSet<int> {  }, OpenStrings = new HashSet<int> { 4,2 } };
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(1, 1, 1));  // struna 4, palec 2, próg 2
                break;

            case "C6":
                chord = new UkuleleChord("C6") { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 4, 3, 2 , 1} };
                break;

            case "C7":
                chord = new UkuleleChord("C7") { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 4, 3, 2 } };
                chord.Tones.Add(new GuitarChordTone(1, 1, 1));  // struna , palec , próg 
                break;

            case "c7":
                chord = new UkuleleChord("c7") { MutedStrings = new HashSet<int> {  }, OpenStrings = new HashSet<int> {  } };
                chord.Tones.Add(new GuitarChordTone(4));  // struna , palec , próg 
                break;

            case "Cis":
                chord = StandardChord("C") as UkuleleChord;
                chord!.TransponeUpBar(1);
                break;

            case "cis":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { } };
                chord.Tones.Add(new GuitarChordTone(4));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(4, 3, 6));  // struna , palec , próg 
                break;

            case "cis7":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { } };
                chord.Tones.Add(new GuitarChordTone(4));  // struna , palec , próg 
                break;

            case "D":
                chord = new UkuleleChord("D") { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 1 } };
                chord.Tones.Add(new GuitarChordTone(4, 1, 2));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(2, 3, 2));  // struna , palec , próg 
                break;

            case "D7":
                chord = new UkuleleChord("D7") { MutedStrings = new HashSet<int> { 4 }, OpenStrings = new HashSet<int> { 3, 1 } };
                chord.Tones.Add(new GuitarChordTone(4, 1, 2));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(2, 3, 2));  // struna , palec , próg 
                break;

            case "d":
                chord = new UkuleleChord("d") { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 1 } };
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna 1, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 3, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna 5, palec 3, próg 3
                break;

            case "d7":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { } };
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna 1, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 3, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna 5, palec 3, próg 3
                chord.Tones.Add(new GuitarChordTone(1, 4, 3));  // struna 5, palec 3, próg 3
                break;

            case "Dis":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 4 } };
                chord.Tones.Add(new GuitarChordTone(3, 2, 3));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(3, 3, 3));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(1, 1, 1));  // struna , palec , próg 
                break;

            case "Dis7":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { } };
                chord.Tones.Add(new GuitarChordTone(1, 2, 4));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(3));  // struna , palec , próg 
                break;

            case "dis":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { } };
                chord.Tones.Add(new GuitarChordTone(4, 3, 3));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(3, 4, 3));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(2, 1, 2));  // struna , palec , próg 
                chord.Tones.Add(new GuitarChordTone(1, 1, 1));  // struna , palec , próg 
                break;

            case "dis7":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { } };
                chord.Tones.Add(new GuitarChordTone(4, 2, 3));  // struna 1, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 3, 3));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(2, 1, 2));  // struna 5, palec 3, próg 3
                chord.Tones.Add(new GuitarChordTone(1, 4, 4));  // struna 5, palec 3, próg 3
                break;

            case "E":
                chord = new UkuleleChord("E") { MutedStrings = new HashSet<int> {  }, OpenStrings = new HashSet<int> {  2} };
                chord.Tones.Add(new GuitarChordTone(4, 1, 1));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(3, 4, 4));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(1, 2, 2));  // struna palec próg 
                break;

            case "E7":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 2 } };
                chord.Tones.Add(new GuitarChordTone(4, 1, 1));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(1, 3, 2));  // struna palec próg 
                break;


            case "e":
                chord = new UkuleleChord("e") { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 4 } };
                chord.Tones.Add(new GuitarChordTone(3, 3, 4));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(2, 2, 2));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(1, 1, 2));  // struna palec próg 
                break;

            case "e7":
                chord = new UkuleleChord("e7") { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> {2, 4 } };
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(1, 3, 2));  // struna palec próg 
                break;


            case "F":
                chord = new UkuleleChord("F") { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 4 } };
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna 4, palec 2, próg 2
                break;

            case "f":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 3} };
                chord.Tones.Add(new GuitarChordTone(4, 1, 1));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(2, 2, 1));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(1, 4, 3));  // struna 4, palec 2, próg 2
                break;

            case "f7":
                chord = StandardChord("e7") as UkuleleChord;
                chord!.TransponeUpBar(1);
                break;

            case "Fis":
                chord = StandardChord("F") as UkuleleChord;
                chord!.TransponeUpBar(1);
                break;

            case "Fis7":
                chord = new UkuleleChord(name) { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> {  } };
                chord.Tones.Add(new GuitarChordTone(4, 2, 3));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 3, 4));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(2, 1, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(1, 4, 3));  // struna 4, palec 2, próg 2
                break;

            case "fis":
                chord = new UkuleleChord("fis") { MutedStrings = new HashSet<int> { }, OpenStrings = new HashSet<int> { 1 } };
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 1, 1));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(2, 3, 2));  // struna 4, palec 2, próg 2
                break;

            case "fis7":
                chord = StandardChord("e7") as UkuleleChord;
                chord!.TransponeUpBar(2);
                break;

            case "G":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> { 4 } };
                chord.Tones.Add(new GuitarChordTone(1, 2, 2));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(3, 1, 2));  // struna 5, palec 3, próg 3
                break;

            case "G7":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> { 4 } };
                chord.Tones.Add(new GuitarChordTone(1, 2, 2));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(3, 1, 2));  // struna 5, palec 3, próg 3
                break;

            case "g":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> { 4 } };
                chord.Tones.Add(new GuitarChordTone(1, 1, 1));  // struna 1, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna 5, palec 3, próg 3
                break;

            case "g7":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> { 4 } };
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 1, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(1, 1, 1));  // struna 5, palec 3, próg 3
                break;

            case "Gis":
                chord = StandardChord("F") as UkuleleChord;
                chord!.TransponeUpBar(3);
                break;

            case "Gis7":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> {  } };
                chord.Tones.Add(new GuitarChordTone(4, 1, 1));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 3, 3));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(2, 2, 2));  // struna 5, palec 3, próg 3
                chord.Tones.Add(new GuitarChordTone(1, 4, 3));  // struna 5, palec 3, próg 3
                break;

            case "gis":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> { 4 } };
                chord.Tones.Add(new GuitarChordTone(4, 1, 1));  // struna 1, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 3, 3));  // struna 1, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(2, 4, 4));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(1, 2, 2));  // struna 5, palec 3, próg 3
                break;

            case "A":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> { 1,2 } };
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(3, 1, 1));  // struna, palec, próg
                break;

            case "A7":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> { 1, 2, 4 } };
                chord.Tones.Add(new GuitarChordTone(3, 1, 1));  // struna, palec, próg
                break;

            case "a":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> { 1, 2, 3 } };
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna, palec, próg
                break;

            case "a7":
                chord = new UkuleleChord(name) { OpenStrings = new HashSet<int> { 1, 2, 3, 4 } };
                break;

            case "B":
                chord = StandardChord("A") as UkuleleChord;
                chord!.TransponeUpBar(1);
                break;

            case "B7":
                chord = StandardChord("A7") as UkuleleChord;
                chord!.TransponeUpBar(1);
                break;

            case "b":
                chord = StandardChord("b") as UkuleleChord;
                chord!.TransponeUpBar(1);
                break;

            case "b7":
                chord = StandardChord("a7") as UkuleleChord;
                chord!.TransponeUpBar(1);
                break;


            case "H":
                chord = StandardChord("A") as UkuleleChord;
                chord!.TransponeUpBar(2);
                break;

            case "H7":
                chord = new UkuleleChord(name, openStrings: new HashSet<int> { 1 }, mutedStrings: new HashSet<int> {  });
                chord.Tones.Add(new GuitarChordTone(4, 3, 4));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(3, 2, 3));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(2, 1, 2));  // struna, palec, próg
                break;

            case "h":
                chord = StandardChord("a") as UkuleleChord;
                chord!.TransponeUpBar(2);
                break;

            case "h7":
                chord = StandardChord("a7") as UkuleleChord;
                chord!.TransponeUpBar(2);
                break;


            default:
                chord = null;
                break;
        }

        if (!string.IsNullOrEmpty(variation))
        {

            var vchord = ChordByAscii(name, variation);
            if (vchord != null)
                chord = vchord as UkuleleChord;
        }

        if (chord != null)
            chord.Name = name;

        return chord;
    }
}
