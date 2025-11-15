using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

public interface IConversionOrchestrator
{
    Task<ConversionResult> ExecuteAsync(
        ConversionRequest request,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken);
}
