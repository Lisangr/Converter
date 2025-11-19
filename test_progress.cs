// Тестовый файл для проверки системы прогресса
// Этот файл можно удалить после тестирования

using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Tests
{
    public class TestProgressService
    {
        private readonly IQueueProcessor _queueProcessor;
        private readonly IQueueRepository _queueRepository;
        private readonly ILogger<TestProgressService> _logger;

        public TestProgressService(
            IQueueProcessor queueProcessor,
            IQueueRepository queueRepository,
            ILogger<TestProgressService> logger)
        {
            _queueProcessor = queueProcessor;
            _queueRepository = queueRepository;
            _logger = logger;
        }

        public async Task TestProgressReporting()
        {
            _logger.LogInformation("Starting progress test");

            // Create a test item
            var testItem = new QueueItem
            {
                Id = Guid.NewGuid(),
                FilePath = @"C:\test\video.mp4", // Use a real file path
                FileSizeBytes = 1024 * 1024, // 1MB
                Status = ConversionStatus.Pending,
                Progress = 0
            };

            // Add to repository
            await _queueRepository.AddAsync(testItem);

            // Start processing
            await _queueProcessor.StartProcessingAsync();

            // Wait for completion
            await Task.Delay(10000); // Wait 10 seconds

            await _queueProcessor.StopProcessingAsync();
            _logger.LogInformation("Progress test completed");
        }
    }
}