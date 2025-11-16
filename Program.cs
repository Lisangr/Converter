using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Converter.Application.Abstractions;
using Converter.Application.Builders;
using Converter.Application.Presenters;
using Converter.Application.Services;
using Converter.Infrastructure.Ffmpeg;
using Converter.Infrastructure.Notifications;
using Converter.Infrastructure.Persistence;

namespace Converter
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                var mainForm = InitializeApplication().GetAwaiter().GetResult();
                System.Windows.Forms.Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Fatal error during application startup: {ex.Message}", 
                    "Application Error", 
                    System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private static async Task<Form> InitializeApplication()
        {
            var services = new ServiceCollection()
                .AddLogging(configure => 
                    configure.AddDebug()
                            .SetMinimumLevel(LogLevel.Debug))
                .AddSingleton<IConversionCommandBuilder, ConversionCommandBuilder>()
                .AddSingleton<IFFmpegExecutor, FFmpegExecutor>()
                .AddSingleton<IConversionOrchestrator, ConversionOrchestrator>()
                .AddSingleton<IQueueService, QueueService>()
                .AddSingleton<INotificationGateway, NotificationGateway>()
                .AddSingleton<IThumbnailProvider, ThumbnailProvider>()
                .AddSingleton<ISettingsStore, FileSettingsStore>()
                .AddSingleton<IPresetRepository, JsonPresetRepository>()
                .BuildServiceProvider();

            // Create and configure the main form
            var view = new Form1() as IMainView;
            var presenter = new MainPresenter(
                view,
                services.GetRequiredService<IQueueService>(),
                services.GetRequiredService<IConversionOrchestrator>(),
                services.GetRequiredService<INotificationGateway>(),
                services.GetRequiredService<ISettingsStore>(),
                services.GetRequiredService<IPresetRepository>(),
                services.GetRequiredService<ILogger<MainPresenter>>());

            // Initialize the presenter asynchronously
            await presenter.StartAsync();
            
            return (Form)view;
        }
    }
}