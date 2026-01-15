using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiteDB;

namespace Zaczy.SongBook.MAUI.ViewModels;

public class UserViewModel : INotifyPropertyChanged
{
    private readonly LiteDatabase _liteDb;
    private const int PrefsId = 1;
    private UserPreferences? _prefs;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserViewModel(LiteDatabase liteDb)
    {
        _liteDb = liteDb;
        Load();
    }

    private void Load()
    {
        var col = _liteDb.GetCollection<UserPreferences>("user_prefs");
        _prefs = col.FindById(PrefsId) ?? new UserPreferences { Id = PrefsId };
    }

    private void Save()
    {
        var col = _liteDb.GetCollection<UserPreferences>("user_prefs");
        col.Upsert(_prefs);
    }

    public double FontSizeAdjustment
    {
        get => _prefs.FontSizeAdjustment;
        set
        {
            if (_prefs == null)
                return;

            if (_prefs != null)
            {
                _prefs.FontSizeAdjustment = value;
                Save();
                OnPropertyChanged();
            }
        }
    }

    public int? AutoScrollSpeed
    {
        get => _prefs?.AutoScrollSpeed;
        set
        {
            if (_prefs.AutoScrollSpeed != value)
            {
                _prefs!.AutoScrollSpeed = value;
                Save();
                OnPropertyChanged();
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}