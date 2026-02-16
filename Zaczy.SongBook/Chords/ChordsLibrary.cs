using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Chords;

public class ChordsLibrary
{

    public static Dictionary<string, string?> ChordsDict = new Dictionary<string, string?>
    {
        { "A", "x02220" },
        { "B", "x24442" },
        { "C", "x32010" },
        { "D", "xx0232" },
        { "d", "xx0232" },
        { "E", "022100" },
        { "F", "133211" },
        { "Dis", "xx1232" },
        { "Cis7", "x4342x" }
    };

    /// <summary>
    /// Akord na podstawie zapisu tekstowego (np. x02220)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="definition"></param>
    /// <returns></returns>
    public static GuitarChord? ChordByAscii(string name, string definition)
    {
        int barFret = -1;
        if (definition?.StartsWith("#") == true && definition?.Length == 8)
        {
            char c = definition[1];
            barFret = c >= 'a' ? c - 'a' + 10 : c - '0';
            definition = definition.Substring(2, 6);
        }

        if (string.IsNullOrEmpty(definition) || definition.Length != 6)
            return null;

        var predefined = ChordsDict.FirstOrDefault(x => x.Value == definition && x.Key == name);
        
        if(!string.IsNullOrEmpty(predefined.Key))
            return StandardChord(predefined.Key, predefined.Value);

        GuitarChord chord = new GuitarChord(name);
        if(barFret > 0)
        {
            chord.Tones.Add(new GuitarChordTone(fret: barFret) { Crossbar = true });
        }

        for (int i=0; i<definition.Length; i++)
        {
            char c = definition[i];
            int stringNumber = 6 - i;
            if(c == 'x' || c == 'X')
            {
                chord.MutedStrings.Add(stringNumber);
            }
            else if(c == '0')
            {
                if(barFret<=0)
                    chord.OpenStrings.Add(stringNumber);
            }
            else if(char.IsAsciiHexDigit(c)) 
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

    public static GuitarChord? StandardChord(string name, string? variation=null)
    {
        if(string.IsNullOrEmpty(name))
            return null;

        GuitarChord? chord = null;

        switch (name)
        {
            case "C":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1, 3 }, mutedStrings: new HashSet<int> { 6 });
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(5, 3, 3));  // struna 5, palec 3, próg 3
                break;

            case "C9/5":
                chord = new GuitarChord("C9/5") { MutedStrings = new HashSet<int> { 6 } };
                chord.Tones.Add(new GuitarChordTone(5, 2, 3));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(4, 1, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna 5, palec 3, próg 3
                chord.Tones.Add(new GuitarChordTone(1, 4, 3));  // struna 5, palec 3, próg 3
                break;

            case "C5":
                chord = new GuitarChord("C5") { MutedStrings = new HashSet<int> { 4,3,2,1 } };
                chord.Tones.Add(new GuitarChordTone(6, 1, 8));  // struna 5, palec 3, próg 3
                chord.Tones.Add(new GuitarChordTone(5, 3, 10));  // struna 5, palec 3, próg 3
                break;

            case "Cadd9":
                chord = new GuitarChord("C9") { MutedStrings = new HashSet<int> { 6 }, OpenStrings = new HashSet<int> { 3, 1 } };
                chord.Tones.Add(new GuitarChordTone(6, 1, 8));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(5, 3, 10));  // struna 2, palec 1, próg 1
                break;

            case "c":
                chord = StandardChord("a");
                chord!.TransponeUpBar(3);
                break;

            case "c7":
                chord = StandardChord("a7");
                chord!.TransponeUpBar(3);
                break;

            case "Cis7":
                if (variation == "x4342x")
                {

                    chord = new GuitarChord(name, openStrings: new HashSet<int> { }, mutedStrings: new HashSet<int> { 1, 6 });
                    chord.Tones.Add(new GuitarChordTone(5, 3, 4));  // struna palec próg 
                    chord.Tones.Add(new GuitarChordTone(4, 2, 3));  // struna palec próg 
                    chord.Tones.Add(new GuitarChordTone(3, 4, 4));  // struna palec próg 
                    chord.Tones.Add(new GuitarChordTone(2, 1, 2));  // struna palec próg 
                }
                else if (string.IsNullOrEmpty(variation))
                {
                    chord = StandardChord("A7");
                    chord!.TransponeUpBar(4);
                    chord.Name = name;
                }
                break;

            case "cis":
                chord = StandardChord("a");
                chord!.TransponeUpBar(4);
                break;

            case "D":
                chord = new GuitarChord("D", openStrings: new HashSet<int> { 4 }, mutedStrings: new HashSet<int> { 5, 6 });
                chord.Tones.Add(new GuitarChordTone(1, 2, 2));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(3, 1, 2));  // struna 5, palec 3, próg 3
                break;

