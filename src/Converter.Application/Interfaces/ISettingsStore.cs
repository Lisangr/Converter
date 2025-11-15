using Converter.Domain.Models;

namespace Converter.Application.Interfaces;

public interface ISettingsStore
{
    Task<string?> GetLastOutputDirectoryAsync(CancellationToken cancellationToken);
    Task SetLastOutputDirectoryAsync(string path, CancellationToken cancellationToken);
    Task<IReadOnlyList<ConversionProfile>> GetProfilesAsync(CancellationToken cancellationToken);
    Task SaveProfileAsync(ConversionProfile profile, CancellationToken cancellationToken);
}
