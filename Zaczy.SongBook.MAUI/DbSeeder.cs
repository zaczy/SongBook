using Zaczy.SongBook.Data;

namespace Zaczy.SongBook.MAUI;
public static class DbSeeder
{
    public static void SeedIfEmpty(SongBookDbContext db)
    {
        if (db.Songs.Any()) return;

        var json = ReadEmbeddedResource("Zaczy.SongBook.Data.seed_songs.json");
        var songs = System.Text.Json.JsonSerializer.Deserialize<List<SongEntity>>(json);
        db.Songs.AddRange(songs);
        db.SaveChanges();
    }

    private static string ReadEmbeddedResource(string name)
    {
        var asm = typeof(DbSeeder).Assembly;
        using var s = asm.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name);
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }
}