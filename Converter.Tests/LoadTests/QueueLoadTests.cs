using FluentAssertions;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Converter.Services.UIServices;
using Microsoft.Extensions.Logging;
using Moq;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.Tests.LoadTests
{
    public class QueueLoadTests : IDisposable
    {
        private const int TestFileCount = 100;
        private const int ConcurrentThreads = 10;
        private readonly Mock<IQueueRepository> _mockQueueRepository;
        private readonly Mock<IQueueProcessor> _mockQueueProcessor;
        private readonly Mock<ILogger<FileOperationsService>> _mockLogger;
        private readonly FileOperationsService _fileService;
        private readonly string _testDirectory;

        public QueueLoadTests()
        {
            _mockQueueRepository = new Mock<IQueueRepository>();
            _mockQueueProcessor = new Mock<IQueueProcessor>();
            _mockLogger = new Mock<ILogger<FileOperationsService>>();
            
            _fileService = new FileOperationsService(
                _mockQueueRepository.Object,
                _mockQueueProcessor.Object,
                _mockLogger.Object);

            _testDirectory = Path.Combine(Path.GetTempPath(), "ConverterLoadTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public async Task AddMultipleFiles_ShouldHandleConcurrentAdds()
        {
            // Arrange
            var files = new List<string>();
            for (int i = 0; i < TestFileCount; i++)
            {
                files.Add(CreateTestFile($"test_{i}.txt", $"Test content {i}"));
            }

            _mockQueueRepository.Setup(x => x.AddAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

            // Act
            var tasks = new List<Task>();
            var batchSize = TestFileCount / ConcurrentThreads;
            
            for (int i = 0; i < ConcurrentThreads; i++)
            {
                var batch = files.Skip(i * batchSize).Take(batchSize).ToArray();
                tasks.Add(_fileService.AddFilesAsync(batch));
            }

            await Task.WhenAll(tasks);

            // Assert
            _mockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(TestFileCount));
        }

        [Fact]
        public async Task ProcessLargeQueue_ShouldHandleLargeNumberOfItems()
        {
            // Arrange
            var files = new List<string>();
            for (int i = 0; i < 500; i++)
            {
                files.Add(CreateTestFile($"large_test_{i}.txt", $"Large test content {i}"));
            }

            _mockQueueRepository.Setup(x => x.AddAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

            // Act
            await _fileService.AddFilesAsync(files);

            // Assert
            _mockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(500));
        }

        [Fact]
        public async Task ConcurrentQueueOperations_ShouldHandleMultipleOperations()
        {
            // Arrange
            var addFiles = new List<string>();
            for (int i = 0; i < 50; i++)
            {
                addFiles.Add(CreateTestFile($"concurrent_{i}.txt", $"Concurrent test content {i}"));
            }

            var initialItems = new List<QueueItem>
            {
                new QueueItem { Id = Guid.NewGuid(), FilePath = "existing1.txt" },
                new QueueItem { Id = Guid.NewGuid(), FilePath = "existing2.txt" },
                new QueueItem { Id = Guid.NewGuid(), FilePath = "existing3.txt" }
            };

            _mockQueueRepository.Setup(x => x.AddAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);
            _mockQueueRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(initialItems);
            _mockQueueRepository.Setup(x => x.RemoveAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act
            var addTask = _fileService.AddFilesAsync(addFiles);
            var getTask = Task.Run(() => _fileService.GetQueueItems());
            var clearTask = _fileService.ClearAllFilesAsync();

            await Task.WhenAll(addTask, getTask, clearTask);

            // Assert
            _mockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(addFiles.Count));
            _mockQueueRepository.Verify(x => x.RemoveAsync(It.IsAny<Guid>()), Times.Exactly(initialItems.Count));
        }

        [Fact]
        public async Task MemoryUsage_WithLargeFiles_ShouldNotExceedLimits()
        {
            // Arrange
            var files = new List<string>();
            var fileSizes = new[] { 1024, 2048, 4096, 8192 }; // Различные размеры файлов

            foreach (var size in fileSizes)
            {
                for (int i = 0; i < 25; i++) // 4 размера × 25 файлов = 100 файлов
                {
                    var fileName = $"memory_test_{size}_{i}.txt";
                    var content = new string('x', size);
                    files.Add(CreateTestFile(fileName, content));
                }
            }

            _mockQueueRepository.Setup(x => x.AddAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

            // Act
            var startMemory = GC.GetTotalMemory(true);
            await _fileService.AddFilesAsync(files);
            var endMemory = GC.GetTotalMemory(false);

            // Assert
            var memoryIncrease = endMemory - startMemory;
            memoryIncrease.Should().BeLessThan(50 * 1024 * 1024); // Менее 50MB увеличения
            _mockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(files.Count));
        }

        [Fact]
        public async Task StressTest_RepeatedOperations_ShouldRemainStable()
        {
            // Arrange
            var addedItems = new List<QueueItem>();
            
            _mockQueueRepository.Setup(x => x.AddAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask)
                .Callback<QueueItem>(item => addedItems.Add(item));
                
            _mockQueueRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(() => new List<QueueItem>(addedItems));
                
            _mockQueueRepository.Setup(x => x.RemoveAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask)
                .Callback<Guid>(id => addedItems.RemoveAll(item => item.Id == id));

            // Act & Assert - повторяем операции 10 раз
            for (int cycle = 0; cycle < 10; cycle++)
            {
                // Добавляем файлы
                var files = new List<string>();
                for (int i = 0; i < 20; i++)
                {
                    files.Add(CreateTestFile($"stress_{cycle}_{i}.txt", $"Stress test content {cycle}_{i}"));
                }

                await _fileService.AddFilesAsync(files);

                // Получаем элементы
                var items = _fileService.GetQueueItems();
                // В каждой итерации очередь должна содержать 20 элементов,
                // так как после ClearAllFilesAsync она полностью очищается
                items.Should().HaveCount(20);

                // Очищаем
                await _fileService.ClearAllFilesAsync();

                _mockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(20 * (cycle + 1)));
            }
        }

        private string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
            _fileService?.Dispose();
            GC.Collect();
        }
    }
}