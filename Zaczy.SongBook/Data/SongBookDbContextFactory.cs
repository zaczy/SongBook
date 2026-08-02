using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Zaczy.SongBook.Enums;

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

        return CreateDbContext(connectionString, SongBookDbProvider.MySql);
    }

    /// <summary>
    /// Utrzymana dla zgodnoœci - domyœlnie MySQL.
    /// </summary>
    //public SongBookDbContext CreateDbContext(string connectionString, )
    //    => CreateDbContext(connectionString, SongBookDbProvider.MySql);

    public SongBookDbContext CreateDbContext(string connectionString, SongBookDbProvider provider)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SongBookDbContext>();

        switch (provider)
        {
            case SongBookDbProvider.Sqlite:
                optionsBuilder.UseSqlite(connectionString);
                break;
            case SongBookDbProvider.MySql:
            default:
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                break;
        }

        return new SongBookDbContext(optionsBuilder.Options);
    }
}
/*
 * dotnet ef migrations add AuthorsColumn
 * dotnet ef database update
 */