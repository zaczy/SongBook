using Zaczy.SongBook.MAUI.Pages;

namespace Zaczy.SongBook.MAUI;

public partial class App : Application
{
    private readonly SongsPage _startPage;

    public App(SongsPage startPage)
    {
        InitializeComponent();
        _startPage = startPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(_startPage));
    }
}