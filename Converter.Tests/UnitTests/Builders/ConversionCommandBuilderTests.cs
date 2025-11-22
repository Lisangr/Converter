using Converter.Application.Builders;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Builders;

public class ConversionCommandBuilderTests
{
    private readonly ConversionCommandBuilder _builder = new();

    [Fact]
    public void BuildCommand_ShouldIncludeInputAndOutput()
    {
        var request = new ConversionRequest("input.mp4", "output.mkv", new ConversionProfile("p", "h264", "aac", "128k", 23));

        var args = _builder.Build(request);

        args.Should().Contain("input.mp4");
        args.Should().Contain("output.mkv");
    }

    [Fact]
    public void BuildCommand_ShouldApplyAudioOptions()
    {
        var profile = new ConversionProfile("p", "h264", "aac", "256k", 20);
        var request = new ConversionRequest("input.mp4", "output.mkv", profile);

        var args = _builder.Build(request);

        args.Should().Contain("-c:a aac");
        args.Should().Contain("-b:a 256k");
    }

    [Fact]
    public void BuildCommand_ShouldApplyVideoOptions()
    {
        var profile = new ConversionProfile("p", "libx265", "", null, 18);
        var request = new ConversionRequest("input.mp4", "output.mkv", profile);

        var args = _builder.Build(request);

        args.Should().Contain("-c:v libx265");
        args.Should().Contain("-crf 18");
    }
}
