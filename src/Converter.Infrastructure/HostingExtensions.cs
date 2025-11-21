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
            builder.Logging.SetMinimumLevel(LogLevel.Information);
            
            return builder;
        }
        
        public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Application layer services
            services.AddInfrastructureServices(configuration);
            
            // Application layer services
            services.AddScoped<IMainView, Form1>();
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

            // Theme services
            services.AddSingleton<IThemeManager, ThemeManager>();
            // IThemeService is already registered in AddInfrastructureServices()

            // Queue processing - наследуется от BackgroundService, поэтому автоматически запускается как hosted service
            // Добавляем как Singleton (для DI) и BackgroundService (для автозапуска)
            services.AddSingleton<IQueueProcessor, QueueProcessor>();
            services.AddHostedService(provider => provider.GetRequiredService<IQueueProcessor>() as QueueProcessor);

            // File system services
            services.AddScoped<IFileOperationsService, FileOperationsService>();
            services.AddSingleton<IFilePicker, WinFormsFilePicker>();
            services.AddSingleton<IFolderPicker, WinFormsFolderPicker>();
            
            return services;
        }
        
        public static IServiceCollection ConfigureHostedServices(this IServiceCollection services)
        {
            // Register FFmpeg bootstrap as hosted service
            services.AddSingleton<FfmpegBootstrapService>();
            services.AddHostedService(provider => provider.GetRequiredService<FfmpegBootstrapService>());
            
            return services;
        }
    }
}