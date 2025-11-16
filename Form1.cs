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

namespace Converter
{
    public partial class Form1 : Form, IMainView
    {
        private ThemeSelectorControl? _themeSelector;
        private Button? _themeMenuButton;
        private bool _themeInitialized;

        // IMainView events
        public event EventHandler? AddFilesRequested;
        public event EventHandler? StartConversionRequested;
        public event EventHandler? CancelConversionRequested;
        public event EventHandler<Converter.Models.ConversionProfile>? PresetSelected;
        public event EventHandler? SettingsChanged;

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

        public Form1()
        {
            InitializeComponent();
            _notificationSettings = LoadNotificationSettings();
            _notificationService = new NotificationService(_notificationSettings);
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
                if (btnStart != null) btnStart.Enabled = !isBusy;
                if (btnAddFiles != null) btnAddFiles.Enabled = !isBusy;
                if (btnStop != null) btnStop.Enabled = isBusy;
            }
            catch { }
        }
        // IMainView: queue view API (bridge to existing queue UI)
        public void AddQueueItem(QueueItem item)
        {
            // Reuse existing queue UI: behave like external "item added" event
            if (InvokeRequired) { BeginInvoke(new Action(() => AddQueueItem(item))); return; }
            if (_queueManager != null)
            {
                _queueManager.AddItem(item);
                RefreshQueueDisplay();
            }
        }

        public void UpdateQueueItem(QueueItem item)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateQueueItem(item))); return; }

            if (_queueManager != null)
            {
                _queueManager.UpdateItem(item.Id, q =>
                {
                    q.Status = item.Status;
                    q.Progress = item.Progress;
                    q.ErrorMessage = item.ErrorMessage;
                    q.OutputPath = item.OutputPath;
                    q.OutputFileSizeBytes = item.OutputFileSizeBytes;
                });
                RefreshQueueDisplay();
            }
        }

        public void UpdateQueueItemProgress(Guid itemId, int progress)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateQueueItemProgress(itemId, progress))); return; }

            if (_queueManager != null)
            {
                _queueManager.UpdateItem(itemId, q => q.Progress = progress);
                RefreshQueueDisplay();
            }
        }

        public void RemoveQueueItem(Guid itemId)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => RemoveQueueItem(itemId))); return; }

            _queueManager?.RemoveItem(itemId);
            RefreshQueueDisplay();
        }

        public void UpdateQueue(IEnumerable<QueueItem> items)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateQueue(items))); return; }

            if (_queueManager != null)
            {
                // Сбросить очередь и добавить новые элементы как Pending
                _queueManager.ClearQueue();
                _queueManager.AddItems(items);
                RefreshQueueDisplay();
            }
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

            ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
            ThemeManager.Instance.ApplyTheme(this);
            UpdateCustomControlsTheme(ThemeManager.Instance.CurrentTheme);

            _themeSelector = new ThemeSelectorControl
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

            ThemeManager.Instance.ApplyTheme(this);
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

            if (_queueItemsPanel != null)
            {
                foreach (var control in _queueItemsPanel.Controls.OfType<QueueItemControl>())
                {
                    control.BackColor = theme["Surface"];
                    control.ForeColor = theme["TextPrimary"];
                    control.Refresh();
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
            try
            {
                _queueManager?.StopQueue();
            }
            catch { }

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
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            catch { }

            try
            {
                _thumbnailService?.Dispose();
            }
            catch { }

            _notificationService?.Dispose();
            base.OnFormClosed(e);
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
