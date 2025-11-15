using Converter.Domain.Models;

namespace Converter.Application.Interfaces;

public interface IPresetRepository
{
    Task<IReadOnlyList<ConversionProfile>> GetBuiltInProfilesAsync(CancellationToken cancellationToken);
}
