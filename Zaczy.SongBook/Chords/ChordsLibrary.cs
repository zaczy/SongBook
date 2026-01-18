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

            case "Cis7":
                if (variation == "x4342x")
                {

                    chord = new GuitarChord(name, openStrings: new HashSet<int> { }, mutedStrings: new HashSet<int> { 1, 6 });
                    chord.Tones.Add(new GuitarChordTone(5, 3, 4));  // struna palec próg 
                    chord.Tones.Add(new GuitarChordTone(4, 2, 3));  // struna palec próg 
                    chord.Tones.Add(new GuitarChordTone(3, 4, 4));  // struna palec próg 
                    chord.Tones.Add(new GuitarChordTone(2, 1, 2));  // struna palec próg 
                }
                else
                {
                    chord = StandardChord("A7");
                    chord!.TransponeUpBar(4);
                    chord.Name = name;
                }
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
                else
                {
                    chord = StandardChord("A");
                    chord!.TransponeUpBar(5);

                }
                break;

            case "E":
                chord = new GuitarChord(name);
                chord.Tones.Add(new GuitarChordTone(3, 1, 1));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(4, 3, 2));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(5, 2, 2));  // struna palec próg 
                break;

            case "e":
                chord = new GuitarChord(name);
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

            case "f":
                chord = StandardChord("e");
                chord!.TransponeUpBar(1);
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

            case "a":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1, 5 }, mutedStrings: new HashSet<int> { 6 });
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna, palec, próg
                chord.Tones.Add(new GuitarChordTone(3, 3, 2));  // struna, palec, próg
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

        if(chord != null)
            chord.Name = name;

        return chord;
    }
}
