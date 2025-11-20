using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

/// <summary>
/// Провайдер миниатюр видеофайлов.
/// Обеспечивает генерацию превью изображений из видеофайлов с настраиваемыми
/// размерами и поддержкой кэширования для оптимизации производительности.
/// </summary>
public interface IThumbnailProvider : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Получает миниатюру видеофайла в указанном размере.
    /// Извлекает кадр из видео и масштабирует его до требуемых размеров.
    /// Поддерживает асинхронное выполнение и отмену операции.
    /// </summary>
    /// <param name="videoPath">Путь к видеофайлу</param>
    /// <param name="width">Ширина миниатюры в пикселях</param>
    /// <param name="height">Высота миниатюры в пикселях</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Поток с изображением миниатюры</returns>
    Task<Stream> GetThumbnailAsync(string videoPath, int width, int height, CancellationToken ct);
}
