using System;
using Converter.Application.Abstractions;

namespace Converter.Application.Services;

public sealed class QueueProgressReporter : IProgressReporter
{
    public IProgress<int> Create(Guid itemId, Action<Guid, int> progressHandler)
    {
        if (progressHandler is null) throw new ArgumentNullException(nameof(progressHandler));
        return new Progress<int>(value => progressHandler(itemId, value));
    }
}
