using System;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.Application.ErrorHandling;

/// <summary>
/// Обработчик ошибок FFmpeg.
/// </summary>
public class FfmpegErrorHandler : IFfmpegErrorHandler
{
    public async Task HandleErrorAsync(string command, int exitCode, string errorOutput, string context)
    {
        // Заглушка для тестов
        await Task.CompletedTask;
    }
}

/// <summary>
/// Обработчик поврежденных файлов.
/// </summary>
public class CorruptedFileHandler
{
    public async Task HandleCorruptedFileAsync(string filePath, string reason)
    {
        // Заглушка для тестов
        await Task.CompletedTask;
    }
}