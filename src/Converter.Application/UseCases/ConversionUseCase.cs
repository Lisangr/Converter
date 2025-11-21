using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Domain.Models;

namespace Converter.Application.UseCases;

/// <summary>
/// Use Case для выполнения конвертации отдельного элемента очереди.
/// </summary>
public class ConversionUseCase : IConversionUseCase
{
    private readonly IConversionOrchestrator _orchestrator;
    private readonly IFileSystem _fileSystem;

    public ConversionUseCase(IConversionOrchestrator orchestrator, IFileSystem fileSystem)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public async Task<ConversionResult> ExecuteAsync(QueueItem item, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        try
        {
            // Проверяем существование входного файла
            var inputExists = await _fileSystem.FileExistsAsync(item.FilePath);
            if (!inputExists)
            {
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = $"Input file not found: {item.FilePath}"
                };
            }

            // Определяем путь вывода
            var outputPath = item.OutputPath ?? System.IO.Path.ChangeExtension(item.FilePath, ".mp4");
            
            // Создаем профиль конвертации (можно взять из настроек элемента)
            var profile = new ConversionProfile("Default", "libx264", "aac", "128k", 23);
            var request = new ConversionRequest(item.FilePath, outputPath, profile);

            // Выполняем конвертацию
            var orchestratorProgress = new Progress<int>(p => progress?.Report(p));
            var outcome = await _orchestrator.ConvertAsync(request, orchestratorProgress, cancellationToken);

            return new ConversionResult
            {
                Success = outcome.Success,
                ErrorMessage = outcome.ErrorMessage,
                OutputPath = outcome.Success ? outputPath : null,
                OutputFileSize = outcome.OutputSize ?? 0
            };
        }
        catch (Exception ex)
        {
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}