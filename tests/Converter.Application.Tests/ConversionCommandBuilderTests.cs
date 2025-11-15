using Converter.Application.Builders;
using Converter.Domain.Models;
using Xunit;

namespace Converter.Application.Tests;

public sealed class ConversionCommandBuilderTests
{
    [Fact]
    public void Build_ComposesCommandWithProfileSettings()
    {
        var profile = new ConversionProfile("Test", "mp4", "libx264", "aac", new Dictionary<string, string>
        {
            ["-preset"] = "fast"
        });
        var request = new ConversionRequest("input.mkv", "c:/out", profile);
        var mediaInfo = new MediaInfo(TimeSpan.FromSeconds(60), 1920, 1080, 30);
        var builder = new ConversionCommandBuilder();

        var command = builder.Build(mediaInfo, request, "c:/out/input.mp4");

        Assert.Contains("-c:v libx264", command);
        Assert.Contains("-preset fast", command);
        Assert.Contains("\"c:/out/input.mp4\"", command);
    }
}
