// IFFmpegExecutor.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

/// <summary>
/// Исполнитель команд FFmpeg.
/// Абстрагирует взаимодействие с FFmpeg, обеспечивая выполнение медиаопераций.
/// Все операции являются асинхронными и поддерживают отмену.
/// </summary>
public interface IFFmpegExecutor
{
    /// <summary>
    /// Анализирует медиафайл для получения информации о его структуре.
    /// Используется для валидации и получения метаданных файла.
    /// </summary>
    /// <param name="inputPath">Путь к анализируемому файлу</param>
    /// <param name="ct">Токен отмены операции</param>
    Task ProbeAsync(string inputPath, CancellationToken ct);
    
    /// <summary>
    /// Выполняет произвольную команду FFmpeg с указанными аргументами.
    /// Поддерживает отслеживание прогресса выполнения.
    /// </summary>
    /// <param name="arguments">Аргументы командной строки для FFmpeg</param>
    /// <param name="progress">Объект для отслеживания прогресса (0.0-1.0)</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Код возврата команды FFmpeg</returns>
    Task<int> ExecuteAsync(string arguments, IProgress<double> progress, CancellationToken ct);
    
    /// <summary>
    /// Получает версию установленного FFmpeg.
    /// Используется для проверки совместимости и возможностей.
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Строка с версией FFmpeg</returns>
    Task<string> GetVersionAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Проверяет доступность FFmpeg в системе.
    /// Определяет, может ли приложение выполнять медиаоперации.
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>True если FFmpeg доступен, иначе false</returns>
    Task<bool> IsFfmpegAvailableAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Получает детальную информацию о медиафайле.
    /// Возвращает JSON или XML с техническими характеристиками.
    /// </summary>
    /// <param name="inputPath">Путь к медиафайлу</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Строка с информацией о медиафайле</returns>
    Task<string> GetMediaInfoAsync(string inputPath, CancellationToken ct = default);
}