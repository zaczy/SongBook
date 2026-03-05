using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Models;
public class User
{
    public string? name { get; set; }
    public string? email { get; set; }
    public string? google_id { get; set; }
    public string? avatar { get; set; }
    public string? api_token { get; set; }
    public int isadmin { get; set; }

    public bool IsAdmin
    {
        get => isadmin==1;
    }

    public int id { get; set; }

    public int is_editor { get; set; }

    public bool IsEditor
    {
        get => is_editor == 1;
    }

    [JsonPropertyName("deezer_arl")]
    public string? DeezerArl { get; set; }
}
