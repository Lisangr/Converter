using Converter.Application.Abstractions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Infrastructure.Ffmpeg;

public sealed class ThumbnailProvider : IThumbnailProvider
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task<Stream> GetThumbnailAsync(string videoPath, int width, int height, CancellationToken ct)
    {
        // Placeholder: return empty stream for now. Can be wired to FFmpeg extraction later.
        Stream s = new MemoryStream();
        return Task.FromResult(s);
    }
}
