using Converter.Application.Interfaces;
using Converter.Domain.Models;

namespace Converter.Infrastructure.Services;

public sealed class PresetRepository : IPresetRepository
{
    private static readonly IReadOnlyList<ConversionProfile> BuiltIn = new List<ConversionProfile>
    {
        new("H.264 MP4", "mp4", "libx264", "aac", 5000, 192, new Dictionary<string, string> { ["-preset"] = "medium" }),
        new("HEVC MKV", "mkv", "libx265", "aac", 3500, 192, new Dictionary<string, string> { ["-preset"] = "slow" })
    };

    public Task<IReadOnlyList<ConversionProfile>> GetBuiltInProfilesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(BuiltIn);
    }
}
