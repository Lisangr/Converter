using System.Collections.Generic;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Провайдер профилей конвертации.
    /// Обеспечивает управление полным жизненным циклом профилей конвертации,
    /// включая получение, создание, обновление и удаление профилей.
    /// Отличается от IPresetRepository более широкой функциональностью.
    /// </summary>
    public interface IProfileProvider
    {
        /// <summary>
        /// Получает все доступные профили конвертации.
        /// Включает встроенные и пользовательские профили.
        /// </summary>
        /// <returns>Коллекция всех профилей конвертации</returns>
        Task<IReadOnlyList<Converter.Application.Models.ConversionProfile>> GetAllProfilesAsync();
        
        /// <summary>
        /// Получает профиль конвертации по уникальному идентификатору.
        /// </summary>
        /// <param name="id">Уникальный идентификатор профиля</param>
        /// <returns>Профиль конвертации или null, если не найден</returns>
        Task<Converter.Application.Models.ConversionProfile> GetProfileByIdAsync(string id);
        
        /// <summary>
        /// Получает профиль конвертации по умолчанию.
        /// Используется при первом запуске или отсутствии выбранного профиля.
        /// </summary>
        /// <returns>Профиль по умолчанию</returns>
        Task<Converter.Application.Models.ConversionProfile> GetDefaultProfileAsync();
        
        /// <summary>
        /// Устанавливает указанный профиль как профиль по умолчанию.
        /// Сохраняет настройки для будущих сеансов.
        /// </summary>
        /// <param name="id">Идентификатор профиля для установки по умолчанию</param>
        Task SetDefaultProfileAsync(string id);
        
        /// <summary>
        /// Сохраняет профиль конвертации (создание нового или обновление существующего).
        /// </summary>
        /// <param name="profile">Профиль для сохранения</param>
        Task SaveProfileAsync(Converter.Application.Models.ConversionProfile profile);
        
        /// <summary>
        /// Удаляет профиль конвертации по идентификатору.
        /// Нельзя удалить встроенные профили.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого профиля</param>
        Task DeleteProfileAsync(string id);
    }
}
