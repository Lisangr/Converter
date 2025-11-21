using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.ErrorHandling;

/// <summary>
/// Обработчик ошибок сети.
/// Отвечает за валидацию и обработку путей к сетевым файлам.
/// </summary>
public interface INetworkPathValidator
{
    /// <summary>
    /// Проверяет, является ли путь сетевым путем.
    /// </summary>
    /// <param name="path">Путь для проверки</param>
    /// <returns>True если путь сетевой, иначе false</returns>
    bool IsNetworkPath(string path);

    /// <summary>
    /// Валидирует сетевой путь и возвращает результат проверки.
    /// </summary>
    /// <param name="path">Путь для валидации</param>
    /// <returns>Результат валидации</returns>
    Task<ValidationResult> ValidateNetworkPathAsync(string path);
}

/// <summary>
/// Результат валидации пути.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsNetworkPath { get; set; }
}