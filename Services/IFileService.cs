using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Services
{
    /// <summary>
    /// Сервис для работы с файлами медиаконтента.
    /// Обеспечивает операции с видео и аудиофайлами, включая получение миниатюр,
    /// анализ файлов и проверку поддерживаемых форматов.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Получает миниатюру для указанного файла.
        /// Генерирует превью изображение для видеофайлов или возвращает заглушку.
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="width">Желаемая ширина миниатюры</param>
        /// <param name="height">Желаемая высота миниатюры</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Объект Image с миниатюрой</returns>
        Task<Image> GetThumbnailAsync(string filePath, int width, int height, CancellationToken cancellationToken);
        
        /// <summary>
        /// Анализирует файл и возвращает его техническую информацию.
        /// Проверяет валидность файла и извлекает метаданные.
        /// </summary>
        /// <param name="filePath">Путь к анализируемому файлу</param>
        /// <returns>Объект FileInfo с информацией о файле</returns>
        Task<FileInfo> ProbeFileAsync(string filePath);
        
        /// <summary>
        /// Получает список поддерживаемых расширений файлов.
        /// Возвращает все форматы, которые может обрабатывать приложение.
        /// </summary>
        /// <returns>Массив поддерживаемых расширений (например, .mp4, .avi)</returns>
        string[] GetSupportedFileExtensions();
        
        /// <summary>
        /// Проверяет, поддерживается ли указанный файл.
        /// Определяет возможность обработки файла на основе его расширения.
        /// </summary>
        /// <param name="filePath">Путь к проверяемому файлу</param>
        /// <returns>True если файл поддерживается, иначе false</returns>
        bool IsFileSupported(string filePath);
        
        /// <summary>
        /// Создает заглушку-миниатюру с текстом.
        /// Используется для файлов, которые не удалось обработать или для ошибок.
        /// </summary>
        /// <param name="width">Ширина изображения</param>
        /// <param name="height">Высота изображения</param>
        /// <param name="text">Текст для отображения на заглушке</param>
        /// <returns>Объект Image с заглушкой</returns>
        Image CreatePlaceholderThumbnail(int width, int height, string text);
    }
}