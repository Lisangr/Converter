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

        public event EventHandler? AddFilesRequested;
        public event EventHandler? StartConversionRequested;
        public event EventHandler? CancelConversionRequested;
        public event EventHandler<ConversionProfile>? PresetSelected;
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

        public ObservableCollection<ConversionProfile> AvailablePresets { get; set; } = new();

        private ConversionProfile? _selectedPreset;
        public ConversionProfile? SelectedPreset
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

        public void UpdatePresetControls(ConversionProfile preset)
        {
            // minimal placeholder: reflect selected preset name in title/status
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdatePresetControls(preset))); return; }
            AppendLog($"Preset: {preset.Name} · {preset.VideoCodec}/{preset.AudioCodec}");
        }

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

        public void SetQueueItems(IEnumerable<QueueItemDto> items)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SetQueueItems(items))); return; }
            // TODO: map DTOs to UI controls; minimal no-op for now
        }

        public void UpdateQueueItem(QueueItemDto item)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateQueueItem(item))); return; }
            // TODO: update corresponding UI control
        }

        public void SetGlobalProgress(int percent, string status)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SetGlobalProgress(percent, status))); return; }
            try
            {
                if (progressBarTotal != null) progressBarTotal.Value = Math.Max(progressBarTotal.Minimum, Math.Min(progressBarTotal.Maximum, percent));
                if (lblStatusTotal != null) lblStatusTotal.Text = status;
            }
            catch { }
        }

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

        public string? ShowOpenFileDialog(string title, string filter)
        {
            using var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = false
            };
            return dlg.ShowDialog(this) == DialogResult.OK ? dlg.FileName : null;
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
