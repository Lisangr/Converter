using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Converter.Application.Abstractions;
using Converter.Models;
using Converter.Domain.Models;
using Converter.Services;
using Converter.UI;
using Converter.UI.Controls;
using Converter.Application.ViewModels;

namespace Converter
{
    public partial class Form1 : Form, IMainView, IDisposable
    {
        private ThemeSelectorControl? _themeSelector;
        private Button? _themeMenuButton;
        private bool _themeInitialized;
        private readonly IThemeService _themeService;
        private readonly INotificationService _notificationService;
        private readonly IThumbnailProvider _thumbnailProvider;
        private readonly IShareService _shareService;
        private readonly IFileService _fileService;
        private readonly Converter.Services.UIServices.IFileOperationsService _fileOperationsService;
        private readonly CancellationTokenSource _lifecycleCts = new();
        private IHost? _host;
        
        private bool _disposed = false;
        private bool _closingInProgress = false;
        
        // Fields for estimation and background operations
        private System.Timers.Timer? _estimateDebounce;
        private CancellationTokenSource? _estimateCts;
        
        

        // IMainView events
        public event EventHandler? AddFilesRequested;
        public event EventHandler? StartConversionRequested;
        public event EventHandler? CancelConversionRequested;
        public event EventHandler<Converter.Models.ConversionProfile>? PresetSelected;
        public event EventHandler? SettingsChanged;
        public event EventHandler<string[]>? FilesDropped;
        public event EventHandler? RemoveSelectedFilesRequested;
        public event EventHandler? ClearAllFilesRequested;

        // Async events for operations that require asynchronous handling
        public event Func<Task>? AddFilesRequestedAsync;
        public event Func<Task>? StartConversionRequestedAsync;
        public event Func<Task>? CancelConversionRequestedAsync;
        public event Func<string[], Task>? FilesDroppedAsync;
        public event Func<Task>? RemoveSelectedFilesRequestedAsync;
        public event Func<Task>? ClearAllFilesRequestedAsync;

        // Ссылка на MainPresenter для делегирования операций
        private Converter.Application.Presenters.MainPresenter? _mainPresenter;

        public void SetMainPresenter(object presenter)
        {
            _mainPresenter = presenter as Converter.Application.Presenters.MainPresenter;
        }

        public void SetHost(IHost host)
        {
            _host = host;
        }

        private string _ffmpegPath = string.Empty;
        public string FfmpegPath
        {
            get => _ffmpegPath;
            set { _ffmpegPath = value ?? string.Empty; }
        }

        private string _outputFolder = string.Empty;
        public string OutputFolder
        {
            get => _outputFolder;
            set { _outputFolder = value ?? string.Empty; }
        }

        public ObservableCollection<Converter.Models.ConversionProfile> AvailablePresets { get; set; } = new();

