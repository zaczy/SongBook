using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Extensions;
using Zaczy.SongBook.Models;

namespace Zaczy.SongBook.Api;

public class SingingGroupApi
{
    string _baseUrl;

    public SingingGroupApi(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Pobierz listę kategorii
    /// </summary>
    /// <returns></returns>
    public async Task<List<SingingGroup>> GetServerGroupsAsync(string? groupName=null, int? groupId=null)
    {
        var apiClient = new ApiClient(_baseUrl);

        string relativeUrl = $"/songbook-singing-groups";

        if (groupId != null)
        {
            relativeUrl += $"?id={groupId}";
        }
        else if (!string.IsNullOrEmpty(groupName))
        {
            relativeUrl += $"?group_name={groupName}";
        }

        var response = await apiClient.GetAsync<List<SingingGroup>>(relativeUrl);
        if (response.IsSuccess && response.Data != null)
        {
            return response.Data;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"GetSingingGroupAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
            return new List<SingingGroup>();
        }
    }

    /// <summary>
    /// Pobierz informacje o aktualnym statusie grupy (czy jest aktywna, do kiedy ważna, kto jest liderem itp.)
    /// </summary>
    /// <param name="groupName"></param>
    /// <param name="groupId"></param>
    /// <returns></returns>
    public async Task<SingingGroup?> GetServerGroupsStatusAsync(string? groupName = null, int? groupId = null, int ignoreOlderThanSeconds=0)
    {
        var apiClient = new ApiClient(_baseUrl);

        string relativeUrl = $"/songbook-singing-groups/current-group-info";

        if (groupId != null)
        {
            relativeUrl += $"?id={groupId}";
        }
        else if (!string.IsNullOrEmpty(groupName))
        {
            relativeUrl += $"?group_name={groupName}";
        }

        var response = await apiClient.GetAsync<SingingGroup>(relativeUrl);
        if (response.IsSuccess && response.Data != null)
        {
            if(ignoreOlderThanSeconds > 0 && response.Data.LastSetDate != null)
            {
                var age = DateTime.UtcNow - response.Data.LastSetDate.Value;
                if(age.TotalSeconds > ignoreOlderThanSeconds)
                {
                    System.Diagnostics.Debug.WriteLine($"GetSingingGroupAsync: Ignoring group info because it's too old: {age.TotalSeconds} seconds");
                    return null;
                }
            }
            return response.Data;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"GetSingingGroupAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
            return null;
        }
    }

    /// <summary>
    /// Utwórz nową grupę
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<SingingGroup> CreateGroupAsync(SingingGroup group)
    {
        var apiClient = new ApiClient(_baseUrl);
        var response = await apiClient.PostAsync<SingingGroup, SingingGroup?>("/songbook-singing-groups", group);
        if (response.IsSuccess && response.Data != null)
        {
            return (SingingGroup)response.Data;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"CreateGroupAsyncAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
            throw new Exception($"API error: {response.ErrorMessage} {response.ErrorDetails}");
        }
    }

    /// <summary>
    /// Zmień/ustaw bieżącą piosenkę
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="songId"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<SingingGroup> ChangeSongAsync(int groupId, int songId, string userRole)
    {
        var apiClient = new ApiClient(_baseUrl);
        var response = await apiClient.PostAsync<SingingGroup, SingingGroup?>($"/songbook-singing-groups/{groupId}/change-current-song", new  SingingGroup { CurrentSongId = songId, LastSetDate = DateTime.Now, CurrentSongUserRole = userRole });
        if (response.IsSuccess && response.Data != null)
        {
            return (SingingGroup)response.Data;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ChangeSongAsyncAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
            throw new Exception($"API error: {response.ErrorMessage} {response.ErrorDetails}");
        }
    }

    public async Task<SingingGroup> ChangeLeaderAsync(int groupId, string userEmail, string appGuid)
    {
        var apiClient = new ApiClient(_baseUrl);
        var response = await apiClient.PostAsync<SingingGroup, SingingGroup?>($"/songbook-singing-groups/{groupId}/change-leader", 
            new SingingGroup { Leader = userEmail, LeaderGuid = appGuid, LastSetDate = DateTime.Now });
        if (response.IsSuccess && response.Data != null)
        {
            return (SingingGroup)response.Data;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ChangeLeaderAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
            throw new Exception($"API error: {response.ErrorMessage} {response.ErrorDetails}");
        }
    }


    public static async Task<int?> CurrentSongForListenersGroupAsync(string baseUrl, int listenersGroupId)
    {
        var singingGroupApi = new SingingGroupApi(baseUrl);
        var status = await singingGroupApi.GetServerGroupsStatusAsync(groupId: listenersGroupId, ignoreOlderThanSeconds: 5 * 60);

        return status?.CurrentSongId;
    }
}
