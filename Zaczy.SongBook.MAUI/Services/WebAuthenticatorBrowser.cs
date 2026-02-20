using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Extensions;
using Zaczy.SongBook.MAUI.Extensions;
using IBrowser = Duende.IdentityModel.OidcClient.Browser.IBrowser;

namespace Zaczy.Songbook.MAUI.Services;

public class WebAuthenticatorBrowser : IBrowser
{

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            // Otwiera systemową bezpieczną kartę przeglądarki
            var result = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(options.StartUrl),
                new Uri(options.EndUrl));

            return new BrowserResult
            {
                Response = ParseResponse(result, options.EndUrl),
                ResultType = BrowserResultType.Success
            };
        }
        catch (TaskCanceledException)
        {
            return new BrowserResult { ResultType = BrowserResultType.UserCancel };
        }
    }

    private string ParseResponse(WebAuthenticatorResult result, string redirectUrl)
    {
        // Formatuje parametry zwrotne (code, state) w adres URL zrozumiały dla OidcClient
        var parameters = result.Properties.Select(pair => $"{pair.Key}={pair.Value}");
        return $"{redirectUrl}?{string.Join("&", parameters)}";
    }

}

