using Android.OS;
using Android.Views;
using Microsoft.Maui.ApplicationModel;

namespace Zaczy.SongBook.MAUI.Platforms.Android.Services;

public static class SystemBarsInfoService
{
    /// <summary>
    /// Sprawdza, czy urz¹dzenie posiada widoczny pasek nawigacyjny (przyciski Home/Back/Recents).
    /// </summary>
    public static bool HasNavigationBar()
    {
        var activity = Platform.CurrentActivity;
        var decorView = activity?.Window?.DecorView;
        if (decorView == null)
            return false;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
#pragma warning disable CA1416
            var insets = decorView.RootWindowInsets;
            if (insets == null)
                return false;

            var navBars = insets.GetInsets(WindowInsets.Type.NavigationBars());
            return navBars.Bottom > 0 || navBars.Left > 0 || navBars.Right > 0;
#pragma warning restore CA1416
        }
        else
        {
#pragma warning disable CA1422, CS0618, CA1416
            var insets = decorView.RootWindowInsets;
            return insets != null && insets.SystemWindowInsetBottom > 0;
#pragma warning restore CA1422, CS0618, CA1416
        }
    }

    /// <summary>
    /// Zwraca wysokoœæ paska nawigacyjnego w pikselach (0 jeœli brak).
    /// </summary>
    public static int GetNavigationBarHeightPx()
    {
        var activity = Platform.CurrentActivity;
        var decorView = activity?.Window?.DecorView;
        if (decorView == null)
            return 0;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
#pragma warning disable CA1416
            var insets = decorView.RootWindowInsets;
            if (insets == null)
                return 0;

            var navBars = insets.GetInsets(WindowInsets.Type.NavigationBars());
            return navBars.Bottom;
#pragma warning restore CA1416
        }
        else
        {
#pragma warning disable CA1422, CS0618, CA1416
            return decorView.RootWindowInsets?.SystemWindowInsetBottom ?? 0;
#pragma warning restore CA1422, CS0618, CA1416
        }
    }

    public enum NavigationType {         
        None,
        GestureBar,
        ThreeButton
    }

    public static NavigationType GetNavigationBarType()
    {
        var activity = Platform.CurrentActivity;
        var density = activity?.Resources?.DisplayMetrics?.Density;
        var heightDp = GetNavigationBarHeightPx() / density;

        if (heightDp == 0) return NavigationType.None;
        if (heightDp < 30) return NavigationType.GestureBar;
        return NavigationType.ThreeButton;
    }

}