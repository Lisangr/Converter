using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Services.FileMedia;

public class FileService
{
    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(path));
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return Task.CompletedTask;
    }

    public Task<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var info = new FileInfo(path);
        return Task.FromResult(info.Exists ? info.Length : 0L);
    }
}
