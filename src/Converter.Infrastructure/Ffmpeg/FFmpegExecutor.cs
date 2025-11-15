using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace Converter.Infrastructure.Ffmpeg;

public sealed class FFmpegExecutor : IFFmpegExecutor
{
    private readonly ILogger<FFmpegExecutor> _logger;
    private readonly Lazy<Task> _initialization;

    public FFmpegExecutor(ILogger<FFmpegExecutor> logger)
    {
        _logger = logger;
        _initialization = new Lazy<Task>(InitializeAsync);
    }

    private async Task InitializeAsync()
    {
        var temp = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        if (!Directory.Exists(temp))
        {
            Directory.CreateDirectory(temp);
        }

        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, temp);
        FFmpeg.SetExecutablesPath(temp);
    }

    public async Task<MediaInfo> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        await _initialization.Value.ConfigureAwait(false);
        var info = await FFmpeg.GetMediaInfo(inputPath).ConfigureAwait(false);
        var video = info.VideoStreams.FirstOrDefault();
        return new MediaInfo(
            info.Duration,
            video?.Width ?? 0,
            video?.Height ?? 0,
            video?.Framerate ?? 0);
    }

    public async Task RunAsync(
        string arguments,
        ConversionRequest request,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken)
    {
        await _initialization.Value.ConfigureAwait(false);
        var conversion = FFmpeg.Conversions.New();
        conversion.SetOverwriteOutput(true);
        conversion.AddParameter(arguments);
        conversion.OnProgress += (_, args) =>
        {
            var remaining = args.TotalLength - args.Duration;
            progress.Report(new ConversionProgress(args.Percent, remaining, request.InputPath));
        };
        await conversion.Start(cancellationToken).ConfigureAwait(false);
    }
}
