namespace Converter.Domain.Models;

public sealed record QueueItem(Guid Id, ConversionRequest Request)
{
    public static QueueItem Create(ConversionRequest request) => new(Guid.NewGuid(), request);
}

public enum QueueItemStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
