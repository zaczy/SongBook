using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI;

public class Helpers
{
    public static async Task<bool> IsSpotifyInstalled()
    {
        bool isSpotifyInstalled;

        try
        {
            isSpotifyInstalled = await Launcher.CanOpenAsync("spotify:");
        }
        catch
        {
            isSpotifyInstalled = false;
        }

        return isSpotifyInstalled;
    }
}
