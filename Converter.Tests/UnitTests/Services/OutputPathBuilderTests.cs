using System;
using System.IO;
using Converter.Application.Services;
using Converter.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Services;

public class OutputPathBuilderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly OutputPathBuilder _builder;

    public OutputPathBuilderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        var logger = Mock.Of<ILogger<OutputPathBuilder>>();
        _builder = new OutputPathBuilder(logger);
    }

    [Fact]
    public void BuildOutputPath_SanitizesFileName_AndAddsMissingDot()
    {
        var item = new QueueItem { FilePath = Path.Combine(_tempDirectory, "inva*lid?.mp4") };

        var outputPath = _builder.BuildOutputPath(item, _tempDirectory, "mp3");

        Path.GetFileName(outputPath).Should().Be("inva_lid_.mp3");
    }

    [Fact]
    public void BuildOutputPath_GeneratesUniqueName_WhenFileExists()
    {
        var item = new QueueItem { FilePath = Path.Combine(_tempDirectory, "video.mp4") };
        var baseOutput = Path.Combine(_tempDirectory, "video.mp4");
        var firstCopy = Path.Combine(_tempDirectory, "video (1).mp4");

        File.WriteAllText(baseOutput, "original");
        File.WriteAllText(firstCopy, "duplicate");

        var outputPath = _builder.BuildOutputPath(item, _tempDirectory, ".mp4");

        outputPath.Should().Be(Path.Combine(_tempDirectory, "video (2).mp4"));
    }

    [Fact]
    public void BuildOutputPath_UsesQueueItemDirectory_WhenProfileOverloadIsUsed()
    {
        var item = new QueueItem
        {
            FilePath = Path.Combine(_tempDirectory, "source.mkv"),
            OutputDirectory = Path.Combine(_tempDirectory, "custom")
        };
        Directory.CreateDirectory(item.OutputDirectory!);

        var outputPath = _builder.BuildOutputPath(item, new Converter.Models.ConversionProfile());

        outputPath.Should().Be(Path.Combine(item.OutputDirectory!, "source.mkv"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
