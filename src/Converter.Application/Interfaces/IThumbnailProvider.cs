using Converter.Domain.Models;

namespace Converter.Application.Interfaces;

public interface IThumbnailProvider : IAsyncDisposable
{
    Task<Stream> GetThumbnailAsync(ThumbnailRequest request, CancellationToken cancellationToken);
}
