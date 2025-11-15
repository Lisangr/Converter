using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

public interface IFFmpegExecutor
{
    Task ProbeAsync(string inputPath, CancellationToken ct);
    Task<int> ExecuteAsync(string arguments, IProgress<double> progress, CancellationToken ct);
}
