namespace Converter.Application.Abstractions;

using System.Collections.Generic;
using System.Threading.Tasks;
using Converter.Models;

public interface IShareService
{
    ShareReport? GenerateReport(List<QueueItem> completedItems);
    void CopyToClipboard(string text);
    Task<string> GenerateImageReport(ShareReport report, string outputPath);
}
