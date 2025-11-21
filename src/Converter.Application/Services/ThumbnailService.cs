using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;

namespace Converter.Application.Services;

/// <summary>
/// Сервис работы с миниатюрами видео.
/// </summary>
public class ThumbnailService : IThumbnailService
{
    private readonly IThumbnailGenerator _thumbnailGenerator;

    public ThumbnailService(IThumbnailGenerator thumbnailGenerator)
    {
        _thumbnailGenerator = thumbnailGenerator ?? throw new ArgumentNullException(nameof(thumbnailGenerator));
    }

    public async Task<Stream> GetThumbnailAsync(string videoPath, int width, int height)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
        return await _thumbnailGenerator.GenerateThumbnailAsync(videoPath, width, height, cts.Token);
    }
}

/// <summary>
/// Генератор миниатюр видеофайлов.
/// </summary>
public class ThumbnailGenerator : IThumbnailGenerator
{
    public async Task<Stream> GenerateThumbnailAsync(string videoPath, int width, int height, CancellationToken ct)
    {
        // Заглушка для тестов - возвращаем пустой поток
        return new MemoryStream();
    }
}