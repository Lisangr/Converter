using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services;

/// <summary>
/// HostedService-обёртка вокруг IQueueProcessor на базе каналов.
/// Отвечает только за жизненный цикл фоновой обработки, не зная о UI.
/// </summary>
public sealed class QueueWorkerHostedService : IHostedService
{
    private readonly IQueueProcessor _queueProcessor;
    private readonly ILogger<QueueWorkerHostedService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _workerTask;

    public QueueWorkerHostedService(
        IQueueProcessor queueProcessor,
        ILogger<QueueWorkerHostedService> logger)
    {
        _queueProcessor = queueProcessor ?? throw new ArgumentNullException(nameof(queueProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting QueueWorkerHostedService");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // QueueWorkerHostedService больше не запускает цикл обработки самостоятельно.
        // Это делает StartConversionCommand, когда пользователь нажимает "Начать конвертацию"
        
        _workerTask = Task.CompletedTask;

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping QueueWorkerHostedService");

        try
        {
            _cts?.Cancel();
            await _queueProcessor.StopProcessingAsync().ConfigureAwait(false);

            if (_workerTask is not null)
            {
                await Task.WhenAny(_workerTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken))
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _workerTask = null;
        }
    }

    private async Task RunProcessingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Инициализируем процессор очереди (загрузка существующих элементов)
            await _queueProcessor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);

            // Основной цикл чтения элементов из Channel и их обработки
            await foreach (var item in _queueProcessor.GetItemsAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    await _queueProcessor.ProcessItemAsync(item, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Processing loop cancelled by user");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing item {ItemId}, continuing with next item", item.Id);
                    // Продолжаем обработку следующих элементов
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("QueueWorkerHostedService processing was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in queue processing loop");
        }
        finally
        {
            _logger.LogInformation("QueueWorkerHostedService processing loop completed");
        }
    }
}
