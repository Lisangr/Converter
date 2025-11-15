using Converter.Application.Services;
using Converter.Domain.Models;
using Xunit;

namespace Converter.Application.Tests;

public class ConversionCommandBuilderTests
{
    [Fact]
    public void Build_ComposesArguments()
    {
        var builder = new ConversionCommandBuilder();
        var mediaInfo = new MediaInfo(
            new MediaStreamInfo("h264", "yuv420p", "1920x1080", TimeSpan.FromMinutes(2)),
            new MediaStreamInfo("aac", null, null, TimeSpan.FromMinutes(2)));
        var profile = new ConversionProfile("mp4", "mp4", "libx264", "aac", 4000, 192, new Dictionary<string, string> { ["-preset"] = "fast" });

        var result = builder.Build(mediaInfo, profile, "input.mp4", "output.mp4");

        Assert.Contains("-b:v", result.Arguments);
        Assert.Contains("4000k", result.Arguments);
        Assert.Contains("output.mp4", result.Arguments.Last());
    }
}
