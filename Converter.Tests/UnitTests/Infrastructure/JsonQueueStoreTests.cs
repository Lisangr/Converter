using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class JsonQueueStoreTests
{
    private readonly JsonQueueStore _store = new("queue.json", Mock.Of<ILogger<JsonQueueStore>>());

    [Fact]
    public async Task TryReserveAsync_WithUniqueId_ShouldSucceedOnce()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var first = await _store.TryReserveAsync(id);
        var second = await _store.TryReserveAsync(id);

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteAsync_ShouldReleaseReservation()
    {
        // Arrange
        var id = Guid.NewGuid();
        await _store.TryReserveAsync(id);

        // Act
        await _store.CompleteAsync(id, ConversionStatus.Completed);
        var reservedAgain = await _store.TryReserveAsync(id);

        // Assert
        reservedAgain.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptySequence()
    {
        // Act
        var items = new List<QueueItem>();
        await foreach (var item in _store.GetAllAsync())
        {
            items.Add(item);
        }

        // Assert
        items.Should().BeEmpty();
    }
}
