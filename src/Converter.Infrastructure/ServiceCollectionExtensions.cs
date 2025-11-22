using System;
using System.IO;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Infrastructure.Ffmpeg;
using Converter.Infrastructure.Notifications;
using Converter.Infrastructure.Persistence;
using Converter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FileService = Converter.Application.Services.FileService;
using IFileService = Converter.Application.Services.IFileService;
using NotificationService = Converter.Infrastructure.NotificationService;
using ThumbnailProvider = Converter.Infrastructure.Ffmpeg.ThumbnailProvider;

namespace Converter.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configuration
            services.Configure<Converter.Domain.Models.AppSettings>(configuration.GetSection("Settings"));

            // Persistence services
            services.AddSingleton<ISettingsStore, JsonSettingsStore>();
            services.AddSingleton<IQueueStore, JsonQueueStore>();
            services.AddSingleton<IPresetRepository>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JsonPresetRepository>>();
                // Для текущей реализации JsonPresetRepository путь используется только как заглушка
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var presetsPath = Path.Combine(appData, "Converter", "presets.json");
                return new JsonPresetRepository(presetsPath, logger);
            });
            
            // FFmpeg services - these are infrastructure services
            services.AddSingleton<IFFmpegExecutor, FFmpegExecutor>();
            services.AddSingleton<FfmpegBootstrapService>(); // This is IHostedService
            services.AddSingleton<IThumbnailProvider, ThumbnailProvider>();
            services.AddSingleton<IThumbnailGenerator, ThumbnailGenerator>();
            services.AddSingleton<IThumbnailService, Converter.Application.Services.ThumbnailService>();

            // Notification infrastructure
            services.AddSingleton<INotificationGateway, NotificationGateway>();
            services.AddSingleton<INotificationSettingsStore, Persistence.NotificationSettingsStore>();
            services.AddSingleton<INotificationService, NotificationService>();
            
            // Theme infrastructure
            services.AddSingleton<IThemeManager, ThemeManager>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddHostedService<ThemeBootstrapHostedService>();
            
            // Conversion settings
            services.AddSingleton<IConversionSettingsService, ConversionSettingsService>();
            
            // Share service
            services.AddSingleton<IShareService, Converter.Infrastructure.ShareService>();
            
            // File services
            services.AddSingleton<IFileService, FileService>(); // application-level file service
            services.AddSingleton<Converter.Services.IFileService, Converter.Services.FileService>(); // UI-level file service for thumbnails
            
            // UI services
            services.AddSingleton<Converter.Services.UIServices.IFileOperationsService, Converter.Services.UIServices.FileOperationsService>();

            // Application shutdown
            services.AddSingleton<IApplicationShutdownService, ApplicationShutdownService>();
            
            return services;
        }
    }
}