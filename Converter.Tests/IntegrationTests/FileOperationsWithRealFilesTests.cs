using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Converter.Services.UIServices;
using Converter.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.IntegrationTests;

public class FileOperationsWithRealFilesTests : FileOperationsTestBase, IDisposable
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock;
    private readonly Mock<IQueueProcessor> _queueProcessorMock;
    private readonly Mock<ILogger<FileOperationsService>> _loggerMock;
    private readonly FileOperationsService _fileService;
    private readonly string _realTestDirectory;

    public FileOperationsWithRealFilesTests()
    {
        _queueRepositoryMock = new Mock<IQueueRepository>();
        _queueProcessorMock = new Mock<IQueueProcessor>();
        _loggerMock = new Mock<ILogger<FileOperationsService>>();

        _fileService = new FileOperationsService(
            _queueRepositoryMock.Object,
            _queueProcessorMock.Object,
            _loggerMock.Object);

        _realTestDirectory = Path.Combine(Path.GetTempPath(), "ConverterRealFilesTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_realTestDirectory);
    }

    [Fact]
    public async Task AddFiles_ShouldHandleRealFilesystem()
    {
        // Arrange
        var testFiles = new[]
        {
            CreateRealTestFile("video1.mp4", "fake mp4 content 1"),
            CreateRealTestFile("video2.avi", "fake avi content 2"),
            CreateRealTestFile("video3.mkv", "fake mkv content 3")
        };

        _queueRepositoryMock.Setup(r => r.AddAsync(It.IsAny<QueueItem>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _fileService.AddFilesAsync(testFiles);

        // Assert
        _queueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(3));
        
        foreach (var file in testFiles)
        {
            File.Exists(file).Should().BeTrue();
        }
    }

    [Fact]
    public async Task RemoveFiles_ShouldUpdateQueue()
    {
        // Arrange
        var queueItems = new[]
        {
            new QueueItem { Id = Guid.NewGuid(), FilePath = CreateRealTestFile("remove1.mp4", "content1") },
            new QueueItem { Id = Guid.NewGuid(), FilePath = CreateRealTestFile("remove2.mp4", "content2") },
            new QueueItem { Id = Guid.NewGuid(), FilePath = CreateRealTestFile("remove3.mp4", "content3") }
        };

        _queueRepositoryMock.Setup(r => r.RemoveAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _fileService.RemoveSelectedFilesAsync(queueItems);

        // Assert
        foreach (var item in queueItems)
        {
            _queueRepositoryMock.Verify(r => r.RemoveAsync(item.Id), Times.Once);
        }
    }

    [Fact]
    public async Task ClearFiles_ShouldCleanResources()
    {
        // Arrange
        var existingItems = new[]
        {
            new QueueItem { Id = Guid.NewGuid(), FilePath = CreateRealTestFile("clear1.mp4", "content1") },
            new QueueItem { Id = Guid.NewGuid(), FilePath = CreateRealTestFile("clear2.mp4", "content2") }
        };

        _queueRepositoryMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(existingItems);
        _queueRepositoryMock.Setup(r => r.RemoveAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileService.ClearAllFilesAsync();

        // Assert
        foreach (var item in existingItems)
        {
            _queueRepositoryMock.Verify(r => r.RemoveAsync(item.Id), Times.Once);
        }
    }

    [Fact]
    public async Task AddFiles_ShouldHandleLargeFiles()
    {
        // Arrange
        var largeFilePath = Path.Combine(_realTestDirectory, "large_video.dat");
        var largeContent = new string('X', 10 * 1024 * 1024); // 10MB
        await File.WriteAllTextAsync(largeFilePath, largeContent);

        _queueRepositoryMock.Setup(r => r.AddAsync(It.IsAny<QueueItem>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileService.AddFilesAsync(new[] { largeFilePath });

        // Assert
        _queueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<QueueItem>()), Times.Once);
        File.Exists(largeFilePath).Should().BeTrue();
        new FileInfo(largeFilePath).Length.Should().Be(largeContent.Length);
    }

    [Fact]
    public async Task AddFiles_ShouldHandleFilesWithSpecialCharacters()
    {
        // Arrange
        var specialFiles = new[]
        {
            CreateRealTestFile("video with spaces.mp4", "content with spaces"),
            CreateRealTestFile("video-with-dashes.avi", "content with dashes"),
            CreateRealTestFile "video_with_unicode_тест.mkv", "unicode content"),
            CreateRealTestFile("video@with#symbols$.mp4", "symbol content")
        };

        _queueRepositoryMock.Setup(r => r.AddAsync(It.IsAny<QueueItem>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileService.AddFilesAsync(specialFiles);

        // Assert
        _queueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(specialFiles.Length));
        
        foreach (var file in specialFiles)
        {
            File.Exists(file).Should().BeTrue();
        }
    }

    [Fact]
    public async Task AddFiles_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var testFiles = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            testFiles.Add(CreateRealTestFile($"concurrent_{i}.mp4", $"content {i}"));
        }

        _queueRepositoryMock.Setup(r => r.AddAsync(It.IsAny<QueueItem>()))
            .Returns(Task.CompletedTask);

        // Act - параллельное добавление файлов
        var tasks = new List<Task>();
        var batchSize = testFiles.Count / 2;
        
        for (int i = 0; i < 2; i++)
        {
            var batch = testFiles.Skip(i * batchSize).Take(batchSize).ToArray();
            tasks.Add(_fileService.AddFilesAsync(batch));
        }

        await Task.WhenAll(tasks);

        // Assert
        _queueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(testFiles.Count));
    }

    [Fact]
    public async Task AddFiles_ShouldValidateFileAccessibility()
    {
        // Arrange
        var accessibleFile = CreateRealTestFile("accessible.mp4", "accessible content");
        var inaccessibleFile = Path.Combine(_realTestDirectory, "inaccessible.txt");
        await File.WriteAllTextAsync(inaccessibleFile, "inaccessible content");
        File.SetAttributes(inaccessibleFile, FileAttributes.ReadOnly);

        try
        {
            _queueRepositoryMock.Setup(r => r.AddAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

            // Act
            await _fileService.AddFilesAsync(new[] { accessibleFile, inaccessibleFile });

            // Assert
            _queueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<QueueItem>()), Times.AtLeastOnce);
        }
        finally
        {
            // Cleanup
            File.SetAttributes(inaccessibleFile, FileAttributes.Normal);
        }
    }

    [Fact]
    public async Task AddFiles_ShouldHandleEmptyFiles()
    {
        // Arrange
        var emptyFile = CreateRealTestFile("empty.mp4", string.Empty);
        var tinyFile = CreateRealTestFile("tiny.mp4", "x");

        _queueRepositoryMock.Setup(r => r.AddAsync(It.IsAny<QueueItem>()))
            .Returns(Task.CompletedTask);

        // Act
        await _fileService.AddFilesAsync(new[] { emptyFile, tinyFile });

        // Assert
        _queueRepositoryMock.Verify(r => r.AddAsync(It.IsAny<QueueItem>()), Times.Exactly(2));
        File.Exists(emptyFile).Should().BeTrue();
        File.Exists(tinyFile).Should().BeTrue();
        new FileInfo(emptyFile).Length.Should().Be(0);
        new FileInfo(tinyFile).Length.Should().Be(1);
    }

    [Fact]
    public async Task AddFiles_ShouldTrackFileMetadata()
    {
        // Arrange
        var testFile = CreateRealTestFile("metadata_test.mp4", "metadata content");
        var fileInfo = new FileInfo(testFile);

        _queueRepositoryMock.Setup(r => r.AddAsync(It.IsAny<QueueItem>()))
            .Callback<QueueItem>(item =>
            {
                // Проверяем, что в QueueItem сохраняется информация о файле
                item.FileSizeBytes.Should().Be(fileInfo.Length);
            })
            .Returns(Task.CompletedTask);

        // Act
        await _fileService.AddFilesAsync(new[] { testFile });

        // Assert
        _queueRepositoryMock.Verify(r => r.AddAsync(It.Is<QueueItem>(i => i.FileSizeBytes == fileInfo.Length)), Times.Once);
    }

    private string CreateRealTestFile(string fileName, string content)
    {
        var filePath = Path.Combine(_realTestDirectory, fileName);
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }
        
        File.WriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        _fileService?.Dispose();
        if (Directory.Exists(_realTestDirectory))
        {
            try
            {
                Directory.Delete(_realTestDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        if (Directory.Exists(TestDirectory))
        {
            try
            {
                Directory.Delete(TestDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}