using Converter.Application.Abstractions;
using Converter.Application.Builders;
using Converter.Application.Presenters;
using Converter.Application.Services;
using Converter.Infrastructure.Ffmpeg;
using Converter.Infrastructure.Notifications;
using Converter.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Converter.WinForms;

internal static class Program
{
    [STAThread]
    private static async Task Main()
    {
        ApplicationConfiguration.Initialize();
        using var host = BuildHost();
        await host.StartAsync().ConfigureAwait(false);

        var presenter = host.Services.GetRequiredService<MainPresenter>();
        await presenter.InitializeAsync(CancellationToken.None).ConfigureAwait(false);

        var view = (Form)host.Services.GetRequiredService<IMainView>();
        Application.Run(view);

        await presenter.DisposeAsync().ConfigureAwait(false);
        await host.StopAsync().ConfigureAwait(false);
    }

    private static IHost BuildHost()
        => Host.CreateDefaultBuilder()
            .ConfigureLogging(builder => builder.AddDebug())
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IMainView, MainForm>();
                services.AddSingleton<MainPresenter>();
                services.AddSingleton<ConversionCommandBuilder>();
                services.AddSingleton<IConversionOrchestrator, ConversionOrchestrator>();
                services.AddSingleton<IQueueService, QueueService>();
                services.AddSingleton<IFFmpegExecutor, FFmpegExecutor>();
                services.AddSingleton<IThumbnailProvider, ThumbnailProvider>();
                services.AddSingleton<INotificationGateway, ToastNotificationGateway>();
                services.AddSingleton<ISettingsStore, SettingsStore>();
                services.AddSingleton<IPresetRepository, PresetRepository>();
            })
            .Build();
}
