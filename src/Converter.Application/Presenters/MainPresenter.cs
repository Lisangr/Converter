using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using Converter.Extensions;
using Converter.Models;
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
        private readonly ILogger<MainPresenter> _logger;
        private bool _disposed;
        private bool _clearingInProgress;
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
            // Placeholder for settings loading
            await Task.CompletedTask;
        }

        private async Task LoadPresetsAsync()
        {
            // Load profiles from provider and push to view
            var profiles = await _profileProvider.GetAllProfilesAsync();
            _view.AvailablePresets = new System.Collections.ObjectModel.ObservableCollection<Converter.Models.ConversionProfile>(profiles);

            _viewModel.Presets.Clear();
            foreach (var profile in profiles)
            {
                _viewModel.Presets.Add(profile);
            }

            var defaultProfile = await _profileProvider.GetDefaultProfileAsync();
            _view.SelectedPreset = defaultProfile;
            _viewModel.SelectedPreset = defaultProfile;
        }

        private void OnPresetSelected(object? sender, Converter.Models.ConversionProfile profile)
        {
            if (profile == null) return;
            _logger.LogInformation("Preset selected: {Name}", profile.Name);
            _viewModel.SelectedPreset = profile;
            _view.ShowInfo($"Preset selected: {profile.Name}");
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            _logger.LogInformation("Settings changed");
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
                    _viewModel.QueueItems.Add(QueueItemViewModel.FromModel(item));
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

                // 1. Cancel the current conversion token
                if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }

                // 2. Stop the queue processor to prevent new items from starting
                await _queueProcessor.StopProcessingAsync();

                // 3. Mark all pending items as cancelled
                var pendingItems = await _queueRepository.GetPendingItemsAsync();
                foreach (var item in pendingItems)
                {
                    if (item.Status == ConversionStatus.Processing)
                    {
                        item.Status = ConversionStatus.Failed;
                        item.ErrorMessage = "Конвертация отменена пользователем";
                        await _queueRepository.UpdateAsync(item);
                    }
                }

                // 4. Reset for next conversion
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
            if (_view is Control control)
            {
                control.InvokeIfRequired(action);
            }
            else
            {
                action();
            }
        }

        private void OnItemAdded(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                // ViewModel
                _viewModel.QueueItems.Add(QueueItemViewModel.FromModel(item));
                _view.StatusText = $"Added {item.FileName} to queue";
            });
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

                // Считаем суммарный прогресс по очереди
                if (_viewModel.QueueItems.Any())
                {
                    var total = _viewModel.QueueItems.Average(x => x.Progress);
                    _view.UpdateTotalProgress((int)total);
                    _logger.LogDebug("Total progress updated: {Total}%", (int)total);
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
                _view.IsBusy = false;
                _view.ShowInfo("All items have been processed");
				_view.UpdateCurrentProgress(0);
				_view.UpdateTotalProgress(100);
            });
        }

        private async Task OnFilesDroppedAsync(object? sender, string[] files)
        {
            var addedCount = 0;
            foreach (var file in files)
            {
                if (!File.Exists(file)) continue;
                if (_viewModel.QueueItems.Any(item => item.FilePath == file)) continue;
                var outputDir = !string.IsNullOrWhiteSpace(_view.OutputFolder)
                    ? _view.OutputFolder
                    : Path.GetDirectoryName(file) ?? string.Empty;
                var item = new QueueItem
                {
                    Id = Guid.NewGuid(),
                    FilePath = file,
                    FileSizeBytes = new FileInfo(file).Length,
                    OutputDirectory = outputDir,
                    Status = ConversionStatus.Pending,
                    AddedAt = DateTime.UtcNow
                };
                
                await _queueRepository.AddAsync(item);
                addedCount++;
            }
            
            if (addedCount > 0)
            {
                _view.StatusText = $"Добавлено файлов: {addedCount}";
                await LoadQueueAsync();
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

                // Используем массовое удаление для лучшей производительности
                var itemIds = selectedItems.Select(item => item.Id).ToList();
                await _queueRepository.RemoveRangeAsync(itemIds);

                _logger.LogDebug("Removed {Count} files from queue using batch operation", selectedItems.Count);

                // НЕ перезагружаем очередь - события от QueueRepository сами обновят UI
                // await LoadQueueAsync(); // УДАЛЕНО - это было причиной дублирования

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

                    var allItems = _viewModel.QueueItems.ToList();
                    
                    // Используем массовое удаление для лучшей производительности
                    var allItemIds = allItems.Select(item => item.Id).ToList();
                    await _queueRepository.RemoveRangeAsync(allItemIds);

                    _logger.LogDebug("Cleared {Count} files from queue using batch operation", allItems.Count);

                    // Перезагружаем очередь для синхронизации
                    await LoadQueueAsync();

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
            // Delegate to async version to ensure proper async handling
            _ = Task.Run(async () => await OnStartConversionRequestedAsync());
        }

        private async Task OnStartConversionRequestedAsync()
        {
            try
            {
                _logger.LogInformation("Start conversion requested");
                
                if (_viewModel.QueueItems.Count == 0)
                {
                    _view.ShowInfo("Нет файлов для конвертации");
                    return;
                }

                _view.IsBusy = true;
                _view.StatusText = "Запуск конвертации...";
                
                // Start the queue processor
                await _queueProcessor.StartProcessingAsync(_cancellationTokenSource.Token);
                
                _view.StatusText = "Конвертация запущена";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversion");
                _view.ShowError($"Ошибка при запуске конвертации: {ex.Message}");
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private async Task OnCancelConversionRequestedAsync()
        {
            await OnCancelConversionRequestedAsync(this, EventArgs.Empty);
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
                    //_view.AddFilesRequestedAsync -= OnAddFilesRequestedAsync;
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

