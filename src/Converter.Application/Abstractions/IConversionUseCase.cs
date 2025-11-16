using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Models;

namespace Converter.Application.Abstractions
{
    public interface IConversionUseCase
    {
        Task<ConversionResult> ExecuteAsync(QueueItem item, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
    }
}
