using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.Infrastructure.Persistence;

public sealed class PresetRepository : IPresetRepository
{
    private static readonly IReadOnlyCollection<ConversionProfile> DefaultProfiles = new List<ConversionProfile>
    {
        new("H.264 MP4", "mp4", "libx264", "aac", new Dictionary<string, string> { ["-preset"] = "medium" }),
        new("HEVC MKV", "mkv", "libx265", "aac", new Dictionary<string, string> { ["-preset"] = "slow" }),
        new("Audio Only", "mp3", "copy", "libmp3lame", new Dictionary<string, string> { ["-vn"] = string.Empty })
    };

    public Task<IReadOnlyCollection<ConversionProfile>> LoadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(DefaultProfiles);
    }
}
