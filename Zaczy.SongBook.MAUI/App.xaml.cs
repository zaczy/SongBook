using Zaczy.SongBook.Api;
using Zaczy.SongBook.MAUI.Extensions;
using Zaczy.SongBook.MAUI.Pages;

namespace Zaczy.SongBook.MAUI;

public partial class App : Application
{
    private readonly SongsPage _startPage;
    private readonly EventApi _eventApi;
    public App(EventApi eventApi, SongsPage startPage)
    {
        InitializeComponent();

        _startPage = startPage;
        _eventApi = eventApi;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(_startPage));
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Obsługa nieprzechwyconych wyjątków
        var exception = e.ExceptionObject as Exception;
        if (exception != null)
        {
            exception.SaveExceptionToFileAsync(" unhandled", eventApi: _eventApi).Wait();
            Console.WriteLine($"Nieprzechwycony wyjątek: {exception?.Message}");
        }
        // Możesz dodać dodatkowe logowanie lub akcje naprawcze
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception != null)
        {
            e.Exception.SaveExceptionToFileAsync(" unobserved", eventApi: _eventApi).Wait();     
            // Obsługa nieprzechwyconych wyjątków zadań
            Console.WriteLine($"Nieprzechwycony wyjątek zadania: {e.Exception.Message}");
        }

        e.SetObserved();
    }
}