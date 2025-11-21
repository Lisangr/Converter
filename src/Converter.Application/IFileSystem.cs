using System;
using System.IO;
using System.Threading.Tasks;

namespace Converter.Application;

/// <summary>
/// Абстракция файловой системы для тестируемости.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Проверяет существование файла.
    /// </summary>
    Task<bool> FileExistsAsync(string path);

    /// <summary>
    /// Проверяет существование директории.
    /// </summary>
    Task<bool> DirectoryExistsAsync(string path);

    /// <summary>
    /// Создает директорию.
    /// </summary>
    Task CreateDirectoryAsync(string path);

    /// <summary>
    /// Получает информацию о файле.
    /// </summary>
    Task<FileInfo> GetFileInfoAsync(string path);
}