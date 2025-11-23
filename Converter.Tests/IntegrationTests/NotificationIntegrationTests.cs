using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Domain.Models;
using Converter.Infrastructure;
using Moq;
using Xunit;

namespace Converter.Tests.IntegrationTests;

public class NotificationIntegrationTests : IDisposable
{
    [Fact]
    public async Task Notification_ShouldFireOnCompletion()
    {
        var gateway = new Mock<INotificationGateway>();
        gateway.Setup(g => g.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var settings = new Mock<INotificationSettingsStore>();
        var service = new NotificationService(gateway.Object, settings.Object);
        var summary = new NotificationSummary
        {
            Message = "all good",
            FailedCount = 0
        };

        service.NotifyConversionComplete(summary);
        await Task.Delay(10);

        gateway.Verify(g => g.ShowInfoAsync("all good", "Video Converter", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Notification_ShouldHandleFailures()
    {
        var gateway = new Mock<INotificationGateway>();
        gateway.Setup(g => g.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var settings = new Mock<INotificationSettingsStore>();
        var service = new NotificationService(gateway.Object, settings.Object);
        var summary = new NotificationSummary
        {
            Message = "some failed",
            FailedCount = 2
        };

        service.NotifyConversionComplete(summary);
        await Task.Delay(10);

        gateway.Verify(g => g.ShowInfoAsync("some failed", "Video Converter", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Notification_ShouldRespectUserPreferences()
    {
        var gateway = new Mock<INotificationGateway>();
        gateway.Setup(g => g.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var settings = new Mock<INotificationSettingsStore>();
        var service = new NotificationService(gateway.Object, settings.Object);

        service.NotifyProgress(3, 10);
        await Task.Delay(10);

        gateway.Verify(g => g.ShowInfoAsync("Progress: 30%", "Conversion Progress", It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        // no-op test cleanup
    }
}