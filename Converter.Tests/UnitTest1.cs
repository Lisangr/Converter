using System;
using System.Collections.Generic;
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
    public class FileOperationsServiceTests : FileOperationsTestBase
    {
        private const string TestContent = "Test content";
        private const string TestFileName = "test.txt";

        [Fact]
        public async Task AddFilesAsync_WithValidFiles_ShouldAddToQueue()
        {
            // Arrange
            var testFiles = new[]
            {
                CreateTestFile("test1.txt", "content1"),
                CreateTestFile("test2.txt", "content2")
            };
            
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(testFiles);

            // Assert
            MockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(2));
            foreach (var file in testFiles)
            {
                VerifyQueueItemAdded(file);
            }
        }

        [Fact]
        public async Task AddFilesAsync_WithNullFilePaths_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                FileService.AddFilesAsync(null!));
        }

        [Fact]
        public async Task AddFilesAsync_WithNonExistentFiles_ShouldSkipNonExistentFiles()
        {
            // Arrange
            var existingFile = CreateTestFile("existing.txt", "content");
            var nonExistentFile = Path.Combine(TestDirectory, "non_existent_file.txt");
            var testFiles = new[] { existingFile, nonExistentFile };
            
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(testFiles);

            // Assert
            MockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Once);
            VerifyQueueItemAdded(existingFile);
            VerifyLogMessage($"Skipping non-existent file: {nonExistentFile}", LogLevel.Warning);
        }

        [Fact]
        public async Task RemoveSelectedFilesAsync_WithValidItems_ShouldRemoveFromQueue()
        {
            // Arrange
            var queueItems = new[]
            {
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test1.txt" },
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test2.txt" }
            };

            MockQueueRepository.Setup(x => x.RemoveAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act
            await FileService.RemoveSelectedFilesAsync(queueItems);

            // Assert
            foreach (var item in queueItems)
            {
                MockQueueRepository.Verify(x => x.RemoveAsync(item.Id), Times.Once);
            }
            VerifyLogMessage($"Removed {queueItems.Length} files from the queue");
        }

        [Fact]
        public async Task ClearAllFilesAsync_ShouldRemoveAllItems()
        {
            // Arrange
            var queueItems = new[]
            {
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test1.txt" },
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test2.txt" }
            };

            MockQueueRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(queueItems);
            MockQueueRepository.Setup(x => x.RemoveAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act
            await FileService.ClearAllFilesAsync();

            // Assert
            foreach (var item in queueItems)
            {
                MockQueueRepository.Verify(x => x.RemoveAsync(item.Id), Times.Once);
            }
            VerifyLogMessage("Cleared all files from the queue");
        }

        [Fact]
        public void GetQueueItems_ShouldReturnCurrentQueueItems()
        {
            // Arrange
            var expectedItem = new QueueItem { Id = Guid.NewGuid(), FilePath = "test1.txt" };
            var queueItems = new[] { expectedItem };

            MockQueueRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(queueItems);

            // Act
            var result = FileService.GetQueueItems();

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainSingle();
            result[0].Should().BeEquivalentTo(expectedItem);
        }

        [Fact]
        public async Task UpdateQueueItem_WithValidItem_ShouldUpdateInRepository()
        {
            // Arrange
            var queueItem = new QueueItem 
            { 
                Id = Guid.NewGuid(), 
                FilePath = "test.txt",
                Status = ConversionStatus.Processing 
            };
            
            MockQueueRepository.Setup(x => x.UpdateAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

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
        public async Task UpdateQueueItem_WithNullItem_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                FileService.UpdateQueueItem(null!));
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
        public async Task AddFilesAsync_WithLargeFile_ShouldHandleGracefully()
        {
            // Arrange
            var largeFilePath = CreateLargeTestFile("largefile.bin", 1024 * 10); // 10MB file
            var testFiles = new[] { largeFilePath };
            
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(testFiles);

            // Assert
            VerifyQueueItemAdded(largeFilePath);
        }
        
        [Fact]
        public async Task AddFilesAsync_WithSpecialCharactersInPath_ShouldHandleCorrectly()
        {
            // Arrange
            var fileName = "test_áéíóúñ_文件_123.txt";
            var filePath = CreateTestFile(fileName, "content with special chars");
            var testFiles = new[] { filePath };
            
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(testFiles);

            // Assert
            VerifyQueueItemAdded(filePath);
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

            // Act & Assert
            await FileService.ClearAllFilesAsync();
            
            // Verify no exceptions are thrown and appropriate log is written
            VerifyLogMessage("No files to clear from the queue");
        }

    }
}