using Converter.Application.Interfaces;
using Converter.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;

namespace Converter.Infrastructure.Services;

public sealed class ThumbnailProvider : IThumbnailProvider
{
    private readonly ILogger<ThumbnailProvider> _logger;
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public ThumbnailProvider(ILogger<ThumbnailProvider> logger)
    {
        _logger = logger;
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 32
        });
    }

    public async Task<Stream> GetThumbnailAsync(ThumbnailRequest request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{request.MediaPath}:{request.Width}x{request.Height}:{request.CapturePosition}";
        if (_cache.TryGetValue<byte[]>(cacheKey, out var cachedBytes))
        {
            return new MemoryStream(cachedBytes, writable: false);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue<byte[]>(cacheKey, out cachedBytes))
            {
                return new MemoryStream(cachedBytes, writable: false);
            }

            var outputPath = Path.Combine(Path.GetTempPath(), $"thumb_{Guid.NewGuid():N}.jpg");
            var conversion = FFmpeg.Conversions.New();
            conversion.AddParameter($"-i \"{request.MediaPath}\" -ss {request.CapturePosition.TotalSeconds} -frames:v 1 -s {request.Width}x{request.Height} \"{outputPath}\"");
            await conversion.Start(cancellationToken).ConfigureAwait(false);
            var bytes = await File.ReadAllBytesAsync(outputPath, cancellationToken).ConfigureAwait(false);
            File.Delete(outputPath);
            _cache.Set(cacheKey, bytes, new MemoryCacheEntryOptions { Size = 1, SlidingExpiration = TimeSpan.FromMinutes(5) });
            return new MemoryStream(bytes, writable: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build thumbnail for {Path}", request.MediaPath);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        _gate.Release();
        _gate.Dispose();
        _cache.Dispose();
    }
}
