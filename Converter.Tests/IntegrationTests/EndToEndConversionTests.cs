using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Application.Services;
using Converter.Domain.Models;
using Converter.Infrastructure.Ffmpeg;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Converter.Application.Abstractions;
using Converter.Application.Builders;
using Converter.Application.Models;
using Converter.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Tests.IntegrationTests;

public class EndToEndConversionTests : IDisposable
{
    [Fact]
    public async Task FullConversion_ShouldProduceExpectedOutput()
    {
        var builder = new Mock<IConversionCommandBuilder>(MockBehavior.Strict);
        builder.Setup(b => b.Build(It.IsAny<ConversionRequest>())).Returns("-i input -o output");
        var executor = new StubExecutor(exitCode: 0, createOutput: true);
        var orchestrator = new ConversionOrchestrator(executor, builder.Object, NullLogger<ConversionOrchestrator>.Instance);

        var request = new ConversionRequest("input.mp4", Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var progress = new Progress<int>();

        var outcome = await orchestrator.ConvertAsync(request, progress, CancellationToken.None);

        outcome.Success.Should().BeTrue();
        File.Exists(request.OutputPath).Should().BeTrue();
        executor.ExecutedArguments.Should().Contain("-i input -o output");
    }

    [Fact]
    public async Task FullConversion_ShouldReportProgress()
    {
        var builder = new Mock<IConversionCommandBuilder>(MockBehavior.Strict);
        builder.Setup(b => b.Build(It.IsAny<ConversionRequest>())).Returns("args");
        var executor = new StubExecutor(exitCode: 0, progressSteps: new[] { 0.1, 0.5, 0.9 });
        var orchestrator = new ConversionOrchestrator(executor, builder.Object, NullLogger<ConversionOrchestrator>.Instance);

        var reported = new List<int>();
        var progress = new Progress<int>(p => reported.Add(p));

        var request = new ConversionRequest("input.mp4", Path.GetTempFileName());
        var outcome = await orchestrator.ConvertAsync(request, progress, CancellationToken.None);

        outcome.Success.Should().BeTrue();
        reported.Should().Contain(10);
        reported.Should().Contain(50);
        reported.Should().Contain(90);
    }

    [Fact]
    public async Task FullConversion_ShouldHandleCancellation()
    {
        var builder = new Mock<IConversionCommandBuilder>(MockBehavior.Strict);
        builder.Setup(b => b.Build(It.IsAny<ConversionRequest>())).Returns("args");
        var executor = new StubExecutor(exitCode: 0, throwOnCancel: true);
        var orchestrator = new ConversionOrchestrator(executor, builder.Object, NullLogger<ConversionOrchestrator>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = new ConversionRequest("input.mp4", Path.GetTempFileName());

        var act = () => orchestrator.ConvertAsync(request, new Progress<int>(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class StubExecutor : IFFmpegExecutor
    {
        private readonly int _exitCode;
        private readonly bool _createOutput;
        private readonly IEnumerable<double>? _progressSteps;
        private readonly bool _throwOnCancel;

        public string? ExecutedArguments { get; private set; }

        public StubExecutor(int exitCode, bool createOutput = false, IEnumerable<double>? progressSteps = null, bool throwOnCancel = false)
        {
            _exitCode = exitCode;
            _createOutput = createOutput;
            _progressSteps = progressSteps;
            _throwOnCancel = throwOnCancel;
        }

        public Task ProbeAsync(string inputPath, CancellationToken ct) => Task.CompletedTask;

        public Task<int> ExecuteAsync(string arguments, IProgress<double> progress, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ExecutedArguments = arguments;
            if (_progressSteps != null)
            {
                foreach (var step in _progressSteps)
                {
                    progress.Report(step);
                }
            }

            if (_createOutput && arguments.Contains("output"))
            {
                var parts = arguments.Split(' ');
                var outputPath = parts[^1];
                File.WriteAllText(outputPath, "data");
            }

            if (_throwOnCancel && ct.IsCancellationRequested)
            {
                throw new OperationCanceledException(ct);
            }

            return Task.FromResult(_exitCode);
        }

        public Task<string> GetVersionAsync(CancellationToken ct = default) => Task.FromResult("ffmpeg 1.0");

        public Task<bool> IsFfmpegAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);

        public Task<string> GetMediaInfoAsync(string inputPath, CancellationToken ct = default) => Task.FromResult("info");
    }
}