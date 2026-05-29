using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Zaczy.SongBook.MAUI.Data;

public enum SingingGroupRole
{
    None,
    /// <summary>Dyrygent wybiera piosenki dla grupy</summary>
    Dyrygent,
    /// <summary>Artysta odbiera sygnały o zmianie piosenki</summary>
    Artysta
}

public class SingingGroupEntity : INotifyPropertyChanged
{
    public int Id { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime? LastSetDate { get; set; }

    public string? Creator { get; set; }

    public string? Leader { get; set; }

    public int? CurrentSongId { get; set; }

    public DateTime UpdatedAt { get; set; }

    private bool _isSelected;
    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RoleBadge));
            }
        }
    }

    private SingingGroupRole _selectedRole;
    [JsonIgnore]
    public SingingGroupRole SelectedRole
    {
        get => _selectedRole;
        set
        {
            if (_selectedRole != value)
            {
                _selectedRole = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RoleBadge));
                OnPropertyChanged(nameof(HasRole));
            }
        }
    }

    /// <summary>Etykieta roli wyświetlana w UI, np. "🎼 Dyrygent"</summary>
    [JsonIgnore]
    public string RoleBadge => SelectedRole switch
    {
        SingingGroupRole.Dyrygent => "🎼 Dyrygent",
        SingingGroupRole.Artysta  => "🎵 Artysta",
        _                         => string.Empty
    };

    [JsonIgnore]
    public bool HasRole => SelectedRole != SingingGroupRole.None;

    /// <summary>
    /// Grupa wyłącznie lokalna (nie synchronizować z serwerem)
    /// </summary>
    [JsonIgnore]
    public bool IsLocalOnly { get; set; }

    [JsonIgnore]
    public bool IsOnline 
    {
        get => !IsLocalOnly;
    }

    [JsonIgnore]
    public bool HasLeader 
    {
        get => !string.IsNullOrEmpty(Leader);
    }


    public override string ToString() => $"{GroupName} ({Id})";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}