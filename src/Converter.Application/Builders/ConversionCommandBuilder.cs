using System.Text;
using Converter.Application.Abstractions;

namespace Converter.Application.Builders;

public interface IConversionCommandBuilder
{
    string Build(ConversionRequest request);
}

public sealed class ConversionCommandBuilder : IConversionCommandBuilder
{
    public string Build(ConversionRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("-y ");
        sb.Append("-i ").Append(Escape(request.InputPath)).Append(' ');

        var profile = request.Profile;
        if (!string.IsNullOrWhiteSpace(profile.VideoCodec))
            sb.Append("-c:v ").Append(profile.VideoCodec).Append(' ');

        if (profile.Crf.HasValue)
            sb.Append("-crf ").Append(profile.Crf.Value).Append(' ');

        if (!string.IsNullOrWhiteSpace(profile.AudioCodec))
            sb.Append("-c:a ").Append(profile.AudioCodec).Append(' ');

        if (!string.IsNullOrWhiteSpace(profile.AudioBitrateK))
            sb.Append("-b:a ").Append(profile.AudioBitrateK).Append(' ');

        // Add resolution scaling if specified
        if (request.TargetWidth.HasValue || request.TargetHeight.HasValue)
        {
            var width = request.TargetWidth.HasValue ? request.TargetWidth.Value : -1;
            var height = request.TargetHeight.HasValue ? request.TargetHeight.Value : -2;
            sb.Append("-vf ").Append($"scale={width}:{height}").Append(' ');
        }

        // container is inferred by output extension
        sb.Append(Escape(request.OutputPath));
        return sb.ToString();
    }

    private static string Escape(string path)
        => $"\"{path.Replace("\"", "\\\"")}\"";
}
