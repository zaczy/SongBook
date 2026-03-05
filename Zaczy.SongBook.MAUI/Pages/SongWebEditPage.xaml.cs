using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Maui.ApplicationModel;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Extensions;
using Zaczy.SongBook.MAUI;
using Zaczy.SongBook.MAUI.Data;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.Songbook.MAUI.Pages;

public partial class SongWebEditPage : ContentPage
{
    private readonly UserViewModel _userViewModel;
    private readonly SongEntity _originalSong;
    private readonly Settings _settings;
    private readonly SongRepositoryLite _songRepositoryLite;    

    public Command SaveCommand { get; set; }

    public SongWebEditPage(SongEntity songEntity, UserViewModel userViewModel, Settings settings, SongRepositoryLite songRepositoryLite)
    {
        _userViewModel = userViewModel;
        _originalSong = songEntity;
        _settings = settings;
        _songRepositoryLite = songRepositoryLite;

        SaveCommand = new Command(async () => await JavascriptSaveSong());

        InitializeComponent();

        BindingContext = this;

        // Subscribe to Navigating to receive callbacks from the page's JavaScript
        WebEditor.Navigating += WebEditor_Navigating;

        _ = NavigateToEditPage();
    }

    /// <summary>
    /// Intercept special URLs from JS and call native methods.
    /// JS should navigate to an app-specific URL (scheme + path), e.g. "app://doAfterSave" or "app://doAfterSave?ok=1"
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void WebEditor_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(e?.Url))
                return;

            const string callbackScheme = "app://";

            if (e.Url.StartsWith(callbackScheme, StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;

                var uri = new Uri(e.Url);
                var action = uri.Host; // for app://doAfterSave host == "doAfterSave"

                if (string.Equals(action, "doAfterSave", StringComparison.OrdinalIgnoreCase))
                {
                    await doAfterSaveOperations();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebEditor_Navigating error: {ex.Message}");
        }
    }

    /// <summary>
    /// Nawiguj do strony edycji piosenki.
    /// </summary>
    /// <returns></returns>
    private async Task NavigateToEditPage()
    {
        string url = $"{_settings.WebBaseUrl}/songs/{_originalSong.Id}/edit?embed=1";

        var requested = AppInfo.RequestedTheme;
        if (requested == AppTheme.Dark)
        {
            url += "&darkMode=1";
        }

        var cookies = new List<string>();
        if (!string.IsNullOrEmpty(_userViewModel.UserToken))
        {
            cookies.Add($"api_token={_userViewModel.UserToken}; Path={url}; HttpOnly");
            //cookies.Add($"api_token={_userViewModel.UserToken}; HttpOnly");
        }

        await this.SetCookiesAndNavigateAsync($"{url}", cookies);
    }

    private async Task SetCookiesAndNavigateAsync(string url, IEnumerable<string> cookies)
    {
        try
        {
            foreach (var cookie in cookies)
            {
                // platform implementation — see below
                CookieHelper.SetCookie(url, cookie);
            }

            // small delay on some platforms to ensure cookie is flushed
            await Task.Delay(50);

            WebEditor.Source = url;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Navigation failed: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Wywo³aj zapis piosenki w JavaScript.
    /// </summary>
    /// <returns></returns>
    public async Task JavascriptSaveSong()
    {
        await WebEditor.EvaluateJavaScriptAsync("submitFormInBackground();");
    }

    /// <summary>
    /// Called after JavaScript saved the form. Fetch updated song from API and update local entity.
    /// Returns true when update succeeded.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> doAfterSaveOperations()
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.ApiBaseUrl))
            {
                System.Diagnostics.Debug.WriteLine("doAfterSaveOperations: ApiBaseUrl is not configured.");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await DisplayAlert("Error", "API base URL is not configured.", "OK"));
                return false;
            }

            var apiClient = new ApiClient(_settings.ApiBaseUrl);

            // Expecting endpoint: GET /songs/{id}
            var response = await apiClient.GetAsync<SongEntity>($"/songs/{_originalSong.Id}");

            if (response.IsSuccess && response.Data != null)
            {
                var updated = response.Data;

                updated.ShallowCopyTo(_originalSong, new List<string> { "HasEditPrivileges", "CategoryColor" }); // Copy all fields except HasEditPrivileges

                await _songRepositoryLite.UpdateAsync(_originalSong);

                try
                {
                    WeakReferenceMessenger.Default.Send(new ValueChangedMessage<SongEntity>(_originalSong));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MessagingCenter.Send failed: {ex.Message}");
                }

                //await MainThread.InvokeOnMainThreadAsync(async () =>
                //{
                //    await DisplayAlert("Zapisano", "Piosenka zosta³a zaktualizowana z serwera.", "OK");
                //    // If you want to reload the editor page to reflect changes, uncomment:
                //    // WebEditor.Reload();
                //});

                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"doAfterSaveOperations: API error: {response.ErrorMessage} {response.ErrorDetails}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await DisplayAlert("Error", "Failed to fetch updated song from server.", "OK"));
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"doAfterSaveOperations exception: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayAlert("Error", ex.Message, "OK"));
            return false;
        }
    }
}
