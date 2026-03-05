using System;
using Foundation;
using WebKit;

namespace Zaczy.SongBook.MAUI;

public static partial class CookieHelper
{
    static partial void PlatformSetCookie(string url, string cookie)
    {
        try
        {
            // Parse basic cookie "name=value; Path=/; ...". Here we create an NSHttpCookie.
            // For simplicity, create a cookie with name/value only; expand as needed.
            var parts = cookie.Split(';')[0].Split('=', 2);
            if (parts.Length < 2) return;
            var name = parts[0].Trim();
            var value = parts[1].Trim();

            // Use Apple NSHTTPCookie property key names directly as NSStrings.
            // Some Xamarin/.NET iOS bindings do not expose the same helper constants or factory method,
            // so create the NSDictionary and attempt multiple creation approaches (binding-agnostic).
            var cprops = new NSDictionary(
                new NSString("NSHTTPCookieName"), new NSString(name),
                new NSString("NSHTTPCookieValue"), new NSString(value),
                new NSString("NSHTTPCookieDomain"), new NSString(new Uri(url).Host),
                new NSString("NSHTTPCookiePath"), new NSString("/")
            );

            NSHttpCookie? nsCookie = null;

            // Try the common static factory first (older bindings)
            var fromPropsMethod = typeof(NSHttpCookie).GetMethod("FromProperties", new[] { typeof(NSDictionary) });
            if (fromPropsMethod != null)
            {
                nsCookie = fromPropsMethod.Invoke(null, new object[] { cprops }) as NSHttpCookie;
            }
            else
            {
                // Try a constructor accepting NSDictionary (some bindings expose ctor)
                var ctor = typeof(NSHttpCookie).GetConstructor(new[] { typeof(NSDictionary) });
                if (ctor != null)
                {
                    nsCookie = ctor.Invoke(new object[] { cprops }) as NSHttpCookie;
                }
            }

            if (nsCookie != null)
            {
                NSHttpCookieStorage.SharedStorage.SetCookie(nsCookie);

                // Also set in WKWebView cookie store (for modern WebView)
                WKWebsiteDataStore.DefaultDataStore.HttpCookieStore.SetCookie(nsCookie, null);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("CookieHelper iOS: unable to create NSHttpCookie (binding mismatch).");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CookieHelper iOS error: {ex.Message}");
        }
    }
}