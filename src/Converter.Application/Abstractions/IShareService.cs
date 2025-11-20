namespace Converter.Application.Abstractions;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Models;

public interface IShareService : IDisposable
{
    ShareReport? GenerateReport(List<QueueItem> completedItems);
    void CopyToClipboard(string text);
    Task<string> GenerateImageReport(ShareReport report, string outputPath);
}
