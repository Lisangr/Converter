using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

public interface IThumbnailProvider : IAsyncDisposable
{
    Task<Stream> GetThumbnailAsync(string videoPath, int width, int height, CancellationToken ct);
}
