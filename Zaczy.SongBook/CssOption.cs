using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook;

public class CssOption
{
    public CssOption(string cssClass, string cssProperty, string value)
    {
        CssClass = cssClass;
        CssProperty = cssProperty;
        Value = value;
    }

    public string CssClass { get; set; }

    public string CssProperty { get; set; }

    public string Value { get; set; }

    public string? Context { get; set; }

    public int? LineNo { get; set; }

    public int? DefinitionMediaSize { get; set; }

    public int? Priority { get; set; }
    
    public override string ToString()
    {
        return $"{CssClass} {{ {CssProperty}: {Value}; }}";
    }

}
