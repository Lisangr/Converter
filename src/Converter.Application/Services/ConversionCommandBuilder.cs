using Converter.Domain.Models;

namespace Converter.Application.Services;

public sealed class ConversionCommandBuilder
{
    public ConversionCommand Build(MediaInfo mediaInfo, ConversionProfile profile, string inputPath, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        var args = new List<string>
        {
            "-y",
            "-i",
            inputPath,
            "-c:v",
            profile.VideoCodec,
            "-c:a",
            profile.AudioCodec
        };

        if (profile.VideoBitrateKbps is { } videoBitrate)
        {
            args.AddRange(new[] { "-b:v", $"{videoBitrate}k" });
        }

        if (profile.AudioBitrateKbps is { } audioBitrate)
        {
            args.AddRange(new[] { "-b:a", $"{audioBitrate}k" });
        }

        foreach (var pair in profile.AdditionalArguments)
        {
            args.Add(pair.Key);
            if (!string.IsNullOrWhiteSpace(pair.Value))
            {
                args.Add(pair.Value);
            }
        }

        args.Add(outputPath);

        return new ConversionCommand(args);
    }
}
