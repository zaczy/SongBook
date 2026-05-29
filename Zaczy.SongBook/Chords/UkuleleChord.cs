using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Chords;

public class UkuleleChord: ChordBase
{
    public UkuleleChord(string name, HashSet<int>? mutedStrings = null, HashSet<int>? openStrings = null) : base(name, mutedStrings, openStrings)
    {
        _stringCount = 4;
    }

    public override string ToString()
    {
        return ToAscii();
    }

}

