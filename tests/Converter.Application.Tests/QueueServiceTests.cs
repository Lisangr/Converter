using Converter.Application.Interfaces;
using Converter.Application.Services;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Converter.Application.Tests;

public class QueueServiceTests
{
    [Fact]
    public async Task EnqueueAsync_RaisesEvents()
    {
        var orchestrator = new FakeOrchestrator();
        await using var service = new QueueService(orchestrator, NullLogger<QueueService>.Instance);
        var progressTcs = new TaskCompletionSource<ConversionProgress>();
        var completionTcs = new TaskCompletionSource<ConversionResult>();
        service.ProgressChanged += (_, payload) => progressTcs.TrySetResult(payload.Progress);
        service.ItemCompleted += (_, payload) => completionTcs.TrySetResult(payload.Result);

        var request = new ConversionRequest("input.mp4", "C:/temp", new ConversionProfile("mp4", "mp4", "libx264", "aac", 1000, 128, new Dictionary<string, string>()), new Dictionary<string, string>(), CancellationToken.None);
        await service.EnqueueAsync(request, CancellationToken.None);

        Assert.NotNull(await progressTcs.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        var result = await completionTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsType<ConversionResult.Success>(result);
    }

    private sealed class FakeOrchestrator : IConversionOrchestrator
    {
        public Task<ConversionResult> ExecuteAsync(ConversionRequest request, IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
        {
            progress.Report(new ConversionProgress(50, TimeSpan.FromSeconds(10), "Running"));
            return Task.FromResult<ConversionResult>(new ConversionResult.Success(request.InputPath, Path.Combine(request.OutputDirectory, "output.mp4"), TimeSpan.FromMinutes(1)));
        }
    }
}
