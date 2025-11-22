using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.UI.Dialogs;

namespace Converter.UI.Controls
{
    public class ThemeSelectorControl : UserControl
    {
        private readonly IThemeService _themeService;
        private readonly ComboBox _themeCombo;
        private readonly Panel _previewPanel;
        private readonly CheckBox _chkAnimations;
        private readonly CheckBox _chkAutoSwitch;
        private readonly Button _btnSettings;
        private readonly List<Theme> _themes;
        private readonly System.Windows.Forms.Timer _manualSelectionTimer;
        private bool _isManualSelection;

        public ThemeSelectorControl(IThemeService themeService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _themeService.ThemeChanged += OnThemeChangedFromService;
            
            DoubleBuffered = true;
            _themes = Theme.GetAllThemes();
            
            // Timer to prevent feedback loops
            _manualSelectionTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _manualSelectionTimer.Tick += (s, e) =>
            {
                _isManualSelection = false;
                _manualSelectionTimer.Stop();
            };
            _isManualSelection = false;

            Size = new Size(320, 170);
            Padding = new Padding(10);

            var lblTheme = new Label
            {
                Text = "🎨 Тема оформления:",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            _themeCombo = new ComboBox
            {
                Location = new Point(10, 35),
                Size = new Size(295, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _themeCombo.SelectedIndexChanged += OnThemeChanged;

            foreach (var theme in _themes)
            {
                _themeCombo.Items.Add(theme.DisplayName);
            }

            _previewPanel = new Panel
            {
                Location = new Point(10, 70),
                Size = new Size(295, 35),
                BorderStyle = BorderStyle.FixedSingle
            };

            var previewLabel = new Label
            {
                Text = "Aa Текст 123",
                Location = new Point(5, 7),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            _previewPanel.Controls.Add(previewLabel);

            _chkAnimations = new CheckBox
            {
                Text = "✨ Анимация переходов",
                Location = new Point(10, 115),
                AutoSize = true,
                Checked = true // Will be updated in LoadCurrentSettings
            };
            _chkAnimations.CheckedChanged += (s, e) =>
            {
                _themeService.EnableAnimations = _chkAnimations.Checked;
            };

            _chkAutoSwitch = new CheckBox
            {
                Text = "🌓 Автоматическое переключение",
                Location = new Point(10, 140),
                AutoSize = true,
                Checked = false // Will be updated in LoadCurrentSettings
            };
            _chkAutoSwitch.CheckedChanged += async (s, e) =>
            {
                await _themeService.EnableAutoSwitchAsync(_chkAutoSwitch.Checked);
            };

            _btnSettings = new Button
            {
                Text = "⚙️",
                Location = new Point(270, 135),
                Size = new Size(35, 25),
                FlatStyle = FlatStyle.Flat
            };
            _btnSettings.FlatAppearance.BorderSize = 0;
            _btnSettings.Click += (s, e) =>
            {
                using var dialog = new ThemeSettingsDialog(_themeService);
                dialog.ShowDialog(this);
            };

            Controls.AddRange(new Control[]
            {
                lblTheme,
                _themeCombo,
                _previewPanel,
                _chkAnimations,
                _chkAutoSwitch,
                _btnSettings
            });

            // Load initial settings
            LoadCurrentSettings();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_themeService != null)
                {
                    _themeService.ThemeChanged -= OnThemeChangedFromService;
                }
                _manualSelectionTimer?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void LoadCurrentSettings()
        {
            var currentTheme = _themeService.CurrentTheme;
            var index = _themes.FindIndex(t => string.Equals(t.Name, currentTheme.Name, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _themeCombo.SelectedIndex = index;
            }
            else if (_themeCombo.Items.Count > 0)
            {
                _themeCombo.SelectedIndex = 0;
            }

            UpdatePreview();
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            _ = OnThemeChangedAsync(sender, e);
        }

        private async Task OnThemeChangedAsync(object? sender, EventArgs e)
        {
            if (_themeCombo.SelectedIndex < 0 || _themeCombo.SelectedIndex >= _themes.Count)
            {
                return;
            }

            _isManualSelection = true;
            var selectedTheme = _themes[_themeCombo.SelectedIndex];
            await _themeService.SetTheme(selectedTheme);
            
            // Reset flag after a short delay
            _manualSelectionTimer.Start();
        }
        
        private void OnThemeChangedFromService(object? sender, Theme theme)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnThemeChangedFromService(sender, theme)));
                return;
            }
            
            var index = _themes.FindIndex(t => string.Equals(t.Name, theme.Name, StringComparison.OrdinalIgnoreCase));
            if (index >= 0 && index != _themeCombo.SelectedIndex)
            {
                _themeCombo.SelectedIndex = index;
            }
            
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var theme = _themeService.CurrentTheme;
            _previewPanel.BackColor = theme["Background"];

            foreach (Control control in _previewPanel.Controls)
            {
                if (control is Label lbl)
                {
                    lbl.ForeColor = theme["TextPrimary"];
                }
            }
        }
    }
}
