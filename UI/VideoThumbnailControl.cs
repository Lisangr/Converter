using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Models;
using Xabe.FFmpeg;

namespace Converter.UI
{
    /// <summary>
    /// Показывает превью видеофайла и основную информацию о нём.
    /// </summary>
    public class VideoThumbnailControl : UserControl
    {
        private readonly PictureBox _thumbnailBox;
        private readonly Label _fileNameLabel;
        private readonly Label _durationLabel;
        private readonly Label _sizeLabel;
        private readonly Label _codecLabel;
        private readonly Panel _overlayPanel;
        private readonly Button _removeButton;

        public string FilePath { get; }
        private readonly IThemeService _themeService;
        private readonly EventHandler<Theme> _themeChangedHandler;
        public event EventHandler? RemoveRequested;
        public event EventHandler? SelectionChanged;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateSelectionVisual();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public VideoThumbnailControl(string filePath, IThemeService themeService)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path must be provided", nameof(filePath));
            }

            FilePath = filePath;
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _themeChangedHandler = (s, theme) => ApplyTheme(theme);

            Size = new Size(180, 220);
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(5);
            Cursor = Cursors.Hand;

            ApplyTheme(_themeService.CurrentTheme);
            _themeService.ThemeChanged += _themeChangedHandler;

            _thumbnailBox = new PictureBox
            {
                Size = new Size(170, 120),
                Location = new Point(5, 5),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            Controls.Add(_thumbnailBox);

            _overlayPanel = new Panel
            {
                Size = _thumbnailBox.Size,
                Location = _thumbnailBox.Location,
                BackColor = Color.FromArgb(150, 0, 0, 0),
                Visible = false
            };

            _removeButton = new Button
            {
                Text = "✕",
                Size = new Size(30, 30),
                Location = new Point((_overlayPanel.Width - 30) / 2, (_overlayPanel.Height - 30) / 2),
                BackColor = Color.FromArgb(220, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            _removeButton.FlatAppearance.BorderSize = 0;
            _removeButton.Click += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            _overlayPanel.Controls.Add(_removeButton);
            Controls.Add(_overlayPanel);

            _fileNameLabel = new Label
            {
                Location = new Point(5, 130),
                Size = new Size(170, 20),
                Text = Path.GetFileName(FilePath),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ForeColor
            };
            Controls.Add(_fileNameLabel);

            var theme = _themeService.CurrentTheme;
            _durationLabel = new Label
            {
                Location = new Point(5, 155),
                Size = new Size(170, 18),
                Text = "Длительность: ...",
                Font = new Font("Segoe UI", 8),
                ForeColor = theme["TextSecondary"]
            };
            Controls.Add(_durationLabel);

            _sizeLabel = new Label
            {
                Location = new Point(5, 175),
                Size = new Size(170, 18),
                Text = FormatFileSize(),
                Font = new Font("Segoe UI", 8),
                ForeColor = theme["TextSecondary"]
            };
            Controls.Add(_sizeLabel);

            _codecLabel = new Label
            {
                Location = new Point(5, 195),
                Size = new Size(170, 18),
                Text = "Кодек: ...",
                Font = new Font("Segoe UI", 8),
                ForeColor = theme["TextSecondary"]
            };
            Controls.Add(_codecLabel);

            MouseEnter += (_, _) => _overlayPanel.Visible = true;
            MouseLeave += (_, _) => _overlayPanel.Visible = false;
            _thumbnailBox.MouseEnter += (_, _) => _overlayPanel.Visible = true;
            _thumbnailBox.MouseLeave += (_, _) => _overlayPanel.Visible = false;

            // Click event for selection
            this.Click += (s, e) => ToggleSelection();

            LoadVideoInfo();
        }

        private string FormatFileSize()
        {
            if (!File.Exists(FilePath))
            {
                return "Размер: неизвестно";
            }

            var fileInfo = new FileInfo(FilePath);
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = fileInfo.Length;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"Размер: {len:0.##} {sizes[order]}";
        }

        private async void LoadVideoInfo()
        {
            try
            {
                var mediaInfo = await FFmpeg.GetMediaInfo(FilePath);

                var duration = mediaInfo.Duration;
                _durationLabel.Text = $"Длительность: {duration:hh\\:mm\\:ss}";

                var videoStream = mediaInfo.VideoStreams?.FirstOrDefault();
                if (videoStream != null)
                {
                    _codecLabel.Text = $"Кодек: {videoStream.Codec} {videoStream.Width}x{videoStream.Height}";
                }

                await GenerateThumbnail(mediaInfo);
            }
            catch (Exception ex)
            {
                _durationLabel.Text = "Ошибка загрузки";
                _codecLabel.Text = ex.Message;
                _thumbnailBox.BackColor = Color.DarkGray;
            }
        }

        private async Task GenerateThumbnail(IMediaInfo mediaInfo)
        {
            try
            {
                var duration = mediaInfo.Duration;
                var seekTime = duration.TotalSeconds > 10
                    ? TimeSpan.FromSeconds(5)
                    : TimeSpan.FromSeconds(Math.Max(1, duration.TotalSeconds / 2));

                var tempThumbnail = Path.Combine(Path.GetTempPath(), $"thumb_{Guid.NewGuid():N}.jpg");

                var conversion = FFmpeg.Conversions.New()
                    .AddParameter($"-ss {seekTime.TotalSeconds}")
                    .AddParameter($"-i \"{FilePath}\"")
                    .AddParameter("-vframes 1")
                    .AddParameter("-vf scale=320:-1")
                    .SetOutput(tempThumbnail);

                await conversion.Start();

                if (File.Exists(tempThumbnail))
                {
                    using var fs = new FileStream(tempThumbnail, FileMode.Open, FileAccess.Read);
                    _thumbnailBox.Image = Image.FromStream(fs);
                    File.Delete(tempThumbnail);
                }
            }
            catch
            {
                _thumbnailBox.BackColor = Color.DarkGray;
            }
        }

        private void ApplyTheme(Theme theme)
        {
            BackColor = theme["Surface"];
            ForeColor = theme["TextPrimary"];
            if (_durationLabel != null) _durationLabel.ForeColor = theme["TextSecondary"];
            if (_sizeLabel != null) _sizeLabel.ForeColor = theme["TextSecondary"];
            if (_codecLabel != null) _codecLabel.ForeColor = theme["TextSecondary"];
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _thumbnailBox?.Image?.Dispose();
                _themeService.ThemeChanged -= _themeChangedHandler;
            }

            base.Dispose(disposing);
        }

        private void ToggleSelection()
        {
            IsSelected = !_isSelected;
        }

        private void UpdateSelectionVisual()
        {
            // Update visual appearance for selection
            this.Invalidate(); // Force redraw
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Draw selection border if selected
            if (_isSelected)
            {
                using var pen = new Pen(Color.FromArgb(0, 120, 215), 3);
                e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 2, this.Height - 2);
            }
            // Draw subtle border when mouse is over (only if not selected)
            else if (this.ClientRectangle.Contains(this.PointToClient(Cursor.Position)))
            {
                using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }
}
