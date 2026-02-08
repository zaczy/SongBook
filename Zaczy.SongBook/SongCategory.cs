using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Zaczy.SongBook;

public class SongCategory
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public List<Song>? Songs { get; set; }

    private int _songsCount;

    [JsonPropertyName("songs_count")]
    public int SongsCount
    {
        get 
        { 
            if(Songs != null)
            {
                _songsCount = Songs.Count;
            }

            return _songsCount; 
        }
        set { _songsCount = value; }
    }

    [JsonPropertyName("symbol_image")]
    public string? SymbolImage { get; set; }

    public string? Description { get; set; }

    [JsonPropertyName("category_color")]
    public string? CategoryColor { get; set; }

    public void initFromEntity(Data.SongCategoryEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        SymbolImage = entity.SymbolImage;
        Description = entity.Description;
        CategoryColor = entity.CategoryColor;
    }

}
