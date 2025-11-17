/// <summary>
/// Реализация <see cref="IProgressReporter"/> для отображения прогресса в UI.
/// Особенности:
/// - Безопасная работа с UI-потоком через <see cref="ControlExtensions.InvokeIfRequired"/>
/// - Поддержка отображения общего прогресса и прогресса по отдельным элементам
/// - Логирование событий прогресса
/// - Обработка и отображение ошибок
/// </summary>
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Converter.Extensions;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public class UiProgressReporter : IProgressReporter
    {
        private readonly IMainView _view;
        private readonly ILogger<UiProgressReporter> _logger;

        public UiProgressReporter(
            IMainView view,
            ILogger<UiProgressReporter> logger)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private void InvokeOnUiThread(Action action)
        {
            if (_view is Control control)
            {
                control.InvokeIfRequired(action);
            }
            else
            {
                action();
            }
        }

        public void ReportItemProgress(QueueItem item, int progress, string? status = null)
        {
            try
            {
                InvokeOnUiThread(() =>
                {
                    item.Progress = progress;
                    // Queue item visual updates are now driven by MainPresenter via ViewModel.
                    if (!string.IsNullOrEmpty(status))
                    {
                        _view.StatusText = $"{status} ({progress}%)";
                    }
                });
                
                _logger.LogDebug("Item {ItemId} progress: {Progress}% - {Status}", 
                    item.Id, progress, status ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting item progress");
            }
        }

        public void ReportGlobalProgress(int progress, string? status = null)
        {
            try
            {
                InvokeOnUiThread(() =>
                {
                    var text = !string.IsNullOrEmpty(status)
                        ? $"{status} ({progress}%)"
                        : $"Global progress: {progress}%";
                    _view.StatusText = text;
                });
                
                _logger.LogDebug("Global progress: {Progress}% - {Status}", progress, status ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting global progress");
            }
        }

        public void ReportError(QueueItem item, string error)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrEmpty(error)) throw new ArgumentException("Error message cannot be null or empty", nameof(error));

            try
            {
                InvokeOnUiThread(() =>
                {
                    item.Status = ConversionStatus.Failed;
                    item.ErrorMessage = error;
                    _view.ShowError(error);
                });
                
                _logger.LogError("Error for item {ItemId}: {Error}", item.Id, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting error for item {ItemId}", item.Id);
            }
        }

        public void ReportWarning(QueueItem item, string warning)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrEmpty(warning)) throw new ArgumentException("Warning message cannot be null or empty", nameof(warning));

            try
            {
                InvokeOnUiThread(() =>
                {
                    _view.ShowInfo($"Warning: {warning}");
                });
                
                _logger.LogWarning("Warning for item {ItemId}: {Warning}", item.Id, warning);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting warning for item {ItemId}", item.Id);
            }
        }

        public void ReportInfo(QueueItem item, string message)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Message cannot be null or empty", nameof(message));

            try
            {
                InvokeOnUiThread(() =>
                {
                    _view.ShowInfo(message);
                });
                
                _logger.LogInformation("Info for item {ItemId}: {Message}", item.Id, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting info for item {ItemId}", item.Id);
            }
        }
    }
}
