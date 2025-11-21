using System;
using System.IO;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Converter.Services.UIServices;
using Converter.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests
{
    public class CancellationTests : FileOperationsTestBase, IDisposable
    {
        private readonly string _testFile;

        public CancellationTests()
        {
            _testFile = Path.Combine(TestDirectory, "test_file.txt");
            File.WriteAllText(_testFile, "Test content");
            
            // Configure default mock behavior
            MockQueueRepository.Setup(x => x.AddAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);
                
            MockQueueRepository.Setup(x => x.RemoveAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
                
            MockQueueRepository.Setup(x => x.UpdateAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);
                
            MockQueueRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(Array.Empty<QueueItem>());
        }

        public new void Dispose()
        {
            try
            {
                if (File.Exists(_testFile))
                {
                    File.Delete(_testFile);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            base.Dispose();
        }

        [Fact]
        public async Task AddFilesAsync_WithValidFile_ShouldAddToQueue()
        {
            // Arrange
            var testFiles = new[] { _testFile };
            
            // Act
            await FileService.AddFilesAsync(testFiles);

            // Assert
            MockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Once);
        }

        [Fact]
        public async Task AddFilesAsync_WithNonExistentFile_ShouldSkipFile()
        {
            // Arrange
            var nonExistentFile = Path.Combine(TestDirectory, "nonexistent.txt");
            var testFiles = new[] { _testFile, nonExistentFile };
            
            // Act
            await FileService.AddFilesAsync(testFiles);

            // Assert
            MockQueueRepository.Verify(x => x.AddAsync(It.Is<QueueItem>(i => i.FilePath == _testFile)), Times.Once);
            VerifyLogMessage($"Skipping non-existent file: {nonExistentFile}", LogLevel.Warning);
        }

        [Fact]
        public async Task RemoveSelectedFilesAsync_WithValidItems_ShouldRemoveFromQueue()
        {
            // Arrange
            var queueItems = new[]
            {
                new QueueItem { Id = Guid.NewGuid(), FilePath = _testFile }
            };
            
            // Act
            await FileService.RemoveSelectedFilesAsync(queueItems);

            // Assert
            MockQueueRepository.Verify(x => x.RemoveAsync(It.IsAny<Guid>()), Times.Once);
            VerifyLogMessage($"Removed {queueItems.Length} files from the queue");
        }

        [Fact]
        public async Task ClearAllFilesAsync_WithItemsInQueue_ShouldRemoveAllItems()
        {
            // Arrange
            var queueItems = new[]
            {
                new QueueItem { Id = Guid.NewGuid(), FilePath = _testFile }
            };
            
            MockQueueRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(queueItems);
                
            // Act
            await FileService.ClearAllFilesAsync();

            // Assert
            MockQueueRepository.Verify(x => x.RemoveAsync(It.IsAny<Guid>()), Times.Once);
            VerifyLogMessage("Cleared all files from the queue");
        }

        [Fact]
        public async Task RemoveSelectedFilesAsync_WithEmptyCollection_ShouldNotCallRepository()
        {
            // Arrange
            var emptyItems = Array.Empty<QueueItem>();

            // Act
            await FileService.RemoveSelectedFilesAsync(emptyItems);

            // Assert
            MockQueueRepository.Verify(x => x.RemoveAsync(It.IsAny<Guid>()), Times.Never);
            VerifyLogMessage("No files to remove from the queue");
        }

        [Fact]
        public async Task ClearAllFilesAsync_WhenQueueIsEmpty_ShouldNotThrow()
        {
            // Arrange
            MockQueueRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(Array.Empty<QueueItem>());

            // Act
            await FileService.ClearAllFilesAsync();
            
            // Assert - No exception should be thrown
            VerifyLogMessage("No files to clear from the queue");
        }

        [Fact]
        public async Task UpdateQueueItem_WithValidItem_ShouldUpdateInRepository()
        {
            // Arrange
            var queueItem = new QueueItem 
            { 
                Id = Guid.NewGuid(), 
                FilePath = _testFile,
                Status = ConversionStatus.Processing 
            };
            
            // Act
            await FileService.UpdateQueueItem(queueItem);

            // Assert
            MockQueueRepository.Verify(x => x.UpdateAsync(
                It.Is<QueueItem>(i => 
                    i.Id == queueItem.Id && 
                    i.FilePath == queueItem.FilePath &&
                    i.Status == queueItem.Status)),
                Times.Once);
                
            VerifyLogMessage($"Updated queue item {queueItem.Id}");
        }

        [Fact]
        public async Task AddFilesAsync_WithEmptyCollection_ShouldNotCallRepository()
        {
            // Arrange
            var emptyFiles = Array.Empty<string>();

            // Act
            await FileService.AddFilesAsync(emptyFiles);

            // Assert
            MockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Never);
            VerifyLogMessage("No files to add to the queue");
        }

        [Fact]
        public async Task UpdateQueueItem_WithNullItem_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                FileService.UpdateQueueItem(null!));
        }

        [Fact]
        public async Task AddFilesAsync_WithNullFilePaths_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                FileService.AddFilesAsync(null!));
        }

        [Fact]
        public async Task AddFilesAsync_WithSpecialCharactersInPath_ShouldHandleCorrectly()
        {
            // Arrange
            var fileName = "test_áéíóúñ_文件_123.txt";
            var filePath = CreateTestFile(fileName, "content with special chars");
            var testFiles = new[] { filePath };
            
            // Act
            await FileService.AddFilesAsync(testFiles);

            // Assert
            VerifyQueueItemAdded(filePath);
        }
    }
}