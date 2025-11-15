using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Models;

namespace Converter.Services
{
    public delegate void ThemeTransitionProgressEventHandler(object? sender, float progress);

    /// <summary>
    /// Управляет текущей темой, анимациями и автоматическим переключением.
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        public Theme CurrentTheme { get; private set; }
        public event EventHandler<Theme>? ThemeChanged;
        public event ThemeTransitionProgressEventHandler? ThemeTransitionProgress;

        private Timer? _transitionTimer;
        private Timer? _autoSwitchTimer;
        private Theme? _targetTheme;
        private Theme? _sourceTheme;
        private float _transitionProgress;

        private bool _enableAnimations = true;
        private int _animationDuration = 300;
        private TimeSpan _darkModeStart = new(20, 0, 0);
        private TimeSpan _darkModeEnd = new(7, 0, 0);
        private string _preferredDarkTheme = "dark";

        public bool EnableAnimations
        {
            get => _enableAnimations;
            set
            {
                if (_enableAnimations == value) return;
                _enableAnimations = value;
                SaveSettings();
            }
        }

        public int AnimationDuration
        {
            get => _animationDuration;
            set
            {
                var clamped = Math.Max(100, value);
                if (_animationDuration == clamped) return;
                _animationDuration = clamped;
                SaveSettings();
            }
        }

        public bool AutoSwitchEnabled { get; private set; }

        public TimeSpan DarkModeStart
        {
            get => _darkModeStart;
            set
            {
                if (_darkModeStart == value) return;
                _darkModeStart = value;
                SaveSettings();
            }
        }

        public TimeSpan DarkModeEnd
        {
            get => _darkModeEnd;
            set
            {
                if (_darkModeEnd == value) return;
                _darkModeEnd = value;
                SaveSettings();
            }
        }

