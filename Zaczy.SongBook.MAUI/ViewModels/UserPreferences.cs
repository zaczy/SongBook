using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI.ViewModels;

public class UserPreferences
{
    
    public int? Id { get; set; }
    public double FontSizeAdjustment { get; set; } = 0;
    public int? AutoScrollSpeed { get; set; }
    
}
