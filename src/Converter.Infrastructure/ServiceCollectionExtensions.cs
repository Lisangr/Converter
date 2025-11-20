using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Infrastructure.Ffmpeg;
using Converter.Infrastructure.Notifications;
using Converter.Infrastructure.Persistence;
using Converter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Converter.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.Configure<SettingsOptions>(configuration.GetSection("Settings"));
            
            // Persistence services
            services.AddSingleton<ISettingsStore, JsonSettingsStore>();
            services.AddSingleton<IQueueStore, JsonQueueStore>();
            
            // FFmpeg services - these are infrastructure services
            services.AddSingleton<IFFmpegExecutor, FFmpegExecutor>();
            services.AddSingleton<FfmpegBootstrapService>(); // This is IHostedService
            services.AddSingleton<IThumbnailProvider, ThumbnailProvider>();
            
            // Notification infrastructure
            services.AddSingleton<INotificationGateway, NotificationGateway>();
            services.AddSingleton<INotificationSettingsStore, NotificationSettingsStore>();
            services.AddSingleton<INotificationService, NotificationService>();
            
            // Theme infrastructure
            services.AddSingleton<IThemeManager, ThemeManager>();
            services.AddSingleton<IThemeService, ThemeService>();
            
            // Share service
            services.AddSingleton<IShareService, ShareService>();
            
            // File service
            services.AddSingleton<IFileService, FileService>();
            
            // UI services
            services.AddSingleton<Converter.Services.UIServices.IFileOperationsService, Converter.Services.UIServices.FileOperationsService>();
            
            return services;
        }
    }
}