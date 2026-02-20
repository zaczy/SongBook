using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Zaczy.SongBook.MAUI
{
    [Activity(
        Theme = "@style/Maui.SplashTheme", 
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density
        )]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            // To pozwala Microsoft.Maui.Authentication odebrać sygnał powrotny
            Platform.OnNewIntent(intent);
        }

    }
}
