using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook;
using Zaczy.SongBook.MAUI.ViewModels;

namespace Zaczy.Songbook.MAUI.Helpers;

public class LyricsVisualisationHelper
{
    /// <summary>
    /// Dopasuj css dla trybu ciemnego
    /// </summary>
    /// <param name="visualizationCssOptions"></param>
    public static void AdjustDarkModeCss(UserViewModel userViewModel, VisualizationCssOptions visualizationCssOptions)
    {
        if (userViewModel.LyricsDarkMode)
        {
            var lyricsDarkBg = Application.Current?.Resources["LyricsDarkBackground"] as Color;
            var lyricsDarkText = Application.Current?.Resources["LyricsDarkText"] as Color;
            var lyricsDarkChords = Application.Current?.Resources["LyricsDarkChords"] as Color;

            visualizationCssOptions.Add("body", "background-color", lyricsDarkBg?.ToHex() ?? "#222", "dark");
            visualizationCssOptions.Add("body", "color", lyricsDarkText?.ToHex() ?? "#BBB", "dark");
            visualizationCssOptions.Add(".chord-diagram", "background-color", lyricsDarkBg?.ToHex() ?? "#1e1e1e", "dark");
            visualizationCssOptions.Add(".chord-diagram .fret", "background-color", lyricsDarkText?.ToHex() ?? "#1e1e1e", "dark");

            visualizationCssOptions.Add(".chords", "color", lyricsDarkChords?.ToHex() ?? "#2c2c2c", "dark");
            visualizationCssOptions.Add(".chords2", "color", lyricsDarkChords?.ToHex() ?? "#2c2c2c", "dark");

            visualizationCssOptions.Add(".block-refren", "border-left", "15px solid #262626", "dark");
        }
        else
            visualizationCssOptions?.CustomOptions?.RemoveWhere(opt => opt.Context == "dark");
    }


    public static SongVisualization CreateVisualizationOptions(SongVisualization songVisualization, VisualizationCssOptions visualizationCssOptions, UserViewModel userViewModel)
    {
        var fontsPath = songVisualization?.CssFontsPath;

        LyricsVisualisationHelper.AdjustDarkModeCss(userViewModel, visualizationCssOptions);

        var visualization = new SongVisualization()
        {
            IncludeFontsAsBase64 = true,
            VisualizationOptions = new SongVisualizationOptions(visualizationCssOptions)
            {
                CustomChordsOnly = userViewModel.ShowOnlyCustomChords,
                SkipLyricChords = userViewModel.SkipLyricChords,
                SkipTabulatures = userViewModel.SkipTabulatures,
                MoveChordsToLyricsLine = userViewModel.MoveChordsToLyricsLine,
                Instrument = userViewModel.ChordsInstrument
            }
        };

        if (userViewModel.LyricsDarkMode == true)
        {
            var lyricsDarkText = Application.Current?.Resources["LyricsDarkText"] as Color;
            visualization.VisualizationOptions.ChordDiagramColor = lyricsDarkText?.ToHex();
        }

        if (fontsPath != null)
            visualization.CssFontsPath = fontsPath;

        return visualization;
    }


}
