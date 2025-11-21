using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Models;
using Converter.Domain.Models;

namespace Converter.Application.Services.Queue;

public class QueueManager
{
    private readonly IQueueItemProcessor _processor;
    private readonly SemaphoreSlim _semaphore;
    private readonly object _lock = new();
    private readonly List<TaskCompletionSource<bool>> _pendingTasks = new();
    private bool _stopped;

    public QueueManager(IQueueItemProcessor processor, int maxConcurrent)
    {
        if (maxConcurrent <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrent));

        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    public Task EnqueueAsync(QueueItem item, CancellationToken cancellationToken = default)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        TaskCompletionSource<bool> tcs;

        lock (_lock)
        {
            if (_stopped)
            {
                throw new TaskCanceledException("Queue manager has been stopped.");
            }

            tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingTasks.Add(tcs);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    var result = await _processor.ProcessAsync(item, cancellationToken).ConfigureAwait(false);
                    if (!tcs.Task.IsCompleted)
                    {
                        tcs.TrySetResult(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (!tcs.Task.IsCompleted)
                    {
                        tcs.TrySetCanceled();
                    }
                }
                catch (Exception ex)
                {
                    if (!tcs.Task.IsCompleted)
                    {
                        tcs.TrySetException(ex);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            finally
            {
                lock (_lock)
                {
                    _pendingTasks.Remove(tcs);
                }
            }
        }, CancellationToken.None);

        return tcs.Task;
    }

    public Task StopAsync()
    {
        List<Task> tasksToCancel;

        lock (_lock)
        {
            if (_stopped)
            {
                return Task.CompletedTask;
            }

            _stopped = true;
            tasksToCancel = _pendingTasks.Select(t => (Task)t.Task).ToList();

            foreach (var tcs in _pendingTasks)
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetCanceled();
                }
            }

            _pendingTasks.Clear();
        }

        if (tasksToCancel.Count == 0)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(tasksToCancel);
    }
}
