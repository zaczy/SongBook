using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Zaczy.SongBook;

internal static class FontBase64Cache
{
    private static readonly ConcurrentDictionary<string, Lazy<string>> _fonts =
        new(StringComparer.Ordinal);

    public static string Get(string fontPath)
    {
        var fullPath = Path.GetFullPath(fontPath);

        var entry = _fonts.GetOrAdd(
            fullPath,
            static path => new Lazy<string>(
                () => LoadFont(path),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var base64 = entry.Value;

            if (base64.Length == 0)
                RemoveEntry(fullPath, entry);

            return base64;
        }
        catch
        {
            RemoveEntry(fullPath, entry);
            throw;
        }
    }

    public static void Invalidate(string fontPath)
    {
        var fullPath = Path.GetFullPath(fontPath);
        _fonts.TryRemove(fullPath, out _);
    }

    private static string LoadFont(string fontPath)
    {
        if (!File.Exists(fontPath))
            return string.Empty;

        var fontBytes = File.ReadAllBytes(fontPath);
        return Convert.ToBase64String(fontBytes);
    }

    private static void RemoveEntry(string fullPath, Lazy<string> entry)
    {
        // Remove only this entry, not a replacement added by another caller.
        var entries =
            (ICollection<KeyValuePair<string, Lazy<string>>>)_fonts;

        entries.Remove(
            new KeyValuePair<string, Lazy<string>>(fullPath, entry));
    }
}