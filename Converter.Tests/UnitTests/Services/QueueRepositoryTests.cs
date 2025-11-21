using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Services
{
    public class QueueRepositoryTests
    {
        private readonly Mock<IQueueStore> _mockQueueStore;
        private readonly Mock<ILogger<QueueRepository>> _mockLogger;
        private readonly QueueRepository _repository;

        public QueueRepositoryTests()
        {
            _mockQueueStore = new Mock<IQueueStore>();
            _mockLogger = new Mock<ILogger<QueueRepository>>();

            _mockQueueStore.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                .Returns(GetAsyncEnumerable(Array.Empty<QueueItem>()));

            _repository = new QueueRepository(_mockLogger.Object, _mockQueueStore.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new QueueRepository(null!, _mockQueueStore.Object));
        }

        [Fact]
        public void Constructor_WithNullStore_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new QueueRepository(_mockLogger.Object, null!));
        }

        [Fact]
        public async Task AddAsync_WithValidItem_ShouldAddToStoreAndCache()
        {
            // Arrange
            var item = CreateItem();

            // Act
            await _repository.AddAsync(item);

            // Assert
            _mockQueueStore.Verify(s => s.AddAsync(item, It.IsAny<CancellationToken>()), Times.Once);
            var all = await _repository.GetAllAsync();
            all.Should().ContainSingle(x => x.Id == item.Id);
        }

        [Fact]
        public async Task UpdateAsync_WithValidItem_ShouldUpdateStoreAndCache()
        {
            var item = CreateItem();
            await _repository.AddAsync(item);

            item.Status = ConversionStatus.Completed;

            await _repository.UpdateAsync(item);

            _mockQueueStore.Verify(s => s.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
            var updated = await _repository.GetByIdAsync(item.Id);
            updated!.Status.Should().Be(ConversionStatus.Completed);
        }

        [Fact]
        public async Task RemoveAsync_WithExistingItem_ShouldRemoveFromStoreAndCache()
        {
            var item = CreateItem();
            await _repository.AddAsync(item);

            await _repository.RemoveAsync(item.Id);

            _mockQueueStore.Verify(s => s.RemoveAsync(item.Id, It.IsAny<CancellationToken>()), Times.Once);
            var pending = await _repository.GetPendingItemsAsync();
            pending.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPendingItemsAsync_ShouldReturnOnlyPending()
        {
            var pending = CreateItem(status: ConversionStatus.Pending);
            var completed = CreateItem(status: ConversionStatus.Completed);

            _mockQueueStore.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                .Returns(GetAsyncEnumerable(new[] { pending, completed }));

            var items = await _repository.GetPendingItemsAsync();

            items.Should().ContainSingle(x => x.Id == pending.Id);
        }

        [Fact]
        public async Task GetPendingCountAsync_ShouldReturnCorrectCount()
        {
            var pending1 = CreateItem(status: ConversionStatus.Pending);
            var pending2 = CreateItem(status: ConversionStatus.Pending);
            var completed = CreateItem(status: ConversionStatus.Completed);

            _mockQueueStore.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                .Returns(GetAsyncEnumerable(new[] { pending1, pending2, completed }));

            var count = await _repository.GetPendingCountAsync();

            count.Should().Be(2);
        }

        [Fact]
        public async Task AddAsync_WithNullItem_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
        }

        [Fact]
        public async Task UpdateAsync_WithNullItem_ShouldThrow()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.UpdateAsync(null!));
        }

        [Fact]
        public async Task RemoveRangeAsync_WithMultipleIds_ShouldRemoveAll()
        {
            var items = Enumerable.Range(0, 3).Select(_ => CreateItem()).ToList();
            foreach (var item in items)
            {
                await _repository.AddAsync(item);
            }

            var ids = items.Select(i => i.Id).ToList();

            await _repository.RemoveRangeAsync(ids);

            foreach (var id in ids)
            {
                _mockQueueStore.Verify(s => s.RemoveAsync(id, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            }
        }

        private static QueueItem CreateItem(ConversionStatus status = ConversionStatus.Pending)
        {
            return new QueueItem
            {
                Id = Guid.NewGuid(),
                FilePath = $"test_{Guid.NewGuid()}.mp4",
                Status = status,
                AddedAt = DateTime.UtcNow
            };
        }

        private static async IAsyncEnumerable<QueueItem> GetAsyncEnumerable(IEnumerable<QueueItem> items)
        {
            foreach (var item in items)
            {
                yield return item;
                await Task.Yield();
            }
        }
    }
}
