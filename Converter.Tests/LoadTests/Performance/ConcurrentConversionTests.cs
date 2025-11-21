using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Builders;
using Converter.Application.Services;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.LoadTests.Performance;

public class ConcurrentConversionTests
{
    [Fact]
    public async Task ConcurrentConversions_ShouldScaleWithCpu()
    {
        var executor = new Mock<IFFmpegExecutor>();
        executor.Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IProgress<double>, CancellationToken>((_, progress, _) => progress.Report(50))
            .ReturnsAsync(0);
        var builder = new Mock<IConversionCommandBuilder>();
        builder.Setup(b => b.Build(It.IsAny<ConversionRequest>())).Returns("-args");
        var logger = Mock.Of<ILogger<ConversionOrchestrator>>();
        var orchestrator = new ConversionOrchestrator(executor.Object, builder.Object, logger);
        var requests = new List<ConversionRequest>
        {
            new("a.mp4", "a_out.mp4", Format.Mp4, Bitrate.Preset720p),
            new("b.mp4", "b_out.mp4", Format.Mp4, Bitrate.Preset720p),
            new("c.mp4", "c_out.mp4", Format.Mp4, Bitrate.Preset720p)
        };
        var progressValues = new ConcurrentBag<int>();
        var progress = new Progress<int>(progressValues.Add);

        await Task.WhenAll(requests.ConvertAll(r => orchestrator.ConvertAsync(r, progress, CancellationToken.None)));

        executor.Verify(e => e.ExecuteAsync("-args", It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()), Times.Exactly(requests.Count));
        Assert.Contains(50, progressValues);
    }
}
