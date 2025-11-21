using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Infrastructure.Ffmpeg;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.LoadTests.Performance;

public class ThumbnailCachingPerformanceTests
{
    [Fact]
    public async Task ThumbnailCaching_ShouldReuseGeneratedImages()
    {
        var executor = new Mock<IFFmpegExecutor>();
        executor.Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IProgress<double>, CancellationToken>((args, _, _) =>
            {
                var match = Regex.Match(args, "\"(?<path>[^\"]+\\.jpg)\"$");
                var path = match.Groups["path"].Value;
                File.WriteAllText(path, "image");
            })
            .ReturnsAsync(0);
        var logger = Mock.Of<ILogger<ThumbnailProvider>>();
        var provider = new ThumbnailProvider(executor.Object, logger);

        await using var first = await provider.GetThumbnailAsync("video.mp4", 120, 90, CancellationToken.None);
        await using var second = await provider.GetThumbnailAsync("video.mp4", 120, 90, CancellationToken.None);

        Assert.NotSame(first, second);
        executor.Verify(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
