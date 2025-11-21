using System;
using System.IO;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.ErrorHandling;

/// <summary>
/// Обработчик ошибок файловой системы.
/// Отвечает за обработку ошибок, связанных с файловыми операциями.
/// </summary>
public interface IFileSystemErrorHandler
{
    /// <summary>
    /// Обрабатывает ошибку файловой системы.
    /// </summary>
    /// <param name="path">Путь к файлу или директории</param>
    /// <param name="exception">Исключение</param>
    /// <param name="context">Контекст операции</param>
    Task HandleErrorAsync(string path, Exception exception, string context);
}

/// <summary>
/// Валидатор файлов.
/// Проверяет корректность и целостность файлов.
/// </summary>
public interface IFileValidator
{
    /// <summary>
    /// Проверяет, является ли файл корректным.
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    /// <returns>Результат проверки</returns>
    Task<ValidationResult> ValidateAsync(string filePath);
}

/// <summary>
/// Проверщик дискового пространства.
/// Отвечает за проверку доступного места на диске.
/// </summary>
public interface IDiskSpaceChecker
{
    /// <summary>
    /// Проверяет доступное место на диске.
    /// </summary>
    /// <param name="path">Путь для проверки</param>
    /// <param name="requiredSpace">Требуемое пространство в байтах</param>
    /// <returns>Результат проверки</returns>
    Task<DiskSpaceResult> CheckAvailableSpaceAsync(string path, long requiredSpace);
}

/// <summary>
/// Результат проверки дискового пространства.
/// </summary>
public class DiskSpaceResult
{
    public bool HasEnoughSpace { get; set; }
    public long AvailableSpace { get; set; }
    public long RequiredSpace { get; set; }
    public string? Message { get; set; }
}