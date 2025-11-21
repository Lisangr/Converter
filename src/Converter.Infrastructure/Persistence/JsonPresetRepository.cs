using Converter.Application.Abstractions;
using Converter.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Infrastructure.Persistence;

/// <summary>
/// Фасад для обратной совместимости с тестами.
/// Предоставляет старые названия методов через новые интерфейсы.
/// </summary>
public class JsonPresetRepository : IPresetRepository
{
    private readonly IPresetRepository _innerRepository;

    public JsonPresetRepository(string testFilePath, Microsoft.Extensions.Logging.ILogger<JsonPresetRepository> logger)
    {
        // В реальной реализации здесь будет логика инициализации
        _innerRepository = new JsonPresetRepositoryImpl();
    }

    public async Task<IReadOnlyList<ConversionProfile>> GetPresetsAsync(CancellationToken ct = default)
    {
        return await _innerRepository.GetPresetsAsync(ct);
    }

    public async Task<ConversionProfile?> GetPresetAsync(string id, CancellationToken ct = default)
    {
        return await _innerRepository.GetPresetAsync(id, ct);
    }

    public async Task SavePresetAsync(ConversionProfile preset, CancellationToken ct = default)
    {
        await _innerRepository.SavePresetAsync(preset, ct);
    }

    public async Task DeletePresetAsync(string id, CancellationToken ct = default)
    {
        await _innerRepository.DeletePresetAsync(id, ct);
    }

    // Приватная реализация для заглушки
    private class JsonPresetRepositoryImpl : IPresetRepository
    {
        public Task<IReadOnlyList<ConversionProfile>> GetPresetsAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<ConversionProfile>>(Array.Empty<ConversionProfile>());
        }

        public Task<ConversionProfile?> GetPresetAsync(string id, CancellationToken ct = default)
        {
            return Task.FromResult<ConversionProfile?>(null);
        }

        public Task SavePresetAsync(ConversionProfile preset, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task DeletePresetAsync(string id, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}