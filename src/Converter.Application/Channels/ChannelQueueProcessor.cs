using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.Application.Channels;

/// <summary>
/// Процессор очереди на основе каналов.
/// </summary>
public class ChannelQueueProcessor<T> where T : QueueItem
{
    private readonly Channel<T> _channel;
    private readonly IQueueItemProcessor _processor;

    public ChannelQueueProcessor(IQueueItemProcessor processor, Channel<T> channel)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            await _processor.ProcessAsync(item, cancellationToken);
        }
    }
}