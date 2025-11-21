using System;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

/// <summary>
/// Настройки уведомлений для сервиса уведомлений.
/// </summary>
public class NotificationSettings
{
    public bool EnableNotifications { get; set; } = true;
    public bool EnableSound { get; set; } = true;
    public bool ShowProgressNotifications { get; set; } = true;
    public string? CustomSoundPath { get; set; }
}