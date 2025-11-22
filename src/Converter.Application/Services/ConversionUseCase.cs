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
using Converter.Application.Models;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public class ConversionUseCase : IConversionUseCase
    {
        private readonly IProfileProvider _profileProvider;
        private readonly IOutputPathBuilder _pathBuilder;
        private readonly ILogger<ConversionUseCase> _logger;
        private readonly IConversionOrchestrator _orchestrator;

        public ConversionUseCase(
            IProfileProvider profileProvider,
            IOutputPathBuilder pathBuilder,
            IConversionOrchestrator orchestrator,
            ILogger<ConversionUseCase> logger)
        {
            _profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
            _pathBuilder = pathBuilder ?? throw new ArgumentNullException(nameof(pathBuilder));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ConversionResult> ExecuteAsync(
            QueueItem item, 
            IProgress<int>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            try
            {
                // Check if cancellation was requested before starting
                cancellationToken.ThrowIfCancellationRequested();

                // Update item status to processing
                item.Status = ConversionStatus.Processing;
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

                // Map profile to orchestrator profile
                var orchestratorProfile = new ConversionProfile(
                    profile.Name,
                    profile.VideoCodec ?? string.Empty,
                    profile.AudioCodec ?? string.Empty,
                    profile.AudioBitrate.HasValue ? $"{profile.AudioBitrate.Value}k" : null,
                    profile.CRF ?? 23);

                var request = new ConversionRequest(
                    item.FilePath, 
                    outputPath, 
                    orchestratorProfile,
                    TargetWidth: profile.Width,
                    TargetHeight: profile.Height);
                var safeProgress = progress ?? new Progress<int>(_ => { });
                var outcome = await _orchestrator.ConvertAsync(request, safeProgress, cancellationToken).ConfigureAwait(false);

                // Only update status if not canceled
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (outcome.Success)
                    {
                        item.Status = ConversionStatus.Completed;
                        
                        var size = outcome.OutputSize;
                        if (!size.HasValue)
                        {
                            try
                            {
                                if (File.Exists(outputPath))
                                {
                                    var fileInfo = new FileInfo(outputPath);
                                    size = fileInfo.Length;
                                }
                            }
                            catch
                            {
                                // ignore size detection errors
                            }
                        }

                        _logger.LogInformation("Successfully converted item {ItemId} to {OutputPath}",
                            item.Id, outputPath);

                        return new ConversionResult
                        {
                            Success = true,
                            OutputFileSize = size ?? 0,
                            OutputPath = outputPath
                        };
                    }
                    else
                    {
                        item.Status = ConversionStatus.Failed;
                        _logger.LogError("Conversion failed for item {ItemId}: {Error}", item.Id, outcome.ErrorMessage);
                        return new ConversionResult
                        {
                            Success = false,
                            ErrorMessage = outcome.ErrorMessage
                        };
                    }
                }

                // If we got here, operation was canceled
                item.Status = ConversionStatus.Cancelled;
                _logger.LogInformation("Conversion for item {ItemId} was cancelled", item.Id);
                throw new OperationCanceledException("Conversion was canceled", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Update item status to canceled
                item.Status = ConversionStatus.Cancelled;
                _logger.LogInformation("Conversion for item {ItemId} was cancelled", item.Id);
                throw; // Re-throw to let the queue processor handle it
            }
            catch (Exception ex)
            {
                // Update item status to failed
                item.Status = ConversionStatus.Failed;
                item.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error converting item {ItemId}", item.Id);
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<Converter.Application.Models.ConversionProfile> GetConversionProfile(QueueItem item)
        {
            // Currently we always use the default profile; per-item profiles are not wired yet
            return await _profileProvider.GetDefaultProfileAsync();
        }
    }
}
