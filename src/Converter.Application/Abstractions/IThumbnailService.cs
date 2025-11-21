using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

/// <summary>
/// Генератор миниатюр видеофайлов.
/// </summary>
public interface IThumbnailGenerator
{
    /// <summary>
    /// Генерирует миниатюру для видеофайла.
    /// </summary>
    Task<Stream> GenerateThumbnailAsync(string videoPath, int width, int height, CancellationToken ct);
}

/// <summary>
/// Сервис работы с миниатюрами.
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Получает миниатюру видео.
    /// </summary>
    Task<Stream> GetThumbnailAsync(string videoPath, int width, int height);
}