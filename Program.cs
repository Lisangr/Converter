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

            IHost? host = null;
            
            try
            {
                // Создаем базовый хост без сложных сервисов
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

                host = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        services
                            .AddLogging(configure =>
                                configure.AddDebug()
                                        .SetMinimumLevel(LogLevel.Information))
                            .AddSingleton<IMainView, Form1>()
                            .AddInfrastructureServices(configuration);
                    })
                    .Build();

                host.StartAsync().GetAwaiter().GetResult();

                // Создаем форму вручную, минуя DI
                using var formScope = host.Services.CreateScope();
                var formServices = formScope.ServiceProvider;
                
                // Создаем форму через её конструктор
                var mainForm = new Form1(
                    formServices.GetRequiredService<IThemeService>(),
                    formServices.GetRequiredService<INotificationService>(),
                    formServices.GetRequiredService<IThumbnailProvider>(),
                    formServices.GetRequiredService<IShareService>(),
                    formServices.GetRequiredService<IFileService>());

                if (mainForm == null)
                {
                    MessageBox.Show("Failed to create main form", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Инициализируем остальные сервисы
                var serviceCollection = new ServiceCollection();
                serviceCollection
                    .AddLogging(configure =>
                        configure.AddDebug()
                                .SetMinimumLevel(LogLevel.Information))
                    .AddSingleton<IMainView>(mainForm)
                    .AddSingleton<MainPresenter>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<IQueueRepository, QueueRepository>()
                    .AddSingleton<IConversionCommandBuilder, ConversionCommandBuilder>()
                    .AddSingleton<IConversionOrchestrator, ConversionOrchestrator>()
                    .AddSingleton<IConversionUseCase, ConversionUseCase>()
                    .AddSingleton<IProfileProvider, ProfileProvider>()
                    .AddSingleton<IOutputPathBuilder, OutputPathBuilder>()
                    .AddSingleton<IProgressReporter, UiProgressReporter>()
                    .AddSingleton<IThemeManager, ThemeManager>()
                    .AddSingleton<IThemeService, ThemeService>()
                    .AddSingleton<IQueueProcessor, ChannelQueueProcessor>()
                    .AddSingleton<IFilePicker, WinFormsFilePicker>()
                    .AddSingleton<IFolderPicker, WinFormsFolderPicker>()
                    .AddSingleton<IFileService, FileService>()
                    .AddInfrastructureServices(configuration);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                // Получаем презентер и инициализируем его
                var presenter = serviceProvider.GetRequiredService<MainPresenter>();
                var queueProcessor = serviceProvider.GetRequiredService<IQueueProcessor>();
                
                // Initialize FFmpeg in background
                var ffmpegBootstrap = serviceProvider.GetRequiredService<FfmpegBootstrapService>();
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await ffmpegBootstrap.EnsureFfmpegAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"FFmpeg initialization error: {ex.Message}");
                    }
                });

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

                // Ensure graceful shutdown of application services
                try
                {
                    presenter?.Dispose();

                    if (queueProcessor != null)
                    {
                        // Stop processing queue before disposing DI container
                        queueProcessor.StopProcessingAsync().GetAwaiter().GetResult();

                        // Dispose async/sync resources if implemented
                        if (queueProcessor is IAsyncDisposable asyncDisposable)
                        {
                            asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
                        }
                        else if (queueProcessor is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log shutdown errors but don't mask original exceptions
                    System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error: {ex.Message}\n\n{ex.StackTrace}", 
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                try
                {
                    if (host != null)
                    {
                        host.StopAsync().GetAwaiter().GetResult();
                        host.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Host shutdown error: {ex.Message}");
                }
            }
        }

    }
}