/// <summary>
/// Сервис, инкапсулирующий логику выполнения конвертации.
/// Основные обязанности:
/// - Настройка параметров конвертации
/// - Создание и валидация <see cref="ConversionRequest"/>
/// - Оркестрация процесса конвертации
/// - Обработка ошибок и уведомление о состоянии
/// </summary>
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public class ConversionUseCase : IConversionUseCase
    {
        private readonly IProfileProvider _profileProvider;
        private readonly IOutputPathBuilder _pathBuilder;
        private readonly ILogger<ConversionUseCase> _logger;

        public ConversionUseCase(
            IProfileProvider profileProvider,
            IOutputPathBuilder pathBuilder,
            ILogger<ConversionUseCase> logger)
        {
            _profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
            _pathBuilder = pathBuilder ?? throw new ArgumentNullException(nameof(pathBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ConversionResult> ExecuteAsync(
            QueueItem item, 
            IProgress<int> progress = null, 
            CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            try
            {
                _logger.LogInformation("Starting conversion for item {ItemId}", item.Id);
                
                // Get the appropriate profile
                var profile = await GetConversionProfile(item);
                
                // Build output path
                var outputPath = _pathBuilder.BuildOutputPath(item, profile);
                item.OutputPath = outputPath;
                
                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Simulate conversion progress
                for (int i = 0; i <= 100; i += 5)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(i);
                    await Task.Delay(100, cancellationToken);
                }

                // Verify output file was created
                if (!File.Exists(outputPath))
                {
                    throw new FileNotFoundException("Output file was not created", outputPath);
                }

                var fileInfo = new FileInfo(outputPath);
                _logger.LogInformation("Successfully converted item {ItemId} to {OutputPath}", 
                    item.Id, outputPath);
                
                return new ConversionResult
                {
                    Success = true,
                    OutputFileSize = fileInfo.Length,
                    OutputPath = outputPath
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Conversion for item {ItemId} was cancelled", item.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting item {ItemId}", item.Id);
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<Converter.Models.ConversionProfile> GetConversionProfile(QueueItem item)
        {
            // Currently we always use the default profile; per-item profiles are not wired yet
            return await _profileProvider.GetDefaultProfileAsync();
        }
    }
}