            case "d":
                chord = new GuitarChord("d", openStrings: new HashSet<int> { 4 }, mutedStrings: new HashSet<int> { 5, 6 });
                chord.Tones.Add(new GuitarChordTone(1, 1, 1));  // struna 1, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna 5, palec 3, próg 3
                break;

            case "Dis":
                if(variation == "xx1232")
                {
                    chord = new GuitarChord(name, openStrings: new HashSet<int> { }, mutedStrings: new HashSet<int> { 5, 6 });
                    chord.Tones.Add(new GuitarChordTone(4, 1, 1));  // struna , palec 1, próg 
                    chord.Tones.Add(new GuitarChordTone(3, 2, 3));  // struna , palec 2, próg 
                    chord.Tones.Add(new GuitarChordTone(2, 4, 4));  // struna , palec 3, próg 
                    chord.Tones.Add(new GuitarChordTone(1, 3, 3));  // struna , palec 4, próg 
                }
                else if(string.IsNullOrEmpty(variation))
                {
                    chord = StandardChord("A");
                    chord!.TransponeUpBar(5);

                }
                break;

            case "E":
                chord = new GuitarChord(name) {  OpenStrings = new HashSet<int> { 6, 2, 1 } };
                chord.Tones.Add(new GuitarChordTone(3, 1, 1));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(4, 3, 2));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(5, 2, 2));  // struna palec próg 
                break;

            case "E7":
                chord = new GuitarChord(name) { OpenStrings = new HashSet<int> { 6, 4, 2, 1 } };
                chord.Tones.Add(new GuitarChordTone(3, 1, 1));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(5, 2, 2));  // struna palec próg 
                break;

            case "Esus4":
                chord = new GuitarChord(name) { OpenStrings = new HashSet<int> { 6, 2, 1 } };
                chord.Tones.Add(new GuitarChordTone(3, 1, 2));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(4, 3, 2));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(5, 2, 2));  // struna palec próg 
                break;

            case "e":
                chord = new GuitarChord(name) {  OpenStrings = new HashSet<int> { 6, 3, 2, 1 } };
                chord.Tones.Add(new GuitarChordTone(5, 2, 2));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(4, 3, 2));  // struna palec próg 
                break;

            case "F":
                chord = new GuitarChord("F");
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(4, 4, 3));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(5, 3, 3));  // struna 5, palec 3, próg 3
                chord.Tones.Add(new GuitarChordTone(1));  // struna 1-6, palec 1, próg 1, crossbar
                break;

            case "F7":
                chord = StandardChord("E7");
                chord!.TransponeUpBar(1);
                break;

            case "F7+":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1 }, mutedStrings: new HashSet<int> { 5, 6 });
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(4, 3, 3));  // struna 5, palec 3, próg 3
                break;


            case "F9/6":
                chord = new GuitarChord("F9/6") {  MutedStrings = new HashSet<int> { 6, 5 } };
                chord.Tones.Add(new GuitarChordTone(4, 2, 3));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(3, 1, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna 5, palec 3, próg 3
                chord.Tones.Add(new GuitarChordTone(1, 4, 3));  // struna 5, palec 3, próg 3
                break;


            case "f":
                if (variation == "8poz")
                {
                    chord = StandardChord("a");
                    chord!.TransponeUpBar(8);
                }
                else
                {
                    chord = StandardChord("e");
                    chord!.TransponeUpBar(1);
                }
                break;

            case "f7":
                if (variation == "8poz")
                {
                    chord = StandardChord("a7");
                    chord!.TransponeUpBar(8);
                }
                else
                {
                    chord = new GuitarChord(name) { MutedStrings = new HashSet<int> { 5, 1 } };
                    chord.Tones.Add(new GuitarChordTone(6, 2, 1));  // struna, palec, próg
                    chord.Tones.Add(new GuitarChordTone(4, 3, 1));  // struna, palec, próg
                    chord.Tones.Add(new GuitarChordTone(3, 3, 1));  // struna, palec, próg
                    chord.Tones.Add(new GuitarChordTone(2, 3, 1));  // struna, palec, próg

                }
                break;

            case "Fis":
                chord = StandardChord("F");
                chord!.TransponeUpBar(1);
                break;

            case "fis":
                chord = StandardChord("e");
                chord!.TransponeUpBar(2);
                break;

            case "G":
                chord = new GuitarChord(name) { OpenStrings = new HashSet<int> { 2, 3, 4} };
                chord.Tones.Add(new GuitarChordTone(6, 2, 3));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(5, 1, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(1, 3, 3));  // struna, palec, próg
                break;

            case "g":
                chord = StandardChord("e");
                chord!.TransponeUpBar(3);
                break;

            case "Gis":
                chord = StandardChord("E");
                chord!.TransponeUpBar(4);
                break;

            case "gis":
                chord = StandardChord("e");
                chord!.TransponeUpBar(4);
                break;

            case "A":
                chord = new GuitarChord(name);
                chord.Tones.Add(new GuitarChordTone(2, 3, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(4, 1, 2));  // struna 4, palec 1, próg 2
                break;

            case "A7":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1, 3, 5, 6 }, mutedStrings: new HashSet<int> { });
                chord.Tones.Add(new GuitarChordTone(2, 3, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(4, 1, 2));  // struna 4, palec 1, próg 2
                break;

            case "A7sus4":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1, 3, 5 }, mutedStrings: new HashSet<int> { 6 });
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna 4, palec 1, próg 2
                break;

            case "Asus2":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1, 2, 5 }, mutedStrings: new HashSet<int> { 6 });
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(4, 3, 2));  // struna 4, palec 1, próg 2
                break;

            case "Asus4":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1, 5 }, mutedStrings: new HashSet<int> { 6 });
                chord.Tones.Add(new GuitarChordTone(4, 1, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(2, 3, 3));  // struna, palec, próg
                break;

            case "a":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1, 5 }, mutedStrings: new HashSet<int> { 6 });
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(3, 3, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna, palec, próg
                break;

            case "a7":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1, 5 }, mutedStrings: new HashSet<int> { 6 });
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna, palec, próg
                break;

            case "B":
                chord = StandardChord("A");
                chord!.TransponeUpBar(1);
                break;

            case "H":
                chord = StandardChord("A");
                chord!.TransponeUpBar(2);
                break;

            case "H7":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 2 }, mutedStrings: new HashSet<int> { 6 });
                chord.Tones.Add(new GuitarChordTone(5, 2, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(4, 1, 1));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(3, 3, 2));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(1, 4, 2));  // struna, palec, próg
                break;

            case "h":
                chord = StandardChord("a");
                chord!.TransponeUpBar(2);
                break;

            default:
                chord = null;
                break;
        }

        if(chord==null && !string.IsNullOrEmpty(variation))
            chord = ChordByAscii(name, variation);

        if (chord != null)
            chord.Name = name;

        return chord;
    }
}
