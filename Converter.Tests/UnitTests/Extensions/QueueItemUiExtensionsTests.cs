using System;
using System.Drawing;
using Converter.Domain.Models;
using Converter.Extensions;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Extensions;

public class QueueItemUiExtensionsTests
{
    [Fact]
    public void GetStatusText_ShouldReturnLocalizedText()
    {
        // Arrange
        var item = new QueueItem { Status = ConversionStatus.Processing, Progress = 25 };

        // Act
        var text = item.GetStatusText();

        // Assert
        text.Should().Be("Конвертация 25%");
    }

    [Fact]
    public void GetStatusColor_ShouldMapStatuses()
    {
        // Arrange
        var pending = new QueueItem { Status = ConversionStatus.Pending };
        var completed = new QueueItem { Status = ConversionStatus.Completed };

        // Act & Assert
        pending.GetStatusColor().Should().Be(Color.Gray);
        completed.GetStatusColor().Should().Be(Color.Green);
    }

    [Fact]
    public void GetEta_WithProgress_ShouldEstimateRemainingTime()
    {
        // Arrange
        var now = new DateTime(2024, 1, 1, 0, 1, 40);
        var item = new QueueItem
        {
            Status = ConversionStatus.Processing,
            Progress = 50,
            StartedAt = new DateTime(2024, 1, 1, 0, 0, 0)
        };

        // Act
        var eta = item.GetEta(now);

        // Assert
        eta.Should().Be("1 мин 40 сек");
    }
}
