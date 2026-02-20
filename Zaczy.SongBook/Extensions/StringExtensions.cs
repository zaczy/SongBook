using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Zaczy.SongBook.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Safe substring helper: returns a substring starting at <paramref name="from"/> with requested <paramref name="length"/>,
    /// but never throws if indices exceed input length. If <paramref name="text"/> is null returns empty string.
    /// Behavior:
    /// - null text -> string.Empty
    /// - from &lt; 0 -> treated as 0
    /// - length &lt;= 0 -> string.Empty
    /// - from >= text.Length -> string.Empty
    /// - if from+length exceeds text length -> returns substring from 'from' to end
    /// </summary>
    public static string SubstringSafe(this string text, int from, int length)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (length <= 0)
            return string.Empty;

        if (from < 0)
            from = 0;

        if (from >= text.Length)
            return string.Empty;

        int maxLength = text.Length - from;
        if (length > maxLength)
            length = maxLength;

        return text.Substring(from, length);
    }

    /// <summary>
    /// camelCase
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string CamelCase(this string text)
    {
        return text.Substring(0, 1).ToLowerInvariant() + text.Substring(1, text.Length - 1);
    }

    public static string? NormalizeInlineWhitespace(this string input)
    {
        if (input == null) return null;
        // [^\S\r\n] = whitespace z WYŁĄCZENIEM CR i LF
        return Regex.Replace(input, @"[^\S\r\n]+", " ");
    }

    public static string ToJson(this object? o)
    {
        if(o == null)
            return "{}";

        return JsonSerializer.Serialize(o, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

}
