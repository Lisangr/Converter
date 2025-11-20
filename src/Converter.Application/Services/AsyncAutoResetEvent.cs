using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Services
{
    /// <summary>
    /// Асинхронный AutoResetEvent для синхронизации задач с поддержкой паузы
    /// </summary>
    public sealed class AsyncAutoResetEvent : IAsyncDisposable
    {
        private readonly object _lock = new();
        private TaskCompletionSource<bool> _tcs = new();
        private readonly CancellationTokenSource _cts = new();
        private bool _isPaused = false;
        private bool _disposed = false;

        public AsyncAutoResetEvent()
        {
            _tcs.SetResult(true); // Изначально сигнализирован
        }

        public bool IsPaused
        {
            get
            {
                lock (_lock)
                {
                    return _isPaused;
                }
            }
        }

        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AsyncAutoResetEvent));

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
            var token = linkedCts.Token;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                lock (_lock)
                {
                    if (!_isPaused)
                    {
                        // Если не на паузе, создаём новый TCS и выходим
                        _tcs = new TaskCompletionSource<bool>();
                        return;
                    }
                }

                // Вне блокировки ждём сигнала
                try
                {
                    await _tcs.Task.ConfigureAwait(false);
                    // После получения сигнала проверим снова состояние
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }

        public void Set()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_isPaused)
                {
                    _isPaused = false;
                    // Сигнализируем текущий TCS
                    _tcs.SetResult(true);
                }
            }
        }

        public void Reset()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                _isPaused = true;
                // Создаём новый TCS для блокировки новых ожиданий
                _tcs = new TaskCompletionSource<bool>();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            lock (_lock)
            {
                _cts.Cancel();
                _tcs.SetCanceled();
            }
        }

        public async ValueTask DisposeAsync()
        {
            Dispose();
            await Task.CompletedTask;
        }
    }
}