using Converter.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace Converter.Infrastructure.Ffmpeg;

public sealed class ThumbnailProvider : IThumbnailProvider
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 64 });
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Lazy<Task> _initialization;

    public ThumbnailProvider()
    {
        _initialization = new Lazy<Task>(InitializeAsync);
    }

    public async Task<Stream> GetAsync(string videoPath, CancellationToken cancellationToken)
    {
        await _initialization.Value.ConfigureAwait(false);
        var key = $"{videoPath}:{File.GetLastWriteTimeUtc(videoPath).Ticks}";
        if (_cache.TryGetValue<byte[]>(key, out var cached))
        {
            return new MemoryStream(cached, writable: false);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue<byte[]>(key, out cached))
            {
                return new MemoryStream(cached, writable: false);
            }

            var tempFile = Path.Combine(Path.GetTempPath(), $"thumb_{Guid.NewGuid():N}.jpg");
            var conversion = FFmpeg.Conversions.New();
            conversion.AddParameter($"-i \"{videoPath}\" -ss 00:00:01 -vframes 1 \"{tempFile}\"");
            await conversion.Start(cancellationToken).ConfigureAwait(false);
            var bytes = await File.ReadAllBytesAsync(tempFile, cancellationToken).ConfigureAwait(false);
            File.Delete(tempFile);
            _cache.Set(key, bytes, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Size = 1
            });
            return new MemoryStream(bytes, writable: false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task InitializeAsync()
    {
        var temp = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        if (!Directory.Exists(temp))
        {
            Directory.CreateDirectory(temp);
        }

        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, temp);
        FFmpeg.SetExecutablesPath(temp);
    }

    public async ValueTask DisposeAsync()
    {
        _cache.Dispose();
        await _gate.DisposeAsync().ConfigureAwait(false);
    }
}
