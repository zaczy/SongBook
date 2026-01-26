using Microsoft.EntityFrameworkCore;

namespace Zaczy.SongBook.Data;

public class SongRepository
{
    private readonly SongBookDbContext _context;

    public SongRepository(SongBookDbContext context)
    {
        _context = context;
    }

    public async Task<List<SongEntity>> GetAllAsync()
    {
        return await _context.Songs.ToListAsync();
    }

    public async Task<SongEntity?> GetByIdAsync(int id)
    {
        return await _context.Songs.FindAsync(id);
    }

    public async Task<SongEntity> AddAsync(Song song, string? sourceHtml = null)
    {
        var entity = new SongEntity
        {
            CreatedAt = DateTime.UtcNow
        };

        entity.initFromSong(song);

        _context.Songs.Add(entity);
        await _context.SaveChangesAsync();
        
        return entity;
    }

    public async Task UpdateAsync(SongEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Songs.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Songs.FindAsync(id);
        if (entity != null)
        {
            _context.Songs.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<SongEntity?> SearchOnlySongAsync(Song song)
    {
        var songs = await _context.Songs
            .Where(s => s.Title == song.Title)
            .ToListAsync();

        if(songs.Count > 0)
        {
            return songs[0];
        }

        return null;
    }

    public async Task<List<SongEntity>> SearchAsync(string query)
    {
        return await _context.Songs
            .Where(s => s.Title!.Contains(query) || s.Artist!.Contains(query))
            .ToListAsync();
    }

    public async Task<SongEntity?> SearchIdAsync(int id)
    {
        return await _context.Songs
            .Where(s => s.Id == id)
            .FirstOrDefaultAsync();
    }

}