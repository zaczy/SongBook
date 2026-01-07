using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.WPF;

public class ViewModel : INotifyPropertyChanged
{
    private readonly SongRepository _repository;

    public ViewModel()
    {
        var connectionString = "Server=localhost;Database=songbook;User=songbook;Password=Qaz43210;";
        var optionsBuilder = new DbContextOptionsBuilder<SongBookDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        
        var context = new SongBookDbContext(optionsBuilder.Options);
        _repository = new SongRepository(context);
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

    /// <summary>
    /// Zapisuje aktualną piosenkę do bazy danych
    /// </summary>
    public async Task SaveCurrentSongAsync()
    {
        if (string.IsNullOrEmpty(SourceSongHtml))
            return;

        var song = Song.CreateFromW(SourceSongHtml);
        await _repository.AddAsync(song, SourceSongHtml);
    }

    /// <summary>
    /// Ładuje wszystkie piosenki z bazy danych
    /// </summary>
    public async Task LoadSongsAsync()
    {
        var songs = await _repository.GetAllAsync();
        Songs = new ObservableCollection<SongEntity>(songs);
    }

    /// <summary>
    /// Usuwa piosenkę z bazy danych
    /// </summary>
    public async Task DeleteSongAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    /// <summary>
    /// Ładuje piosenkę z encji do edycji
    /// </summary>
    public void LoadSongFromEntity(SongEntity entity)
    {
        ConvertedSong = new Song
        {
            Title = entity.Title,
            Artist = entity.Artist,
            Capo = entity.Capo,
            Lyrics = entity.Lyrics,
            LyricsAuthor = entity.LyricsAuthor,
            MusicAuthor = entity.MusicAuthor
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
