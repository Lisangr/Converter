namespace Converter.Application.Abstractions;

public interface IThumbnailProvider : IAsyncDisposable
{
    Task<Stream> GetAsync(string videoPath, CancellationToken cancellationToken);
}
