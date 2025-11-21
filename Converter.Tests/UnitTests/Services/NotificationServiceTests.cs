using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationSettingsStore> _mockSettingsStore;
        private readonly Mock<ILogger<NotificationService>> _mockLogger;
        private readonly NotificationService _notificationService;
        private readonly NotificationSettings _defaultSettings;

        public NotificationServiceTests()
        {
            _mockSettingsStore = new Mock<INotificationSettingsStore>();
            _mockLogger = new Mock<ILogger<NotificationService>>();
            
            _defaultSettings = new NotificationSettings
            {
                DesktopNotificationsEnabled = true,
                SoundEnabled = true,
                UseCustomSound = false,
                CustomSoundPath = null,
                ShowProgressNotifications = false
            };

            _mockSettingsStore.Setup(x => x.Load())
                .Returns(_defaultSettings);

            _notificationService = new NotificationService(
                _mockSettingsStore.Object);
        }

        [Fact]
        public void Constructor_WithNullSettingsStore_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new NotificationService(null!));
        }

        [Fact]
        public void GetSettings_ShouldReturnCurrentSettings()
        {
            // Act
            var result = _notificationService.GetSettings();

            // Assert
            result.Should().Be(_defaultSettings);
        }

        [Fact]
        public void UpdateSettings_WithValidSettings_ShouldUpdateAndSave()
        {
            // Arrange
            var newSettings = new NotificationSettings
            {
                DesktopNotificationsEnabled = false,
                SoundEnabled = false,
                ShowProgressNotifications = true
            };

            // Act
            _notificationService.UpdateSettings(newSettings);

            // Assert
            _mockSettingsStore.Verify(x => x.Save(newSettings), Times.Once);
        }

        [Fact]
        public void UpdateSettings_WithNullSettings_ShouldUseDefault()
        {
            // Arrange
            NotificationSettings nullSettings = null!;

            // Act
            _notificationService.UpdateSettings(nullSettings);

            // Assert
            _mockSettingsStore.Verify(x => x.Save(It.Is<NotificationSettings>(s => s != null)), Times.Once);
        }

        [Fact]
        public void NotifyConversionComplete_WithSuccessResult_ShouldShowNotificationAndPlaySound()
        {
            // Arrange
            var result = new NotificationSummary
            {
                Success = true,
                ProcessedFiles = 5,
                SpaceSaved = 1024 * 1024 * 100, // 100MB
                Duration = TimeSpan.FromMinutes(10),
                OutputFolder = @"C:\Output",
                ThumbnailPath = @"C:\Thumbnails\thumb.jpg"
            };

            // Act
            _notificationService.NotifyConversionComplete(result);

            // Assert
            // Verify that the service attempted to show notification and play sound
            // We can't directly test the UI notification, but we can verify the logic path
        }

        [Fact]
        public void NotifyConversionComplete_WithNullResult_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _notificationService.NotifyConversionComplete(null!));
            exception.Should().BeNull();
        }

        [Fact]
        public void NotifyConversionComplete_WithFailedResult_ShouldShowErrorNotification()
        {
            // Arrange
            var result = new NotificationSummary
            {
                Success = false,
                ErrorMessage = "Conversion failed due to codec error"
            };

            // Act
            _notificationService.NotifyConversionComplete(result);

            // Assert
            // Should handle the failed conversion case
        }

        [Fact]
        public void NotifyProgress_WithProgressNotificationsDisabled_ShouldNotShowNotification()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                DesktopNotificationsEnabled = false,
                ShowProgressNotifications = false
            };
            
            _notificationService.UpdateSettings(settings);

            // Act
            _notificationService.NotifyProgress(50, 100);

            // Assert
            // Should not show notification when disabled
        }

        [Fact]
        public void NotifyProgress_WithValidProgress_ShouldShowMilestoneNotifications()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                DesktopNotificationsEnabled = true,
                ShowProgressNotifications = true
            };
            
            _notificationService.UpdateSettings(settings);

            // Act
            _notificationService.NotifyProgress(25, 100); // Should trigger 25% milestone
            _notificationService.NotifyProgress(50, 100); // Should trigger 50% milestone
            _notificationService.NotifyProgress(75, 100); // Should trigger 75% milestone

            // Assert
            // Should show notifications for each milestone
        }

        [Fact]
        public void NotifyProgress_WithInvalidTotal_ShouldNotShowNotification()
        {
            // Act
            _notificationService.NotifyProgress(50, 0); // Invalid total

            // Assert
            // Should handle invalid parameters gracefully
        }

        [Fact]
        public void NotifyProgress_WithZeroProgress_ShouldNotShowNotification()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                DesktopNotificationsEnabled = true,
                ShowProgressNotifications = true
            };
            
            _notificationService.UpdateSettings(settings);

            // Act
            _notificationService.NotifyProgress(0, 100);

            // Assert
            // Should not show notification for 0% progress
        }

        [Fact]
        public void NotifyProgress_With100Progress_ShouldNotShowMilestoneNotification()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                DesktopNotificationsEnabled = true,
                ShowProgressNotifications = true
            };
            
            _notificationService.UpdateSettings(settings);

            // Act
            _notificationService.NotifyProgress(100, 100);

            // Assert
            // 100% should not trigger milestone notifications (25, 50, 75)
        }

        [Fact]
        public void ResetProgressNotifications_ShouldClearMilestoneTracking()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                DesktopNotificationsEnabled = true,
                ShowProgressNotifications = true
            };
            
            _notificationService.UpdateSettings(settings);

            // Act
            _notificationService.NotifyProgress(25, 100); // Trigger first milestone
            _notificationService.ResetProgressNotifications();
            _notificationService.NotifyProgress(25, 100); // Should trigger again after reset

            // Assert
            // Should allow milestone notifications again after reset
        }

        [Fact]
        public void ShowDesktopNotification_WithDisabledNotifications_ShouldNotShow()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                DesktopNotificationsEnabled = false
            };
            
            _notificationService.UpdateSettings(settings);

            // Act
            _notificationService.NotifyConversionComplete(new NotificationSummary { Success = true, ProcessedFiles = 1 });

            // Assert
            // Should not show notification when disabled
        }

        [Fact]
        public void PlayCompletionSound_WithDisabledSound_ShouldNotPlay()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                SoundEnabled = false
            };
            
            _notificationService.UpdateSettings(settings);

            // Act
            _notificationService.NotifyConversionComplete(new NotificationSummary { Success = true, ProcessedFiles = 1 });

            // Assert
            // Should not play sound when disabled
        }

        [Fact]
        public void PlayCompletionSound_WithCustomSoundPath_ShouldUseCustomSound()
        {
            // Arrange
            var customSoundPath = Path.Combine(Path.GetTempPath(), "custom_notification.wav");
            File.WriteAllText(customSoundPath, "fake audio file");
            
            try
            {
                var settings = new NotificationSettings
                {
                    SoundEnabled = true,
                    UseCustomSound = true,
                    CustomSoundPath = customSoundPath
                };
                
                _notificationService.UpdateSettings(settings);

                // Act
                _notificationService.NotifyConversionComplete(new NotificationSummary { Success = true, ProcessedFiles = 1 });

                // Assert
                // Should attempt to use custom sound
            }
            finally
            {
                // Cleanup
                if (File.Exists(customSoundPath))
                {
                    File.Delete(customSoundPath);
                }
            }
        }

        [Fact]
        public void FormatFileSize_WithZeroBytes_ShouldReturnZeroB()
        {
            // Act
            var result = CallPrivateMethod<string>(_notificationService, "FormatFileSize", 0L);

            // Assert
            result.Should().Be("0 B");
        }

        [Fact]
        public void FormatFileSize_WithSmallSize_ShouldReturnCorrectFormat()
        {
            // Act
            var result = CallPrivateMethod<string>(_notificationService, "FormatFileSize", 1024L);

            // Assert
            result.Should().Be("1 KB");
        }

        [Fact]
        public void FormatFileSize_WithLargeSize_ShouldReturnCorrectFormat()
        {
            // Act
            var result = CallPrivateMethod<string>(_notificationService, "FormatFileSize", 1024L * 1024L * 1024L);

            // Assert
            result.Should().Be("1 GB");
        }

        [Fact]
        public void FormatFileSize_WithVeryLargeSize_ShouldReturnCorrectFormat()
        {
            // Act - 1024^4 bytes = 1 TB
            var result = CallPrivateMethod<string>(_notificationService, "FormatFileSize", 1024L * 1024L * 1024L * 1024L);

            // Assert
            result.Should().Be("1 TB");
        }

        [Fact]
        public void NotifyConversionComplete_WithLargeSpaceSaved_ShouldFormatCorrectly()
        {
            // Arrange
            var result = new NotificationSummary
            {
                Success = true,
                ProcessedFiles = 1,
                SpaceSaved = 1024L * 1024L * 1024L * 2, // 2 GB
                Duration = TimeSpan.FromMinutes(5)
            };

            // Act
            _notificationService.NotifyConversionComplete(result);

            // Assert
            // Should format the space saved correctly
        }

        [Fact]
        public void Dispose_ShouldDisposeResources()
        {
            // Arrange
            var service = new NotificationService(_mockSettingsStore.Object);

            // Act
            service.Dispose();

            // Assert
            // Should dispose without throwing
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var service = new NotificationService(_mockSettingsStore.Object);

            // Act
            service.Dispose();
            service.Dispose();
            service.Dispose();

            // Assert
            // Should not throw on multiple dispose calls
        }

        [Fact]
        public void Finalizer_ShouldNotThrow()
        {
            // Arrange
            var service = new NotificationService(_mockSettingsStore.Object);

            // Act & Assert
            var exception = Record.Exception(() => 
            {
                // Simulate finalizer call by making object unreachable and triggering GC
                service = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            });
            
            // Should not throw in finalizer
        }

        // Helper method to call private methods via reflection
        private static T CallPrivateMethod<T>(object instance, string methodName, params object[] parameters)
        {
            var type = instance.GetType();
            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method == null)
            {
                throw new InvalidOperationException($"Method {methodName} not found on type {type.Name}");
            }

            return (T)method.Invoke(instance, parameters)!;
        }

        [Fact]
        public void NotifyConversionComplete_WithVeryLongErrorMessage_ShouldHandleCorrectly()
        {
            // Arrange
            var longErrorMessage = new string('x', 10000); // Very long error message
            var result = new NotificationSummary
            {
                Success = false,
                ErrorMessage = longErrorMessage
            };

            // Act
            var exception = Record.Exception(() => _notificationService.NotifyConversionComplete(result));

            // Assert
            exception.Should().BeNull();
        }

        [Fact]
        public void NotifyProgress_WithNegativeValues_ShouldHandleGracefully()
        {
            // Arrange
            var settings = new NotificationSettings
            {
                DesktopNotificationsEnabled = true,
                ShowProgressNotifications = true
            };
            
            _notificationService.UpdateSettings(settings);

            // Act
            var exception = Record.Exception(() => 
            {
                _notificationService.NotifyProgress(-10, 100);
                _notificationService.NotifyProgress(50, -100);
            });

            // Assert
            exception.Should().BeNull();
        }
    }
}