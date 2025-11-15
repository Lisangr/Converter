using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

public interface ISettingsStore
{
    Task<IReadOnlyCollection<ConversionProfile>> GetProfilesAsync(CancellationToken cancellationToken);
    Task SaveProfilesAsync(IEnumerable<ConversionProfile> profiles, CancellationToken cancellationToken);
}
