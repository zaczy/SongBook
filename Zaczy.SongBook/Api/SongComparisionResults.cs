using Zaczy.SongBook.Data;
using Zaczy.SongBook.Extensions;

namespace Zaczy.SongBook.Api;

public class SongComparisionResults
{
    public string? SongTitle { get; set; }

    public string? DiffSummary { get; set; }

    public List<SongDiffSpecification>? DiffSpecification { get; set; }

    public string FieldsSummary 
    { 
        get => DiffSpecification != null ? string.Join(", ", DiffSpecification.Select(d => d.FieldName)) : string.Empty;
    }
    public SongEntity? BaseSongEntity { get; set; }
    public SongEntity? ApiSong { get; internal set; }
}