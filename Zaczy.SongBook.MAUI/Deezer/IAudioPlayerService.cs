using System.IO;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI.Services;

public interface IAudioPlayerService
{
    Task PlayFromStreamAsync(Stream audioStream, string fileName);
    Task PlayFromBytesAsync(byte[] audioBytes, string fileName);
    void Stop();
}

public class AudioPlayerService : IAudioPlayerService
{
    private string? _currentTempFile;

    public async Task PlayFromStreamAsync(Stream audioStream, string fileName)
    {
        Stop(); // Zatrzymaj poprzednie odtwarzanie
        
        var tempFile = Path.Combine(FileSystem.CacheDirectory, fileName);
        _currentTempFile = tempFile;

        using (var fileStream = File.Create(tempFile))
        {
            await audioStream.CopyToAsync(fileStream);
        }

        await LaunchFileAsync(tempFile);
    }

    public async Task PlayFromBytesAsync(byte[] audioBytes, string fileName)
    {
        Stop();
        
        var tempFile = Path.Combine(FileSystem.CacheDirectory, fileName);
        _currentTempFile = tempFile;

        await File.WriteAllBytesAsync(tempFile, audioBytes);
        await LaunchFileAsync(tempFile);
    }

    private async Task LaunchFileAsync(string filePath)
    {
        try
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"B³¹d odtwarzania: {ex.Message}");
        }
    }

    public void Stop()
    {
        // Usuñ poprzedni plik tymczasowy
        if (!string.IsNullOrEmpty(_currentTempFile) && File.Exists(_currentTempFile))
        {
            try
            {
                File.Delete(_currentTempFile);
            }
            catch { }
        }
    }
}