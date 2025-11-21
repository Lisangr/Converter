using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Application.Models;

namespace Converter.UI.Dialogs
{
    public class ThemeSettingsDialog : Form
    {
        private readonly DateTimePicker _timeDarkStart;
        private readonly DateTimePicker _timeDarkEnd;
        private readonly ComboBox _comboDarkTheme;
        private readonly NumericUpDown _numAnimationSpeed;
        private readonly Button _btnSave;
        private readonly Button _btnCancel;
        private readonly List<Theme> _darkThemes;
        private readonly IThemeService _themeService;

        public ThemeSettingsDialog(IThemeService themeService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            Text = "Настройки темы";
            Size = new Size(460, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _darkThemes = Theme.GetAllThemes()
                .Where(t => t.Name.Contains("dark", StringComparison.OrdinalIgnoreCase) || t.Name == "midnight")
                .ToList();

            var lblAutoSwitch = new Label
            {
                Text = "⏰ Автоматическое переключение",
                Location = new Point(20, 20),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            var lblDarkStart = new Label
            {
                Text = "Включать темную тему с:",
                Location = new Point(20, 55),
                AutoSize = true
            };

            _timeDarkStart = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Location = new Point(220, 52),
                Size = new Size(120, 25)
            };

            var lblDarkEnd = new Label
            {
                Text = "Выключать темную тему в:",
                Location = new Point(20, 85),
                AutoSize = true
            };

            _timeDarkEnd = new DateTimePicker
            {
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Location = new Point(220, 82),
                Size = new Size(120, 25)
            };

            var lblDarkTheme = new Label
            {
                Text = "Темная тема по умолчанию:",
                Location = new Point(20, 115),
                AutoSize = true
            };

            _comboDarkTheme = new ComboBox
            {
                Location = new Point(220, 112),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var theme in _darkThemes)
            {
                _comboDarkTheme.Items.Add(theme.DisplayName);
            }

            var lblAnimations = new Label
            {
                Text = "✨ Анимации",
                Location = new Point(20, 160),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            var lblAnimSpeed = new Label
            {
                Text = "Скорость анимации (мс):",
                Location = new Point(20, 195),
                AutoSize = true
            };

            _numAnimationSpeed = new NumericUpDown
            {
                Location = new Point(220, 192),
                Size = new Size(100, 25),
                Minimum = 100,
                Maximum = 1000,
                Increment = 50
            };

            var lblHint = new Label
            {
                Text = "💡 Совет: меньшее значение = быстрее, больше = плавнее",
                Location = new Point(20, 225),
                Size = new Size(400, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };

            _btnSave = new Button
            {
                Text = "💾 Сохранить",
                Location = new Point(150, 275),
                Size = new Size(120, 35),
                DialogResult = DialogResult.OK
            };
            _btnSave.Click += OnSave;

            _btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(280, 275),
                Size = new Size(120, 35),
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(new Control[]
            {
                lblAutoSwitch,
                lblDarkStart,
                _timeDarkStart,
                lblDarkEnd,
                _timeDarkEnd,
                lblDarkTheme,
                _comboDarkTheme,
                lblAnimations,
                lblAnimSpeed,
                _numAnimationSpeed,
                lblHint,
                _btnSave,
                _btnCancel
            });

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;

            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var service = _themeService;
            _timeDarkStart.Value = DateTime.Today.Add(service.DarkModeStart);
            _timeDarkEnd.Value = DateTime.Today.Add(service.DarkModeEnd);
            _numAnimationSpeed.Value = service.AnimationDuration;

            if (_darkThemes.Count == 0)
            {
                return;
            }

            var index = _darkThemes.FindIndex(t => string.Equals(t.Name, service.PreferredDarkTheme, StringComparison.OrdinalIgnoreCase));
            _comboDarkTheme.SelectedIndex = Math.Max(0, index);
        }

        private void OnSave(object? sender, EventArgs e)
        {
            var service = _themeService;
            service.DarkModeStart = _timeDarkStart.Value.TimeOfDay;
            service.DarkModeEnd = _timeDarkEnd.Value.TimeOfDay;
            service.AnimationDuration = (int)_numAnimationSpeed.Value;

            if (_comboDarkTheme.SelectedIndex >= 0 && _comboDarkTheme.SelectedIndex < _darkThemes.Count)
            {
                service.PreferredDarkTheme = _darkThemes[_comboDarkTheme.SelectedIndex].Name;
            }

            Close();
        }
    }
}
