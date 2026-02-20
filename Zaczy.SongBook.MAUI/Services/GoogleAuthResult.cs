using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.Songbook.MAUI.Services;

public class GoogleAuthResult
{
    public string? AccessToken { get; set; }
    public string? LoginInfo { get; set; }
    public string? IdToken { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Picture { get; set; }
}
