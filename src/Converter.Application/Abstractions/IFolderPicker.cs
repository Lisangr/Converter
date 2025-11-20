using System;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Интерфейс для выбора папки через пользовательский интерфейс.
    /// Используется для выбора директории сохранения результатов конвертации
    /// и других операций, требующих выбора папки.
    /// </summary>
    public interface IFolderPicker
    {
        /// <summary>
        /// Открывает диалог выбора папки и возвращает путь к выбранной директории.
        /// </summary>
        /// <param name="description">Описание назначения выбора папки</param>
        /// <returns>Путь к выбранной папке или null, если выбор отменен</returns>
        string? PickFolder(string description);
    }
}
