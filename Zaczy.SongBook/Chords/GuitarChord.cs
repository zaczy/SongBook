using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Chords;

public class GuitarChord: ChordBase
{
    public GuitarChord(string name, HashSet<int>? mutedStrings= null, HashSet<int>? openStrings= null) : base(name, mutedStrings, openStrings)
    {
        _stringCount = 6;
    }

public override string ToString()
    {
        return ToAscii();
    }
}
