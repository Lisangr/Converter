using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Models;
using Converter.Services;
using Converter.UI.Dialogs;

namespace Converter.UI.Controls
{
    public class ThemeSelectorControl : UserControl
    {
        private readonly ComboBox _themeCombo;
        private readonly CheckBox _chkAutoSwitch;
        private readonly CheckBox _chkAnimations;
        private readonly Button _btnSettings;
        private readonly Panel _previewPanel;
        private readonly List<Theme> _themes;
        private readonly EventHandler<Theme> _themeChangedHandler;

        public ThemeSelectorControl()
        {
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
                Checked = ThemeManager.Instance.EnableAnimations
            };
            _chkAnimations.CheckedChanged += (s, e) =>
            {
                ThemeManager.Instance.EnableAnimations = _chkAnimations.Checked;
            };

            _chkAutoSwitch = new CheckBox
            {
                Text = "🌓 Автоматическое переключение",
                Location = new Point(10, 140),
                AutoSize = true,
                Checked = ThemeManager.Instance.AutoSwitchEnabled
            };
            _chkAutoSwitch.CheckedChanged += (s, e) =>
            {
                ThemeManager.Instance.EnableAutoSwitch(_chkAutoSwitch.Checked);
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

            _themeChangedHandler = (_, theme) => OnThemeManagerThemeChanged(theme);
            ThemeManager.Instance.ThemeChanged += _themeChangedHandler;

            LoadCurrentSettings();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.Instance.ThemeChanged -= _themeChangedHandler;
            }

            base.Dispose(disposing);
        }

        private void LoadCurrentSettings()
        {
            var currentTheme = ThemeManager.Instance.CurrentTheme;
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
            if (_themeCombo.SelectedIndex < 0 || _themeCombo.SelectedIndex >= _themes.Count)
            {
                return;
            }

            var selectedTheme = _themes[_themeCombo.SelectedIndex];
            ThemeManager.Instance.SetTheme(selectedTheme);
        }

        private void OnThemeManagerThemeChanged(Theme theme)
        {
            var index = _themes.FindIndex(t => string.Equals(t.Name, theme.Name, StringComparison.OrdinalIgnoreCase));
            if (index >= 0 && _themeCombo.SelectedIndex != index)
            {
                _themeCombo.SelectedIndexChanged -= OnThemeChanged;
                _themeCombo.SelectedIndex = index;
                _themeCombo.SelectedIndexChanged += OnThemeChanged;
            }

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var theme = ThemeManager.Instance.CurrentTheme;
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
