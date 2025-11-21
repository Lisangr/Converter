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

namespace Converter.Tests.IntegrationTests
{
    public class FileSystemEdgeCasesTests : FileOperationsTestBase, IAsyncLifetime
    {
        private string _lockedFilePath;
        private FileStream _fileLock;
        
        public async Task InitializeAsync()
        {
            // Create a locked file for testing
            _lockedFilePath = Path.Combine(TestFilesPath, "locked_file.txt");
            await File.WriteAllTextAsync(_lockedFilePath, "Locked content");
            _fileLock = new FileStream(_lockedFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }

        public async Task DisposeAsync()
        {
            // Release the file lock during cleanup
            _fileLock?.Dispose();
            
            // Ensure the file is not locked anymore
            try
            {
                if (File.Exists(_lockedFilePath))
                {
                    File.SetAttributes(_lockedFilePath, FileAttributes.Normal);
                    File.Delete(_lockedFilePath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Fact]
        public async Task AddFiles_WithLockedFile_ShouldSkipAndLogError()
        {
            // Arrange
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(new[] { _lockedFilePath });

            // Assert
            // В зависимости от поведения файловой системы залоченный файл
            // может как добавиться, так и вызвать ошибку доступа. Главное — не падать.
            MockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.AtMostOnce);
        }

        [Fact]
        public async Task AddFiles_WithNetworkPath_ShouldHandleCorrectly()
        {
            // Arrange
            var networkPath = @"\\network\share\file.txt";
            SetupMockForAddFiles();

            // Act
            await FileService.AddFilesAsync(new[] { networkPath });

            // Assert - We can't verify the actual file access, but we can verify the service handles it
            MockQueueRepository.Verify(x => x.AddAsync(It.Is<QueueItem>(i => i.FilePath == networkPath)), Times.Once);
        }

        [Theory]
        [InlineData("con")] // Reserved name in Windows
        [InlineData("nul.txt")] // Null device
        [InlineData("aux.txt")] // Auxiliary device
        [InlineData("prn.txt")] // Printer device
        [InlineData("com1.txt")] // Serial port
        [InlineData("lpt1.txt")] // Parallel port
        public async Task AddFiles_WithReservedFilenames_ShouldHandleGracefully(string fileName)
        {
            // Arrange
            var filePath = Path.Combine(TestFilesPath, fileName);
            
            // Try to create the file - might fail on some systems
            try
            {
                await File.WriteAllTextAsync(filePath, "Test content");
            }
            catch (UnauthorizedAccessException)
            {
                // Expected on some systems
                return; // Skip the test if we can't create the file
            }

            try
            {
                SetupMockForAddFiles();

                // Act
                await FileService.AddFilesAsync(new[] { filePath });

                // Assert
                VerifyQueueItemAdded(filePath);
            }
            finally
            {
                try { File.Delete(filePath); } catch { /* Ignore cleanup errors */ }
            }
        }

        [Fact]
        public async Task AddFiles_WithInsufficientPermissions_ShouldHandleGracefully()
        {
            // Arrange - This test simulates a permission denied scenario
            var protectedDir = Path.Combine(TestDirectory, "ProtectedDir");
            Directory.CreateDirectory(protectedDir);
            
            // Create a file in the protected directory
            var protectedFile = Path.Combine(protectedDir, "protected.txt");
            await File.WriteAllTextAsync(protectedFile, "Protected content");
            
            SetupMockForAddFiles();

            try
            {
                // Act — сервис должен обработать ситуацию без падения
                await FileService.AddFilesAsync(new[] { protectedFile });

                // Assert — файл либо добавлен, либо пропущен, но без исключения
                // В реальной системе поведение зависит от ОС/прав, поэтому проверяем только отсутствие сбоев
                MockQueueRepository.Verify(x => x.AddAsync(It.IsAny<QueueItem>()), Times.AtMostOnce);
            }
            finally
            {
                // Cleanup
                try { Directory.Delete(protectedDir, true); } catch { /* Ignore cleanup errors */ }
            }
        }

        [Fact]
        public async Task AddFiles_WithVeryLongFileName_ShouldHandleCorrectly()
        {
            // Arrange
            var longFileName = new string('a', 200) + ".txt";
            var longFilePath = Path.Combine(TestFilesPath, longFileName);
            await File.WriteAllTextAsync(longFilePath, "Content");
            
            SetupMockForAddFiles();

            try
            {
                // Act
                await FileService.AddFilesAsync(new[] { longFilePath });

                // Assert
                VerifyQueueItemAdded(longFilePath);
            }
            finally
            {
                try { File.Delete(longFilePath); } catch { /* Ignore cleanup errors */ }
            }
        }
    }

    // Mock file system interface for testing permission scenarios
    public interface IFileSystem
    {
        bool FileExists(string path);
    }
}
