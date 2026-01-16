using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook;

public class VisualizationCssOptions
{
    private HashSet<CssOption>? _customOptions;

    public HashSet<CssOption> CustomOptions
    {
        get
        {
            if (_customOptions == null)
                _customOptions = new HashSet<CssOption>();

            return _customOptions;
        }

        set
        {
            _customOptions = value;
        }
    }

    /// <summary>
    /// Dodaj dyrektywę
    /// </summary>
    /// <param name="cssClass"></param>
    /// <param name="cssProperty"></param>
    /// <param name="value"></param>
    public void Add(string cssClass, string cssProperty, string value)
    {
        CustomOptions.Add(new CssOption(cssClass, cssProperty, value));
    }

    /// <summary>
    /// Zwraca wartość z definicji (o ile została zdefiniowana)
    /// </summary>
    /// <param name="cssClass"></param>
    /// <param name="cssProperty"></param>
    /// <returns></returns>
    public string? CssValue(string cssClass, string cssProperty)
    {
        var op = CustomOptions.Where(o => o.CssClass == cssClass && o.CssProperty == cssProperty).FirstOrDefault();

        return op?.Value ?? null;
    }

    /// <summary>
    /// Generuj kod css dla wybranej klasy
    /// </summary>
    /// <param name="cssClass"></param>
    /// <returns></returns>
    public string? GenerateCss(string cssClass)
    {
        string? css = null;
        string properties = String.Empty;

        foreach(var op in CustomOptions.Where(o => o.CssClass == cssClass))
        {
            if (!string.IsNullOrEmpty(op.CssProperty) && !string.IsNullOrEmpty(op.Value))
            {
                properties += $"{op.CssProperty}: {op.Value} !important; ";                
            }
        }

        if (!string.IsNullOrEmpty(properties))
            css = $"{cssClass} {{ {properties} }}";

        return css;
    }

    /// <summary>
    /// Generuj kod css dla wszystkich zdefiniowanych klas
    /// </summary>
    /// <returns></returns>
    public string? GenerateCss()
    {
        string css = string.Empty;

        var classes = CustomOptions.GroupBy(o => o.CssClass).Select(g => g.Key).ToList();

        foreach(string c in classes)
        {
            string? clsCss = this.GenerateCss(c);
            if (!string.IsNullOrEmpty(clsCss))
                css += $"{clsCss}\n";
        }

        return !string.IsNullOrEmpty(css) ? css : null;
    }


}
