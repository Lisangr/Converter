using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Converter.Services.UIServices;
using Moq;
using Xunit;

namespace Converter.Tests.IntegrationTests;

public class FileOperationsWithRealFilesTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Mock<IQueueRepository> _repository;
    private readonly Mock<IQueueProcessor> _processor;
    private readonly FileOperationsService _service;

    public FileOperationsWithRealFilesTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _repository = new Mock<IQueueRepository>();
        _processor = new Mock<IQueueProcessor>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<FileOperationsService>>();
        _service = new FileOperationsService(_repository.Object, _processor.Object, logger.Object);
    }

    [Fact]
    public async Task AddFiles_ShouldHandleRealFilesystem()
    {
        var existing = CreateTempFile("one.txt");
        var missing = Path.Combine(_tempDir, "missing.txt");
        _repository.Setup(r => r.AddAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);

        await _service.AddFilesAsync(new[] { existing, missing });

        _repository.Verify(r => r.AddAsync(It.Is<QueueItem>(q => q.FilePath == existing)), Times.Once);
    }

    [Fact]
    public async Task RemoveFiles_ShouldUpdateQueue()
    {
        var items = new List<QueueItem>
        {
            new() { Id = Guid.NewGuid(), FilePath = "a" },
            new() { Id = Guid.NewGuid(), FilePath = "b" }
        };
        _repository.Setup(r => r.RemoveAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

        await _service.RemoveSelectedFilesAsync(items);

        _repository.Verify(r => r.RemoveAsync(items[0].Id), Times.Once);
        _repository.Verify(r => r.RemoveAsync(items[1].Id), Times.Once);
    }

    [Fact]
    public async Task ClearFiles_ShouldCleanResources()
    {
        var items = new List<QueueItem>
        {
            new() { Id = Guid.NewGuid(), FilePath = "c" },
            new() { Id = Guid.NewGuid(), FilePath = "d" }
        };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        _repository.Setup(r => r.RemoveAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

        await _service.ClearAllFilesAsync();

        _repository.Verify(r => r.RemoveAsync(items[0].Id), Times.Once);
        _repository.Verify(r => r.RemoveAsync(items[1].Id), Times.Once);
    }

    private string CreateTempFile(string name)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, "content");
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
        }
    }
}
