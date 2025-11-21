using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationGateway> _gatewayMock;
        private readonly Mock<INotificationSettingsStore> _settingsStoreMock;
        private readonly NotificationService _service;

        public NotificationServiceTests()
        {
            _gatewayMock = new Mock<INotificationGateway>(MockBehavior.Strict);
            _settingsStoreMock = new Mock<INotificationSettingsStore>(MockBehavior.Strict);

            _service = new NotificationService(_gatewayMock.Object, _settingsStoreMock.Object);
        }

        [Fact]
        public void Constructor_WithNullGateway_ShouldThrow()
        {
            // Act
            Action act = () => new NotificationService(null!, _settingsStoreMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("gateway");
        }

        [Fact]
        public void Constructor_WithNullSettingsStore_ShouldThrow()
        {
            // Act
            Action act = () => new NotificationService(_gatewayMock.Object, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("settingsStore");
        }

        [Fact]
        public async Task NotifyConversionComplete_WithSuccessResult_ShouldShowInfoNotification()
        {
            // Arrange
            var summary = new NotificationSummary
            {
                Message = "Completed",
                FailedCount = 0
            };

            _gatewayMock
                .Setup(g => g.ShowInfoAsync("Completed", "Video Converter", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            _service.NotifyConversionComplete(summary);

            // Give background task a moment to execute
            await Task.Delay(50);

            // Assert
            _gatewayMock.Verify();
        }

        [Fact]
        public async Task NotifyProgress_WithValidValues_ShouldShowProgressNotification()
        {
            // Arrange
            _gatewayMock
                .Setup(g => g.ShowInfoAsync("Progress: 50%", "Conversion Progress", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            _service.NotifyProgress(50, 100);

            // Give background task a moment to execute
            await Task.Delay(50);

            // Assert
            _gatewayMock.Verify();
        }

        [Fact]
        public void GetSettings_ShouldReturnNonNullOptions()
        {
            // Act
            var result = _service.GetSettings();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<Converter.Domain.Models.NotificationOptions>();
        }

        [Fact]
        public void UpdateSettings_WithValidSettings_ShouldNotThrow()
        {
            // Arrange
            var options = new Converter.Domain.Models.NotificationOptions();

            // Act
            Action act = () => _service.UpdateSettings(options);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Act
            _service.Dispose();
            _service.Dispose();

            // Assert
            // Should not throw
        }
    }
}
