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
            // FFmpeg services
            services.AddSingleton<IFFmpegExecutor, FFmpegExecutor>();
            services.AddSingleton<FfmpegBootstrapService>();
            services.AddSingleton<IThumbnailProvider, ThumbnailProvider>();
            
            // Other infrastructure services
            services.AddSingleton<INotificationGateway, NotificationGateway>();
            services.Configure<SettingsOptions>(configuration.GetSection("Settings"));
            services.AddSingleton<ISettingsStore, JsonSettingsStore>();
            services.AddSingleton<IQueueStore, JsonQueueStore>();
            services.AddSingleton<INotificationSettingsStore, NotificationSettingsStore>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IShareService, ShareService>();
            
            return services;
        }
    }
}