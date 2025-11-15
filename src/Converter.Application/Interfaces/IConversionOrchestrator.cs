using Converter.Domain.Models;

namespace Converter.Application.Interfaces;

public interface IConversionOrchestrator
{
    Task<ConversionResult> ExecuteAsync(ConversionRequest request, IProgress<ConversionProgress> progress, CancellationToken cancellationToken);
}
