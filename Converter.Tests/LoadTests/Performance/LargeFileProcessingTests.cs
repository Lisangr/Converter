using System;
using System.IO;
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

public class LargeFileProcessingTests
{
    [Fact]
    public async Task Conversion_ShouldHandleLargeFiles()
    {
        var executor = new Mock<IFFmpegExecutor>();
        executor.Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var builder = new Mock<IConversionCommandBuilder>();
        builder.Setup(b => b.Build(It.IsAny<ConversionRequest>())).Returns("-i input -o output");
        var logger = Mock.Of<ILogger<ConversionOrchestrator>>();
        var orchestrator = new ConversionOrchestrator(executor.Object, builder.Object, logger);
        var outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");
        await File.WriteAllBytesAsync(outputFile, new byte[1024 * 1024]);
        var profile = new Converter.Application.Models.ConversionProfile("Default", "libx264", "aac", "128k", 23);
        var request = new ConversionRequest("input.mp4", outputFile, profile);
        var progress = new Progress<int>();

        var result = await orchestrator.ConvertAsync(request, progress, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.OutputSize >= 1024 * 1024);
        File.Delete(outputFile);
    }
}
