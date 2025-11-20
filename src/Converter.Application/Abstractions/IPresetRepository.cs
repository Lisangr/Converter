namespace Converter.Application.Abstractions;

/// <summary>
/// Репозиторий пресетов конвертации.
/// Обеспечивает CRUD операции с пользовательскими пресетами конвертации,
/// включая загрузку, сохранение и удаление настроек конвертации.
/// </summary>
public interface IPresetRepository
{
    /// <summary>
    /// Получает все доступные пресеты конвертации.
    /// Включает как встроенные, так и пользовательские пресеты.
    /// </summary>
    /// <returns>Коллекция всех доступных пресетов</returns>
    Task<IReadOnlyList<Converter.Models.ConversionProfile>> GetAllPresetsAsync();
    
    /// <summary>
    /// Сохраняет пользовательский пресет конвертации.
    /// Обновляет существующий пресет или создает новый.
    /// </summary>
    /// <param name="preset">Пресет для сохранения</param>
    Task SavePresetAsync(Converter.Models.ConversionProfile preset);
    
    /// <summary>
    /// Удаляет пользовательский пресет по имени.
    /// Не может удалить встроенные пресеты.
    /// </summary>
    /// <param name="presetName">Имя удаляемого пресета</param>
    Task DeletePresetAsync(string presetName);
}
