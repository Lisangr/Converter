namespace Converter.Domain.Models;

public class AppSettings
{
    // FFmpeg settings
    public string? FfmpegPath { get; set; }
    public bool AutoDownloadFfmpeg { get; set; } = true;
    
    // Application behavior
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public bool SaveQueueOnExit { get; set; } = true;
    public bool AutoStartConversions { get; set; } = false;
    
    // Default paths
    public string? DefaultOutputFolder { get; set; }
    public string? TempFolderPath { get; set; }
    
    // Performance settings
    public int MaxConcurrentConversions { get; set; } = Environment.ProcessorCount;
    public bool UseHardwareAcceleration { get; set; } = true;
    
    // UI settings
    public bool ShowPreviewThumbnails { get; set; } = true;
    public int ThumbnailCacheSize { get; set; } = 100;
    
    // Validation
    public bool ValidateInputFiles { get; set; } = true;
    public bool CheckDiskSpace { get; set; } = true;
    public long MinFreeSpaceMB { get; set; } = 1000; // 1GB minimum
    
    // Logging
    public bool EnableDetailedLogging { get; set; } = false;
    public string? LogFilePath { get; set; }
    
    public AppSettings Clone()
    {
        return new AppSettings
        {
            FfmpegPath = FfmpegPath,
            AutoDownloadFfmpeg = AutoDownloadFfmpeg,
            CheckForUpdatesOnStartup = CheckForUpdatesOnStartup,
            SaveQueueOnExit = SaveQueueOnExit,
            AutoStartConversions = AutoStartConversions,
            DefaultOutputFolder = DefaultOutputFolder,
            TempFolderPath = TempFolderPath,
            MaxConcurrentConversions = MaxConcurrentConversions,
            UseHardwareAcceleration = UseHardwareAcceleration,
            ShowPreviewThumbnails = ShowPreviewThumbnails,
            ThumbnailCacheSize = ThumbnailCacheSize,
            ValidateInputFiles = ValidateInputFiles,
            CheckDiskSpace = CheckDiskSpace,
            MinFreeSpaceMB = MinFreeSpaceMB,
            EnableDetailedLogging = EnableDetailedLogging,
            LogFilePath = LogFilePath
        };
    }
}