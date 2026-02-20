using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.MAUI;

namespace Zaczy.SongBook.Api;

public class EventApi
{
    private readonly Settings _settings;
    public EventApi(IOptions<Settings> settingsOptions)
    {
        _settings = settingsOptions.Value;
    }

    public EventApi(Settings settings)
    {
        _settings = settings;
    }

    public async Task SendEventAsync(string eventName, string details)
    {
        string appVersion = AppInfo.Current.VersionString;
        string buildNumber = AppInfo.Current.BuildString;

        var apiClient = new ApiClient(_settings.ApiBaseUrl);

        var request = new 
        {
            @event = eventName,
            connect_time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            app_version = $"{appVersion}",
            details = details,
        };

        var headers = new Dictionary<string, string>
        {
            {  "Authorization", $"Bearer {_settings.ApiToken}" }
        };

        var response = await apiClient.PostAsync("/playerEvent", request, headers);
        if (!response.IsSuccess)
        {
            System.Diagnostics.Debug.WriteLine($"SendEventAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
        }
    }
}
