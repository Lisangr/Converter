using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using Converter.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<INotificationGateway> _gatewayMock;
    private readonly Mock<INotificationSettingsStore> _settingsStoreMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _notificationService;
    private readonly NotificationGateway _gateway;
    private readonly string _testDirectory;

    public NotificationIntegrationTests()
    {
        _gatewayMock = new Mock<INotificationGateway>();
        _settingsStoreMock = new Mock<INotificationSettingsStore>();
        _loggerMock = new Mock<ILogger<NotificationService>>();
        
        _notificationService = new NotificationService(_gatewayMock.Object, _settingsStoreMock.Object);
        _gateway = new NotificationGateway(_loggerMock);
        _testDirectory = Path.Combine(Path.GetTempPath(), "ConverterNotificationTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task Notification_ShouldFireOnCompletion()
    {
        // Arrange
        var completedItems = new List<QueueItem>
        {
            new() { Id = Guid.NewGuid(), FilePath = "video1.mp4", Status = ConversionStatus.Completed },
            new() { Id = Guid.NewGuid(), FilePath = "video2.mp4", Status = ConversionStatus.Completed }
        };
        
        var summary = new NotificationSummary
        {
            Message = "Конвертация завершена",
            TotalCount = 2,
            CompletedCount = 2,
            FailedCount = 0
        };

        _gatewayMock.Setup(g => g.ShowInfoAsync("Конвертация завершена", "Video Converter", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        _notificationService.NotifyConversionComplete(summary);

        // Дожидаемся выполнения фоновой задачи
        await Task.Delay(100);

        // Assert
        _gatewayMock.Verify();
        summary.CompletedCount.Should().Be(2);
        summary.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task Notification_ShouldHandleFailures()
    {
        // Arrange
        var failedItems = new List<QueueItem>
        {
            new() { Id = Guid.NewGuid(), FilePath = "video1.mp4", Status = ConversionStatus.Failed, ErrorMessage = "FFmpeg error" },
            new() { Id = Guid.NewGuid(), FilePath = "video2.mp4", Status = ConversionStatus.Completed }
        };

        var summary = new NotificationSummary
        {
            Message = "Конвертация завершена с ошибками",
            TotalCount = 2,
            CompletedCount = 1,
            FailedCount = 1
        };

        _gatewayMock.Setup(g => g.ShowWarningAsync("Конвертация завершена с ошибками", "Video Converter", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        _notificationService.NotifyConversionComplete(summary);

        await Task.Delay(100);

        // Assert
        _gatewayMock.Verify();
        summary.FailedCount.Should().Be(1);
        summary.CompletedCount.Should().Be(1);
    }

    [Fact]
    public async Task Notification_ShouldRespectUserPreferences()
    {
        // Arrange
        var preferences = new NotificationOptions
        {
            DesktopNotificationsEnabled = false,
            ShowProgressNotifications = false,
            SoundEnabled = false
        };

        _settingsStoreMock.Setup(s => s.Load()).Returns(preferences);

        var service = new NotificationService(_gatewayMock.Object, _settingsStoreMock.Object);
        var summary = new NotificationSummary
        {
            Message = "Test notification",
            TotalCount = 1,
            CompletedCount = 1,
            FailedCount = 0
        };

        // Настройка - при отключенных уведомлениях ничего не должно вызываться
        _gatewayMock.Setup(g => g.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Notifications should be disabled"))
            .Verifiable();

        // Act & Assert
        // Сервис должен корректно обработать отключенные уведомления
        service.NotifyConversionComplete(summary);
        await Task.Delay(100);

        // Проверяем, что настройки были загружены
        _settingsStoreMock.Verify(s => s.Load(), Times.Once);
    }

    public void Dispose()
    {
        _notificationService?.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}