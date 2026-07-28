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

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += AndroidUnhandledExceptionRaiser;
#endif
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(_startPage));
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        if (exception != null)
        {
            System.Diagnostics.Debug.WriteLine($"[CRASH] UnhandledException: {exception}");
            exception.SaveExceptionToFileAsync("unhandled_domain", eventApi: _eventApi).GetAwaiter().GetResult();
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[CRASH] UnobservedTaskException: {e.Exception}");
        e.Exception?.SaveExceptionToFileAsync("unobserved_task", eventApi: _eventApi).GetAwaiter().GetResult();
        e.SetObserved();
    }

#if ANDROID
    private void AndroidUnhandledExceptionRaiser(object? sender, Android.Runtime.RaiseThrowableEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[CRASH] AndroidUnhandledException: {e.Exception}");
        e.Exception?.SaveExceptionToFileAsync("unhandled_android", eventApi: _eventApi).GetAwaiter().GetResult();
        e.Handled = true;
    }
#endif
}