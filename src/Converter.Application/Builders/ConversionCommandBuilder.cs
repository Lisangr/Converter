using System.Text;
using Converter.Domain.Models;

namespace Converter.Application.Builders;

public sealed class ConversionCommandBuilder
{
    public string Build(MediaInfo mediaInfo, ConversionRequest request, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(mediaInfo);
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required", nameof(outputPath));
        }

        var builder = new StringBuilder();
        builder.Append("-i \"").Append(request.InputPath).Append("\" ");
        builder.Append("-c:v ").Append(request.Profile.VideoCodec).Append(' ');
        builder.Append("-c:a ").Append(request.Profile.AudioCodec).Append(' ');

        foreach (var kvp in request.Profile.ExtraParameters)
        {
            builder.Append(kvp.Key).Append(' ');
            if (!string.IsNullOrWhiteSpace(kvp.Value))
            {
                builder.Append(kvp.Value).Append(' ');
            }
        }

        var durationSeconds = Math.Max(1, mediaInfo.Duration.TotalSeconds);
        double fallbackBitrate = mediaInfo.Width * mediaInfo.Height * mediaInfo.FrameRate / durationSeconds;
        if (!request.Metadata.TryGetValue("bitrate", out var bitrateString) || !int.TryParse(bitrateString, out var bitrate))
        {
            bitrate = (int)Math.Max(128000, fallbackBitrate);
        }
        builder.Append("-b:v ").Append(bitrate).Append(' ');

        builder.Append('"').Append(outputPath).Append('"');
        return builder.ToString();
    }
}
