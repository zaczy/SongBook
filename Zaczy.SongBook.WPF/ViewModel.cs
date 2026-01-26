using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;
using Zaczy.SongBook.Enums;

namespace Zaczy.SongBook.WPF;

public class ViewModel : INotifyPropertyChanged
{
    //private readonly SongRepository _repository;
    //public SongRepository SongRepository { get => _repository; }
    
    private readonly AppSettings _settings;
    public AppSettings AppSettings { get => _settings; }

    public ViewModel(IOptions<AppSettings> opts)
    {
        //_repository = repository;
        _settings = opts.Value;
    }

    private string? _sourceSongFilename;
    public string? SourceSongFilename 
    {
        get => _sourceSongFilename;
        set
        {
            if (_sourceSongFilename != value)
            {
                _sourceSongFilename = value;
                OnPropertyChanged(nameof(SourceSongFilename));
            }
        }
    }

    private string? _sourceSongHtml;

    /// <summary>
    /// Tekst źródłowy piosenki w formacie HTML
    /// </summary>
    public string? SourceSongHtml 
    { 
        get => _sourceSongHtml;
        set
        {
            if (_sourceSongHtml != value)
            {
                _sourceSongHtml = value;
                OnPropertyChanged(nameof(SourceSongHtml));
            }
        }
    }

    private Song? _convertedSong;

    /// <summary>
    /// Przekonwertowany tekst piosenki w formacie tekstowym
    /// </summary>
    public Song? ConvertedSong
    {
        get 
        {
            if (_convertedSong == null)
                _convertedSong = new Song();
            return _convertedSong; 
        }
        set 
        {
            if (_convertedSong != value)
            {
                _convertedSong = value;
                OnPropertyChanged(nameof(ConvertedSong));
            }
        }
    }

    private ObservableCollection<SongEntity> _songs = new();
    public ObservableCollection<SongEntity> Songs
    {
        get => _songs;
        set
        {
            _songs = value;
            OnPropertyChanged(nameof(Songs));
        }
    }

    private LyricsHtmlVersion _lyricsHtmlVersion = LyricsHtmlVersion.Pre;
    /// <summary>
    /// Versja HTML tekstów piosenek
    /// </summary>
    public LyricsHtmlVersion LyricsHtmlVersion
    {
        get { return _lyricsHtmlVersion; }
        set
        {
            if (_lyricsHtmlVersion != value)
            {
                _lyricsHtmlVersion = value;
                OnPropertyChanged(nameof(LyricsHtmlVersion));
            }
        }
    }


    /// <summary>
    /// Zapisuje aktualną piosenkę do bazy danych
    /// </summary>
    public async Task SaveCurrentSongAsync()
    {
        if (string.IsNullOrEmpty(SourceSongHtml))
            return;

        this.CheckDbSettingsValid();

        var song = Song.CreateFromW(SourceSongHtml);

        var factory = new SongBookDbContextFactory();
        var songRepository = new SongRepository(factory.CreateDbContext(AppSettings.ConnectionStrings.SongBookDb));

        await songRepository.AddAsync(song, SourceSongHtml);
    }

    private void CheckDbSettingsValid()
    {
        if (string.IsNullOrEmpty(AppSettings.ConnectionStrings.SongBookDb))
            throw new InvalidOperationException("Connection string for SongBookDb is not set.");
    }

    /// <summary>
    /// Ładuje wszystkie piosenki z bazy danych
    /// </summary>
    public async Task LoadSongsAsync()
    {
        this.CheckDbSettingsValid();

        var factory = new SongBookDbContextFactory();
        var songRepository = new SongRepository(factory.CreateDbContext(AppSettings!.ConnectionStrings!.SongBookDb!));

        var songs = await songRepository.GetAllAsync();
        Songs = new ObservableCollection<SongEntity>(songs);
        OnPropertyChanged(nameof(Songs));
    }

    /// <summary>
    /// Usuwa piosenkę z bazy danych
    /// </summary>
    public async Task DeleteSongAsync(int id)
    {
        this.CheckDbSettingsValid();

        var factory = new SongBookDbContextFactory();
        var songRepository = new SongRepository(factory.CreateDbContext(AppSettings!.ConnectionStrings!.SongBookDb!));

        await songRepository.DeleteAsync(id);
    }

    /// <summary>
    /// Ładuje piosenkę z encji do edycji
    /// </summary>
    public async Task LoadSongFromEntity(SongEntity entity)
    {
        this.CheckDbSettingsValid();

        var factory = new SongBookDbContextFactory();
        var songRepository = new SongRepository(factory.CreateDbContext(AppSettings!.ConnectionStrings!.SongBookDb!));

        var song = await songRepository.SearchIdAsync(entity.Id);

        if(song!=null)
            ConvertedSong = new Song(song);
        else
            ConvertedSong = new Song();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
