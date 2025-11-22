using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions
{
    public interface IAddFilesCommand
    {
        Task ExecuteAsync(IEnumerable<string> filePaths, string? outputFolder, CancellationToken cancellationToken = default);
        Task ExecuteAsync(IEnumerable<string> filePaths, string? outputFolder, string? namingPattern, CancellationToken cancellationToken = default);
    }

    public interface IStartConversionCommand
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }

    public interface ICancelConversionCommand
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }

    public interface IRemoveSelectedFilesCommand
    {
        Task ExecuteAsync(IEnumerable<Guid> itemIds, CancellationToken cancellationToken = default);
    }

    public interface IClearQueueCommand
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
