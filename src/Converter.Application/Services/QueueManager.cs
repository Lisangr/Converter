using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Application.Abstractions;

namespace Converter.Application.Services;

/// <summary>
/// Менеджер очереди конвертации с поддержкой параллельной обработки.
/// Управляет выполнением элементов очереди с ограничением по количеству одновременных операций.
/// </summary>
public class QueueManager : IDisposable
{
    private readonly IQueueItemProcessor _processor;
    private readonly int _maxConcurrent;
    private readonly Queue<QueueItem> _queue = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;
    private bool _isRunning = false;

    public QueueManager(IQueueItemProcessor processor, int maxConcurrent = 1)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _maxConcurrent = maxConcurrent > 0 ? maxConcurrent : 1;
        _semaphore = new SemaphoreSlim(_maxConcurrent, _maxConcurrent);
    }

    /// <summary>
    /// Добавляет элемент в очередь для обработки.
    /// </summary>
    /// <param name="item">Элемент для добавления</param>
    public async Task EnqueueAsync(QueueItem item)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(QueueManager));
        if (item == null) throw new ArgumentNullException(nameof(item));

        _queue.Enqueue(item);

        if (!_isRunning)
        {
            _isRunning = true;
            _ = Task.Run(ProcessQueueAsync, _cts.Token);
        }
    }

    /// <summary>
    /// Останавливает обработку очереди.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning) return;

        _cts.Cancel();
        _isRunning = false;

        // Ожидаем завершения текущих операций
        await Task.Delay(100);
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested && _queue.Count > 0)
            {
                await _semaphore.WaitAsync(_cts.Token);
                
                if (_queue.TryDequeue(out var item))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _processor.ProcessAsync(item, _cts.Token);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }, _cts.Token);
                }
                else
                {
                    _semaphore.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ожидаемое поведение при отмене
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _cts.Cancel();
        _semaphore?.Dispose();
        _cts?.Dispose();
    }
}