using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Converter.Services.UIServices
{
    public class ConversionTask
    {
        public string Name { get; }
        public Func<IProgress<int>, Task> WorkAsync { get; }

        public ConversionTask(string name, Func<IProgress<int>, Task> workAsync)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            WorkAsync = workAsync ?? throw new ArgumentNullException(nameof(workAsync));
        }
    }

    public class ConversionTaskEventArgs : EventArgs
    {
        public ConversionTask Task { get; }

        public ConversionTaskEventArgs(ConversionTask task)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
        }
    }

    public class ConversionTaskProgressEventArgs : EventArgs
    {
        public ConversionTask Task { get; }
        public int Progress { get; }

        public ConversionTaskProgressEventArgs(ConversionTask task, int progress)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
            Progress = progress;
        }
    }

    /// <summary>
    /// Простая очередь задач конвертации, которая последовательно выполняет задачи
    /// и репортит прогресс через события. Подходит как пример интеграции с WinForms UI.
    /// </summary>
    public class SimpleConversionQueue
    {
        private readonly Queue<ConversionTask> _queue = new();
        private bool _isProcessing;

        public event EventHandler<ConversionTaskEventArgs>? TaskEnqueued;
        public event EventHandler<ConversionTaskEventArgs>? TaskStarted;
        public event EventHandler<ConversionTaskEventArgs>? TaskCompleted;
        public event EventHandler<ConversionTaskProgressEventArgs>? TaskProgressChanged;

        public void Enqueue(ConversionTask task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            lock (_queue)
            {
                _queue.Enqueue(task);
                TaskEnqueued?.Invoke(this, new ConversionTaskEventArgs(task));

                if (!_isProcessing)
                {
                    _ = ProcessNextAsync();
                }
            }
        }

        private async Task ProcessNextAsync()
        {
            ConversionTask? task = null;

            lock (_queue)
            {
                if (_queue.Count == 0)
                {
                    _isProcessing = false;
                    return;
                }

                _isProcessing = true;
                task = _queue.Dequeue();
            }

            if (task == null)
            {
                return;
            }

            TaskStarted?.Invoke(this, new ConversionTaskEventArgs(task));

            var progress = new Progress<int>(p =>
            {
                if (p < 0) p = 0;
                if (p > 100) p = 100;
                TaskProgressChanged?.Invoke(this, new ConversionTaskProgressEventArgs(task, p));
            });

            await task.WorkAsync(progress).ConfigureAwait(false);

            TaskCompleted?.Invoke(this, new ConversionTaskEventArgs(task));

            await ProcessNextAsync().ConfigureAwait(false);
        }
    }
}
