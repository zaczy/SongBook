using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Chords;

public interface IChordsLibrary
{
    public ChordBase? ChordByAscii(string name, string definition);
    public ChordBase? StandardChord(string name, string? variation = null);

}
