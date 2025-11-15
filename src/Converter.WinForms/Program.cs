using Converter.Application.Interfaces;
using Converter.Application.Presenters;
using Converter.Application.Services;
using Converter.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Converter.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var host = BuildHost();
        host.Start();
        var mainForm = host.Services.GetRequiredService<MainForm>();
        _ = host.Services.GetRequiredService<MainPresenter>();
        Application.Run(mainForm);
        host.StopAsync().GetAwaiter().GetResult();
    }

    private static IHost BuildHost() => Host.CreateDefaultBuilder()
        .ConfigureLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
            builder.AddDebug();
        })
        .ConfigureServices(services =>
        {
            services.AddSingleton<MainForm>();
            services.AddSingleton<IMainView>(sp => sp.GetRequiredService<MainForm>());
            services.AddSingleton<MainPresenter>();
            services.AddSingleton<IQueueService, QueueService>();
            services.AddSingleton<IConversionOrchestrator, ConversionOrchestrator>();
            services.AddSingleton<ConversionCommandBuilder>();
            services.AddSingleton<IFFmpegExecutor, FfmpegExecutor>();
            services.AddSingleton<IThumbnailProvider, ThumbnailProvider>();
            services.AddSingleton<INotificationGateway, NotificationGateway>();
            services.AddSingleton<ISettingsStore, SettingsStore>();
            services.AddSingleton<IPresetRepository, PresetRepository>();
        })
        .Build();
}
