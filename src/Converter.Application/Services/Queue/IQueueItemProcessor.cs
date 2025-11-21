using Converter.Application.Abstractions;
using QueueItem = Converter.Domain.Models.QueueItem;

namespace Converter.Application.Services.Queue;

// Re-export of the core IQueueItemProcessor abstraction for services namespace.
public interface IQueueItemProcessor : Converter.Application.Abstractions.IQueueItemProcessor
{
}
