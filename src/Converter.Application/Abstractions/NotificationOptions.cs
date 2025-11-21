// Устаревший файл - используйте Converter.Domain.Models.NotificationOptions
// Этот класс был переименован в ApplicationNotificationOptions для избежания конфликтов
namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Настройки уведомлений (Application layer).
    /// Устаревший класс - используйте Converter.Domain.Models.NotificationOptions
    /// </summary>
    [System.Obsolete("Используйте Converter.Domain.Models.NotificationOptions")]
    public class NotificationOptions
    {
        public bool Enabled { get; set; } = true;
        public bool ShowErrors { get; set; } = true;
        public bool ShowSuccess { get; set; } = true;
        public bool ShowProgress { get; set; } = false;
        public bool PlaySounds { get; set; } = true;
        public int ToastDurationSeconds { get; set; } = 5;
    }
}