using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;
using Zaczy.SongBook.MAUI.ViewModels;
using AView = Android.Views.View;

namespace Zaczy.SongBook.MAUI;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density
    )]
public class MainActivity : MauiAppCompatActivity
{
    private UserViewModel? _userViewModel;

    public MainActivity()
    {
        _userViewModel = IPlatformApplication.Current?.Services.GetService<UserViewModel>();
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (_userViewModel?.EnableEdgeToEdge != true)
        {
            var contentView = FindViewById(Android.Resource.Id.Content);
            if (contentView != null)
            {
                contentView.SetOnApplyWindowInsetsListener(new InsetsPaddingListener());
                contentView.RequestApplyInsets();
            }
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Platform.OnNewIntent(intent);
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus && _userViewModel?.EnableEdgeToEdge == true)
        {
            // Wymuś ukrycie pasków ponownie po odzyskaniu fokusa
            EnableFullscreen();
        }
    }

    /// <summary>
    /// Aktywuj tryb pełnoekranowy, ukrywając paski systemowe (status bar i navigation bar).
    /// </summary>
    private void EnableFullscreen()
    {
        if (Window == null) return;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // Android 11+ (API 30+)
        {
#pragma warning disable CA1416
#pragma warning disable CA1422
            Window.SetDecorFitsSystemWindows(false);
            var controller = Window.InsetsController;
            if (controller != null)
            {
                controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
                controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
            }
        }
        else
        {
            var flags = SystemUiFlags.HideNavigation
                        | SystemUiFlags.Fullscreen
                        | SystemUiFlags.ImmersiveSticky
                        | SystemUiFlags.LayoutStable
                        | SystemUiFlags.LayoutHideNavigation
                        | SystemUiFlags.LayoutFullscreen;
#pragma warning disable CS0618
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)flags;
#pragma warning restore CS0618
#pragma warning restore CA1422
#pragma warning restore CA1416
        }
    }

    private sealed class InsetsPaddingListener : Java.Lang.Object, AView.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(AView v, WindowInsets insets)
        {
            int top, bottom, left, right;

#pragma warning disable CA1416
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                var bars = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
                top = bars.Top;
                bottom = bars.Bottom;
                left = bars.Left;
                right = bars.Right;
            }
            else
            {
#pragma warning disable CA1422
                top = insets.SystemWindowInsetTop;
                bottom = insets.SystemWindowInsetBottom;
                left = insets.SystemWindowInsetLeft;
                right = insets.SystemWindowInsetRight;
#pragma warning restore CA1422
            }

            v.SetPadding(left, top, right, bottom);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                return WindowInsets.Consumed!;
            }
#pragma warning restore CA1416

#pragma warning disable CA1422
            return insets.ConsumeSystemWindowInsets();
#pragma warning restore CA1422
        }
    }
}
