using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Zaczy.SongBook.Api;
using Zaczy.SongBook.Extensions;
using Zaczy.SongBook.MAUI;
using Zaczy.SongBook.MAUI.Extensions;

namespace Zaczy.Songbook.MAUI.Services;

public class WebAuthenticationBrowserClient
{
    private readonly Settings _settings;    
    public WebAuthenticationBrowserClient(Settings settings)
    {
        _settings = settings;
    }

    public static async Task<GoogleAuthResult?> LoginWithGoogle(Settings settings)
    {
        //GoogleAuthResult? result;
        EventApi eventApi = new EventApi(settings);

        var policy = new DiscoveryPolicy
        {
            ValidateEndpoints = false,
            ValidateIssuerName = true // Zalecane pozostawienie dla bezpieczeństwa
        };

        var options = new OidcClientOptions
        {
            Authority = "https://accounts.google.com",
            ClientId = "84331651713-fcrfbobjt30t1jt6rlu4vg7ee988sep2.apps.googleusercontent.com",
            Scope = "openid profile email",
            RedirectUri = "com.googleusercontent.apps.84331651713-fcrfbobjt30t1jt6rlu4vg7ee988sep2:/oauth2redirect",
            Browser = new WebAuthenticatorBrowser(),
            Policy = new Policy
            {
                Discovery = new DiscoveryPolicy
                {
                    ValidateEndpoints = false,
                    ValidateIssuerName = false
                }
            }
        };

        try
        {
            var client = new OidcClient(options);
            var loginResult = await client.LoginAsync();

            if (loginResult?.IsError == true)
            {
                return new GoogleAuthResult() { AccessToken = null, LoginInfo = $"Error: {loginResult.Error}" };
            }

            var accessToken = loginResult?.AccessToken;
            var idToken = loginResult?.IdentityToken;

            string logInfo = string.Empty;

            // Prefer claims returned by the OIDC flow
            string? email = loginResult?.User?.FindFirst("email")?.Value
                            ?? loginResult?.User?.FindFirst(ClaimTypes.Email)?.Value;

            string? name = loginResult?.User?.FindFirst("name")?.Value
                           ?? loginResult?.User?.FindFirst(ClaimTypes.Name)?.Value
                           ?? loginResult?.User?.Identity?.Name;

            string? picture = loginResult?.User?.FindFirst("picture")?.Value;

            // Fallback: if some fields are missing, call Google userinfo endpoint using access token
            if ((string.IsNullOrEmpty(email) || string.IsNullOrEmpty(picture)) && !string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    logInfo = "Fallback";
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var json = await http.GetStringAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                    if (!string.IsNullOrEmpty(json))
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("email", out var emailProp) && string.IsNullOrEmpty(email))
                            email = emailProp.GetString();
                        if (root.TryGetProperty("name", out var nameProp) && string.IsNullOrEmpty(name))
                            name = nameProp.GetString();
                        if (root.TryGetProperty("picture", out var pictureProp) && string.IsNullOrEmpty(picture))
                            picture = pictureProp.GetString();
                    }
                }
                catch
                {
                    // swallow - user info fallback is optional
                }
            }

            if(!string.IsNullOrEmpty(email))
            {
                var userApi = new UserApi(settings.ApiBaseUrl);
                await userApi.CreateOrUpdateUserAsync(email, idToken, picture);
            }

            return new GoogleAuthResult()
            {
                AccessToken = accessToken,
                IdToken = idToken,
                Email = email,
                Name = name,
                Picture = picture,
                LoginInfo = logInfo
            };
        }
        catch (Exception ex)
        {
            await ex.SaveExceptionToFileAsync(eventApi: eventApi, eventPostfix: "_GoogleAuth");
            return new GoogleAuthResult() { AccessToken = null, LoginInfo = $"Error: {ex.Message}" };
        }
    }

}
