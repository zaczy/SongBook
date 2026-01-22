using Microsoft.EntityFrameworkCore;

namespace Zaczy.SongBook.Data;

public class SongBookDbContext : DbContext
{
    public DbSet<SongEntity> Songs { get; set; }

    public SongBookDbContext(DbContextOptions<SongBookDbContext> options) 
        : base(options)
    {
    }

    public SongBookDbContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = "Server=localhost;Database=songbook;User=songbook;Password=Qaz432101;";
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SongEntity>(entity =>
        {
            entity.HasIndex(e => e.Title);
            entity.HasIndex(e => e.Artist);
        });
    }
}