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
    /// Получает миниатюру видео в виде массива байт.
    /// Application-слой не зависит от System.Drawing и передаёт сырые данные выше.
    /// </summary>
    Task<byte[]> GetThumbnailAsync(string videoPath, int width, int height, CancellationToken ct = default);
}