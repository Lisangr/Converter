using System;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.ErrorHandling;

/// <summary>
/// Обработчик ошибок FFmpeg.
/// Отвечает за обработку ошибок, связанных с выполнением команд FFmpeg.
/// </summary>
public interface IFfmpegErrorHandler
{
    /// <summary>
    /// Обрабатывает ошибку FFmpeg.
    /// </summary>
    /// <param name="command">Команда FFmpeg</param>
    /// <param name="exitCode">Код возврата</param>
    /// <param name="errorOutput">Вывод ошибок</param>
    /// <param name="context">Контекст операции</param>
    Task HandleErrorAsync(string command, int exitCode, string errorOutput, string context);
}