using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public sealed class AddFilesCommand : IAddFilesCommand
    {
        private readonly IQueueRepository _queueRepository;
        private readonly ILogger<AddFilesCommand> _logger;

        public AddFilesCommand(IQueueRepository queueRepository, ILogger<AddFilesCommand> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync(IEnumerable<string> filePaths, string? outputFolder, CancellationToken cancellationToken = default)
        {
            if (filePaths == null) return;

            var items = new List<QueueItem>();

            foreach (var file in filePaths)
            {
                if (!File.Exists(file))
                    continue;

                var directory = !string.IsNullOrWhiteSpace(outputFolder)
                    ? outputFolder
                    : Path.GetDirectoryName(file) ?? string.Empty;

                items.Add(new QueueItem
                {
                    Id = Guid.NewGuid(),
                    FilePath = file,
                    FileSizeBytes = new FileInfo(file).Length,
                    OutputDirectory = directory,
                    Status = ConversionStatus.Pending,
                    Progress = 0,
                    AddedAt = DateTime.UtcNow
                });
            }

            if (items.Count == 0)
                return;

            await _queueRepository.AddRangeAsync(items).ConfigureAwait(false);
            _logger.LogInformation("Added {Count} files to queue", items.Count);
        }
    }

    public sealed class StartConversionCommand : IStartConversionCommand
    {
        private readonly IQueueProcessor _queueProcessor;
        private Task? _workerTask;
        private bool _isRunning;
        private readonly object _syncRoot = new();

        public StartConversionCommand(IQueueProcessor queueProcessor)
        {
            _queueProcessor = queueProcessor ?? throw new ArgumentNullException(nameof(queueProcessor));
        }

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            lock (_syncRoot)
            {
                if (_isRunning)
                {
                    // Уже запущено, повторный вызов игнорируем
                    return Task.CompletedTask;
                }

                _isRunning = true;
                _workerTask = RunAsync(cancellationToken);
                return Task.CompletedTask;
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Инициализируем процессор (загрузка Pending-элементов в канал)
                await _queueProcessor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);

                // Основной цикл обработки: читаем элементы из канала и обрабатываем их
                await foreach (var item in _queueProcessor.GetItemsAsync(cancellationToken).ConfigureAwait(false))
                {
                    await _queueProcessor.ProcessItemAsync(item, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                lock (_syncRoot)
                {
                    _isRunning = false;
                    _workerTask = null;
                }
            }
        }
    }

    public sealed class CancelConversionCommand : ICancelConversionCommand
    {
        private readonly IQueueRepository _queueRepository;
        private readonly IQueueProcessor _queueProcessor;
        private readonly ILogger<CancelConversionCommand> _logger;

        public CancelConversionCommand(
            IQueueRepository queueRepository,
            IQueueProcessor queueProcessor,
            ILogger<CancelConversionCommand> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _queueProcessor = queueProcessor ?? throw new ArgumentNullException(nameof(queueProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Canceling all conversions via command");

            await _queueProcessor.StopProcessingAsync().ConfigureAwait(false);

            var items = await _queueRepository.GetAllAsync().ConfigureAwait(false);

            foreach (var item in items.Where(i => i.Status == ConversionStatus.Processing))
            {
                item.Status = ConversionStatus.Failed;
                item.ErrorMessage = "Конвертация отменена пользователем";
                await _queueRepository.UpdateAsync(item).ConfigureAwait(false);
            }
        }
    }

    public sealed class RemoveSelectedFilesCommand : IRemoveSelectedFilesCommand
    {
        private readonly IQueueRepository _queueRepository;
        private readonly ILogger<RemoveSelectedFilesCommand> _logger;

        public RemoveSelectedFilesCommand(IQueueRepository queueRepository, ILogger<RemoveSelectedFilesCommand> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync(IEnumerable<Guid> itemIds, CancellationToken cancellationToken = default)
        {
            if (itemIds == null)
                return;

            var ids = itemIds.ToList();
            if (ids.Count == 0)
                return;

            await _queueRepository.RemoveRangeAsync(ids).ConfigureAwait(false);
            _logger.LogInformation("Removed {Count} items from queue", ids.Count);
        }
    }

    public sealed class ClearQueueCommand : IClearQueueCommand
    {
        private readonly IQueueRepository _queueRepository;
        private readonly ILogger<ClearQueueCommand> _logger;

        public ClearQueueCommand(IQueueRepository queueRepository, ILogger<ClearQueueCommand> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var items = await _queueRepository.GetAllAsync().ConfigureAwait(false);
            var ids = items.Select(i => i.Id).ToList();

            if (ids.Count == 0)
                return;

            await _queueRepository.RemoveRangeAsync(ids).ConfigureAwait(false);
            _logger.LogInformation("Cleared {Count} items from queue", ids.Count);
        }
    }
}
