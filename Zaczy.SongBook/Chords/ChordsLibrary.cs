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
        { "F", "133211" }
    };

    public static GuitarChord? StandardChord(string name, string? variation=null)
    {
        if(string.IsNullOrEmpty(name))
            return null;

        GuitarChord? chord = null;

        switch (name)
        {
            case "A":
                chord = new GuitarChord(name);
                chord.Tones.Add(new GuitarChordTone(2, 3, 2));  // struna 2, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 3, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(4, 1, 2));  // struna 4, palec 1, próg 2
                break;

            case "B":
                chord = StandardChord("A");
                chord!.TransponeUpBar(1);
                break;

            case "C":
                chord = new GuitarChord(name, openStrings: new HashSet<int> { 1, 3 }, mutedStrings: new HashSet<int> { 6 });
                chord.Tones.Add(new GuitarChordTone(2, 1, 1));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(4, 2, 2));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(5, 3, 3));  // struna 5, palec 3, próg 3
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

            case "E":
                chord = new GuitarChord(name);
                chord.Tones.Add(new GuitarChordTone(3, 1, 1));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(4, 3, 2));  // struna palec próg 
                chord.Tones.Add(new GuitarChordTone(5, 2, 2));  // struna palec próg 
                break;

            case "F":
                chord = new GuitarChord("F");
                chord.Tones.Add(new GuitarChordTone(3, 2, 2));  // struna 2, palec 1, próg 1
                chord.Tones.Add(new GuitarChordTone(4, 4, 3));  // struna 4, palec 2, próg 2
                chord.Tones.Add(new GuitarChordTone(5, 3, 3));  // struna 5, palec 3, próg 3
                chord.Tones.Add(new GuitarChordTone(1));  // struna 1-6, palec 1, próg 1, crossbar
                break;


            default:
                chord = null;
                break;
        }

        return chord;
    }
}
