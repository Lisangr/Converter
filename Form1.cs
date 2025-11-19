using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Models;
using Converter.Services;
using Converter.UI.Controls;
using Converter.Application.Abstractions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Converter
{
    public partial class Form1 : Form, IMainView
    {
        private ThemeSelectorControl? _themeSelector;
        private Button? _themeMenuButton;
        private bool _themeInitialized;
        private readonly IThemeService _themeService;
        private readonly IThemeManager _themeManager;
        private readonly INotificationService _notificationService;
        private readonly IThumbnailProvider _thumbnailProvider;
        private readonly IShareService _shareService;
        private readonly CancellationTokenSource _lifecycleCts = new();
        
        // UI controls for SetBusy method
        private Button? btnStart;
        private Button? btnAddFiles;
        private Button? btnStop;
        private Button? btnRemoveSelected;
        private Button? btnClearAll;
        private Button? btnSavePreset;
        private Button? btnLoadPreset;
        private Button? _btnShare;
        private Button? _btnOpenEditor;
        private Button? _btnNotificationSettings;

        // IMainView events
        public event EventHandler? AddFilesRequested;
        public event EventHandler? StartConversionRequested;
        public event EventHandler? CancelConversionRequested;
        public event EventHandler<Converter.Models.ConversionProfile>? PresetSelected;
        public event EventHandler? SettingsChanged;
        public event EventHandler<string[]>? FilesDropped;
        public event EventHandler? RemoveSelectedFilesRequested;
        public event EventHandler? ClearAllFilesRequested;

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
            IThemeManager themeManager,
            INotificationService notificationService,
            IThumbnailProvider thumbnailProvider,
            IShareService shareService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _thumbnailProvider = thumbnailProvider ?? throw new ArgumentNullException(nameof(thumbnailProvider));
            _shareService = shareService ?? throw new ArgumentNullException(nameof(shareService));

            InitializeComponent();

            _notificationSettings = _notificationService.GetSettings();
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
                
                // Update main buttons
                if (btnStart != null) btnStart.Enabled = !isBusy;
                if (btnAddFiles != null) btnAddFiles.Enabled = !isBusy;
                if (btnStop != null) btnStop.Enabled = isBusy; // Enable stop button when busy
                
                // Update file management buttons
                if (btnRemoveSelected != null) btnRemoveSelected.Enabled = !isBusy;
                if (btnClearAll != null) btnClearAll.Enabled = !isBusy;
                
                // Update other control buttons
                if (_btnShare != null) _btnShare.Enabled = !isBusy;
                if (_btnOpenEditor != null) _btnOpenEditor.Enabled = !isBusy;
                if (_btnNotificationSettings != null) _btnNotificationSettings.Enabled = !isBusy;
                if (btnSavePreset != null) btnSavePreset.Enabled = !isBusy;
                if (btnLoadPreset != null) btnLoadPreset.Enabled = !isBusy;
                
                // Update button appearance based on busy state
                UpdateButtonStates(isBusy);
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

            btnStart.Tag = "AccentButton";

            _themeService.ThemeChanged -= OnThemeChanged;
            _themeService.ThemeChanged += OnThemeChanged;

            // применяем текущую тему ко всей форме
            _themeService.ApplyTheme(this);
            UpdateCustomControlsTheme(_themeService.CurrentTheme);

            _themeSelector = new ThemeSelectorControl(_themeService, _themeManager)
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

        private void UpdateButtonStates(bool isBusy)
        {
            // Visual feedback for button states
            try
            {
                if (btnStart != null)
                {
                    btnStart.BackColor = isBusy ? Color.FromArgb(100, 100, 100) : Color.FromArgb(0, 120, 215);
                }
                
                if (btnStop != null)
                {
                    btnStop.BackColor = isBusy ? Color.FromArgb(180, 50, 50) : Color.FromArgb(100, 100, 100);
                }
            }
            catch { }
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CancelBackgroundOperations();
            base.OnFormClosing(e);
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

            try
            {
                _notificationService.Dispose();
            }
            catch { }

            try
            {
                _themeService.Dispose();
            }
            catch { }

            try
            {
                _thumbnailProvider.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch { }
        }

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
    }
}
