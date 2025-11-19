using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Builders;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    /// <summary>
    /// Mock converter for testing purposes only.
    /// This simulates FFmpeg conversion with progress reporting.
    /// </summary>
    public class MockConverter : IConversionOrchestrator
    {
        private readonly ILogger<MockConverter> _logger;

        public MockConverter(ILogger<MockConverter> logger)
        {
            _logger = logger;
        }

        public Task ProbeAsync(string filePath, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public async Task<ConversionOutcome> ConvertAsync(ConversionRequest request, IProgress<int> progress, CancellationToken ct)
        {
            _logger.LogInformation("Starting mock conversion: {Input} -> {Output}", request.InputPath, request.OutputPath);

            try
            {
                // Simulate conversion progress
                for (int i = 0; i <= 100; i += 10)
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.LogInformation("Mock conversion cancelled");
                        throw new OperationCanceledException();
                    }

                    progress.Report(i);
                    _logger.LogDebug("Mock progress: {Progress}%", i);
                    await Task.Delay(500, ct); // Simulate processing time
                }

                _logger.LogInformation("Mock conversion completed successfully");
                return new ConversionOutcome(true, 1024 * 1024, null); // 1MB output
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Mock conversion was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mock conversion failed");
                return new ConversionOutcome(false, null, ex.Message);
            }
        }
    }
}