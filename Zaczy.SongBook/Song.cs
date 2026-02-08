using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using Zaczy.SongBook.Chords;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook;

public class Song: INotifyPropertyChanged
{
    public Song() { }

    public int? ServerId { get; set; }

    public Song(SongEntity entity): this()
    {
        Title = entity.Title;
        Artist = entity.Artist;
        Capo = entity.Capo;
        Lyrics = entity.Lyrics;
        LyricsAuthor = entity.LyricsAuthor;
        MusicAuthor = entity.MusicAuthor;
        ChordsVariations = entity.ChordsVariations;
        ScrollingDelay = entity.ScrollingDelay;
        SongDuration = entity.SongDuration;
        SpotifyLink = entity.SpotifyLink;
        MoreInfo = entity.MoreInfo;
        Source = entity.Source;
        ServerId = entity.Id;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private string? _title;
    public string? Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }
    private string? _artist;
    public string? Artist
    {
        get => _artist;
        set
        {
            if (_artist != value)
            {
                _artist = value;
                OnPropertyChanged(nameof(Artist));
            }
        }
    }

    private string? _lyricsAuthor;
    [JsonPropertyName("lyrics_author")]
    public string? LyricsAuthor
    {
        get => _lyricsAuthor;
        set
        {
            if (_lyricsAuthor != value)
            {
                _lyricsAuthor = value;
                OnPropertyChanged(nameof(LyricsAuthor));
            }
        }
    }

    [JsonPropertyName("music_author")]
    private string? _musicAuthor;
    public string? MusicAuthor
    {
        get => _musicAuthor;
        set
        {
            if (_musicAuthor != value)
            {
                _musicAuthor = value;
                OnPropertyChanged(nameof(MusicAuthor));
            }
        }
    }

    private string? _capo;
    public string? Capo
    {
        get => _capo;
        set
        {
            if (_capo != value)
            {
                _capo = value;
                OnPropertyChanged(nameof(Capo));
            }
        }
     }

    private string? _lyrics;
    public string? Lyrics 
    {
        get => _lyrics;
        set
        {
            if(_lyrics != value)
            {
                _lyrics = value;
                OnPropertyChanged(nameof(Lyrics));
            }
        }
    }

    private string? _chordsVariations;
    /// <summary>
    /// Wykaz niestandardowych wersji akordów
    /// </summary>
    [JsonPropertyName("chords_variations")]
    public string? ChordsVariations
    {
        get { return _chordsVariations; }
        set { _chordsVariations = value; }
    }


    private int? _scrollingDelay;
    [JsonPropertyName("scrolling_delay")]
    public int? ScrollingDelay
    {
        get { return _scrollingDelay; }
        set { _scrollingDelay = value; }
    }

    private int? _songDuration;
    [JsonPropertyName("song_duration")]
    public int? SongDuration
    {
        get { return _songDuration; }
        set { _songDuration = value; }
    }

    private string? _spotifyLink;
    [JsonPropertyName("spotify_link")]
    public string? SpotifyLink
    {
        get { return _spotifyLink; }
        set { _spotifyLink = value; }
    }

