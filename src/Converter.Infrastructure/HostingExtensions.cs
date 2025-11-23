using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Converter.Application.Abstractions;
using Converter.Application.Presenters;
using Converter.Application.Services;
using Converter.Application.ViewModels;
using Converter.Application.Builders;
using Converter.Infrastructure.Ffmpeg;
using Converter.Infrastructure.Notifications;
using Converter.Infrastructure.Persistence;
using Converter.Services;
using Converter.UI;
using Converter.UI.Controls;
using Converter.Services.UIServices;

namespace Converter.Infrastructure
{
    public static class HostingExtensions
    {
        public static HostApplicationBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            
            // Add configuration
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            
            // Add logging
            builder.Logging.ClearProviders();
            builder.Logging.AddDebug();
            builder.Logging.AddFilter("Converter.Infrastructure.Ffmpeg", LogLevel.Trace);
            
            return builder;
        }
        
        public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Application layer services
            services.AddInfrastructureServices(configuration);
            
            // Application layer services
            services.AddSingleton<IMainView, Form1>();
            services.AddSingleton<MainPresenter>();
            services.AddSingleton<MainViewModel>();

            // Core application services
            services.AddSingleton<IQueueRepository, QueueRepository>();
            services.AddSingleton<IConversionOrchestrator, ConversionOrchestrator>();
            services.AddSingleton<IConversionUseCase, ConversionUseCase>();
            services.AddSingleton<IConversionCommandBuilder, ConversionCommandBuilder>();
            services.AddSingleton<IProfileProvider, ProfileProvider>();
            services.AddSingleton<IOutputPathBuilder, OutputPathBuilder>();
            services.AddSingleton<IProgressReporter, UiProgressReporter>();

            // Preset and Estimation services
            services.AddSingleton<IPresetService, Converter.Infrastructure.PresetService>();
            services.AddSingleton<Converter.Services.EstimationService>();
            services.AddSingleton<IConversionEstimationService, Converter.Infrastructure.ConversionEstimationService>();

            // Queue commands
            services.AddSingleton<IAddFilesCommand, AddFilesCommand>();
            services.AddSingleton<IStartConversionCommand, StartConversionCommand>();
            services.AddSingleton<ICancelConversionCommand, CancelConversionCommand>();
            services.AddSingleton<IRemoveSelectedFilesCommand, RemoveSelectedFilesCommand>();
            services.AddSingleton<IClearQueueCommand, ClearQueueCommand>();

            // Queue processing - используем ChannelQueueProcessor и отдельный hosted service
            // ChannelQueueProcessor инкапсулирует обработку через Channel<QueueItem>,
            // а QueueWorkerHostedService управляет его жизненным циклом в качестве фонового сервиса
            services.AddSingleton<IQueueProcessor, ChannelQueueProcessor>();
            services.AddHostedService<QueueWorkerHostedService>();

            // UI dispatcher for marshaling calls to the UI thread
            services.AddSingleton<IUiDispatcher, WinFormsUiDispatcher>();

            // File system services
            services.AddScoped<IFileOperationsService, FileOperationsService>();
            services.AddSingleton<IFilePicker, WinFormsFilePicker>();
            services.AddSingleton<IFolderPicker, WinFormsFolderPicker>();
            
            return services;
        }
        
        public static IServiceCollection ConfigureHostedServices(this IServiceCollection services)
        {
            // Register FFmpeg bootstrap as hosted service
            // FfmpegBootstrapService уже зарегистрирован как Singleton в AddInfrastructureServices,
            // поэтому просто регистрируем его как IHostedService
            services.AddHostedService(provider => provider.GetRequiredService<FfmpegBootstrapService>());
            
            // QueueWorkerHostedService уже зарегистрирован в ConfigureApplicationServices()
            // ThemeBootstrapHostedService уже зарегистрирован в AddInfrastructureServices()
            
            return services;
        }
    }
}