using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Converter.Services.UIServices;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;

namespace Converter.Tests.TestBase
{
    public abstract class FileOperationsTestBase : IDisposable
    {
        protected readonly Mock<IQueueRepository> MockQueueRepository;
        protected readonly Mock<IQueueProcessor> MockQueueProcessor;
        protected readonly Mock<ILogger<FileOperationsService>> MockLogger;
        protected readonly FileOperationsService FileService;
        protected readonly string TestDirectory;
        protected readonly string TestFilesPath;
        protected bool Disposed;

        protected FileOperationsTestBase()
        {
            MockQueueRepository = new Mock<IQueueRepository>();
            MockQueueProcessor = new Mock<IQueueProcessor>();
            MockLogger = new Mock<ILogger<FileOperationsService>>();
            
            FileService = new FileOperationsService(
                MockQueueRepository.Object,
                MockQueueProcessor.Object,
                MockLogger.Object);

            TestDirectory = Path.Combine(Path.GetTempPath(), "ConverterTests_" + Guid.NewGuid());
            TestFilesPath = Path.Combine(TestDirectory, "TestFiles");
            Directory.CreateDirectory(TestFilesPath);
        }

        protected string CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(TestFilesPath, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        protected string CreateLargeTestFile(string fileName, int sizeInKb)
        {
            var filePath = Path.Combine(TestFilesPath, fileName);
            var rnd = new Random();
            var data = new byte[sizeInKb * 1024];
            rnd.NextBytes(data);
            File.WriteAllBytes(filePath, data);
            return filePath;
        }

        protected void SetupMockForAddFiles()
        {
            MockQueueRepository.Setup(x => x.AddAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);
        }

        protected void VerifyQueueItemAdded(string expectedFilePath, int times = 1)
        {
            MockQueueRepository.Verify(x => x.AddAsync(
                It.Is<QueueItem>(item => 
                    item.FilePath == expectedFilePath && 
                    item.Status == ConversionStatus.Pending)),
                Times.Exactly(times));
        }

        protected void VerifyLogMessage(string expectedMessage, LogLevel logLevel = LogLevel.Information)
        {
            MockLogger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing && Directory.Exists(TestDirectory))
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
                Disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
