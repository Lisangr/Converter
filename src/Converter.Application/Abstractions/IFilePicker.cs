using System;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Интерфейс для выбора файлов через пользовательский интерфейс.
    /// Абстрагирует платформо-специфичную логику выбора файлов,
    /// обеспечивая кроссплатформенность и тестируемость.
    /// </summary>
    public interface IFilePicker
    {
        /// <summary>
        /// Открывает диалог выбора файлов и возвращает массив выбранных путей.
        /// </summary>
        /// <param name="title">Заголовок диалога выбора файлов</param>
        /// <param name="filter">Фильтр типов файлов (например, "Video Files|*.mp4;*.avi")</param>
        /// <returns>Массив путей к выбранным файлам</returns>
        string[] PickFiles(string title, string filter);
    }
}