    private List<string>? _lines;
    /// <summary>
    /// Wiersze tekstu piosenki
    /// </summary>
    public List<string>? Lines
    {
        get
        {
            if (_lines == null && !string.IsNullOrEmpty(Lyrics))
            {
                _lines = new List<string>(Lyrics.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
            }

            return _lines;
        }
        set
        {
            _lines = value;
        }
    }

    [JsonIgnore]
    public List<string>? LinesLazyLoad
    {
        get
        {
            if (_lines == null)
            {
                _lines = new List<string>();
            }

            return _lines;
        }
    }

    [JsonPropertyName("more_info")]
    public string? MoreInfo { get; internal set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    // Słownik mapujący numery progów na polskie nazwy
    private static readonly Dictionary<int, string> FretNames = new()
    {
        [0] = "Bez kapodastra",
        [1] = "Pierwszy próg",
        [2] = "Drugi próg",
        [3] = "Trzeci próg",
        [4] = "Czwarty próg",
        [5] = "Piąty próg",
        [6] = "Szósty próg",
        [7] = "Siódmy próg",
        [8] = "Ósmy próg",
        [9] = "Dziewiąty próg",
        [10] = "Dziesiąty próg",
        [11] = "Jedenasty próg",
        [12] = "Dwunasty próg"
    };

    // Słownik mapujący polskie nazwy na numery progów
    private static readonly Dictionary<string, int> FretNumbers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pierwszy próg"] = 1, ["Pierwszy"] = 1, ["1"] = 1,
        ["Drugi próg"] = 2, ["Drugi"] = 2, ["2"] = 2,
        ["Trzeci próg"] = 3, ["Trzeci"] = 3, ["3"] = 3,
        ["Czwarty próg"] = 4, ["Czwarty"] = 4, ["4"] = 4,
        ["Piąty próg"] = 5, ["Piąty"] = 5, ["5"] = 5,
        ["Szósty próg"] = 6, ["Szósty"] = 6, ["6"] = 6,
        ["Siódmy próg"] = 7, ["Siódmy"] = 7, ["7"] = 7,
        ["Ósmy próg"] = 8, ["Ósmy"] = 8, ["8"] = 8,
        ["Dziewiąty próg"] = 9, ["Dziewiąty"] = 9, ["9"] = 9,
        ["Dziesiąty próg"] = 10, ["Dziesiąty"] = 10, ["10"] = 10,
        ["Jedenasty próg"] = 11, ["Jedenasty"] = 11, ["11"] = 11,
        ["Dwunasty próg"] = 12, ["Dwunasty"] = 12, ["12"] = 12
    };

    /// <summary>
    /// Zmienia tonację wszystkich akordów w piosence o wskazaną liczbę półtonów
    /// </summary>
    /// <param name="semitones">Liczba półtonów (dodatnia = w górę, ujemna = w dół)</param>
    public void AdjustTonation(int semitones)
    {
        if (Lines == null || Lines.Count == 0 || semitones == 0)
            return;

        for (int i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (Chord.IsChordLine(line))
            {
                Lines[i] = Chord.TransposeLine(line, semitones);
                continue;
            }

            int chordStart = Chord.ChordPartStart(line);
            if (chordStart > 0)
            {
                var textPart = line.Substring(0, chordStart);
                var chordPart = line.Substring(chordStart);
                Lines[i] = textPart + Chord.TransposeLine(chordPart, semitones);
            }
        }

        Lyrics = string.Join(Environment.NewLine, Lines);

        AdjustCapo(semitones);
    }

    /// <summary>
    /// Dostosowuje pozycję kapodastra o wskazaną liczbę półtonów
    /// </summary>
    /// <param name="semitones">Liczba półtonów</param>
    private void AdjustCapo(int semitones)
    {
        if (string.IsNullOrWhiteSpace(Capo))
            return;

        int currentFret = 0;
        foreach (var entry in FretNumbers)
        {
            if (Capo.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
            {
                currentFret = entry.Value;
                break;
            }
        }

        int newFret = currentFret - semitones;
        
        newFret = ((newFret % 12) + 12) % 12;

        if (FretNames.TryGetValue(newFret, out var newCapoName))
        {
            Capo = newCapoName;
        }
    }

    /// <summary>
    /// Utwórz z obiektu W
    /// </summary>
    /// <param name="htmlContent"></param>
    /// <returns></returns>
    public static Song CreateFromW(string htmlContent)
    {
        var converter = new Html2Text_W(htmlContent);
        var lyrics = converter.ConvertToText();
        var song = new Song
        {
            Lyrics = lyrics,
            Title = converter.GetTitle(),
            Artist = converter.GetArtist(),
            Capo = converter.GetCapoInfo(),
            LyricsAuthor = converter.GetLyricsAuthor(),
            MusicAuthor = converter.GetMusicAuthor(),
            ScrollingDelay = 0,
            SongDuration = null,
            MoreInfo = string.Empty,
            Source = "Wywrota"
        };
        return song;
    }

    public override string ToString()
    {
        return $"{Title} - {Artist}";
    }
}
