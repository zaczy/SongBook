using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI.Spotify;

public interface ISpotifyRemoteService
{
    Task<bool> ConnectAsync();
    Task PlayAsync(string spotifyUri);
    Task PauseAsync();
    Task ResumeAsync();
    Task NextAsync();
    Task PreviousAsync();
}
