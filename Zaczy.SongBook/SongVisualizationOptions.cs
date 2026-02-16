using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook;

public class SongVisualizationOptions
{

    public SongVisualizationOptions()
    {
    }

    public SongVisualizationOptions(VisualizationCssOptions visualizationCssOptions)
    {
        _visualizationCssOptions = visualizationCssOptions;
    }

    private VisualizationCssOptions? _visualizationCssOptions;

    public VisualizationCssOptions VisualizationCssOptions
    {
        get
        {
            if (_visualizationCssOptions == null)
                _visualizationCssOptions = new VisualizationCssOptions();

            return _visualizationCssOptions;
        }
        set { _visualizationCssOptions = value; }
    }

    private bool _customChordsOnly;
    /// <summary>
    /// Tylko specyficzne akordy
    /// </summary>
    public bool CustomChordsOnly
    {
        get { return _customChordsOnly; }
        set { _customChordsOnly = value; }
    }

    public bool SkipTabulatures { get; set; } = false;
    public bool SkipLyricChords { get; set; } = false;

    public bool MoveChordsToLyricsLine { get; set; } = false;
    public string? ChordDiagramColor { get; set; }
}
