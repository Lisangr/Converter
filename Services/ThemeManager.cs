using System;
using System.Drawing;
using System.Windows.Forms;
using Converter.Models;

namespace Converter.Services
{
    public delegate void ThemeChangedEventHandler(object? sender, Theme theme);

    /// <summary>
    /// Управляет текущей темой и применяет её к контролам.
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        private System.Windows.Forms.Timer? _transitionTimer;
        private float _transitionProgress;
        private System.Windows.Forms.Timer? _autoSwitchTimer;

        private ThemeManager()
        {
            CurrentTheme = LoadSavedTheme();
        }

        internal ThemeManager(bool loadSavedTheme)
        {
            CurrentTheme = loadSavedTheme ? LoadSavedTheme() : Theme.Light;
        }

        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public Theme CurrentTheme { get; private set; }

        public bool EnableThemeAnimation { get; set; } = true;

        public event ThemeChangedEventHandler? ThemeChanged;

        public void SetTheme(Theme theme)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));

            StopTransitionTimer();
            CurrentTheme = CloneTheme(theme);
            SaveTheme(CurrentTheme);
            ThemeChanged?.Invoke(this, CurrentTheme);
        }

        public void ToggleTheme()
        {
            var nextTheme = string.Equals(CurrentTheme.Name, Theme.Light.Name, StringComparison.Ordinal)
                ? Theme.Dark
                : Theme.Light;

            if (EnableThemeAnimation)
            {
                SetThemeWithAnimation(nextTheme);
            }
            else
            {
                SetTheme(nextTheme);
            }
        }

        public void SetThemeWithAnimation(Theme newTheme, int durationMs = 300)
        {
            if (!EnableThemeAnimation || durationMs <= 0)
            {
                SetTheme(newTheme);
                return;
            }

            var startTheme = CloneTheme(CurrentTheme);
            var endTheme = CloneTheme(newTheme);

            StopTransitionTimer();

            _transitionProgress = 0f;
            _transitionTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _transitionTimer.Tick += (s, e) =>
            {
                _transitionProgress += 16f / durationMs;
                if (_transitionProgress >= 1f)
                {
                    StopTransitionTimer();
                    SetTheme(endTheme);
                    return;
                }

                CurrentTheme = InterpolateTheme(startTheme, endTheme, _transitionProgress);
                ThemeChanged?.Invoke(this, CurrentTheme);
            };
            _transitionTimer.Start();
        }

        public void EnableAutoSwitch(bool enable)
        {
            if (enable)
            {
                if (_autoSwitchTimer == null)
                {
                    _autoSwitchTimer = new System.Windows.Forms.Timer { Interval = 60000 };
                    _autoSwitchTimer.Tick += AutoSwitchTimerOnTick;
                }

                _autoSwitchTimer.Start();
                CheckTimeAndSwitch();
            }
            else
            {
                if (_autoSwitchTimer != null)
                {
                    _autoSwitchTimer.Stop();
                    _autoSwitchTimer.Tick -= AutoSwitchTimerOnTick;
                    _autoSwitchTimer.Dispose();
                    _autoSwitchTimer = null;
                }
            }
        }

        public void ApplyTheme(Form form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            form.BackColor = CurrentTheme.BackgroundPrimary;
            form.ForeColor = CurrentTheme.TextPrimary;
            ApplyThemeToControls(form.Controls);
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control.Tag?.ToString() == "CustomColor")
                {
                    if (control.HasChildren)
                    {
                        ApplyThemeToControls(control.Controls);
                    }
                    continue;
                }

                switch (control)
                {
                    case Button btn:
                        ApplyButtonTheme(btn);
                        break;
                    case TextBox tb:
                        tb.BackColor = CurrentTheme.BackgroundSecondary;
                        tb.ForeColor = CurrentTheme.TextPrimary;
                        tb.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case ComboBox cb:
                        cb.BackColor = CurrentTheme.BackgroundSecondary;
                        cb.ForeColor = CurrentTheme.TextPrimary;
                        cb.FlatStyle = FlatStyle.Flat;
                        break;
                    case Label lbl:
                        lbl.ForeColor = lbl.Font.Bold ? CurrentTheme.TextPrimary : CurrentTheme.TextSecondary;
                        break;
                    case GroupBox gb:
                        gb.ForeColor = CurrentTheme.TextPrimary;
                        gb.BackColor = CurrentTheme.BackgroundSecondary;
                        break;
                    case TabPage tabPage:
                        tabPage.BackColor = CurrentTheme.BackgroundSecondary;
                        tabPage.ForeColor = CurrentTheme.TextPrimary;
                        break;
                    case Panel panel:
                        panel.BackColor = CurrentTheme.BackgroundSecondary;
                        break;
                    case TabControl tab:
                        tab.BackColor = CurrentTheme.BackgroundPrimary;
                        tab.ForeColor = CurrentTheme.TextPrimary;
                        break;
                    case DataGridView dgv:
                        ApplyDataGridViewTheme(dgv);
                        break;
                    case SplitContainer split:
                        split.BackColor = CurrentTheme.BackgroundPrimary;
                        ApplyThemeToControls(split.Panel1.Controls);
                        ApplyThemeToControls(split.Panel2.Controls);
                        break;
                    default:
                        control.BackColor = CurrentTheme.BackgroundPrimary;
                        control.ForeColor = CurrentTheme.TextPrimary;
                        break;
                }

                if (!(control is SplitContainer) && control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls);
                }
            }
        }

        private void ApplyButtonTheme(Button btn)
        {
            if (btn.Tag?.ToString() == "AccentButton")
            {
                btn.BackColor = CurrentTheme.Accent;
                btn.ForeColor = Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
            }
            else
            {
                btn.BackColor = CurrentTheme.BackgroundSecondary;
                btn.ForeColor = CurrentTheme.TextPrimary;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = CurrentTheme.Border;
                btn.FlatAppearance.BorderSize = 1;
            }
        }

        private void ApplyDataGridViewTheme(DataGridView dgv)
        {
            dgv.BackgroundColor = CurrentTheme.BackgroundPrimary;
            dgv.DefaultCellStyle.BackColor = CurrentTheme.BackgroundSecondary;
            dgv.DefaultCellStyle.ForeColor = CurrentTheme.TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = CurrentTheme.Accent;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = CurrentTheme.BackgroundPrimary;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = CurrentTheme.TextPrimary;
            dgv.EnableHeadersVisualStyles = false;
            dgv.GridColor = CurrentTheme.Border;
        }

        private void CheckTimeAndSwitch()
        {
            var hour = DateTime.Now.Hour;
            var shouldBeDark = hour >= 20 || hour < 7;
            var targetTheme = shouldBeDark ? Theme.Dark : Theme.Light;

            if (!string.Equals(CurrentTheme.Name, targetTheme.Name, StringComparison.Ordinal))
            {
                SetTheme(targetTheme);
            }
        }

        private void AutoSwitchTimerOnTick(object? sender, EventArgs e) => CheckTimeAndSwitch();

        private void StopTransitionTimer()
        {
            if (_transitionTimer != null)
            {
                _transitionTimer.Stop();
                _transitionTimer.Dispose();
                _transitionTimer = null;
            }
        }

        private Theme LoadSavedTheme()
        {
            var themeName = Properties.Settings.Default.Theme ?? "Light";
            return string.Equals(themeName, "Dark", StringComparison.OrdinalIgnoreCase) ? Theme.Dark : Theme.Light;
        }

        private void SaveTheme(Theme theme)
        {
            Properties.Settings.Default.Theme = theme.Name;
            Properties.Settings.Default.Save();
        }

        private static Theme CloneTheme(Theme theme)
        {
            return new Theme
            {
                Name = theme.Name,
                BackgroundPrimary = theme.BackgroundPrimary,
                BackgroundSecondary = theme.BackgroundSecondary,
                TextPrimary = theme.TextPrimary,
                TextSecondary = theme.TextSecondary,
                Accent = theme.Accent,
                Border = theme.Border,
                Success = theme.Success,
                Error = theme.Error,
                Warning = theme.Warning
            };
        }

        private static Theme InterpolateTheme(Theme from, Theme to, float progress)
        {
            progress = Math.Clamp(progress, 0f, 1f);
            return new Theme
            {
                Name = to.Name,
                BackgroundPrimary = InterpolateColor(from.BackgroundPrimary, to.BackgroundPrimary, progress),
                BackgroundSecondary = InterpolateColor(from.BackgroundSecondary, to.BackgroundSecondary, progress),
                TextPrimary = InterpolateColor(from.TextPrimary, to.TextPrimary, progress),
                TextSecondary = InterpolateColor(from.TextSecondary, to.TextSecondary, progress),
                Accent = InterpolateColor(from.Accent, to.Accent, progress),
                Border = InterpolateColor(from.Border, to.Border, progress),
                Success = InterpolateColor(from.Success, to.Success, progress),
                Error = InterpolateColor(from.Error, to.Error, progress),
                Warning = InterpolateColor(from.Warning, to.Warning, progress)
            };
        }

        private static Color InterpolateColor(Color from, Color to, float progress)
        {
            return Color.FromArgb(
                (int)(from.A + (to.A - from.A) * progress),
                (int)(from.R + (to.R - from.R) * progress),
                (int)(from.G + (to.G - from.G) * progress),
                (int)(from.B + (to.B - from.B) * progress));
        }
    }
}

