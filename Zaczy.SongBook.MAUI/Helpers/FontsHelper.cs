using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook;

namespace Zaczy.Songbook.MAUI.Helpers;

public static class FontsHelper
{
    private static string[] _fontsAvailable = new[]
    {
        "Antykwa Półtawskiego",
        "Inconsolata",
        "Roboto",
        "Lato Regular"
    };

    /// <summary>
    /// Mapowanie nazw na ścieżki plików czcionek
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, string> FontsAssetsDictionary()
    {
        var fontAssets = new Dictionary<string, string>
        {
            { "Inconsolata", "assets/css/Inconsolata/Inconsolata-VariableFont_wdth,wght.ttf" },
            { "Antykwa Półtawskiego",   "assets/css/Poltawski_Nowy/PoltawskiNowy-VariableFont_wght.ttf" },
            { "Roboto",   "assets/css/Roboto/Roboto-VariableFont_wdth,wght.ttf" },
            { "Lato Regular",   "assets/css/Lato/Lato-Regular.ttf" }
        };

        return fontAssets;

    }

    /// <summary>
    /// Lista dostępnych czcionek
    /// </summary>
    /// <returns></returns>
    public static string[] FontsAvailable()
    {
        return _fontsAvailable;
    }

    /// <summary>
    /// Kopiuje pliki czcionek z pakietu do AppDataDirectory (jak w SongDetailsPage).
    /// </summary>
    public static async Task EnsureFontsAvailableAsync(SongVisualization visualization)
    {
        var fontAssets = FontsHelper.FontsAssetsDictionary();

        var appData = FileSystem.AppDataDirectory;
        visualization.CssFontsPath ??= new Dictionary<string, string>();

        foreach (var kv in fontAssets)
        {
            var destPath = Path.Combine(appData, kv.Value.Replace('/', Path.DirectorySeparatorChar));
            var destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir!);

            if (!File.Exists(destPath))
            {
                try
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync(kv.Value);
                    using var outFs = File.Create(destPath);
                    await stream.CopyToAsync(outFs);
                }
                catch
                {
                    continue;
                }
            }

            visualization.CssFontsPath[kv.Key] = destPath;
        }
    }


}
