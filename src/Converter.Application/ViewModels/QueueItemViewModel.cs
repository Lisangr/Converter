using System;
using Converter.Models;

namespace Converter.Application.ViewModels
{
    public class QueueItemViewModel
    {
        public Guid Id { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; }
        public bool IsSelected { get; set; }

        public ConversionStatus Status { get; set; }
        public int Progress { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OutputPath { get; set; }
        public long? OutputFileSizeBytes { get; set; }

        public static QueueItemViewModel FromModel(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            return new QueueItemViewModel
            {
                Id = item.Id,
                FileName = Path.GetFileName(item.FilePath),
                FilePath = item.FilePath,
                FileSizeBytes = item.FileSizeBytes,
                Status = item.Status,
                Progress = item.Progress,
                ErrorMessage = item.ErrorMessage,
                OutputPath = item.OutputPath,
                OutputFileSizeBytes = item.OutputFileSizeBytes
            };
        }

        public void UpdateFromModel(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            Status = item.Status;
            Progress = item.Progress;
            ErrorMessage = item.ErrorMessage;
            OutputPath = item.OutputPath;
            OutputFileSizeBytes = item.OutputFileSizeBytes;
        }
    }
}
