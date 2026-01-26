using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zaczy.SongBook.Data;

public class SongBookDbContextFactory : IDesignTimeDbContextFactory<SongBookDbContext>
{
    public SongBookDbContext CreateDbContext(string[] args)
    {
        string connectionString = string.Empty; // "Server=localhost;Database=songbook;User=songbook;Password=Qaz43210;";
        
        if (args?.Length > 0)
        {
            return CreateDbContext(args[0]);
        }

        return null!;
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