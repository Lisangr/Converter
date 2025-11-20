using Converter.Application.Abstractions;
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
            
            // Share service
            services.AddSingleton<IShareService, ShareService>();
            
            return services;
        }
    }
}