using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Chords;


public class GuitarChordTone
{
    public GuitarChordTone(int guitarString, int finger, int fret, bool crossbar=false)
    {
        GuitarString = guitarString;
        Finger = finger;
        Fret = fret;
        Crossbar = crossbar;
    }

    public GuitarChordTone(int fret)
    {
        Fret = fret;
        Crossbar = true;
    }


    /// <summary>
    /// E1, H2, G3, D4, A5, E6
    /// </summary>
    public int GuitarString { get; set; }

    public int Finger { get; set; }

    public int Fret { get; set; }

    public bool Crossbar { get; set; } = false;

    public override string ToString()
    {
        return $"{GuitarString}:{Finger}:{Fret}:{(Crossbar ? "X" : "")}";
    }
}
