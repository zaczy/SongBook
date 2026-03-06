using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Models;

namespace Zaczy.SongBook.Api;

public class UserApi
{
    string _baseUrl;

    public UserApi(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Wyślij wszystkie piosenki do API, aby zsynchronizować dane. API powinno obsługiwać endpoint POST /songs/sync, który przyjmuje listę piosenek i aktualizuje bazę danych na serwerze.
    /// </summary>
    /// <param name="songRepository"></param>
    /// <returns></returns>
    public async Task CreateOrUpdateUserAsync(string email, string? token=null, string? picture=null)
    {
        try
        {
            var apiClient = new ApiClient(_baseUrl);

            var request = new
            {
                email = email,
                name = string.Empty,
                autorization_type = "google",
                picture = picture,
                api_token = token
            };

            var response = await apiClient.PostAsync("/songbook-users/update-or-create", request);
        }
        catch
        {
        }
    }

    /// <summary>
    ///  Pobierz informacje o użytkowniku na podstawie tokena API. 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<User?> GetUserByTokenAsync(string token)
    {
        try
        {
            var apiClient = new ApiClient(_baseUrl);
            var response = await apiClient.GetAsync<User>($"/songbook-users/token/{token}", new Dictionary<string, string>());
            if (response.IsSuccess && response.Data != null)
            {
                return response.Data;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"GetUserByTokenAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetUserByTokenAsync: Exception: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///  Pobierz informacje o użytkowniku na podstawie tokena API. 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<User?> GetUserByEmailAsync(string token)
    {
        try
        {
            var apiClient = new ApiClient(_baseUrl);
            var response = await apiClient.GetAsync<User>($"/songbook-users/email/{token}", new Dictionary<string, string>());
            if (response.IsSuccess && response.Data != null)
            {
                return response.Data;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"GetUserByEmailAsync: API error: {response.ErrorMessage} {response.ErrorDetails}");
                return null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetUserByEmailAsync: Exception: {ex.Message}");
            return null;
        }
    }


}