        private Converter.Models.ConversionProfile? _selectedPreset;
        public Converter.Models.ConversionProfile? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                _selectedPreset = value;
                if (value != null)
                {
                    PresetSelected?.Invoke(this, value);
                }
            }
        }

        // IMainView binding-related properties (bridge to existing UI)
        private System.ComponentModel.BindingList<Converter.Application.ViewModels.QueueItemViewModel>? _queueItemsBinding;
        public System.ComponentModel.BindingList<Converter.Application.ViewModels.QueueItemViewModel>? QueueItemsBinding
        {
            get => _queueItemsBinding;
            set
            {
                _queueItemsBinding = value;
                if (_queueBindingSource != null)
                {
                    _queueBindingSource.DataSource = value ?? new System.ComponentModel.BindingList<Converter.Application.ViewModels.QueueItemViewModel>();
                }
            }
        }

        public bool IsBusy
        {
            get => Cursor == Cursors.WaitCursor;
            set => SetBusy(value);
        }

        public string StatusText
        {
            get => lblStatusTotal?.Text ?? string.Empty;
            set => SetStatusText(value);
        }

        public Form1(
            IThemeService themeService,
            INotificationService notificationService,
            IThumbnailProvider thumbnailProvider,
            IShareService shareService,
            IFileService fileService,
            Converter.Services.UIServices.IFileOperationsService fileOperationsService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _thumbnailProvider = thumbnailProvider ?? throw new ArgumentNullException(nameof(thumbnailProvider));
            _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _fileOperationsService = fileOperationsService ?? throw new ArgumentNullException(nameof(fileOperationsService));
            
            InitializeComponent();
            InitializeAdvancedTheming();
            
            // Подписываемся на обновления очереди
            _fileOperationsService.QueueUpdated += OnQueueUpdated;
            
            // Инициализируем логгер для UI компонента
            // Логгер будет настроен через DI в Program.cs
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            SetLogger(logger);
        }

        public void UpdatePresetControls(Converter.Models.ConversionProfile preset)
        {
            // minimal placeholder: reflect selected preset name in title/status
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdatePresetControls(preset))); return; }
            AppendLog($"Preset: {preset.Name} · {preset.VideoCodec}/{preset.AudioCodec}");
        }

        // IMainView: global busy state
        public void SetBusy(bool isBusy)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SetBusy(isBusy))); return; }
            try
            {
                Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;
                
                // Update UI controls if they exist
                UpdateControlsState(isBusy);
            }
            catch { }
        }

        private void UpdateControlsState(bool isBusy)
        {
            try
            {
                // Try to find controls by name in the form
                TryUpdateButton("btnStart", !isBusy);
                TryUpdateButton("btnStop", isBusy);
                TryUpdateButton("btnAddFiles", !isBusy);
                TryUpdateButton("btnRemoveSelected", !isBusy);
                TryUpdateButton("btnClearAll", !isBusy);
                TryUpdateButton("_btnShare", !isBusy);
                TryUpdateButton("_btnOpenEditor", !isBusy);
                TryUpdateButton("_btnNotificationSettings", !isBusy);
                TryUpdateButton("btnSavePreset", !isBusy);
                TryUpdateButton("btnLoadPreset", !isBusy);
            }
            catch { }
        }

        private void TryUpdateButton(string buttonName, bool enabled)
        {
            try
            {
                var button = this.Controls.Find(buttonName, true).FirstOrDefault() as Button;
                if (button != null)
                {
                    button.Enabled = enabled;
                }
            }
            catch { }
        }

        public void SetStatusText(string status)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SetStatusText(status))); return; }

            // Минимально: показываем статус в логах и при наличии статус-лейбла внизу
            AppendLog(status);
            try
            {
                if (lblStatusTotal != null) lblStatusTotal.Text = status;
            }
            catch { }
        }

        // IMainView: notifications
        public void ShowError(string message)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => ShowError(message))); return; }
            AppendLog($"❌ {message}");
        }

        public void ShowInfo(string message)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => ShowInfo(message))); return; }
            AppendLog($"ℹ {message}");
        }

		public void UpdateCurrentProgress(int percent)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateCurrentProgress(percent))); return; }
            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCurrentProgress called: {percent}%");
                if (progressBarCurrent != null)
                {
                    progressBarCurrent.Value = Math.Max(progressBarCurrent.Minimum,
                        Math.Min(progressBarCurrent.Maximum, percent));
                    System.Diagnostics.Debug.WriteLine($"ProgressBarCurrent updated to: {progressBarCurrent.Value}%");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ProgressBarCurrent is null!");
                }
                
                // Update current status label
                if (lblStatusCurrent != null)
                {
                    lblStatusCurrent.Text = $"Текущий: {percent}%";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LblStatusCurrent is null!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateCurrentProgress: {ex.Message}");
            }
        }

        public void UpdateTotalProgress(int percent)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateTotalProgress(percent))); return; }
            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTotalProgress called: {percent}%");
                if (progressBarTotal != null)
                {
                    progressBarTotal.Value = Math.Max(progressBarTotal.Minimum,
                        Math.Min(progressBarTotal.Maximum, percent));
                    System.Diagnostics.Debug.WriteLine($"ProgressBarTotal updated to: {progressBarTotal.Value}%");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ProgressBarTotal is null!");
                }
                
                // Update total status label
                if (lblStatusTotal != null)
                {
                    lblStatusTotal.Text = $"Общий прогресс: {percent}%";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LblStatusTotal is null!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateTotalProgress: {ex.Message}");
            }
        }

        // IMainView: file dialogs
        public string[] ShowOpenFileDialog(string title, string filter)
        {
            using var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = true
            };
            return dlg.ShowDialog(this) == DialogResult.OK ? dlg.FileNames : Array.Empty<string>();
        }

        public IEnumerable<string> ShowOpenMultipleFilesDialog(string title, string filter)
        {
            using var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = true
            };
            return dlg.ShowDialog(this) == DialogResult.OK ? dlg.FileNames : Array.Empty<string>();
        }

        public string? ShowFolderBrowserDialog(string description)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };
            return dlg.ShowDialog(this) == DialogResult.OK ? dlg.SelectedPath : null;
        }

        private void InitializeAdvancedTheming()
        {
            if (_themeInitialized)
            {
                return;
            }

            // Безопасно работаем с кнопкой запуска: на ранних стадиях инициализации контрол может быть ещё не создан
            if (btnStart != null)
            {
                btnStart.Tag = "AccentButton";
            }

            // Переподписываемся на событие смены темы (с проверкой на null на всякий случай)
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
                _themeService.ThemeChanged += OnThemeChanged;
            }

            // применяем текущую тему ко всей форме, только если сервис темы доступен
            if (_themeService != null)
            {
                _themeService.ApplyTheme(this);
                UpdateCustomControlsTheme(_themeService.CurrentTheme);
            }

            _themeSelector = new ThemeSelectorControl(_themeService)
            {
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(_themeSelector);

            _themeMenuButton = new Button
            {
                Text = "🎨",
                Size = new Size(35, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Tag = "NoTheme"
            };
            _themeMenuButton.FlatAppearance.BorderSize = 0;
            _themeMenuButton.Click += (s, e) =>
            {
                if (_themeSelector == null) return;
                _themeSelector.Visible = !_themeSelector.Visible;
                _themeSelector.BringToFront();
            };
            Controls.Add(_themeMenuButton);

            Resize -= OnThemeControlsResize;
            Resize += OnThemeControlsResize;
            PositionThemeControls();

            _themeInitialized = true;
        }

        private void OnThemeChanged(object? sender, Theme theme)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnThemeChanged(sender, theme)));
                return;
            }

            _themeService.ApplyTheme(this);
            UpdateCustomControlsTheme(theme);
            Refresh();
        }

        private void UpdateCustomControlsTheme(Theme theme)
        {
            if (_estimatePanel != null)
            {
                _estimatePanel.UpdateTheme(theme);
            }

            if (filesPanel != null)
            {
                foreach (FileListItem item in filesPanel.Controls.OfType<FileListItem>())
                {
                    item.ApplyTheme(theme);
                }
            }

            if (progressBarTotal != null)
            {
                progressBarTotal.ForeColor = theme["Accent"];
            }

            if (progressBarCurrent != null)
            {
                progressBarCurrent.ForeColor = theme["Accent"];
            }
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            // Защита от множественных вызовов закрытия
            if (_closingInProgress)
            {
                return;
            }
            
            if (_host != null)
            {
                // Помечаем, что процесс закрытия уже запущен, чтобы не вызывать его повторно
                _closingInProgress = true;

                // Отменяем закрытие формы, пока не завершатся фоновые задачи
                e.Cancel = true;
                _ = CloseApplicationAsync();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        private async Task CloseApplicationAsync()
        {
            try
            {
                // Отменяем все фоновые операции
                CancelBackgroundOperations();

                // При выходе очищаем очередь только через презентер (единая точка входа)
                if (_mainPresenter != null)
                {
                    try
                    {
                        await _mainPresenter.OnClearAllFilesRequested().ConfigureAwait(false);
                    }
                    catch { }
                }
                else
                {
                    // Fallback для старого кода
                    try
                    {
                        ClearQueue();
                    }
                    catch { }
                }
                
                // Ожидаем завершения критических операций
                await Task.WhenAny(
                    Task.Delay(TimeSpan.FromSeconds(5)),
                    Task.Run(() => _host?.StopAsync())
                );
            }
            finally
            {
                // Принудительно завершаем приложение
                System.Windows.Forms.Application.Exit();
            }
        }

        private void CancelBackgroundOperations()
        {
            try
            {
                if (!_lifecycleCts.IsCancellationRequested)
                {
                    _lifecycleCts.Cancel();
                }
            }
            catch
            {
                // ignored
            }
        }

        private void DisposeManagedResources()
        {
            _themeService.ThemeChanged -= OnThemeChanged;
            CancelBackgroundOperations();

            try
            {
                _estimateDebounce?.Stop();
                _estimateDebounce?.Dispose();
            }
            catch { }

            try
            {
                _estimateCts?.Cancel();
                _estimateCts?.Dispose();
            }
            catch { }

            try
            {
                _lifecycleCts.Dispose();
            }
            catch { }

            // Safely dispose notification service
            try
            {
                if (_notificationService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch { }

            // Safely dispose share service
            try
            {
                if (_shareService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch { }

            // Safely dispose theme service
            try
            {
                if (_themeService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch { }

            // Safely dispose thumbnail provider
            try
            {
                if (_thumbnailProvider is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                else if (_thumbnailProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch { }

            // Safely dispose file service
            try
            {
                if (_fileService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch { }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _lifecycleCts?.Cancel();
                _lifecycleCts?.Dispose();
                _estimateDebounce?.Dispose();
                _estimateCts?.Dispose();
                
                // Отписываемся от событий
                if (_fileOperationsService != null)
                {
                    _fileOperationsService.QueueUpdated -= OnQueueUpdated;
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        ~Form1()
        {
            Dispose(disposing: false);
        }

        #endregion

        #region Event Handlers

        private void OnQueueUpdated(object? sender, Converter.Services.UIServices.QueueUpdatedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnQueueUpdated(sender, e)));
                return;
            }

            try
            {
                // 1. Обновляем грид очереди (если биндинг уже инициализирован)
                if (_queueBindingSource != null)
                {
                    var viewModels = e.QueueItems
                        .Select(item => QueueItemViewModel.FromModel(item))
                        .ToList();

                    _queueBindingSource.DataSource = null;
                    _queueBindingSource.DataSource = viewModels;
                }

                // 2. Синхронизируем панель файлов с очередью
                var currentFiles = filesPanel.Controls.OfType<FileListItem>()
                    .Select(f => f.FilePath)
                    .ToList();

                var queueFiles = e.QueueItems
                    .Select(q => q.FilePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .ToList();

                var newFiles = queueFiles
                    .Except(currentFiles, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var removedFiles = currentFiles
                    .Except(queueFiles, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Удаляем файлы, которых больше нет в очереди
                foreach (var filePath in removedFiles)
                {
                    var fileItem = filesPanel.Controls.OfType<FileListItem>()
                        .FirstOrDefault(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

                    if (fileItem != null)
                    {
                        filesPanel.Controls.Remove(fileItem);
                        fileItem.Dispose();
                    }
                }

                // Добавляем новые файлы через существующую логику (с превью и обработчиками)
                if (newFiles.Length > 0)
                {
                    AddFilesToList(newFiles, syncDragDropPanel: false);
                }

                // 3. Обновляем состояние UI
                UpdateEditorButtonState();
                UpdateShareButtonState();
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка при обновлении интерфейса очереди: {ex.Message}");
            }
        }

        #endregion

        private void OnThemeControlsResize(object? sender, EventArgs e) => PositionThemeControls();

        private void PositionThemeControls()
        {
            if (_themeMenuButton == null || _themeSelector == null)
            {
                return;
            }

            var padding = 10;
            var buttonX = Math.Max(padding, ClientSize.Width - _themeMenuButton.Width - padding);
            _themeMenuButton.Location = new Point(buttonX, padding);
            _themeMenuButton.BringToFront();

            var selectorX = Math.Max(padding, buttonX - _themeSelector.Width - 10);
            var selectorY = _themeMenuButton.Bottom + 5;
            _themeSelector.Location = new Point(selectorX, selectorY);
            _themeSelector.BringToFront();
        }

        // Обработчики для кнопок удаления файлов - нормализованные через единый источник правды
        private void OnRemoveSelectedFilesRequested(object? sender, EventArgs e)
        {
            try
            {
                // Делегируем удаление в MainPresenter (основная очередь)
                if (_mainPresenter != null)
                {
                    // Сначала синхронизируем UI-файлы с выбранными элементами очереди
                    if (_queueItemsBinding != null)
                    {
                        var selectedItems = _queueItemsBinding
                            .Where(item => item.IsSelected)
                            .ToList();

                        foreach (var vm in selectedItems)
                        {
                            if (!string.IsNullOrWhiteSpace(vm.FilePath))
                            {
                                RemoveFileByPath(vm.FilePath);
                            }
                        }
                    }

                    _mainPresenter.OnRemoveSelectedFilesRequested();
                    return;
                }

                // Fallback: используем нормализованный метод
                RemoveSelectedFilesFromQueue();
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка при удалении выбранных файлов: {ex.Message}");
            }
        }

        private void OnClearAllFilesRequested(object? sender, EventArgs e)
        {
            try
            {
                // Делегируем очистку в MainPresenter (основная очередь)
                if (_mainPresenter != null)
                {
                    _mainPresenter.OnClearAllFilesRequested();
                    return;
                }

                // Fallback: используем нормализованный метод
                ClearQueue();
                ClearAllFiles();
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка при очистке файлов: {ex.Message}");
            }
        }

        #region Async Event Invocation Methods

        public async Task RaiseAddFilesRequestedAsync()
        {
            if (AddFilesRequestedAsync == null)
            {
                return;
            }

            var handlers = AddFilesRequestedAsync
                .GetInvocationList()
                .Cast<Func<Task>>()
                .Select(h => h())
                .ToArray();

            if (handlers.Length == 0)
            {
                return;
            }

            await Task.WhenAll(handlers).ConfigureAwait(false);
        }

        public async Task RaiseStartConversionRequestedAsync()
        {
            if (StartConversionRequestedAsync == null)
            {
                return;
            }

            var handlers = StartConversionRequestedAsync
                .GetInvocationList()
                .Cast<Func<Task>>()
                .Select(h => h())
                .ToArray();

            if (handlers.Length == 0)
            {
                return;
            }

            await Task.WhenAll(handlers).ConfigureAwait(false);
        }

        public async Task RaiseCancelConversionRequestedAsync()
        {
            if (CancelConversionRequestedAsync == null)
            {
                return;
            }

            var handlers = CancelConversionRequestedAsync
                .GetInvocationList()
                .Cast<Func<Task>>()
                .Select(h => h())
                .ToArray();

            if (handlers.Length == 0)
            {
                return;
            }

            await Task.WhenAll(handlers).ConfigureAwait(false);
        }

        public async Task RaiseFilesDroppedAsync(string[] files)
        {
            if (FilesDroppedAsync == null)
            {
                return;
            }

            var handlers = FilesDroppedAsync
                .GetInvocationList()
                .Cast<Func<string[], Task>>()
                .Select(h => h(files))
                .ToArray();

            if (handlers.Length == 0)
            {
                return;
            }

            await Task.WhenAll(handlers).ConfigureAwait(false);
        }

        public async Task RaiseRemoveSelectedFilesRequestedAsync()
        {
            if (RemoveSelectedFilesRequestedAsync == null)
            {
                return;
            }

            var handlers = RemoveSelectedFilesRequestedAsync
                .GetInvocationList()
                .Cast<Func<Task>>()
                .Select(h => h())
                .ToArray();

            if (handlers.Length == 0)
            {
                return;
            }

            await Task.WhenAll(handlers).ConfigureAwait(false);
        }

        public async Task RaiseClearAllFilesRequestedAsync()
        {
            if (ClearAllFilesRequestedAsync == null)
            {
                return;
            }

            var handlers = ClearAllFilesRequestedAsync
                .GetInvocationList()
                .Cast<Func<Task>>()
                .Select(h => h())
                .ToArray();

            if (handlers.Length == 0)
            {
                return;
            }

            await Task.WhenAll(handlers).ConfigureAwait(false);
        }

        #endregion

        #region Unified Data Source Methods

        /// <summary>
        /// Нормализованный метод для добавления файлов - использует единый источник правды
        /// </summary>
        public void AddFilesToQueue(IEnumerable<string> filePaths)
        {
            if (filePaths == null) return;

            var queueItems = filePaths
                .Where(filePath => File.Exists(filePath))
                .Select(filePath => new Converter.Application.ViewModels.QueueItemViewModel
                {
                    Id = Guid.NewGuid(),
                    FileName = Path.GetFileName(filePath),
                    FilePath = filePath,
                    FileSizeBytes = new FileInfo(filePath).Length,
                    Status = Converter.Domain.Models.ConversionStatus.Pending,
                    Progress = 0
                })
                .ToList();

            // Единый источник правды: QueueItemsBinding
            if (_queueItemsBinding != null)
            {
                foreach (var item in queueItems)
                {
                    _queueItemsBinding.Add(item);
                }
                AppendLog($"Добавлено файлов в очередь: {queueItems.Count}");
            }
        }

        /// <summary>
        /// Нормализованный метод для удаления выбранных файлов
        /// </summary>
        public void RemoveSelectedFilesFromQueue()
        {
            if (_queueItemsBinding == null) return;

            var selectedItems = _queueItemsBinding.Where(item => item.IsSelected).ToList();
            foreach (var item in selectedItems)
            {
                _queueItemsBinding.Remove(item);
            }
            AppendLog($"Удалено файлов из очереди: {selectedItems.Count}");
        }

        /// <summary>
        /// Нормализованный метод для очистки всей очереди
        /// </summary>
        public void ClearQueue()
        {
            if (_queueItemsBinding != null)
            {
                var count = _queueItemsBinding.Count;
                _queueItemsBinding.Clear();
                AppendLog($"Очищена очередь: {count} файлов");
            }
        }

        /// <summary>
        /// Нормализованный метод для обновления прогресса элемента
        /// </summary>
        public void UpdateQueueItemProgress(Guid itemId, int progress, string status = null)
        {
            if (_queueItemsBinding == null) return;

            var item = _queueItemsBinding.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                item.Progress = progress;
                if (!string.IsNullOrEmpty(status))
                {
                    // Можно добавить поле StatusText в QueueItemViewModel если нужно
                }
            }
        }

        #endregion
    }
}
