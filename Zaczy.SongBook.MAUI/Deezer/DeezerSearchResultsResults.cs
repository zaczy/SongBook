using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI.Deezer;

public class DeezerSearchResultsResults
{
    public DeezerSearchDataTrack? TRACK { get; set; }
    public DeezerSearchDataArtist? ARTIST { get; set; }
    public DeezerSearchDataAlbum? ALBUM { get; set; }
}
