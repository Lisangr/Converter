using Xabe.FFmpeg;

namespace Converter.Services;

/// <summary>
/// Provides a lossless concatenation workflow using FFmpeg concat demuxer.
/// All inputs must share the same container/codec parameters. At minimum,
/// the same file extension is required; ideally, probe with FFmpeg for
/// codec/stream compatibility.
/// </summary>
public static class LosslessConcatService
{
    /// <summary>
    /// Concatenates multiple input files into one output without re-encoding using the concat demuxer.
    /// Requires identical stream parameters across inputs.
    /// </summary>
    public static async Task<IConversionResult> ConcatLosslessAsync(
        IReadOnlyList<string> inputFiles,
        string outputPath)
    {
        if (inputFiles == null || inputFiles.Count == 0)
            throw new ArgumentException("No input files.", nameof(inputFiles));

        // Basic validation: same extension
        var firstExt = Path.GetExtension(inputFiles[0]).ToLowerInvariant();
        if (inputFiles.Any(f => Path.GetExtension(f).ToLowerInvariant() != firstExt))
            throw new InvalidOperationException("All input files must have the same container/extension for lossless concat.");

        // Optional: deeper validation using media info (codec names)
        try
        {
            var firstInfo = await FFmpeg.GetMediaInfo(inputFiles[0]).ConfigureAwait(false);
            var vCodec = firstInfo.VideoStreams.FirstOrDefault()?.Codec;
            var aCodec = firstInfo.AudioStreams.FirstOrDefault()?.Codec;

            foreach (var file in inputFiles.Skip(1))
            {
                var info = await FFmpeg.GetMediaInfo(file).ConfigureAwait(false);
                if (info.VideoStreams.FirstOrDefault()?.Codec != vCodec ||
                    info.AudioStreams.FirstOrDefault()?.Codec != aCodec)
                {
                    throw new InvalidOperationException("All input files must share the same codecs for lossless concatenation.");
                }
            }
        }
        catch
        {
            // If probing fails, proceed; concat may still fail and bubble up a clear error from ffmpeg.
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "ConverterConcat");
        Directory.CreateDirectory(tempDir);
        var listFilePath = Path.Combine(tempDir, $"concat_{Guid.NewGuid():N}.txt");

        // concat demuxer expects: file 'C:\\path\\to\\file'
        var lines = inputFiles.Select(path =>
        {
            var safe = path.Replace("'", "''");
            return $"file '{safe}'";
        });
        await File.WriteAllLinesAsync(listFilePath, lines).ConfigureAwait(false);

        try
        {
            string args =
                $"-f concat -safe 0 -i \"{listFilePath}\" " +
                "-c copy " +
                $"\"{outputPath}\"";

            return await FFmpeg.Conversions.New().Start(args).ConfigureAwait(false);
        }
        finally
        {
            try { if (File.Exists(listFilePath)) File.Delete(listFilePath); } catch { /* ignore */ }
        }
    }
}
