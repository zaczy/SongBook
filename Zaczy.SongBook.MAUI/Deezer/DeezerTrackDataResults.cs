using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI.Deezer;


public class DeezerTrackDataResults
{
    public List<object>? error;
    public DeezerTrack? results { get; set; }
}
