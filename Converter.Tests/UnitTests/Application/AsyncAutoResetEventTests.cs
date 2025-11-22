using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Application;

public class AsyncAutoResetEventTests
{
    [Fact]
    public async Task WaitAsync_ShouldResumeWhenSetIsCalled()
    {
        var ev = new AsyncAutoResetEvent();
        ev.Reset();

        var cts = new CancellationTokenSource(millisecondsDelay: 2000);
        var waitTask = Task.Run(() => ev.WaitAsync(cts.Token));

        // Даём немного времени, чтобы WaitAsync успел заблокироваться
        await Task.Delay(100);
        ev.Set();

        await waitTask; // не должно упасть по таймауту
    }

    [Fact]
    public void Dispose_ShouldCancelWaitingTasks()
    {
        var ev = new AsyncAutoResetEvent();
        ev.Reset();

        var cts = new CancellationTokenSource();
        var waitTask = ev.WaitAsync(cts.Token);

        ev.Dispose();

        waitTask.Invoking(t => t.GetAwaiter().GetResult())
            .Should().Throw<OperationCanceledException>();
    }
}
