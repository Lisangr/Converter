using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class NotificationGatewayTests
{
    private readonly Mock<ILogger<NotificationGateway>> _loggerMock = new();
    private readonly NotificationGateway _gateway;

    public NotificationGatewayTests()
    {
        _gateway = new NotificationGateway(_loggerMock.Object);
    }

    [Fact]
    public async Task ShowInfoAsync_ShouldLogInformation()
    {
        // Act
        await _gateway.ShowInfoAsync("message", "title");

        // Assert
        VerifyLog(LogLevel.Information, "Info: message");
    }

    [Fact]
    public async Task ShowWarningAsync_ShouldLogWarning()
    {
        // Act
        await _gateway.ShowWarningAsync("warn", "title");

        // Assert
        VerifyLog(LogLevel.Warning, "Warning: warn");
    }

    [Fact]
    public async Task CreateProgressReporter_ShouldReportItemProgress()
    {
        // Arrange
        var reporter = _gateway.CreateProgressReporter("operation");
        var item = new QueueItem { Id = Guid.NewGuid() };

        // Act
        reporter.ReportItemProgress(item, 50, "status");
        reporter.ReportError(item, "error");
        reporter.ReportWarning(item, "warn");
        reporter.ReportInfo(item, "info");
        reporter.Report(0.25);
        reporter.Report(75);
        reporter.ReportGlobalProgress(90, "nearly done");

        // Assert
        _loggerMock.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(item.Id.ToString())),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeast(4));
    }

    [Fact]
    public async Task ShowConfirmationAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _gateway.ShowConfirmationAsync("msg", "title", "ok", "cancel", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShowErrorAsync_ShouldLogError()
    {
        // Act
        await _gateway.ShowErrorAsync("error");

        // Assert
        VerifyLog(LogLevel.Error, "Error: error");
    }

    private void VerifyLog(LogLevel level, string contains)
    {
        _loggerMock.Verify(l => l.Log(
            level,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(contains)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
}
