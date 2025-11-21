using System;
using System.Threading;
using Converter.Application.Abstractions;

namespace Converter.Application.UI;

/// <summary>
/// Репортер прогресса для UI с поддержкой контекста синхронизации.
/// Обеспечивает безопасное отображение прогресса в UI-потоке.
/// </summary>
public class UiProgressReporter
{
    private readonly ISynchronizationContextProvider _syncContextProvider;

    public UiProgressReporter(ISynchronizationContextProvider syncContextProvider)
    {
        _syncContextProvider = syncContextProvider ?? throw new ArgumentNullException(nameof(syncContextProvider));
    }

    /// <summary>
    /// Отправляет отчет о прогрессе в UI-поток.
    /// </summary>
    /// <param name="progress">Значение прогресса от 0.0 до 1.0</param>
    public void Report(double progress)
    {
        var syncContext = _syncContextProvider.Current;
        if (syncContext != null)
        {
            syncContext.Post(_ =>
            {
                ProgressChanged?.Invoke(this, progress);
            }, null);
        }
        else
        {
            ProgressChanged?.Invoke(this, progress);
        }
    }

    /// <summary>
    /// Создает объект прогресса для асинхронного отчета.
    /// </summary>
    public IProgress<double> CreateProgress()
    {
        return new Progress<double>(Report);
    }

    /// <summary>
    /// Событие изменения прогресса.
    /// </summary>
    public event EventHandler<double>? ProgressChanged;
}