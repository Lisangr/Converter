using Converter.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions
{
    public interface IQueueProcessor
    {
        event EventHandler<QueueItem> ItemStarted;
        event EventHandler<QueueItem> ItemCompleted;
        event EventHandler<QueueItem> ItemFailed;
        event EventHandler<QueueProgressEventArgs> ProgressChanged;
        event EventHandler QueueCompleted;
        
        bool IsRunning { get; }
        bool IsPaused { get; }
        
        Task StartProcessingAsync(CancellationToken cancellationToken = default);
        Task StopProcessingAsync();
        Task PauseProcessingAsync();
        Task ResumeProcessingAsync();
        Task ProcessItemAsync(QueueItem item, CancellationToken cancellationToken = default);
    }

    public class QueueProgressEventArgs : EventArgs
    {
        public QueueItem Item { get; }
        public int Progress { get; }
        public string Status { get; }

        public QueueProgressEventArgs(QueueItem item, int progress, string status = null)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Progress = progress;
            Status = status;
        }
    }
}
