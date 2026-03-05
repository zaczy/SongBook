using System;
using Android.Webkit;

namespace Zaczy.SongBook.MAUI;

public static partial class CookieHelper
{
    static partial void PlatformSetCookie(string url, string cookie)
    {
        try
        {
            var mgr = CookieManager.Instance;

            if (mgr != null)
            {
                mgr.SetAcceptCookie(true);
                mgr.SetCookie(url, cookie); // cookie string: "name=value; Path=/; HttpOnly"
                mgr.Flush(); // ensures cookies persisted / visible to WebView
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CookieHelper Android error: {ex.Message}");
        }
    }
}