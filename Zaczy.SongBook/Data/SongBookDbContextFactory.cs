using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zaczy.SongBook.Data;

public class SongBookDbContextFactory : IDesignTimeDbContextFactory<SongBookDbContext>
{
    public SongBookDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Server=localhost;Database=songbook;User=songbook;Password=Qaz43210;";
        
        var optionsBuilder = new DbContextOptionsBuilder<SongBookDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new SongBookDbContext(optionsBuilder.Options);
    }
}
/*
 * dotnet ef migrations add AuthorsColumn
 * dotnet ef database update
 */