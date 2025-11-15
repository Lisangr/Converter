using Converter.Application.Interfaces;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace Converter.Infrastructure.Services;

public sealed class FfmpegExecutor : IFFmpegExecutor, IAsyncDisposable
{
    private readonly ILogger<FfmpegExecutor> _logger;
    private readonly SemaphoreSlim _ffmpegGate = new(1, 1);
    private string? _ffmpegPath;

    public FfmpegExecutor(ILogger<FfmpegExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<MediaInfo> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        await EnsureFfmpegAsync(cancellationToken).ConfigureAwait(false);
        var info = await FFmpeg.GetMediaInfo(inputPath).ConfigureAwait(false);
        var video = info.VideoStreams?.FirstOrDefault();
        var audio = info.AudioStreams?.FirstOrDefault();
        if (video is null || audio is null)
        {
            throw new InvalidOperationException("Input file does not contain both video and audio streams");
        }

        return new MediaInfo(
            new MediaStreamInfo(video.Codec, video.PixelFormat, $"{video.Width}x{video.Height}", video.Duration),
            new MediaStreamInfo(audio.Codec, audio.SampleFormat, null, audio.Duration));
    }

    public async Task ExecuteAsync(ConversionCommand command, IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
    {
        await EnsureFfmpegAsync(cancellationToken).ConfigureAwait(false);
        var conversion = FFmpeg.Conversions.New();
        conversion.SetOverwriteOutput(true);
        conversion.OnProgress += (_, args) =>
        {
            progress.Report(new ConversionProgress(args.Percent, args.EstimatedTimeLeft, $"{args.ProcessedDuration:g}"));
        };
        conversion.AddParameter(string.Join(' ', command.Arguments));
        await conversion.Start(cancellationToken).ConfigureAwait(false);
    }

    public string ResolveExecutable()
    {
        if (_ffmpegPath is null)
        {
            throw new InvalidOperationException("FFmpeg binaries are not ready yet");
        }

        return _ffmpegPath;
    }

    private async Task EnsureFfmpegAsync(CancellationToken cancellationToken)
    {
        if (_ffmpegPath is not null)
        {
            return;
        }

        await _ffmpegGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_ffmpegPath is not null)
            {
                return;
            }

            var binariesFolder = Path.Combine(Path.GetTempPath(), "ffmpeg-binaries");
            Directory.CreateDirectory(binariesFolder);
            FFmpeg.SetExecutablesPath(binariesFolder);
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, binariesFolder).ConfigureAwait(false);
            _ffmpegPath = FFmpeg.ExecutablesPath;
            _logger.LogInformation("FFmpeg ready at {Path}", _ffmpegPath);
        }
        finally
        {
            _ffmpegGate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _ffmpegGate.Dispose();
        return ValueTask.CompletedTask;
    }
}
