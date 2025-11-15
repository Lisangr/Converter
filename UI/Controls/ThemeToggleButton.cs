using System;
using System.Drawing;
using System.Windows.Forms;
using Converter.Models;
using Converter.Services;

namespace Converter.UI.Controls
{
    public class ThemeToggleButton : Button
    {
        private bool _isDarkMode;
        private readonly ThemeChangedEventHandler _themeChangedHandler;

        public ThemeToggleButton()
        {
            Size = new Size(80, 32);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 1;
            Text = string.Empty;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            Tag = "CustomColor";

            _themeChangedHandler = OnThemeChanged;

            Click += (_, _) => ThemeManager.Instance.ToggleTheme();
            ThemeManager.Instance.ThemeChanged += _themeChangedHandler;

            UpdateAppearance();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.Instance.ThemeChanged -= _themeChangedHandler;
            }

            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var theme = ThemeManager.Instance.CurrentTheme;

            var track = new Rectangle(10, Height / 2 - 8, 60, 16);
            var thumb = new Rectangle(_isDarkMode ? 46 : 14, Height / 2 - 10, 20, 20);

            using (var brush = new SolidBrush(_isDarkMode ? theme.Accent : theme.Border))
            {
                g.FillRoundedRectangle(brush, track, 8);
            }

            using (var brush = new SolidBrush(Color.White))
            {
                g.FillEllipse(brush, thumb);
            }

            var sunIcon = "☀";
            var moonIcon = "🌙";
            using var sunBrush = new SolidBrush(Color.FromArgb(255, 193, 7));
            using var moonBrush = new SolidBrush(Color.FromArgb(255, 255, 255));

            g.DrawString(sunIcon, Font, sunBrush, new Point(2, 6));
            g.DrawString(moonIcon, Font, moonBrush, new Point(62, 6));
        }

        private void UpdateAppearance()
        {
            _isDarkMode = ThemeManager.Instance.CurrentTheme.Name == "Dark";
            BackColor = ThemeManager.Instance.CurrentTheme.BackgroundPrimary;
            FlatAppearance.BorderColor = ThemeManager.Instance.CurrentTheme.Border;
            Invalidate();
        }

        private void OnThemeChanged(object? sender, Theme theme)
        {
            _isDarkMode = theme.Name == "Dark";
            UpdateAppearance();
        }
    }

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius)
        {
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, cornerRadius, cornerRadius, 180, 90);
            path.AddArc(bounds.Right - cornerRadius, bounds.Y, cornerRadius, cornerRadius, 270, 90);
            path.AddArc(bounds.Right - cornerRadius, bounds.Bottom - cornerRadius, cornerRadius, cornerRadius, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - cornerRadius, cornerRadius, cornerRadius, 90, 90);
            path.CloseFigure();
            graphics.FillPath(brush, path);
        }
    }
}

