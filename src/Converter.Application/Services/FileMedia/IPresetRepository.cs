using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Models;

namespace Converter.Application.Services.FileMedia;

public interface IPresetRepository
{
    Task<IReadOnlyList<PresetProfile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PresetProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task SaveAsync(PresetProfile preset, CancellationToken cancellationToken = default);
}
