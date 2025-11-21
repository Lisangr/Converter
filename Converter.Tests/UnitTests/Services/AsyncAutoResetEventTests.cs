using System;
using System.Threading.Tasks;
using Converter.Application.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services;

public class AsyncAutoResetEventTests
{
    [Fact]
    public async Task WaitAsync_ReturnsImmediately_WhenNotPaused()
    {
        var resetEvent = new AsyncAutoResetEvent();

        var waitTask = resetEvent.WaitAsync();

        await waitTask; // Should not block
        resetEvent.IsPaused.Should().BeFalse();
    }

    [Fact]
    public async Task WaitAsync_BlocksWhenPaused_AndResumesOnSet()
    {
        var resetEvent = new AsyncAutoResetEvent();
        resetEvent.Reset();

        var waitTask = resetEvent.WaitAsync();

        await Task.Delay(50);
        waitTask.IsCompleted.Should().BeFalse();

        resetEvent.Set();

        await waitTask; // Should complete after Set
        resetEvent.IsPaused.Should().BeFalse();
    }

    [Fact]
    public async Task WaitAsync_ThrowsWhenDisposedDuringWait()
    {
        var resetEvent = new AsyncAutoResetEvent();
        resetEvent.Reset();

        var waitTask = resetEvent.WaitAsync();

        await Task.Delay(20);
        resetEvent.Dispose();

        await Assert.ThrowsAsync<TaskCanceledException>(() => waitTask);
    }
}
