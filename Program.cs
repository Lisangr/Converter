using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Converter.Application.Abstractions;
using Converter.Application.Presenters;
using Converter.Application.Services;
using Converter.Application.Builders;
using Converter.Application.ViewModels;
using Converter.Infrastructure;
using Converter.Infrastructure.Ffmpeg;
using Converter.Services;
using Converter.UI;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Converter
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            using var services = new ServiceCollection()
                .AddLogging(configure =>
                    configure.AddDebug()
                            .SetMinimumLevel(LogLevel.Information))
                .AddSingleton<IMainView, Form1>()
                .AddSingleton<MainPresenter>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<IQueueRepository, QueueRepository>()
                .AddSingleton<IConversionCommandBuilder, ConversionCommandBuilder>()
#if DEBUG
                .AddSingleton<IConversionOrchestrator, MockConverter>()  // Use mock for testing
#else
                .AddSingleton<IConversionOrchestrator, ConversionOrchestrator>()
#endif
                .AddSingleton<IConversionUseCase, ConversionUseCase>()
                .AddSingleton<IProfileProvider, ProfileProvider>()
                .AddSingleton<IOutputPathBuilder, OutputPathBuilder>()
                .AddSingleton<IProgressReporter, UiProgressReporter>()
                .AddSingleton<IThemeManager, ThemeManager>()
                .AddSingleton<IThemeService, ThemeService>()
                .AddSingleton<IQueueProcessor, ChannelQueueProcessor>()
                .AddSingleton<IFilePicker, WinFormsFilePicker>()
                .AddSingleton<IFolderPicker, WinFormsFolderPicker>()
                .AddInfrastructureServices(configuration)
                .BuildServiceProvider();

            try
            {
                
                // Ensure FFmpeg is available and configured before the UI is shown
                var ffmpegBootstrap = services.GetRequiredService<FfmpegBootstrapService>();
                ffmpegBootstrap.EnsureFfmpegAsync().GetAwaiter().GetResult();

                var mainForm = services.GetRequiredService<IMainView>() as Form;
                var presenter = services.GetRequiredService<MainPresenter>();

                try
                {
                    presenter.InitializeAsync().GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("Application initialization was cancelled.",
                        "Initialization Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to initialize: {ex.GetBaseException().Message}",
                        "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                System.Windows.Forms.Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error: {ex.Message}\n\n{ex.StackTrace}", 
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}