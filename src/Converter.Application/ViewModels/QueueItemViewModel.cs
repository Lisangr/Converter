using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Converter.Domain.Models;

namespace Converter.Application.ViewModels
{
    public class QueueItemViewModel : INotifyPropertyChanged
    {
        public Guid Id { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private ConversionStatus _status;
        public ConversionStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private string? _outputPath;
        public string? OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }

        private long? _outputFileSizeBytes;
        public long? OutputFileSizeBytes
        {
            get => _outputFileSizeBytes;
            set => SetProperty(ref _outputFileSizeBytes, value);
        }

        private bool _isStarred;
        public bool IsStarred
        {
            get => _isStarred;
            set => SetProperty(ref _isStarred, value);
        }

        private int _priority;
        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        private string? _namingPattern;
        public string? NamingPattern
        {
            get => _namingPattern;
            set => SetProperty(ref _namingPattern, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (propertyName == null) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

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
                OutputFileSizeBytes = item.OutputFileSizeBytes,
                IsStarred = item.IsStarred,
                Priority = item.Priority,
                NamingPattern = item.NamingPattern
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
            IsStarred = item.IsStarred;
            Priority = item.Priority;
            NamingPattern = item.NamingPattern;
        }
    }
}
