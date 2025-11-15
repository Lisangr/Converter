using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Models;
using Converter.Services;
using Converter.UI.Controls;

namespace Converter
{
    public partial class Form1 : Form
    {
        private ThemeToggleButton? _themeToggle;
        private bool _themeInitialized;

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeThemeSupport()
        {
            if (_themeInitialized || panelLeftTop == null)
            {
                return;
            }

            _themeToggle = new ThemeToggleButton
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            panelLeftTop.Controls.Add(_themeToggle);
            PositionThemeToggle();

            btnStart.Tag = "AccentButton";

            ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
            ThemeManager.Instance.ApplyTheme(this);
            ApplyThemeToDynamicControls(ThemeManager.Instance.CurrentTheme);

            panelLeftTop.Resize -= PanelLeftTopOnResize;
            panelLeftTop.Resize += PanelLeftTopOnResize;

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
            ApplyThemeToDynamicControls(theme);
            Refresh();
        }

        private void ApplyThemeToDynamicControls(Theme theme)
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
                progressBarTotal.ForeColor = theme.Accent;
            }

            if (progressBarCurrent != null)
            {
                progressBarCurrent.ForeColor = theme.Accent;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
            base.OnFormClosed(e);
        }

        private void PanelLeftTopOnResize(object? sender, EventArgs e) => PositionThemeToggle();

        private void PositionThemeToggle()
        {
            if (_themeToggle == null || panelLeftTop == null) return;
            var x = Math.Max(10, panelLeftTop.Width - _themeToggle.Width - 10);
            _themeToggle.Location = new Point(x, 8);
            _themeToggle.BringToFront();
        }
    }
}
