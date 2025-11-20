using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using Converter.Application.Abstractions;

namespace Converter.Services
{
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly INotificationSettingsStore _settingsStore;
        private NotificationSettings _settings;
        private readonly HashSet<int> _notifiedMilestones = new();
        private SoundPlayer? _soundPlayer;
        private bool _disposed = false;
        public NotificationService(INotificationSettingsStore settingsStore)
        {
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _settings = _settingsStore.Load();
        }

        public NotificationSettings GetSettings() => _settings;

        public void UpdateSettings(NotificationSettings settings)
        {
            _settings = settings ?? new NotificationSettings();
            _settingsStore.Save(_settings);
        }

        public void ShowDesktopNotification(string title, string message, string? imagePath = null, string? folderPath = null)
        {
            if (!_settings.DesktopNotificationsEnabled)
            {
                return;
            }

            // UI-agnostic: log instead of MessageBox to avoid blocking UI thread from service layer
            Debug.WriteLine($"Notification: {title} - {message}");
        }

        public void PlayCompletionSound()
        {
            if (!_settings.SoundEnabled)
            {
                return;
            }

            try
            {
                _soundPlayer?.Stop();
                _soundPlayer?.Dispose();

                if (_settings.UseCustomSound && !string.IsNullOrWhiteSpace(_settings.CustomSoundPath) && File.Exists(_settings.CustomSoundPath))
                {
                    _soundPlayer = new SoundPlayer(_settings.CustomSoundPath);
                    _soundPlayer.Play();
                }
                else
                {
                    SystemSounds.Asterisk.Play();
                }
            }
            catch
            {
                SystemSounds.Beep.Play();
            }
        }

        public void NotifyConversionComplete(NotificationSummary result)
        {
            if (result == null)
            {
                return;
            }

            var message = result.Success
                ? $"Успешно конвертировано: {result.ProcessedFiles} файлов\nСэкономлено: {FormatFileSize(result.SpaceSaved)}\nВремя: {result.Duration:hh\\:mm\\:ss}"
                : result.ErrorMessage ?? "Ошибка конвертации";

            if (result.Success)
            {
                ShowAdvancedNotification(result);
                PlayCompletionSound();
            }
            else
            {
                ShowDesktopNotification("❌ Ошибка конвертации", message, result.ThumbnailPath, result.OutputFolder);
                SystemSounds.Hand.Play();
            }
        }

        public void NotifyProgress(int current, int total)
        {
            if (!_settings.ShowProgressNotifications || !_settings.DesktopNotificationsEnabled || total <= 0)
            {
                return;
            }

            var progress = (int)Math.Round((double)current / total * 100);
            foreach (var milestone in new[] { 25, 50, 75 })
            {
                if (progress >= milestone && _notifiedMilestones.Add(milestone))
                {
                    ShowDesktopNotification(
                        "🔄 Конвертация в процессе",
                        $"Обработано {current} из {total} файлов ({progress}% )"
                    );
                    break;
                }
            }
        }

        public void ResetProgressNotifications() => _notifiedMilestones.Clear();

        public void ShowAdvancedNotification(NotificationSummary result)
        {
            if (!_settings.DesktopNotificationsEnabled)
            {
                return;
            }

            ShowDesktopNotification("✅ Конвертация завершена", $"Обработано: {result.ProcessedFiles} файлов\nСэкономлено: {FormatFileSize(result.SpaceSaved)}", result.ThumbnailPath, result.OutputFolder);
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 B";
            }

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _soundPlayer?.Stop();
                _soundPlayer?.Dispose();
                _disposed = true;
            }
        }

        ~NotificationService()
        {
            Dispose(disposing: false);
        }
    }

    public class NotificationSettings
    {
        public bool DesktopNotificationsEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;
        public bool UseCustomSound { get; set; }
        public string? CustomSoundPath { get; set; }
        public bool ShowProgressNotifications { get; set; }
    }

    public class NotificationSummary
    {
        public bool Success { get; set; }
        public int ProcessedFiles { get; set; }
        public long SpaceSaved { get; set; }
        public TimeSpan Duration { get; set; }
        public string? OutputFolder { get; set; }
        public string? ThumbnailPath { get; set; }
        public string? ErrorMessage { get; set; }
        public long? OutputSize { get; set; }

        public NotificationSummary() { }

        public NotificationSummary(bool success, long? outputSize, string? errorMessage)
        {
            Success = success;
            OutputSize = outputSize;
            ErrorMessage = errorMessage;
        }
    }
}
