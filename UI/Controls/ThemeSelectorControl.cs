using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Models;
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

        public ThemeSelectorControl(IThemeService themeService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _themeService.ThemeChanged += OnThemeChangedFromService;
            
            DoubleBuffered = true;
            _themes = Theme.GetAllThemes();

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
                Checked = true // Default value, will be updated in LoadCurrentSettings
            };
            _chkAnimations.CheckedChanged += (s, e) =>
            {
                // Animation settings would need to be moved to IThemeService if needed
                // For now, we'll leave this as is since it's not critical for the theme switching
            };

            _chkAutoSwitch = new CheckBox
            {
                Text = "🌓 Автоматическое переключение",
                Location = new Point(10, 140),
                AutoSize = true,
                Checked = false // Default value, will be updated in LoadCurrentSettings
            };
            _chkAutoSwitch.CheckedChanged += (s, e) =>
            {
                // Auto-switch settings would need to be moved to IThemeService if needed
                // For now, we'll leave this as is since it's not critical for the theme switching
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
                using var dialog = new ThemeSettingsDialog();
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

        private async void OnThemeChanged(object? sender, EventArgs e)
        {
            if (_themeCombo.SelectedIndex < 0 || _themeCombo.SelectedIndex >= _themes.Count)
            {
                return;
            }

            var selectedTheme = _themes[_themeCombo.SelectedIndex];
            await _themeService.SetTheme(selectedTheme);
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

        private void OnThemeManagerThemeChanged(Theme theme)
        {
            // This method is kept for backward compatibility
            // but should no longer be used directly
            OnThemeChangedFromService(this, theme);
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
