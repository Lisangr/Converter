using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using Converter.Extensions;
using Converter.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Presenters
{
    public sealed class MainPresenter : IDisposable
    {
        public bool IsProcessing => _queueProcessor?.IsProcessing ?? false;
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
        private readonly IApplicationShutdownService _shutdownService;
        private readonly IConversionEstimationService _estimationService;
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
            IApplicationShutdownService shutdownService,
            IConversionEstimationService estimationService,
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
            _shutdownService = shutdownService ?? throw new ArgumentNullException(nameof(shutdownService));
            _estimationService = estimationService ?? throw new ArgumentNullException(nameof(estimationService));
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
            var profilesList = profiles.ToList();
            
            _logger.LogInformation("Loaded {Count} presets from ProfileProvider", profilesList.Count);
            
            _view.AvailablePresets = new System.Collections.ObjectModel.ObservableCollection<Converter.Application.Models.ConversionProfile>(profilesList);

            _viewModel.Presets.Clear();
            foreach (var profile in profilesList)
            {
                _viewModel.Presets.Add(profile);
                _logger.LogDebug("Added preset: {Name} (Category: {Category})", profile.Name, profile.Category);
            }

            var defaultProfile = await _profileProvider.GetDefaultProfileAsync();
            _view.SelectedPreset = defaultProfile;
            _viewModel.SelectedPreset = defaultProfile;
            
            _logger.LogInformation("AvailablePresets count: {Count}", _view.AvailablePresets?.Count ?? 0);
            
            // Уведомляем View о том, что пресеты загружены, чтобы перестроить вкладку
            if (_view is Form1 form1)
            {
                _logger.LogInformation("Calling RebuildPresetsTab after loading {Count} presets", profilesList.Count);
                form1.RebuildPresetsTab();
            }
        }

private void OnPresetSelected(object? sender, Converter.Application.Models.ConversionProfile profile)
{
    if (profile == null) return;
    _logger.LogInformation("Preset selected: {Name}", profile.Name);
    _viewModel.SelectedPreset = profile;
    _view.ShowInfo($"Preset selected: {profile.Name}");
    
    // Trigger estimate update when preset changes
    _ = RequestEstimateUpdateAsync();
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

        // Use InvokeIfRequired to ensure we're on the UI thread
        _view.RunOnUiThread(() =>
        {
            _viewModel.QueueItems.Clear();

            foreach (var item in list)
            {
                var vm = QueueItemViewModel.FromModel(item);
                _viewModel.QueueItems.Add(vm);
                _ = LoadThumbnailForItemAsync(item, vm, _cancellationTokenSource.Token);
            }
        });

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
                _view.RunOnUiThread(() =>
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


        private void OnItemAdded(object? sender, QueueItem item)
{
                _view.RunOnUiThread(() =>
                {
                    var vm = QueueItemViewModel.FromModel(item);
                    _viewModel.QueueItems.Add(vm);
                    _ = LoadThumbnailForItemAsync(item, vm, _cancellationTokenSource.Token);
                    _view.StatusText = $"Added {item.FileName} to queue";
    
                    // Инициализируем оценку времени на основе размера файла (будет обновлена при оценке конвертации)
                    var initialEstimate = EstimateDurationFromFileSize(item.FileSizeBytes);
                    SetEstimatedDurationForItem(item.Id, TimeSpan.FromSeconds(initialEstimate));
                });}

        private async Task LoadThumbnailForItemAsync(QueueItem item, QueueItemViewModel vm, CancellationToken ct)
{
    try
    {
        var bytes = await _thumbnailService.GetThumbnailAsync(item.FilePath, 160, 90, ct).ConfigureAwait(false);
        // Ensure we're on the UI thread when updating the view model
        _view.RunOnUiThread(() => 
        {
            vm.ThumbnailBytes = bytes;
        });
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
            _view.RunOnUiThread(() =>
            {
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                vm?.UpdateFromModel(item);
                _view.StatusText = $"Updated {item.FileName} - {item.Status}";
            });
        }

        private void OnItemRemoved(object? sender, Guid itemId)
        {
            _view.RunOnUiThread(() =>
            {
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == itemId);
                if (vm != null)
                {
                    _viewModel.QueueItems.Remove(vm);
                }
                
                // Удаляем оценку времени и время начала из кэша
                _itemEstimatedDurations.Remove(itemId);
                _itemStartTimes.Remove(itemId);
                
                _view.StatusText = "Item removed from queue";
            });
        }

        private void OnItemStarted(object? sender, QueueItem item)
        {
            _view.RunOnUiThread(() =>
            {
                item.Status = ConversionStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                
                // Сохраняем время начала для расчета прогресса
                _itemStartTimes[item.Id] = item.StartedAt.Value;
                
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = item.Status;
                    vm.Progress = 0; // Reset progress when starting
                }
                // Сбрасываем нижний прогрессбар для текущего файла, чтобы он снова шел 0→100
                _view.UpdateCurrentProgress(0);
                _view.StatusText = $"Processing {item.FileName}...";
                _view.AppendLog($"🎬 Начало конвертации: {item.FileName}");
                _view.AppendLog($"📁 Входной файл: {item.FilePath}");
                if (!string.IsNullOrEmpty(item.OutputPath))
                {
                    _view.AppendLog($"📁 Выходной файл: {item.OutputPath}");
                }
                
                // Получаем оценку времени для логирования
                var estimatedDuration = GetEstimatedDurationForItem(item.Id);
                if (estimatedDuration.TotalSeconds > 0)
                {
                    _view.AppendLog($"⏱️ Оценка времени конвертации: {FormatDuration(estimatedDuration)}");
                }
                
                // Логируем параметры конвертации
                // Команда FFmpeg логируется в ConversionOrchestrator через _logger
                // Для отображения в UI нужно получить команду из ConversionUseCase
                // Пока логируем только основную информацию
                
                // Обновляем состояние кнопок
                UpdateConversionButtonsState();
            });
        }
        
        /// <summary>
        /// Рассчитывает прогресс текущего элемента на основе прошедшего времени относительно оценки времени.
        /// </summary>
        private int CalculateCurrentItemProgress(QueueItem item)
        {
            // Если элемент не в обработке, возвращаем его текущий прогресс
            if (item.Status != ConversionStatus.Processing)
            {
                return item.Progress;
            }
            
            // Получаем время начала и оценку времени
            if (!_itemStartTimes.TryGetValue(item.Id, out var startTime))
            {
                // Если нет времени начала, используем StartedAt из item
                if (!item.StartedAt.HasValue)
                {
                    return item.Progress;
                }
                startTime = item.StartedAt.Value;
                _itemStartTimes[item.Id] = startTime;
            }
            
            var estimatedDuration = GetEstimatedDurationForItem(item.Id);
            if (estimatedDuration.TotalSeconds <= 0)
            {
                // Если нет оценки времени, используем прогресс от FFmpeg
                return item.Progress;
            }
            
            // Рассчитываем прошедшее время
            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            
            // Рассчитываем прогресс на основе времени: (прошедшее время / оценка времени) * 100
            var timeBasedProgress = (elapsed / estimatedDuration.TotalSeconds) * 100.0;
            
            // Ограничиваем прогресс: не превышаем 99% до завершения, и не меньше текущего прогресса от FFmpeg
            // Но не позволяем прогрессу быть меньше, чем 90% от прогресса FFmpeg (чтобы не было слишком медленно)
            var minProgress = Math.Max(0, item.Progress * 0.9);
            var maxProgress = Math.Min(99.0, Math.Max(timeBasedProgress, minProgress));
            
            return (int)Math.Round(maxProgress);
        }
        
        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours} ч {duration.Minutes} мин";
            if (duration.TotalMinutes >= 1)
                return $"{(int)duration.TotalMinutes} мин {duration.Seconds} сек";
            return $"{duration.Seconds} сек";
        }
        
        private void UpdateConversionButtonsState()
        {
            // Обновляем состояние кнопок на основе состояния очереди
            bool hasProcessing = _viewModel.QueueItems.Any(x => 
                x.Status == ConversionStatus.Processing || x.Status == ConversionStatus.Pending);
            
            // Обновляем состояние кнопок через UpdateControlsState
            if (_view is Form1 form1)
            {
                form1.UpdateConversionButtons(hasProcessing);
            }
        }

        private void OnItemCompleted(object? sender, QueueItem item)
        {
            _view.RunOnUiThread(() =>
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
                
                // Устанавливаем прогресс в 100%
                _view.UpdateCurrentProgress(100);
                
                // Удаляем из кэша времени начала
                _itemStartTimes.Remove(item.Id);
                
                _view.StatusText = $"Completed: {item.FileName}";
                _view.AppendLog($"✅ Завершено: {item.FileName}");
                
                // Обновляем состояние кнопок
                UpdateConversionButtonsState();
                
                // Проверяем, завершена ли вся очередь
                CheckQueueCompletion();
            });
        }
        
        private void CheckQueueCompletion()
        {
            var allItems = _viewModel.QueueItems.ToList();
            var hasProcessing = allItems.Any(x => 
                x.Status == ConversionStatus.Processing || x.Status == ConversionStatus.Pending);
            
            if (!hasProcessing && allItems.Count > 0)
            {
                // Все элементы обработаны, вызываем OnQueueCompleted
                OnQueueCompleted(this, EventArgs.Empty);
            }
        }

        private void OnItemFailed(object? sender, QueueItem item)
        {
            _view.RunOnUiThread(() =>
            {
                item.Status = ConversionStatus.Failed;
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = item.Status;
                    vm.ErrorMessage = item.ErrorMessage;
                }
                _view.ShowError($"Failed to process {item.FileName}: {item.ErrorMessage}");
                _view.AppendLog($"❌ Ошибка: {item.FileName} - {item.ErrorMessage}");
                
                // Обновляем состояние кнопок
                UpdateConversionButtonsState();
                
                // Проверяем, завершена ли вся очередь
                CheckQueueCompletion();
            });
        }

        // Кэш оценок времени для элементов очереди
        private readonly Dictionary<Guid, TimeSpan> _itemEstimatedDurations = new();
        // Кэш времени начала конвертации для расчета прогресса на основе времени
        private readonly Dictionary<Guid, DateTime> _itemStartTimes = new();
        
        private void OnProgressChanged(object? sender, QueueProgressEventArgs e)
        {
            _view.RunOnUiThread(() =>
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

                // Прогресс текущего файла синхронизируем с тем же процентом, что и в очереди
                _view.UpdateCurrentProgress(e.Progress);

                // Считаем суммарный прогресс по очереди на основе оценки времени
                if (_viewModel.QueueItems.Any())
                {
                    var newTotalProgress = CalculateTotalProgressBasedOnTime();
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
        
        /// <summary>
        /// Рассчитывает общий прогресс очереди на основе оценки времени для каждого элемента.
        /// Использует прошедшее время относительно оценки времени вместо простого усреднения процентов.
        /// </summary>
        private int CalculateTotalProgressBasedOnTime()
        {
            if (!_viewModel.QueueItems.Any())
                return 0;
            
            var now = DateTime.UtcNow;
            double totalWeightedProgress = 0;
            double totalWeight = 0;
            
            foreach (var vm in _viewModel.QueueItems)
            {
                // Получаем оценку времени для этого элемента
                var estimatedDuration = GetEstimatedDurationForItem(vm.Id);
                
                double itemProgress = 0;
                double itemWeight = 1.0; // Вес по умолчанию
                
                if (vm.Status == ConversionStatus.Completed)
                {
                    // Завершенные элементы считаются как 100%
                    itemProgress = 100.0;
                    itemWeight = estimatedDuration.TotalSeconds > 0 ? estimatedDuration.TotalSeconds : 1.0;
                }
                else if (vm.Status == ConversionStatus.Processing)
                {
                    // Для элементов в обработке используем текущий прогресс от FFmpeg
                    // Но применяем сглаживание на основе оценки времени, чтобы избежать резких скачков
                    itemProgress = vm.Progress;
                    
                    // Вес элемента пропорционален оценке времени конвертации
                    // Это означает, что элементы с большей оценкой времени имеют больший вес в общем прогрессе
                    itemWeight = estimatedDuration.TotalSeconds > 0 ? estimatedDuration.TotalSeconds : 1.0;
                }
                else if (vm.Status == ConversionStatus.Failed)
                {
                    // Неудачные элементы не учитываются в общем прогрессе
                    continue;
                }
                else
                {
                    // Ожидающие элементы считаются как 0%
                    itemProgress = 0.0;
                    itemWeight = estimatedDuration.TotalSeconds > 0 ? estimatedDuration.TotalSeconds : 1.0;
                }
                
                totalWeightedProgress += itemProgress * itemWeight;
                totalWeight += itemWeight;
            }
            
            if (totalWeight <= 0)
                return 0;
            
            var result = (int)Math.Round(totalWeightedProgress / totalWeight);
            return Math.Clamp(result, 0, 100);
        }
        
        /// <summary>
        /// Получает оценку времени для элемента очереди.
        /// Если оценка еще не сохранена, пытается получить её из последней оценки конвертации.
        /// </summary>
        private TimeSpan GetEstimatedDurationForItem(Guid itemId)
        {
            // Проверяем кэш
            if (_itemEstimatedDurations.TryGetValue(itemId, out var cached))
            {
                return cached;
            }
            
            // Если нет в кэше, пытаемся получить из ViewModel
            var vm = _viewModel.QueueItems.FirstOrDefault(x => x.Id == itemId);
            if (vm != null)
            {
                // Пытаемся оценить время на основе размера файла и текущих настроек
                // Это приблизительная оценка, но лучше чем ничего
                var estimatedSeconds = EstimateDurationFromFileSize(vm.FileSizeBytes);
                var estimated = TimeSpan.FromSeconds(estimatedSeconds);
                _itemEstimatedDurations[itemId] = estimated;
                return estimated;
            }
            
            // Если ничего не найдено, возвращаем минимальную оценку
            return TimeSpan.FromSeconds(10);
        }
        
        /// <summary>
        /// Обновляет оценки времени для всех элементов очереди на основе последней оценки конвертации.
        /// </summary>
        private async Task UpdateEstimatedDurationsFromEstimateAsync(ConversionEstimate? estimate, string[] files)
        {
            if (estimate == null || files == null || files.Length == 0)
                return;
            
            // Если оценка для одного файла, распределяем время равномерно
            if (files.Length == 1)
            {
                var item = _viewModel.QueueItems.FirstOrDefault(x => x.FilePath == files[0]);
                if (item != null)
                {
                    SetEstimatedDurationForItem(item.Id, estimate.EstimatedDuration);
                }
            }
            else
            {
                // Для нескольких файлов распределяем время пропорционально размеру
                var totalSize = files.Sum(f => 
                {
                    try
                    {
                        if (File.Exists(f))
                            return new FileInfo(f).Length;
                    }
                    catch { }
                    return 0L;
                });
                
                if (totalSize > 0)
                {
                    foreach (var file in files)
                    {
                        try
                        {
                            if (File.Exists(file))
                            {
                                var fileSize = new FileInfo(file).Length;
                                var ratio = (double)fileSize / totalSize;
                                var itemDuration = TimeSpan.FromTicks((long)(estimate.EstimatedDuration.Ticks * ratio));
                                
                                var item = _viewModel.QueueItems.FirstOrDefault(x => x.FilePath == file);
                                if (item != null)
                                {
                                    SetEstimatedDurationForItem(item.Id, itemDuration);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error calculating estimated duration for file {File}", file);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Приблизительно оценивает время конвертации на основе размера файла.
        /// Используется как fallback, когда точная оценка недоступна.
        /// </summary>
        private double EstimateDurationFromFileSize(long fileSizeBytes)
        {
            // Приблизительная оценка: 1 MB ≈ 1 секунда конвертации (для среднего качества)
            // Это очень приблизительно, но лучше чем ничего
            var sizeInMb = fileSizeBytes / (1024.0 * 1024.0);
            return Math.Max(5.0, sizeInMb * 0.5); // Минимум 5 секунд
        }
        
        /// <summary>
        /// Сохраняет оценку времени для элемента очереди.
        /// Вызывается при добавлении файла в очередь или при обновлении оценки.
        /// </summary>
        private void SetEstimatedDurationForItem(Guid itemId, TimeSpan estimatedDuration)
        {
            _itemEstimatedDurations[itemId] = estimatedDuration;
        }

        private void OnQueueCompleted(object? sender, EventArgs e)
        {
            _view.RunOnUiThread(() =>
            {
                // Check if there are still items in the queue that are being processed
                var allItems = _viewModel.QueueItems.ToList();
                var completedItems = allItems.Where(i => i.Status == ConversionStatus.Completed).ToList();
                var failedItems = allItems.Where(i => i.Status == ConversionStatus.Failed).ToList();
                var processingItems = allItems.Where(i => i.Status == ConversionStatus.Processing || i.Status == ConversionStatus.Pending).ToList();

                var total = allItems.Count;
                var ok = completedItems.Count;
                var failed = failedItems.Count;

                // Only show completion message if all items are processed (no pending or processing items)
                if (processingItems.Count == 0 && total > 0)
                {
                    var spaceSavedBytes = CalculateSpaceSaved(completedItems);
                    var spaceSavedText = FormatFileSize(spaceSavedBytes);

                    _view.StatusText = "Конвертация завершена";
                    _view.ShowInfo($"Конвертация завершена. Успешно: {ok}/{total}. Ошибки: {failed}. Сэкономлено места: {spaceSavedText}.");
                    _view.UpdateCurrentProgress(0);
                    _view.UpdateTotalProgress(100);
                    _view.IsBusy = false; // Unlock UI and disable "Stop" button after completion
                    
                    _logger.LogInformation("Queue processing completed: {Ok}/{Total} successful, {Failed} failed", ok, total, failed);
                }
            });
        }

        private static long CalculateSpaceSaved(IEnumerable<QueueItemViewModel> items)
        {
            long total = 0;
            foreach (var item in items)
            {
                var inputSize = item.FileSizeBytes;
                var outputSize = item.OutputFileSizeBytes ?? inputSize;
                if (outputSize < inputSize)
                {
                    total += inputSize - outputSize;
                }
            }

            return total;
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 B";
            }

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[order]}";
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
                    
                    // Очищаем кэш оценок времени и времени начала
                    _itemEstimatedDurations.Clear();
                    _itemStartTimes.Clear();

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

                // НЕ устанавливаем IsBusy = true, так как конвертация асинхронная и не блокирует UI
                // Управление кнопками происходит через UpdateControlsState в Form1
                _view.StatusText = "Запуск конвертации...";

                // QueueProcessor уже запущен как HostedService, активируем обработку через команду
                await _startConversionCommand
                    .ExecuteAsync(_cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                _view.StatusText = "Конвертация запущена";
                _view.ShowInfo("Процесс конвертации начат");
                // IsBusy будет сброшен в OnQueueCompleted или при отмене конвертации
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

        public async Task RemoveFileFromQueue(string filePath, bool fromView = false)
        {
            try
            {
                var viewModelItem = _viewModel.QueueItems.FirstOrDefault(x => x.FilePath == filePath);
                if (viewModelItem != null)
                {
                    // Convert ViewModel to Domain Model
                    var domainItem = new QueueItem
                    {
                        Id = viewModelItem.Id,
                        FilePath = viewModelItem.FilePath,
                        FileSizeBytes = viewModelItem.FileSizeBytes,
                        Status = viewModelItem.Status,
                        Progress = viewModelItem.Progress,
                        ErrorMessage = viewModelItem.ErrorMessage,
                        OutputPath = viewModelItem.OutputPath,
                        OutputFileSizeBytes = viewModelItem.OutputFileSizeBytes,
                        IsStarred = viewModelItem.IsStarred,
                        Priority = viewModelItem.Priority,
                        NamingPattern = viewModelItem.NamingPattern
                    };
                    
                    await RemoveItemAsync(domainItem, fromView);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing file from queue: {FilePath}", filePath);
            }
        }

        /// <summary>
        /// Запрашивает мягкое завершение работы приложения:
        /// останавливает конвертацию/очередь и затем инициирует shutdown хоста.

        public async Task RequestShutdownAsync()
        {
            try
            {
                _logger.LogInformation("UI requested application shutdown");

                // 1. Попытаться отменить текущую конвертацию
                try
                {
                    await OnCancelConversionRequestedAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while canceling conversions during shutdown request");
                }

                // 2. Попробовать очистить очередь (не критично, если не получится)
                try
                {
                    await OnClearAllFilesRequestedAsync(this, EventArgs.Empty).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while clearing queue during shutdown request");
                }
            }
            finally
            {
                // 3. В любом случае сигнализируем хосту о завершении работы
                _shutdownService.RequestShutdown();
            }
        }

        public async Task<List<QueueItem>> GetCompletedItemsAsync()
        {
            var items = await _queueRepository.GetAllAsync().ConfigureAwait(false);
            return items
                .Where(x => x.Status == ConversionStatus.Completed)
                .ToList();
        }

        public async Task<ConversionEstimate> EstimateConversionAsync(
        string[] files,
        int targetBitrateKbps,
        int? targetWidth,
        int? targetHeight,
        string videoCodec,
        bool includeAudio,
        int? audioBitrateKbps,
        int? crf = null,
        bool audioCopy = false)
        {
            var totalEstimate = new ConversionEstimate
            {
                InputFileSizeBytes = 0,
                EstimatedOutputSizeBytes = 0,
                EstimatedDuration = TimeSpan.Zero,
                CompressionRatio = 0,
                SpaceSavedBytes = 0
            };

            int processedFiles = 0;
            var fileEstimates = new Dictionary<string, ConversionEstimate>();
            
            foreach (var file in files)
            {
                if (!System.IO.File.Exists(file))
                    continue;

                try
                {
                    var estimate = await _estimationService.EstimateConversion(
                        file,
                        targetBitrateKbps,
                        targetWidth,
                        targetHeight,
                        videoCodec,
                        includeAudio,
                        audioBitrateKbps,
                        crf,
                        audioCopy,
                        CancellationToken.None);

                    fileEstimates[file] = estimate;
                    
                    totalEstimate.InputFileSizeBytes += estimate.InputFileSizeBytes;
                    totalEstimate.EstimatedOutputSizeBytes += estimate.EstimatedOutputSizeBytes;
                    totalEstimate.EstimatedDuration = totalEstimate.EstimatedDuration.Add(estimate.EstimatedDuration);
                    totalEstimate.SpaceSavedBytes += estimate.SpaceSavedBytes;
                    processedFiles++;
                    
                    // Сохраняем оценку времени для элемента очереди, если он уже добавлен
                    var item = _viewModel.QueueItems.FirstOrDefault(x => x.FilePath == file);
                    if (item != null)
                    {
                        SetEstimatedDurationForItem(item.Id, estimate.EstimatedDuration);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка оценки файла {System.IO.Path.GetFileName(file)}");
                }
            }

            if (processedFiles > 0)
            {
                totalEstimate.CompressionRatio = totalEstimate.EstimatedOutputSizeBytes > 0
                    ? Math.Min(1.0, Math.Max(0.0, totalEstimate.EstimatedOutputSizeBytes / (double)Math.Max(1, totalEstimate.InputFileSizeBytes)))
                    : 0;
            }

            return totalEstimate;
        }


        public async Task RemoveItemAsync(QueueItem item, bool fromView = false, CancellationToken cancellationToken = default)
        {
            try
            {
                // Only notify the view if this call didn't originate from the view
                if (!fromView)
                {
                    _view.RemoveFileFromQueue(item.FilePath);
                }

                // Remove the item from the queue processor using IQueueProcessor.RemoveItemAsync
                await _queueProcessor.RemoveItemAsync(item, cancellationToken);
                _logger.LogInformation("Successfully removed item {ItemId} from queue processor.", item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item {ItemId} from queue", item.Id);
                _view.ShowError($"Error removing file '{item.FileName}' from queue: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохраняет пресет в файл (для экспорта пресета из UI)
        /// </summary>
        public void SavePresetToFile(PresetProfile preset, string filePath)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required", nameof(filePath));

            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = System.Text.Json.JsonSerializer.Serialize(preset, options);
                System.IO.File.WriteAllText(filePath, json);
                _logger.LogInformation("Preset saved to file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving preset to file: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Загружает пресет из файла (для импорта пресета в UI)
        /// </summary>
        public PresetProfile LoadPresetFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required", nameof(filePath));
            if (!System.IO.File.Exists(filePath)) throw new System.IO.FileNotFoundException($"Preset file not found: {filePath}", filePath);

            try
            {
                var json = System.IO.File.ReadAllText(filePath);
                var preset = System.Text.Json.JsonSerializer.Deserialize<PresetProfile>(json);

                if (preset == null)
                {
                    throw new InvalidOperationException("Не удалось загрузить пресет: неверный формат файла");
                }

                // Гарантируем наличие Id
                if (string.IsNullOrWhiteSpace(preset.Id))
                {
                    preset.Id = Guid.NewGuid().ToString("N");
                }

                _logger.LogInformation("Preset loaded from file: {FilePath}", filePath);
                return preset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading preset from file: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Применяет пресет к View через событие PresetSelected
        /// </summary>
        public void ApplyPreset(PresetProfile preset)
        {
            if (preset == null) return;

            try
            {
                // Конвертируем PresetProfile в ConversionProfile для события
                var conversionProfile = new ConversionProfile
                {
                    Id = preset.Id,
                    Name = preset.Name,
                    Description = preset.Description,
                    Category = preset.Category,
                    VideoCodec = preset.VideoCodec,
                    Bitrate = preset.Bitrate,
                    Width = preset.Width,
                    Height = preset.Height,
                    CRF = preset.CRF,
                    Format = preset.Format,
                    AudioCodec = preset.AudioCodec,
                    AudioBitrate = preset.AudioBitrate,
                    IncludeAudio = preset.IncludeAudio,
                    MaxFileSizeMB = preset.MaxFileSizeMB,
                    MaxDurationSeconds = preset.MaxDurationSeconds,
                    Icon = preset.Icon,
                    ColorHex = preset.ColorHex,
                    IsPro = preset.IsPro
                };

                _viewModel.SelectedPreset = conversionProfile;
                _view.SelectedPreset = conversionProfile;
                
                // Событие PresetSelected будет вызвано автоматически через setter SelectedPreset в Form1
                
                _logger.LogInformation("Preset applied: {PresetName}", preset.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying preset: {PresetName}", preset.Name);
                _view.ShowError($"Ошибка при применении пресета: {ex.Message}");
            }
        }

        /// <summary>
        /// Запрашивает обновление оценки конвертации на основе текущих файлов и настроек
        /// </summary>
        public async Task RequestEstimateUpdateAsync()
        {
            try
            {
                if (_viewModel.QueueItems.Count == 0)
                {
                    _view.ShowEstimateCalculating();
                    return;
                }

                var files = _viewModel.QueueItems
                    .Where(x => !string.IsNullOrWhiteSpace(x.FilePath) && System.IO.File.Exists(x.FilePath))
                    .Select(x => x.FilePath)
                    .ToArray();

                if (files.Length == 0)
                {
                    _view.ShowEstimateCalculating();
                    return;
                }

                // Получаем эффективные настройки из текущего выбранного профиля.
                // Он формируется либо из XML-пресета, либо из текущего состояния UI (Form1.BuildPresetFromUi).
                var preset = _viewModel.SelectedPreset;

                int targetBitrateKbps = 0;
                int? targetWidth = null;
                int? targetHeight = null;
                string videoCodec = "libx264";
                bool includeAudio = true;
                int? audioBitrateKbps = 128;
                int? crf = 23;
                bool audioCopy = false;

                if (preset != null)
                {
                    if (preset.Bitrate.HasValue)
                        targetBitrateKbps = preset.Bitrate.Value;
                    if (preset.Width.HasValue)
                        targetWidth = preset.Width.Value;
                    if (preset.Height.HasValue)
                        targetHeight = preset.Height.Value;

                    if (!string.IsNullOrWhiteSpace(preset.VideoCodec))
                        videoCodec = preset.VideoCodec!;

                    includeAudio = preset.IncludeAudio;

                    if (preset.AudioBitrate.HasValue)
                        audioBitrateKbps = preset.AudioBitrate.Value;

                    if (preset.CRF.HasValue)
                        crf = preset.CRF.Value;
                }

                var estimate = await EstimateConversionAsync(
                    files,
                    targetBitrateKbps,
                    targetWidth,
                    targetHeight,
                    videoCodec,
                    includeAudio,
                    audioBitrateKbps,
                    crf,
                    audioCopy
                );

                _view.ShowEstimate(estimate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating estimate");
                _view.ShowEstimateCalculating();
            }
        }
    }
}