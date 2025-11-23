using System.IO;
using Converter.Application.Models;
using Converter.Application.Services;
using Converter.Domain.Models;
using Converter.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Converter.Tests.UnitTests.Application;

public class OutputPathBuilderTests
{
    [Fact]
    public void BuildOutputPath_WithSimpleParams_ShouldReturnPathWithExtension()
    {
        var builder = new OutputPathBuilder(NullLogger<OutputPathBuilder>.Instance);
        var item = new QueueItem { FilePath = "C:/input/video.mp4" };

        var path = builder.BuildOutputPath(item, "C:/output", ".mkv");

        path.Should().StartWith("C:/output");
        Path.GetExtension(path).Should().Be(".mkv");
    }

    [Fact]
    public void BuildOutputPath_WithPattern_ShouldRespectNamingPattern()
    {
        var builder = new OutputPathBuilder(NullLogger<OutputPathBuilder>.Instance);
        var item = new QueueItem
        {
            FilePath = "C:/input/video.mp4",
            NamingPattern = "{original}-{format}-{resolution}"
        };
        var profile = new ConversionProfile("p", "libx264", "aac", "128k", 23)
        {
            Width = 1920,
            Height = 1080,
            Format = "mp4"
        };

        var path = builder.BuildOutputPath(item, profile);

        Path.GetFileNameWithoutExtension(path)
            .Should().Contain("video").And.Contain("MP4").And.Contain("1920x1080");
    }
}
