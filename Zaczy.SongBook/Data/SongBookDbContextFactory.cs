using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zaczy.SongBook.Data;

public class SongBookDbContextFactory : IDesignTimeDbContextFactory<SongBookDbContext>
{
    // Default connection string used at design-time
    private const string DefaultConnectionString = "Server=localhost;Database=songbook;User=songbook;Password=Qaz43210;";

    public SongBookDbContext CreateDbContext(string[] args)
    {
        // If EF supplies a connection string via args, use it; otherwise use the default.
        var connectionString = (args?.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            ? args[0]
            : DefaultConnectionString;

        return CreateDbContext(connectionString);
    }

    public SongBookDbContext CreateDbContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SongBookDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new SongBookDbContext(optionsBuilder.Options);
    }
}
/*
 * dotnet ef migrations add AuthorsColumn
 * dotnet ef database update
 */