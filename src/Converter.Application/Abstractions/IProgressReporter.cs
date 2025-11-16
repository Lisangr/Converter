using System;
using System.Threading.Tasks;
using Converter.Models;

namespace Converter.Application.Abstractions
{
    public interface IProgressReporter
    {
        void ReportItemProgress(QueueItem item, int progress, string? status = null);
        void ReportGlobalProgress(int progress, string? status = null);
        void ReportError(QueueItem item, string error);
        void ReportWarning(QueueItem item, string warning);
        void ReportInfo(QueueItem item, string message);
    }
}
