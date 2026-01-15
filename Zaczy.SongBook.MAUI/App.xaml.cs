using Zaczy.SongBook.MAUI.Pages;

namespace Zaczy.SongBook.MAUI;

public partial class App : Application
{
    public App(SongsPage startPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(startPage);
    }
}