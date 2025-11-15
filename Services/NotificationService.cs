using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Converter.Services
{
    public class NotificationService : IDisposable
    {
        private readonly NotificationSettings _settings;
        private readonly HashSet<int> _notifiedMilestones = new();
        private SoundPlayer? _soundPlayer;

        public NotificationService(NotificationSettings settings)
        {
            _settings = settings ?? new NotificationSettings();
        }

        public void ShowDesktopNotification(string title, string message, string? imagePath = null, string? folderPath = null)
        {
            if (!_settings.DesktopNotificationsEnabled)
            {
                return;
            }

            try
            {
                var toastContent = new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message);

                if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
                {
                    toastContent.AddInlineImage(new Uri(imagePath));
                }

                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    toastContent.AddArgument("folder", folderPath);
                    toastContent.AddButton(new ToastButton()
                        .SetContent("📁 Открыть папку")
                        .AddArgument("action", "openFolder")
                        .SetBackgroundActivation());
                }

                toastContent
                    .AddButton(new ToastButton()
                        .SetContent("Закрыть")
                        .AddArgument("action", "dismiss")
                        .SetBackgroundActivation())
                    .Show();
            }
            catch
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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

        public void NotifyConversionComplete(ConversionResult result)
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

        public void ShowAdvancedNotification(ConversionResult result)
        {
            if (!_settings.DesktopNotificationsEnabled)
            {
                return;
            }

            try
            {
                var builder = new ToastContentBuilder()
                    .AddText("✅ Конвертация завершена")
                    .AddText($"Обработано: {result.ProcessedFiles} файлов")
                    .AddProgressBar(value: 1.0, title: "Завершено", status: $"Сэкономлено {FormatFileSize(result.SpaceSaved)}");

                if (!string.IsNullOrWhiteSpace(result.ThumbnailPath) && File.Exists(result.ThumbnailPath))
                {
                    builder.AddInlineImage(new Uri(result.ThumbnailPath));
                }

                if (!string.IsNullOrWhiteSpace(result.OutputFolder))
                {
                    builder.AddArgument("folder", result.OutputFolder);
                    builder.AddButton(new ToastButton()
                        .SetContent("📁 Открыть папку")
                        .AddArgument("action", "openFolder")
                        .SetBackgroundActivation());
                }

                builder
                    .AddButton(new ToastButton()
                        .SetContent("🔄 Конвертировать ещё")
                        .AddArgument("action", "newConversion")
                        .SetBackgroundActivation())
                    .AddAudio(new ToastAudio().SetSrc(new Uri("ms-winsoundevent:Notification.Default")))
                    .Show();
            }
            catch
            {
                ShowDesktopNotification("✅ Конвертация завершена", $"Обработано: {result.ProcessedFiles} файлов", result.ThumbnailPath, result.OutputFolder);
            }
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
            _soundPlayer?.Stop();
            _soundPlayer?.Dispose();
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

    public class ConversionResult
    {
        public bool Success { get; set; }
        public int ProcessedFiles { get; set; }
        public long SpaceSaved { get; set; }
        public TimeSpan Duration { get; set; }
        public string? OutputFolder { get; set; }
        public string? ThumbnailPath { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
