using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using Converter.Extensions;
using Converter.Application.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Presenters
{
    public sealed class MainPresenter : IDisposable
    {
        private readonly IMainView _view;
        private readonly MainViewModel _viewModel;
        private readonly IQueueRepository _queueRepository;
        private readonly IQueueProcessor _queueProcessor;
        private readonly IProfileProvider _profileProvider;
        private readonly IOutputPathBuilder _pathBuilder;
        private readonly IProgressReporter _progressReporter;
        private readonly IFilePicker _filePicker;
        private readonly IConversionSettingsService _conversionSettingsService;
        private readonly IThumbnailService _thumbnailService;
        private readonly ILogger<MainPresenter> _logger;
        private bool _disposed;
        private bool _clearingInProgress;
        private readonly IAddFilesCommand _addFilesCommand;
        private readonly IStartConversionCommand _startConversionCommand;
        private readonly ICancelConversionCommand _cancelConversionCommand;
        private readonly IRemoveSelectedFilesCommand _removeSelectedFilesCommand;
        private readonly IClearQueueCommand _clearQueueCommand;
        private CancellationTokenSource _cancellationTokenSource;

        public MainPresenter(
            IMainView view,
            MainViewModel viewModel,
            IQueueRepository queueRepository,
            IQueueProcessor queueProcessor,
            IProfileProvider profileProvider,
            IOutputPathBuilder pathBuilder,
            IProgressReporter progressReporter,
            IFilePicker filePicker,
            IConversionSettingsService conversionSettingsService,
            IThumbnailService thumbnailService,
            IAddFilesCommand addFilesCommand,
            IStartConversionCommand startConversionCommand,
            ICancelConversionCommand cancelConversionCommand,
            IRemoveSelectedFilesCommand removeSelectedFilesCommand,
            IClearQueueCommand clearQueueCommand,
            ILogger<MainPresenter> logger)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _queueProcessor = queueProcessor ?? throw new ArgumentNullException(nameof(queueProcessor));
            _profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
            _pathBuilder = pathBuilder ?? throw new ArgumentNullException(nameof(pathBuilder));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
            _conversionSettingsService = conversionSettingsService ?? throw new ArgumentNullException(nameof(conversionSettingsService));
            _thumbnailService = thumbnailService ?? throw new ArgumentNullException(nameof(thumbnailService));
            _addFilesCommand = addFilesCommand ?? throw new ArgumentNullException(nameof(addFilesCommand));
            _startConversionCommand = startConversionCommand ?? throw new ArgumentNullException(nameof(startConversionCommand));
            _cancelConversionCommand = cancelConversionCommand ?? throw new ArgumentNullException(nameof(cancelConversionCommand));
            _removeSelectedFilesCommand = removeSelectedFilesCommand ?? throw new ArgumentNullException(nameof(removeSelectedFilesCommand));
            _clearQueueCommand = clearQueueCommand ?? throw new ArgumentNullException(nameof(clearQueueCommand));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cancellationTokenSource = new CancellationTokenSource();

            // Subscribe to queue events
            _queueRepository.ItemAdded += OnItemAdded;
            _queueRepository.ItemUpdated += OnItemUpdated;
            _queueRepository.ItemRemoved += OnItemRemoved;
            
            // Subscribe to queue processor events for progress updates
            _queueProcessor.ItemStarted += OnItemStarted;
            _queueProcessor.ItemCompleted += OnItemCompleted;
            _queueProcessor.ItemFailed += OnItemFailed;
            _queueProcessor.ProgressChanged += OnProgressChanged;
            _queueProcessor.QueueCompleted += OnQueueCompleted;

            // Subscribe to sync view events
            _view.StartConversionRequested += OnStartConversionRequested;

            // Subscribe to async view events (нормализованный подход)
            _view.PresetSelected += OnPresetSelected;
            _view.SettingsChanged += OnSettingsChanged;
            _view.AddFilesRequestedAsync += OnAddFilesRequestedAsync;
            _view.StartConversionRequestedAsync += OnStartConversionRequestedAsync;
            _view.CancelConversionRequestedAsync += OnCancelConversionRequestedAsync;
            _view.FilesDroppedAsync += OnFilesDroppedAsync;
            _view.RemoveSelectedFilesRequestedAsync += OnRemoveSelectedFilesRequestedAsync;
            _view.ClearAllFilesRequestedAsync += OnClearAllFilesRequestedAsync;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing MainPresenter");

            try
            {
                _view.IsBusy = true;
                _view.StatusText = "Initializing application...";
                
                // Load settings and presets in parallel
                await Task.WhenAll(
                    LoadSettingsAsync(),
                    LoadPresetsAsync()
                );
                
                // Initialize UI bindings - используем тот же список, что и в ViewModel
                _view.QueueItemsBinding = _viewModel.QueueItems;
                
                // Load initial queue (это перезаполнит _viewModel.QueueItems)
                await LoadQueueAsync();
                
                // Убеждаемся, что связь всё ещё установлена после LoadQueueAsync
                _view.QueueItemsBinding = _viewModel.QueueItems;
                
                _view.StatusText = "Ready";
                _logger.LogInformation("MainPresenter initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing MainPresenter");
                _view.ShowError($"Failed to start application: {ex.Message}");
                throw; // Re-throw to allow the application to handle the error
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private async Task LoadSettingsAsync()
        {
            // Загружаем настройки конвертации через application-сервис
            await _conversionSettingsService.LoadAsync().ConfigureAwait(false);
            var settings = _conversionSettingsService.Current;

            // Синхронизируем ViewModel и View
            _viewModel.FfmpegPath = settings.FfmpegPath ?? string.Empty;
            _viewModel.OutputFolder = settings.OutputFolder ?? string.Empty;

            _view.FfmpegPath = settings.FfmpegPath ?? string.Empty;
            _view.OutputFolder = settings.OutputFolder ?? string.Empty;
            _view.NamingPattern = settings.NamingPattern;
        }

        private async Task LoadPresetsAsync()
        {
            // Load profiles from provider and push to view
            var profiles = await _profileProvider.GetAllProfilesAsync();
            _view.AvailablePresets = new System.Collections.ObjectModel.ObservableCollection<Converter.Application.Models.ConversionProfile>(profiles);

            _viewModel.Presets.Clear();
            foreach (var profile in profiles)
            {
                _viewModel.Presets.Add(profile);
            }

            var defaultProfile = await _profileProvider.GetDefaultProfileAsync();
            _view.SelectedPreset = defaultProfile;
            _viewModel.SelectedPreset = defaultProfile;
        }

        private void OnPresetSelected(object? sender, Converter.Application.Models.ConversionProfile profile)
        {
            if (profile == null) return;
            _logger.LogInformation("Preset selected: {Name}", profile.Name);
            _viewModel.SelectedPreset = profile;
            _view.ShowInfo($"Preset selected: {profile.Name}");
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            _ = SaveSettingsAsync();
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                _logger.LogInformation("Settings changed");

                var current = _conversionSettingsService.Current;
                current.FfmpegPath = _view.FfmpegPath;
                current.OutputFolder = _view.OutputFolder;
                current.NamingPattern = _view.NamingPattern;

                await _conversionSettingsService.SaveAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving conversion settings");
                _view.ShowError($"Failed to save settings: {ex.Message}");
            }
        }

        private async Task LoadQueueAsync()
        {
            try
            {
                _logger.LogInformation("Loading queue");
                var items = await _queueRepository.GetAllAsync();
                var list = items.ToList();

                // Очищаем текущую очередь в ViewModel
                _viewModel.QueueItems.Clear();
                
                // Добавляем элементы в ViewModel
                foreach (var item in list)
                {
                    var vm = QueueItemViewModel.FromModel(item);
                    _viewModel.QueueItems.Add(vm);
                    _ = LoadThumbnailForItemAsync(item, vm, _cancellationTokenSource.Token);
                }

                _logger.LogInformation("Loaded {Count} items into the queue", items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading queue");
                _view.ShowError($"Failed to load queue: {ex.Message}");
            }
        }

        private async Task OnCancelConversionRequestedAsync(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Canceling all conversions");
                _view.IsBusy = true;

                // Отменяем через команду (останавливает процессор и помечает элементы)
                await _cancelConversionCommand.ExecuteAsync().ConfigureAwait(false);

                // Reset for next conversion (создаем новый токен только после полной остановки)
                await Task.Delay(1000).ConfigureAwait(false); // Даем время для завершения текущих операций
                ResetProcessingCancellationToken();

                // 5. Reset UI state
                InvokeOnUiThread(() =>
                {
                    _view.UpdateCurrentProgress(0);
                    _view.UpdateTotalProgress(0);
                    _view.StatusText = "Все конвертации отменены";
                });

                _view.ShowInfo("Все конвертации отменены");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling conversions");
                _view.ShowError($"Ошибка при отмене конвертации: {ex.Message}");
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private void InvokeOnUiThread(Action action)
        {
            _view.RunOnUiThread(action);
        }

        private void OnItemAdded(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                // ViewModel
                var vm = QueueItemViewModel.FromModel(item);
                _viewModel.QueueItems.Add(vm);
                _ = LoadThumbnailForItemAsync(item, vm, _cancellationTokenSource.Token);
                _view.StatusText = $"Added {item.FileName} to queue";
            });
        }

        private async Task LoadThumbnailForItemAsync(QueueItem item, QueueItemViewModel vm, CancellationToken ct)
        {
            try
            {
                var bytes = await _thumbnailService.GetThumbnailAsync(item.FilePath, 160, 90, ct).ConfigureAwait(false);
                vm.ThumbnailBytes = bytes;
            }
            catch (OperationCanceledException)
            {
                // ignore cancellation
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate thumbnail for {FilePath}", item.FilePath);
            }
        }

        private void OnItemUpdated(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                vm?.UpdateFromModel(item);
                _view.StatusText = $"Updated {item.FileName} - {item.Status}";
            });
        }

        private void OnItemRemoved(object? sender, Guid itemId)
        {
            InvokeOnUiThread(() =>
            {
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == itemId);
                if (vm != null)
                {
                    _viewModel.QueueItems.Remove(vm);
                }
                _view.StatusText = "Item removed from queue";
            });
        }

        private void OnItemStarted(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                item.Status = ConversionStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = item.Status;
                    vm.Progress = 0; // Reset progress when starting
                }
                // Сбрасываем нижний прогрессбар для текущего файла, чтобы он снова шел 0→100
                _view.UpdateCurrentProgress(0);
                _view.StatusText = $"Processing {item.FileName}...";
            });
        }

        private void OnItemCompleted(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                item.Status = ConversionStatus.Completed;
                item.CompletedAt = DateTime.UtcNow;
                item.Progress = 100;
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = item.Status;
                    vm.Progress = 100;
                    vm.OutputFileSizeBytes = item.OutputFileSizeBytes;
                }
                _view.StatusText = $"Completed: {item.FileName}";
            });
        }

        private void OnItemFailed(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                item.Status = ConversionStatus.Failed;
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = item.Status;
                    vm.ErrorMessage = item.ErrorMessage;
                }
                _view.ShowError($"Failed to process {item.FileName}: {item.ErrorMessage}");
            });
        }

        private void OnProgressChanged(object? sender, QueueProgressEventArgs e)
        {
            InvokeOnUiThread(() =>
            {
                _logger.LogDebug("Progress changed for item {ItemId}: {Progress}%", e.Item.Id, e.Progress);
                
                // Обновляем ViewModel
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == e.Item.Id);
                if (vm != null)
                {
                    _logger.LogDebug("Updating ViewModel {ItemId}: Progress={Progress}, Status={Status}", 
                        vm.Id, e.Progress, e.Item.Status);
                    vm.Progress = e.Progress;
                    vm.Status = e.Item.Status;
                    vm.ErrorMessage = e.Item.ErrorMessage;
                }
                else
                {
                    _logger.LogWarning("ViewModel not found for item {ItemId}", e.Item.Id);
                }

                // Обновляем прогресс текущего элемента
                _view.UpdateCurrentProgress(e.Progress);

                // Считаем суммарный прогресс по очереди, но только если он изменился достаточно
                // Это предотвратит слишком частые и дорогие перерасчеты
                if (_viewModel.QueueItems.Any())
                {
                    var newTotalProgress = (int)_viewModel.QueueItems.Average(x => x.Progress);
                    if (Math.Abs(newTotalProgress - _view.TotalProgress) >= 1) // Обновляем только при изменении на 1% и более
                    {
                        _view.UpdateTotalProgress(newTotalProgress);
                        _logger.LogDebug("Total progress updated: {Total}%", newTotalProgress);
                    }
                }

                if (!string.IsNullOrEmpty(e.Status))
                {
                    _view.StatusText = $"{e.Status} ({e.Progress}%)";
                }
                else
                {
                    _view.StatusText = $"Processing {e.Item.FileName} - {e.Progress}%";
                }
            });
        }

        private void OnQueueCompleted(object? sender, EventArgs e)
        {
            InvokeOnUiThread(() =>
            {
                _view.StatusText = "Queue processing completed";
                _view.ShowInfo("All items have been processed");
				_view.UpdateCurrentProgress(0);
				_view.UpdateTotalProgress(100);
                _view.IsBusy = false; // Разблокируем UI и отключаем кнопку "Остановить" после завершения
            });
        }

        private async Task OnAddFilesRequestedAsync()
        {
            try
            {
                var files = _filePicker.PickFiles("Выбор файлов для конвертации", "All Files|*.*");
                
                if (files == null || files.Length == 0)
                {
                    _view.ShowInfo("Файлы не выбраны");
                    return;
                }

                _view.IsBusy = true;
                _view.StatusText = "Добавление файлов в очередь...";
                
                await _addFilesCommand
                    .ExecuteAsync(files, _view.OutputFolder, _view.NamingPattern)
                    .ConfigureAwait(false);

                await LoadQueueAsync().ConfigureAwait(false);

                _view.StatusText = $"Добавлено файлов: {files.Length}";
                _view.ShowInfo($"Добавлено файлов в очередь: {files.Length}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnAddFilesRequestedAsync");
                _view.ShowError($"Ошибка при добавлении файлов: {ex.Message}");
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private async Task OnFilesDroppedAsync(object? sender, string[] files)
        {
            try
            {
                _logger.LogInformation("Files dropped: {Count}", files?.Length ?? 0);

                await _addFilesCommand
                    .ExecuteAsync(files ?? Array.Empty<string>(), _view.OutputFolder, _view.NamingPattern)
                    .ConfigureAwait(false);

                _view.StatusText = $"Добавлено файлов: {(files?.Length ?? 0)}";
                await LoadQueueAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnFilesDroppedAsync");
                _view.ShowError($"Ошибка при добавлении файлов: {ex.Message}");
            }
        }

        private async Task OnRemoveSelectedFilesRequestedAsync(object? sender, EventArgs e)
        {
            var selectedItems = _viewModel.QueueItems
                .Where(item => item.IsSelected)
                .ToList();

            if (selectedItems.Count == 0)
            {
                _view.ShowInfo("Нет выбранных файлов для удаления");
                return;
            }

            try
            {
                _view.IsBusy = true;
                _view.StatusText = $"Удаление {selectedItems.Count} файла(ов)...";
                _logger.LogInformation("Removing {Count} selected files from queue", selectedItems.Count);

                // Используем команду удаления выбранных
                var itemIds = selectedItems.Select(item => item.Id).ToList();
                await _removeSelectedFilesCommand
                    .ExecuteAsync(itemIds)
                    .ConfigureAwait(false);

                _view.StatusText = $"Удалено файлов: {selectedItems.Count}";
                _view.ShowInfo($"Удалено файлов: {selectedItems.Count} из очереди");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnRemoveSelectedFilesRequested");
                _view.ShowError($"Ошибка при удалении файлов: {ex.Message}");
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private async Task OnClearAllFilesRequestedAsync(object? sender, EventArgs e)
        {
            // Защита от рекурсивных вызовов
            if (_clearingInProgress)
            {
                _logger.LogWarning("ClearAllFiles already in progress, skipping duplicate call");
                return;
            }

            _clearingInProgress = true;
            
            try
            {
                if (_viewModel.QueueItems.Count == 0)
                {
                    _view.ShowInfo("Очередь уже пуста");
                    return;
                }

                try
                {
                    _view.IsBusy = true;
                    _view.StatusText = "Очистка очереди...";
                    _logger.LogInformation("Clearing all files from queue");

                    // Используем команду полной очистки
                    await _clearQueueCommand.ExecuteAsync().ConfigureAwait(false);

                    // Перезагружаем очередь для синхронизации
                    await LoadQueueAsync().ConfigureAwait(false);

                    _view.StatusText = "Очередь очищена";
                    _view.ShowInfo("Все файлы удалены из очереди");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnClearAllFilesRequested");
                    _view.ShowError($"Ошибка при очистке очереди: {ex.Message}");
                }
                finally
                {
                    _view.IsBusy = false;
                }
            }
            finally
            {
                _clearingInProgress = false;
            }
        }

        // Async event handlers (нормализованный подход)
        private void OnStartConversionRequested(object? sender, EventArgs e)
        {
            // Delegate to async version to ensure proper async handling without extra Task.Run
            _ = OnStartConversionRequestedAsync();
        }

        private async Task OnStartConversionRequestedAsync()
        {
            try
            {
                _logger.LogInformation("Start conversion requested");
                
                if (_viewModel.QueueItems.Count == 0)
                {
                    _view.ShowInfo("Нет файлов для конвертации");
                    _view.IsBusy = false;
                    return;
                }

                // Помечаем UI как занятый на всё время обработки очереди.
                // Пока IsBusy = true, кнопка "Старт" отключена, а "Остановить" активна.
                _view.IsBusy = true;
                _view.StatusText = "Запуск конвертации...";

                // QueueProcessor уже запущен как HostedService, активируем обработку через команду
                await _startConversionCommand
                    .ExecuteAsync(_cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                _view.StatusText = "Конвертация запущена";
                _view.ShowInfo("Процесс конвертации начат");
                // Далее IsBusy будет сброшен в OnQueueCompleted или при отмене конвертации
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversion");
                _view.ShowError($"Ошибка при запуске конвертации: {ex.Message}");
            }
        }

        private async Task OnCancelConversionRequestedAsync()
{
    try
    {
        _logger.LogInformation("User requested to cancel all conversions");
        _view.StatusText = "Остановка конвертации...";

        // Cancel the current operation
        await _cancelConversionCommand.ExecuteAsync().ConfigureAwait(false);

        // Reset the cancellation token source for future operations
        ResetProcessingCancellationToken();

        _view.StatusText = "Конвертация отменена";
        _view.ShowInfo("Конвертация была отменена пользователем");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error while canceling conversion");
        _view.ShowError($"Ошибка при отмене конвертации: {ex.Message}");
    }
    finally
    {
        _view.IsBusy = false;
    }
}

        private async Task OnFilesDroppedAsync(string[] files)
        {
            await OnFilesDroppedAsync(this, files);
        }

        private async Task OnRemoveSelectedFilesRequestedAsync()
        {
            await OnRemoveSelectedFilesRequestedAsync(this, EventArgs.Empty);
        }

        private async Task OnClearAllFilesRequestedAsync()
        {
            await OnClearAllFilesRequestedAsync(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Unsubscribe from events
                if (_queueRepository != null)
                {
                    _queueRepository.ItemAdded -= OnItemAdded;
                    _queueRepository.ItemUpdated -= OnItemUpdated;
                    _queueRepository.ItemRemoved -= OnItemRemoved;
                }

                // НЕ уничтожаем _queueProcessor - это Singleton сервис, управляемый Host
                if (_queueProcessor != null)
                {
                    _queueProcessor.ItemStarted -= OnItemStarted;
                    _queueProcessor.ItemCompleted -= OnItemCompleted;
                    _queueProcessor.ItemFailed -= OnItemFailed;
                    _queueProcessor.ProgressChanged -= OnProgressChanged;
                    _queueProcessor.QueueCompleted -= OnQueueCompleted;
                    // НЕ вызываем (_queueProcessor as IDisposable)?.Dispose();
                }

                // Отписка от асинхронных событий
                if (_view != null)
                {
                    _view.AddFilesRequestedAsync -= OnAddFilesRequestedAsync;
                    _view.StartConversionRequested -= OnStartConversionRequested;
                    _view.StartConversionRequestedAsync -= OnStartConversionRequestedAsync;
                    _view.CancelConversionRequestedAsync -= OnCancelConversionRequestedAsync;
                    _view.FilesDroppedAsync -= OnFilesDroppedAsync;
                    _view.RemoveSelectedFilesRequestedAsync -= OnRemoveSelectedFilesRequestedAsync;
                    _view.ClearAllFilesRequestedAsync -= OnClearAllFilesRequestedAsync;
                }

                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }

        private void EnsureProcessingCancellationToken()
        {
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                ResetProcessingCancellationToken();
            }
        }

        private void ResetProcessingCancellationToken()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        // Публичные методы для делегирования из Form1
        public async Task OnRemoveSelectedFilesRequested()
        {
            await OnRemoveSelectedFilesRequestedAsync(this, EventArgs.Empty);
        }

        public async Task OnClearAllFilesRequested()
        {
            await OnClearAllFilesRequestedAsync(this, EventArgs.Empty);
        }
    }
}

