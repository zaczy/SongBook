using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI;//.Platforms.Android;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "com.googleusercontent.apps.84331651713-fcrfbobjt30t1jt6rlu4vg7ee988sep2")] 
public class WebAuthenticatorCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        System.Diagnostics.Debug.WriteLine("WebAuthenticatorActivity: OnCreate called");
        System.Diagnostics.Debug.WriteLine($"WebAuthenticatorActivity: Intent Data = {Intent?.Data}");
        System.Diagnostics.Debug.WriteLine($"WebAuthenticatorActivity: Intent Action = {Intent?.Action}");
        System.Diagnostics.Debug.WriteLine($"WebAuthenticatorActivity: Intent DataString = {Intent?.DataString}");
        base.OnCreate(savedInstanceState);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        System.Diagnostics.Debug.WriteLine($"WebAuthenticatorActivity: OnNewIntent - {intent?.Data}");
        System.Diagnostics.Debug.WriteLine($"WebAuthenticatorActivity: Intent Action = {intent?.Action}");
        System.Diagnostics.Debug.WriteLine($"WebAuthenticatorActivity: Intent DataString = {intent?.DataString}");
        base.OnNewIntent(intent);
    }

    protected override void OnResume()
    {
        System.Diagnostics.Debug.WriteLine("WebAuthenticatorActivity: OnResume called");
        base.OnResume();
    }

}
