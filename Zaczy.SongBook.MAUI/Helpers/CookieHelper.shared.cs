using System;

namespace Zaczy.SongBook.MAUI;

public static partial class CookieHelper
{
    // Public API always available
    public static void SetCookie(string url, string cookie)
    {
        PlatformSetCookie(url, cookie);
    }

    // Platform-specific implementation (no accessibility modifiers)
    static partial void PlatformSetCookie(string url, string cookie);
}