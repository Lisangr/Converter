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

namespace Converter.Tests.IntegrationTests
{
    public class FileOperationsIntegrationTests : FileOperationsTestBase
    {
        private const string TestContent = "Integration test content";
        private const string TestFileName = "integration_test.txt";
        private const string TestSubdirectory = "Subfolder";

        [Fact]
        public async Task AddFiles_WithValidFile_ShouldAddToQueue()
        {
            // Arrange
            var testFile = CreateTestFile(TestFileName, TestContent);
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(new[] { testFile });

            // Assert
            VerifyQueueItemAdded(testFile);
            // В сервисе используется русское сообщение
            VerifyLogMessage($"Файл добавлен в очередь: {testFile}");
        }

        [Fact]
        public async Task AddFiles_WithMultipleFiles_ShouldAddAllToQueue()
        {
            // Arrange
            var testFiles = new[]
            {
                CreateTestFile("test1.txt", "Content 1"),
                CreateTestFile("test2.txt", "Content 2"),
                CreateTestFile("test3.txt", "Content 3")
            };
            
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(testFiles);

            // Assert
            foreach (var file in testFiles)
            {
                VerifyQueueItemAdded(file);
            }
            // Лог сервиса: "Успешно добавлено {FileCount} файлов в очередь"
            VerifyLogMessage("Успешно добавлено 3 файлов в очередь");
        }

        [Fact]
        public async Task AddFiles_WithNonExistentFiles_ShouldSkipNonExistentFiles()
        {
            // Arrange
            var existingFile = CreateTestFile("existing.txt", "Content");
            var nonExistentFile1 = Path.Combine(TestDirectory, "non_existent1.txt");
            var nonExistentFile2 = Path.Combine(TestDirectory, "non_existent2.txt");
            
            var testFiles = new[] { existingFile, nonExistentFile1, nonExistentFile2 };
            
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(testFiles);

            // Assert
            VerifyQueueItemAdded(existingFile);
            VerifyLogMessage($"Skipping non-existent file: {nonExistentFile1}", LogLevel.Warning);
            VerifyLogMessage($"Skipping non-existent file: {nonExistentFile2}", LogLevel.Warning);
        }

        [Fact]
        public async Task AddFiles_WithEmptyCollection_ShouldNotCallRepository()
        {
            // Act
            await FileService.AddFilesAsync(Array.Empty<string>());

            // Assert
            MockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.Never);
            VerifyLogMessage("No files to add to the queue");
        }

        [Fact]
        public async Task RemoveSelectedFiles_WithValidItems_ShouldRemoveAllFromQueue()
        {
            // Arrange
            var itemsToRemove = new[]
            {
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test1.txt" },
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test2.txt" },
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test3.txt" }
            };

            MockQueueRepository.Setup(x => x.RemoveAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act
            await FileService.RemoveSelectedFilesAsync(itemsToRemove);

            // Assert
            foreach (var item in itemsToRemove)
            {
                MockQueueRepository.Verify(x => x.RemoveAsync(item.Id), Times.Once);
            }
            VerifyLogMessage($"Removed {itemsToRemove.Length} files from the queue");
        }

        [Fact]
        public async Task ClearAllFiles_WithItemsInQueue_ShouldRemoveAllItems()
        {
            // Arrange
            var currentItems = new[]
            {
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test1.txt" },
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test2.txt" }
            };

            MockQueueRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(currentItems);
            MockQueueRepository.Setup(x => x.RemoveAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act
            await FileService.ClearAllFilesAsync();

            // Assert
            foreach (var item in currentItems)
            {
                MockQueueRepository.Verify(x => x.RemoveAsync(item.Id), Times.Once);
            }
            VerifyLogMessage("Cleared all files from the queue");
        }

        [Fact]
        public async Task AddFiles_WithFilesInSubdirectories_ShouldHandleCorrectly()
        {
            // Arrange
            var subDir = Path.Combine(TestFilesPath, TestSubdirectory);
            Directory.CreateDirectory(subDir);
            
            var fileInSubdir = Path.Combine(subDir, "nested_file.txt");
            File.WriteAllText(fileInSubdir, "Nested content");
            
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(new[] { fileInSubdir });

            // Assert
            VerifyQueueItemAdded(fileInSubdir);
        }

        [Fact]
        public async Task AddFiles_WithReadOnlyFile_ShouldHandleGracefully()
        {
            // Arrange
            var readOnlyFile = CreateTestFile("readonly.txt", "Read only content");
            File.SetAttributes(readOnlyFile, FileAttributes.ReadOnly);
            
            SetupMockForAddFiles();

            try 
            {
                // Act
                await FileService.AddFilesAsync(new[] { readOnlyFile });

                // Assert
                VerifyQueueItemAdded(readOnlyFile);
            }
            finally
            {
                // Cleanup
                File.SetAttributes(readOnlyFile, FileAttributes.Normal);
            }
        }

        [Fact]
        public async Task AddFiles_WithVeryLongPath_ShouldHandleGracefully()
        {
            // Arrange
            var longPath = TestFilesPath;
            for (int i = 0; i < 10; i++)
            {
                longPath = Path.Combine(longPath, new string('x', 50));
            }
            
            // Ensure the directory exists
            Directory.CreateDirectory(longPath);
            
            var longFilePath = Path.Combine(longPath, "test.txt");
            File.WriteAllText(longFilePath, "Content in long path");
            
            SetupMockForAddFiles();

            // Act & Assert
            await FileService.AddFilesAsync(new[] { longFilePath });
            
            // Verify the file was processed
            VerifyQueueItemAdded(longFilePath);
        }
    }
}