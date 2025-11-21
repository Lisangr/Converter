using Converter.Application.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

/// <summary>
/// Репозиторий для работы с пресетами конвертации.
/// </summary>
public interface IPresetRepository
{
    /// <summary>
    /// Получает все доступные пресеты.
    /// </summary>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Коллекция пресетов</returns>
    Task<IReadOnlyList<ConversionProfile>> GetPresetsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Получает пресет по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пресета</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Пресет или null если не найден</returns>
    Task<ConversionProfile?> GetPresetAsync(string id, CancellationToken ct = default);
    
    /// <summary>
    /// Сохраняет пресет.
    /// </summary>
    /// <param name="preset">Пресет для сохранения</param>
    /// <param name="ct">Токен отмены</param>
    Task SavePresetAsync(ConversionProfile preset, CancellationToken ct = default);
    
    /// <summary>
    /// Удаляет пресет.
    /// </summary>
    /// <param name="id">Идентификатор пресета</param>
    /// <param name="ct">Токен отмены</param>
    Task DeletePresetAsync(string id, CancellationToken ct = default);
}