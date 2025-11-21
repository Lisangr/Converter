using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Models;

namespace Converter.Application.Services.FileMedia;

public class PresetService
{
    private readonly IPresetRepository _repository;

    public PresetService(IPresetRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<IReadOnlyList<PresetProfile>> GetPresetsAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public Task<PresetProfile?> GetPresetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));
        }

        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task SavePresetAsync(PresetProfile preset, CancellationToken cancellationToken = default)
    {
        if (preset == null) throw new ArgumentNullException(nameof(preset));
        return _repository.SaveAsync(preset, cancellationToken);
    }
}
