using System.Text.Json;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.Infrastructure.Persistence;

public sealed class SettingsStore : ISettingsStore
{
    private readonly string _storagePath;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

    public SettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _storagePath = Path.Combine(appData, "Converter", "profiles.json");
    }

    public async Task<IReadOnlyCollection<ConversionProfile>> GetProfilesAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_storagePath))
        {
            return Array.Empty<ConversionProfile>();
        }

        await using var stream = File.OpenRead(_storagePath);
        var profiles = await JsonSerializer.DeserializeAsync<List<ConversionProfile>>(stream, _options, cancellationToken).ConfigureAwait(false);
        return profiles ?? new List<ConversionProfile>();
    }

    public async Task SaveProfilesAsync(IEnumerable<ConversionProfile> profiles, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
        await using var stream = File.Create(_storagePath);
        await JsonSerializer.SerializeAsync(stream, profiles, _options, cancellationToken).ConfigureAwait(false);
    }
}
