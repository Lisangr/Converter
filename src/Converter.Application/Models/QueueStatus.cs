using Converter.Domain.Models;

// Алиасы для обратной совместимости с тестами
namespace Converter.Application.Models
{
    /// <summary>
    /// Алиас для ConversionStatus для обратной совместимости.
    /// </summary>
    public enum QueueStatus
    {
        Pending = ConversionStatus.Pending,
        Processing = ConversionStatus.Processing,
        Completed = ConversionStatus.Completed,
        Failed = ConversionStatus.Failed,
        Paused = ConversionStatus.Paused,
        Cancelled = ConversionStatus.Cancelled
    }
}