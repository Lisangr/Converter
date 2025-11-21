using System;
using Converter.Domain.Models;
using Converter.Application.Models;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Построитель путей для файлов вывода.
    /// Отвечает за генерацию корректных путей сохранения конвертированных файлов
    /// с учетом настроек пользователя, пресетов и правил именования.
    /// </summary>
    public interface IOutputPathBuilder
    {
        /// <summary>
        /// Строит путь для файла вывода на основе элемента очереди и директории.
        /// Учитывает расширение файла и обеспечивает уникальность имени.
        /// </summary>
        /// <param name="item">Элемент очереди с информацией о файле</param>
        /// <param name="outputDirectory">Директория для сохранения</param>
        /// <param name="fileExtension">Расширение выходного файла (например, .mp4)</param>
        /// <returns>Полный путь к файлу вывода</returns>
        string BuildOutputPath(QueueItem item, string outputDirectory, string fileExtension);
        
        /// <summary>
        /// Строит путь для файла вывода на основе элемента очереди и пресета.
        /// Автоматически определяет расширение и параметры на основе пресета.
        /// </summary>
        /// <param name="item">Элемент очереди с информацией о файле</param>
        /// <param name="profile">Профиль конвертации с настройками</param>
        /// <returns>Полный путь к файлу вывода</returns>
        string BuildOutputPath(QueueItem item, Models.ConversionProfile profile);
        
        /// <summary>
        /// Генерирует уникальное имя файла, добавляя числовые суффиксы при необходимости.
        /// Предотвращает перезапись существующих файлов.
        /// </summary>
        /// <param name="basePath">Базовый путь к файлу (без суффиксов)</param>
        /// <returns>Уникальный путь к файлу</returns>
        string GenerateUniqueFileName(string basePath);
    }
}