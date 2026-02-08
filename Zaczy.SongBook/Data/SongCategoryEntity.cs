using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Zaczy.SongBook.Data;
public class SongCategoryEntity : INotifyPropertyChanged
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    [Column("songs_count")]
    [JsonPropertyName("songs_count")]
    public int? SongsCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("symbol_image")]
    [Column("symbol_image")]
    public string? SymbolImage { get; set; }

    public string? Description { get; set; }

    // Marker used by the UI for per-category selection state.
    // Not serialized to JSON.
    private bool _isSelected;
    [JsonIgnore]
    [Column("is_selected")]
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _categoryColor;
    [JsonPropertyName("category_color")]
    [Column("category_color")]
    [MaxLength(20)]
    public string? CategoryColor 
    {
        get => _categoryColor;
        set
        {
            if(_categoryColor != value)
            {
                _categoryColor = value;
                OnPropertyChanged(nameof(CategoryColor));
                OnPropertyChanged(nameof(CategoryColorThemed));
                OnPropertyChanged(nameof(HasCategoryColor));
                
            }
        }
    }

    private string? _categoryColorThemed;
    [JsonPropertyName("category_color_themed")]
    [Column("category_color_themed")]
    [MaxLength(20)]
    public string? CategoryColorThemed 
    {
        get => _categoryColorThemed;
        set
        {
            if (_categoryColorThemed != value)
            {
                _categoryColorThemed = value;
                OnPropertyChanged(nameof(CategoryColorThemed));
                OnPropertyChanged(nameof(HasCategoryColor));
            }
        }
    }

    [NotMapped]
    public bool HasCategoryColor
    {
        get
        {
            var c = !string.IsNullOrEmpty(CategoryColor) && CategoryColor.ToUpper() != "#FFFFFF" && CategoryColor.ToUpper() != "#000000";
            return c;
        }
    }

    public override string ToString() => $"{Name} ({Id})";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
