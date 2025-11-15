namespace Converter.Domain.Models;

public sealed class NotificationOptions
{
    public bool DesktopNotificationsEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool UseCustomSound { get; set; }
    public string? CustomSoundPath { get; set; }
    public bool ShowProgressNotifications { get; set; } = true;
}
