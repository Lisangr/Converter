using System.Threading;

namespace Converter.Application.Abstractions;

/// <summary>
/// Провайдер контекста синхронизации для работы с UI-потоком.
/// Обеспечивает безопасное выполнение операций в правильном контексте.
/// </summary>
public interface ISynchronizationContextProvider
{
    /// <summary>
    /// Получает текущий контекст синхронизации.
    /// Может быть null, если контекст не установлен.
    /// </summary>
    SynchronizationContext? Current { get; }
}