using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services;

/// <summary>
/// HostedService для инициализации IThemeService при старте приложения.
/// Вызывает InitializeAsync один раз и больше ничего не делает.
/// </summary>
public sealed class ThemeBootstrapHostedService : IHostedService
{
    private readonly IThemeService _themeService;
    private readonly ILogger<ThemeBootstrapHostedService> _logger;

    public ThemeBootstrapHostedService(
        IThemeService themeService,
        ILogger<ThemeBootstrapHostedService> logger)
    {
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing ThemeService");
        await _themeService.InitializeAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("ThemeService initialized");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Ничего не делаем – жизненным циклом IThemeService управляет DI/Dispose
        return Task.CompletedTask;
    }
}
