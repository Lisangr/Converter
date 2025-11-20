using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Services
{
    /// <summary>
    /// Простая реализация асинхронного AutoResetEvent для синхронизации задач
    /// </summary>
    public sealed class AsyncAutoResetEvent
    {
        private TaskCompletionSource<bool> _tcs = new();
        private readonly CancellationTokenSource _cts = new();
        private bool _isPaused = false;

        public AsyncAutoResetEvent()
        {
            _tcs.SetResult(true); // Изначально сигнализирован
        }

        public bool IsPaused => _isPaused;

        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
            try
            {
                await _tcs.Task;
            }
            catch (TaskCanceledException) when (linkedCts.Token.IsCancellationRequested)
            {
                // Ожидание отменено
            }
        }

        public void Set()
        {
            _isPaused = false;
            
            // Атомарно заменяем TaskCompletionSource
            var oldTcs = Interlocked.Exchange(ref _tcs, new TaskCompletionSource<bool>());
            oldTcs.SetResult(true);
        }

        public void Reset()
        {
            _isPaused = true;
            _tcs = new TaskCompletionSource<bool>();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _tcs.SetCanceled();
        }
    }
}