        public string PreferredDarkTheme
        {
            get => _preferredDarkTheme;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                if (string.Equals(_preferredDarkTheme, value, StringComparison.OrdinalIgnoreCase)) return;
                _preferredDarkTheme = value;
                SaveSettings();
            }
        }

        private ThemeManager()
        {
            LoadSettings();
            CurrentTheme = LoadSavedTheme();
            InitializeAutoSwitch();
        }

        public void SetTheme(Theme theme, bool animate = true)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            if (CurrentTheme != null && string.Equals(theme.Name, CurrentTheme.Name, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (animate && EnableAnimations)
            {
                if (AnimationDuration <= 0)
                {
                    AnimationDuration = 300;
                }

                StartThemeTransition(CurrentTheme, theme);
            }
            else
            {
                CurrentTheme = theme;
                SaveTheme(theme);
                ThemeChanged?.Invoke(this, CurrentTheme);
            }
        }

        private void StartThemeTransition(Theme? from, Theme to)
        {
            _sourceTheme = from ?? to;
            _targetTheme = to;
            _transitionProgress = 0f;

            _transitionTimer?.Stop();
            _transitionTimer?.Dispose();
            _transitionTimer = new Timer { Interval = 16 };
            _transitionTimer.Tick += (s, e) =>
            {
                _transitionProgress += 16f / AnimationDuration;
                if (_transitionProgress >= 1f)
                {
                    _transitionProgress = 1f;
                    _transitionTimer?.Stop();
                    CurrentTheme = _targetTheme!;
                    SaveTheme(_targetTheme!);
                }
                else
                {
                    CurrentTheme = InterpolateThemes(_sourceTheme!, _targetTheme!, _transitionProgress);
                }

                ThemeChanged?.Invoke(this, CurrentTheme);
                ThemeTransitionProgress?.Invoke(this, _transitionProgress);

                if (_transitionProgress >= 1f)
                {
                    _transitionTimer?.Dispose();
                    _transitionTimer = null;
                }
            };

            _transitionTimer.Start();
        }

        private Theme InterpolateThemes(Theme from, Theme to, float progress)
        {
            progress = Math.Clamp(progress, 0f, 1f);
            var interpolated = new Theme
            {
                Name = to.Name,
                DisplayName = to.DisplayName,
                Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            };

            foreach (var key in to.Colors.Keys)
            {
                var fromColor = from.Colors.TryGetValue(key, out var colorFrom) ? colorFrom : to.Colors[key];
                interpolated.Colors[key] = InterpolateColor(fromColor, to.Colors[key], progress);
            }

            return interpolated;
        }

        private Color InterpolateColor(Color from, Color to, float progress)
        {
            var t = progress < 0.5f
                ? 4f * progress * progress * progress
                : 1f - (float)Math.Pow(-2f * progress + 2f, 3f) / 2f;

            return Color.FromArgb(
                (int)(from.A + (to.A - from.A) * t),
                (int)(from.R + (to.R - from.R) * t),
                (int)(from.G + (to.G - from.G) * t),
                (int)(from.B + (to.B - from.B) * t));
        }

        public void ApplyTheme(Form form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            form.BackColor = CurrentTheme["Background"];
            form.ForeColor = CurrentTheme["TextPrimary"];

            ApplyThemeToControls(form.Controls);
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control.Tag?.ToString() == "NoTheme")
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
                        tb.BackColor = CurrentTheme["Surface"];
                        tb.ForeColor = CurrentTheme["TextPrimary"];
                        tb.BorderStyle = BorderStyle.FixedSingle;
                        break;

                    case ComboBox cb:
                        cb.BackColor = CurrentTheme["Surface"];
                        cb.ForeColor = CurrentTheme["TextPrimary"];
                        cb.FlatStyle = FlatStyle.Flat;
                        break;

                    case Label lbl:
                        if (lbl.Tag?.ToString() == "Title")
                        {
                            lbl.ForeColor = CurrentTheme["TextPrimary"];
                        }
                        else if (lbl.Tag?.ToString() == "Subtitle")
                        {
                            lbl.ForeColor = CurrentTheme["TextSecondary"];
                        }
                        else
                        {
                            lbl.ForeColor = lbl.Font.Bold
                                ? CurrentTheme["TextPrimary"]
                                : CurrentTheme["TextSecondary"];
                        }
                        break;

                    case Panel panel:
                        panel.BackColor = panel.Tag?.ToString() == "Surface"
                            ? CurrentTheme["Surface"]
                            : CurrentTheme["BackgroundSecondary"];
                        break;

                    case GroupBox gb:
                        gb.ForeColor = CurrentTheme["TextPrimary"];
                        gb.BackColor = CurrentTheme["BackgroundSecondary"];
                        break;

                    case TabControl tab:
                        tab.BackColor = CurrentTheme["Background"];
                        tab.ForeColor = CurrentTheme["TextPrimary"];
                        break;

                    case TabPage page:
                        page.BackColor = CurrentTheme["Background"];
                        page.ForeColor = CurrentTheme["TextPrimary"];
                        break;

                    case DataGridView dgv:
                        ApplyDataGridViewTheme(dgv);
                        break;

                    case ProgressBar pb:
                        pb.ForeColor = CurrentTheme["Accent"];
                        break;

                    case StatusStrip ss:
                        ss.BackColor = CurrentTheme["Surface"];
                        ss.ForeColor = CurrentTheme["TextPrimary"];
                        break;

                    case ToolStrip ts:
                        ts.BackColor = CurrentTheme["Surface"];
                        ts.ForeColor = CurrentTheme["TextPrimary"];
                        break;

                    case MenuStrip ms:
                        ms.BackColor = CurrentTheme["Surface"];
                        ms.ForeColor = CurrentTheme["TextPrimary"];
                        break;

                    case SplitContainer split:
                        split.BackColor = CurrentTheme["Background"];
                        ApplyThemeToControls(split.Panel1.Controls);
                        ApplyThemeToControls(split.Panel2.Controls);
                        break;

                    default:
                        control.BackColor = CurrentTheme["Background"];
                        control.ForeColor = CurrentTheme["TextPrimary"];
                        break;
                }

                if (control is not SplitContainer && control.HasChildren)
                {
                    ApplyThemeToControls(control.Controls);
                }
            }
        }

        private void ApplyButtonTheme(Button btn)
        {
            var tag = btn.Tag?.ToString();
            switch (tag)
            {
                case "AccentButton":
                case "PrimaryButton":
                    btn.BackColor = CurrentTheme["Accent"];
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = CurrentTheme["AccentHover"];
                    break;

                case "SuccessButton":
                    btn.BackColor = CurrentTheme["Success"];
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    break;

                case "DangerButton":
                    btn.BackColor = CurrentTheme["Error"];
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    break;

                default:
                    btn.BackColor = CurrentTheme["Surface"];
                    btn.ForeColor = CurrentTheme["TextPrimary"];
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = CurrentTheme["Border"];
                    btn.FlatAppearance.MouseOverBackColor = CurrentTheme["BackgroundSecondary"];
                    break;
            }
        }

        private void ApplyDataGridViewTheme(DataGridView dgv)
        {
            dgv.BackgroundColor = CurrentTheme["Background"];
            dgv.DefaultCellStyle.BackColor = CurrentTheme["Surface"];
            dgv.DefaultCellStyle.ForeColor = CurrentTheme["TextPrimary"];
            dgv.DefaultCellStyle.SelectionBackColor = CurrentTheme["Accent"];
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = CurrentTheme["BackgroundSecondary"];

            dgv.ColumnHeadersDefaultCellStyle.BackColor = CurrentTheme["Surface"];
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = CurrentTheme["TextPrimary"];
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = CurrentTheme["Accent"];

            dgv.EnableHeadersVisualStyles = false;
            dgv.GridColor = CurrentTheme["Border"];
            dgv.BorderStyle = BorderStyle.None;
        }

        private void InitializeAutoSwitch()
        {
            if (AutoSwitchEnabled)
            {
                SetupAutoSwitchTimer();
            }
        }

        private void SetupAutoSwitchTimer()
        {
            _autoSwitchTimer ??= new Timer { Interval = 60000 };
            _autoSwitchTimer.Tick -= AutoSwitchTimerOnTick;
            _autoSwitchTimer.Tick += AutoSwitchTimerOnTick;
            _autoSwitchTimer.Start();
            CheckAndAutoSwitch();
        }

        public void EnableAutoSwitch(bool enable)
        {
            AutoSwitchEnabled = enable;

            if (enable)
            {
                SetupAutoSwitchTimer();
            }
            else if (_autoSwitchTimer != null)
            {
                _autoSwitchTimer.Stop();
            }

            SaveSettings();
        }

        private void AutoSwitchTimerOnTick(object? sender, EventArgs e)
        {
            CheckAndAutoSwitch();
        }

        private void CheckAndAutoSwitch()
        {
            var now = DateTime.Now.TimeOfDay;
            var shouldBeDark = IsDarkModeTime(now);
            var targetThemeName = shouldBeDark ? PreferredDarkTheme : "light";
            var targetTheme = Theme.GetAllThemes().FirstOrDefault(t => t.Name == targetThemeName) ?? Theme.Light;

            if (!string.Equals(CurrentTheme.Name, targetTheme.Name, StringComparison.OrdinalIgnoreCase))
            {
                SetTheme(targetTheme, animate: EnableAnimations);
            }
        }

        private bool IsDarkModeTime(TimeSpan now)
        {
            if (DarkModeStart < DarkModeEnd)
            {
                return now >= DarkModeStart && now < DarkModeEnd;
            }

            return now >= DarkModeStart || now < DarkModeEnd;
        }

        private Theme LoadSavedTheme()
        {
            var themeName = Properties.Settings.Default.ThemeName ?? "light";
            return Theme.GetAllThemes().FirstOrDefault(t => t.Name == themeName) ?? Theme.Light;
        }

        private void SaveTheme(Theme theme)
        {
            Properties.Settings.Default.ThemeName = theme.Name;
            Properties.Settings.Default.Save();
        }

        private void LoadSettings()
        {
            _enableAnimations = Properties.Settings.Default.ThemeAnimations;
            var duration = Properties.Settings.Default.ThemeAnimationDuration;
            _animationDuration = duration > 0 ? duration : 300;
            AutoSwitchEnabled = Properties.Settings.Default.ThemeAutoSwitch;
            _preferredDarkTheme = Properties.Settings.Default.PreferredDarkTheme ?? "dark";

            if (TimeSpan.TryParse(Properties.Settings.Default.DarkModeStart, out var start))
            {
                _darkModeStart = start;
            }

            if (TimeSpan.TryParse(Properties.Settings.Default.DarkModeEnd, out var end))
            {
                _darkModeEnd = end;
            }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.ThemeAnimations = _enableAnimations;
            Properties.Settings.Default.ThemeAnimationDuration = _animationDuration;
            Properties.Settings.Default.ThemeAutoSwitch = AutoSwitchEnabled;
            Properties.Settings.Default.PreferredDarkTheme = _preferredDarkTheme;
            Properties.Settings.Default.DarkModeStart = DarkModeStart.ToString();
            Properties.Settings.Default.DarkModeEnd = DarkModeEnd.ToString();
            Properties.Settings.Default.Save();
        }
    }
}
