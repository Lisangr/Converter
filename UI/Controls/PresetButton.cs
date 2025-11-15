using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Converter.UI.Controls
{
    public class PresetButton : Button
    {
        public string IconText { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public Color AccentColor { get; set; } = Color.FromArgb(230, 230, 240);

        private bool _hover;
        private readonly ToolTip _tooltip = new ToolTip();

        public PresetButton()
        {
            DoubleBuffered = true;
            Width = 160;
            Height = 100;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            Margin = new Padding(6);
                       
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        public void SetTooltip(string text)
        {
            _tooltip.SetToolTip(this, text);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;

            float scale = _hover ? 1.05f : 1.0f;
            int newW = (int)(rect.Width * scale);
            int newH = (int)(rect.Height * scale);
            var dx = (rect.Width - newW) / 2;
            var dy = (rect.Height - newH) / 2;
            var card = new Rectangle(rect.X + dx, rect.Y + dy, newW, newH);

            using var path = Rounded(card, 10);
            using var bg = new SolidBrush(BackColor);
            using var border = new Pen(AccentColor, 2);
            g.FillPath(bg, path);
            g.DrawPath(border, path);

            // icon
            using var iconBrush = new SolidBrush(Color.Black);
            var iconFont = new Font("Segoe UI Emoji", 20f, FontStyle.Regular);
            var iconSize = g.MeasureString(IconText ?? string.Empty, iconFont);
            g.DrawString(IconText ?? string.Empty, iconFont, iconBrush, card.X + (card.Width - iconSize.Width) / 2, card.Y + 8);

            // title
            using var titleBrush = new SolidBrush(Color.Black);
            var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            var titleRect = new Rectangle(card.X + 10, card.Y + 40, card.Width - 20, 22);
            TextRenderer.DrawText(g, Title ?? string.Empty, titleFont, titleRect, Color.Black, TextFormatFlags.EndEllipsis);

            // description
            using var descBrush = new SolidBrush(Color.DimGray);
            var descFont = new Font("Segoe UI", 8f, FontStyle.Regular);
            var descRect = new Rectangle(card.X + 10, card.Y + 62, card.Width - 20, 30);
            TextRenderer.DrawText(g, Description ?? string.Empty, descFont, descRect, Color.DimGray, TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
