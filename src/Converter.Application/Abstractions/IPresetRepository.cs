using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

public interface IPresetRepository
{
    Task<IReadOnlyCollection<ConversionProfile>> LoadAsync(CancellationToken cancellationToken);
}
