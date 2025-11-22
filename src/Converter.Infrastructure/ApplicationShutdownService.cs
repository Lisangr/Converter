using System;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure
{
    /// <summary>
    /// Реализация IApplicationShutdownService через IHostApplicationLifetime.
    /// </summary>
    public sealed class ApplicationShutdownService : IApplicationShutdownService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<ApplicationShutdownService> _logger;
        private bool _shutdownRequested;

        public ApplicationShutdownService(
            IHostApplicationLifetime lifetime,
            ILogger<ApplicationShutdownService> logger)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RequestShutdown()
        {
            if (_shutdownRequested)
            {
                _logger.LogDebug("Shutdown already requested, skipping duplicate call");
                return;
            }

            _shutdownRequested = true;

            try
            {
                _logger.LogInformation("Application shutdown requested from UI/presenter");
                _lifetime.StopApplication();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while requesting application shutdown");
                throw;
            }
        }
    }
}
