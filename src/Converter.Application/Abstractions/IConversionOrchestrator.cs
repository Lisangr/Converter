using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

/// <summary>
/// Оркестратор процесса конвертации видео.
/// Координирует все этапы конвертации, включая анализ файла и выполнение конвертации.
/// Обеспечивает высокоуровневый контроль над процессом преобразования медиафайлов.
/// </summary>
public interface IConversionOrchestrator
{
    /// <summary>
    /// Анализирует видеофайл для получения метаданных и технической информации.
    /// Проверяет совместимость файла с системой конвертации.
    /// </summary>
    /// <param name="filePath">Путь к анализируемому файлу</param>
    /// <param name="ct">Токен отмены операции</param>
    Task ProbeAsync(string filePath, CancellationToken ct);
    
    /// <summary>
    /// Выполняет конвертацию видеофайла согласно указанным параметрам.
    /// Поддерживает отслеживание прогресса и может быть отменен через CancellationToken.
    /// </summary>
    /// <param name="request">Запрос на конвертацию с параметрами</param>
    /// <param name="progress">Объект для отслеживания прогресса (0-100%)</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат конвертации с информацией об успешности, размере и ошибках</returns>
    Task<ConversionOutcome> ConvertAsync(ConversionRequest request, IProgress<int> progress, CancellationToken ct);
}

public sealed record ConversionRequest(string InputPath, string OutputPath, ConversionProfile Profile, int? TargetWidth = null, int? TargetHeight = null);
public sealed record ConversionProfile(string Name, string VideoCodec, string AudioCodec, string? AudioBitrateK, int? Crf);
public sealed record ConversionOutcome(bool Success, long? OutputSize, string? ErrorMessage);
