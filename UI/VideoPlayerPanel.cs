using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xabe.FFmpeg;

namespace Converter.UI
{
    /// <summary>
    /// Простая панель воспроизведения с предпросмотром и оверлеем субтитров.
    /// </summary>
    public class VideoPlayerPanel : Panel
    {
        private readonly PictureBox _pictureBox;
        private readonly Label _subtitleLabel;
        private readonly TrackBar _trackSeek;
        private readonly Button _btnPlay;
        private readonly Button _btnPause;
        private readonly Label _lblTime;
        private readonly Label _lblDuration;

        private IMediaInfo? _currentMedia;
        private readonly Timer _playbackTimer;
        private TimeSpan _currentPosition = TimeSpan.Zero;
        private bool _isPlaying;
        private string? _currentVideoPath;

        private string? currentSubtitleText;
        private Font? currentSubtitleFont;
        private Color? currentSubtitleForeColor;
        private Color? currentSubtitleBackColor;
        private ContentAlignment currentSubtitleAlignment = ContentAlignment.BottomCenter;
        private int currentOutlineThickness;

        public event Action<TimeSpan>? PositionChanged;

        public VideoPlayerPanel()
        {
            BackColor = Color.Black;

            _pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            Controls.Add(_pictureBox);

            _subtitleLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.BottomCenter
            };
            _subtitleLabel.Paint += subtitleLabel_Paint;
            _pictureBox.Controls.Add(_subtitleLabel);

            var controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(45, 45, 45)
            };
            Controls.Add(controlPanel);

            _trackSeek = new TrackBar
            {
                Location = new Point(10, 5),
                Width = Width - 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TickStyle = TickStyle.None,
                Maximum = 1000
            };
            _trackSeek.ValueChanged += TrackSeek_ValueChanged;
            controlPanel.Controls.Add(_trackSeek);

            _btnPlay = new Button
            {
                Text = "▶",
                Location = new Point(10, 30),
                Size = new Size(40, 25),
                Font = new Font("Segoe UI", 10)
            };
            _btnPlay.Click += BtnPlay_Click;
            controlPanel.Controls.Add(_btnPlay);

            _btnPause = new Button
            {
                Text = "⏸",
                Location = new Point(55, 30),
                Size = new Size(40, 25),
                Font = new Font("Segoe UI", 10),
                Enabled = false
            };
            _btnPause.Click += BtnPause_Click;
            controlPanel.Controls.Add(_btnPause);

            _lblTime = new Label
            {
                Text = "00:00:00",
                Location = new Point(110, 33),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            controlPanel.Controls.Add(_lblTime);

            var lblSeparator = new Label
            {
                Text = "/",
                Location = new Point(170, 33),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            controlPanel.Controls.Add(lblSeparator);

            _lblDuration = new Label
            {
                Text = "00:00:00",
                Location = new Point(180, 33),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            controlPanel.Controls.Add(_lblDuration);

            _playbackTimer = new Timer { Interval = 100 };
            _playbackTimer.Tick += PlaybackTimer_Tick;
        }

        public async Task LoadVideoAsync(string videoPath, IMediaInfo mediaInfo)
        {
            // Перенаправляем на UI-поток, чтобы контролы инициализировались корректно.
            if (InvokeRequired)
            {
                await (Task)Invoke(new Func<Task>(() => LoadVideoAsync(videoPath, mediaInfo))); // UI marshal
                return;
            }

            _currentVideoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));
            _currentMedia = mediaInfo ?? throw new ArgumentNullException(nameof(mediaInfo));
            _lblDuration.Text = FormatTime(mediaInfo.Duration);
            _currentPosition = TimeSpan.Zero;
            UpdateUi();

            var thumbnailPath = Path.Combine(Path.GetTempPath(), $"video_preview_{Guid.NewGuid():N}.jpg");

            try
            {
                await Task.Run(async () =>
                {
                    var conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-i \"{videoPath}\"")
                        .AddParameter("-vframes 1")
                        .AddParameter("-ss 00:00:01")
                        .SetOutput(thumbnailPath);

                    await conversion.Start().ConfigureAwait(false);
                }).ConfigureAwait(false);

                if (File.Exists(thumbnailPath))
                {
                    using var fs = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var image = Image.FromStream(fs);
                    RunOnUiThread(() =>
                    {
                        _pictureBox.Image?.Dispose();
                        _pictureBox.Image = new Bitmap(image);
                    });
                }
            }
            catch
            {
                // Игнорируем ошибки получения миниатюры, чтобы не срывать загрузку.
            }
            finally
            {
                if (File.Exists(thumbnailPath))
                {
                    try { File.Delete(thumbnailPath); } catch { /* ignore */ }
                }
            }
        }

        public TimeSpan GetCurrentTime() => _currentPosition;

        public void SeekTo(TimeSpan time)
        {
            var clamped = time;
            if (_currentMedia != null)
            {
                clamped = TimeSpan.FromSeconds(Math.Max(0, Math.Min(time.TotalSeconds, _currentMedia.Duration.TotalSeconds)));
            }

            _currentPosition = clamped;
            UpdateUi();
            PositionChanged?.Invoke(_currentPosition);
        }

        public void SetSubtitleOverlay(string? text, Font? font, Color? foreColor, Color? backColor,
            ContentAlignment alignment, int outlineThickness)
        {
            currentSubtitleText = text;

            // Dispose previous font if it differs from the new one
            if (currentSubtitleFont != null && font != null && !currentSubtitleFont.Equals(font))
            {
                currentSubtitleFont.Dispose();
            }

            currentSubtitleFont = font;
            currentSubtitleForeColor = foreColor;
            currentSubtitleBackColor = backColor;
            currentSubtitleAlignment = alignment;
            currentOutlineThickness = outlineThickness;

            _subtitleLabel.Invalidate();
        }

        private void BtnPlay_Click(object? sender, EventArgs e)
        {
            _isPlaying = true;
            _btnPlay.Enabled = false;
            _btnPause.Enabled = true;
            _playbackTimer.Start();
        }

        private void BtnPause_Click(object? sender, EventArgs e)
        {
            _isPlaying = false;
            _btnPlay.Enabled = true;
            _btnPause.Enabled = false;
            _playbackTimer.Stop();
        }

        private void PlaybackTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isPlaying || _currentMedia == null)
            {
                return;
            }

            _currentPosition = _currentPosition.Add(TimeSpan.FromMilliseconds(_playbackTimer.Interval));

            if (_currentPosition >= _currentMedia.Duration)
            {
                _currentPosition = _currentMedia.Duration;
                BtnPause_Click(null, EventArgs.Empty);
            }

            UpdateUi();
            PositionChanged?.Invoke(_currentPosition);
        }

        private void TrackSeek_ValueChanged(object? sender, EventArgs e)
        {
            if (_currentMedia == null)
            {
                return;
            }

            var percent = _trackSeek.Value / (double)_trackSeek.Maximum;
            SeekTo(TimeSpan.FromSeconds(_currentMedia.Duration.TotalSeconds * percent));
        }

        private void UpdateUi()
        {
            RunOnUiThread(() =>
            {
                _lblTime.Text = FormatTime(_currentPosition);

                if (_currentMedia != null && _currentMedia.Duration.TotalSeconds > 0)
                {
                    var percent = _currentPosition.TotalSeconds / _currentMedia.Duration.TotalSeconds;
                    _trackSeek.Value = Math.Clamp((int)(percent * _trackSeek.Maximum), 0, _trackSeek.Maximum);
                }
            });
        }

        private static string FormatTime(TimeSpan time) => time.ToString("hh\\:mm\\:ss");

        private void RunOnUiThread(Action action)
        {
            if (InvokeRequired)
            {
                BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        private void subtitleLabel_Paint(object? sender, PaintEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentSubtitleText) || currentSubtitleFont == null || !currentSubtitleForeColor.HasValue)
            {
                return;
            }

            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Determine colors
            var foreColor = currentSubtitleForeColor.Value;
            // For outline, use a semi-transparent black or a color derived from background/theme
            var outlineColor = Color.FromArgb(160, 0, 0, 0); // Semi-transparent black for outline

            // Set background for the label if it has one
            if (currentSubtitleBackColor.HasValue)
            {
                using var backgroundBrush = new SolidBrush(currentSubtitleBackColor.Value);
                g.FillRectangle(backgroundBrush, e.ClipRectangle);
            }

            // Prepare text format flags based on alignment
            TextFormatFlags flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl;
            switch (currentSubtitleAlignment)
            {
                case ContentAlignment.BottomCenter: flags |= TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter; break;
                case ContentAlignment.TopCenter: flags |= TextFormatFlags.Top | TextFormatFlags.HorizontalCenter; break;
                case ContentAlignment.MiddleCenter: flags |= TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter; break;
            }

            // Draw outline if thickness is greater than 0
            if (currentOutlineThickness > 0)
            {
                // Draw outline by drawing text with offsets in outline color
                for (int xOffset = -currentOutlineThickness; xOffset <= currentOutlineThickness; xOffset++)
                {
                    for (int yOffset = -currentOutlineThickness; yOffset <= currentOutlineThickness; yOffset++)
                    {
                        // Avoid drawing outline on the center position if it's the same as foreground
                        if (Math.Abs(xOffset) == currentOutlineThickness && Math.Abs(yOffset) == currentOutlineThickness && currentOutlineThickness > 0) continue; // Skip corners for a cleaner look if desired

                        // Draw the outline text at an offset rectangle
                        var offsetRect = new Rectangle(
                            e.ClipRectangle.Left + xOffset,
                            e.ClipRectangle.Top + yOffset,
                            e.ClipRectangle.Width,
                            e.ClipRectangle.Height);

                        TextRenderer.DrawText(g, currentSubtitleText, currentSubtitleFont, offsetRect, outlineColor, flags);
                    }
                }
            }

            // Draw the main text
            TextRenderer.DrawText(g, currentSubtitleText, currentSubtitleFont, e.ClipRectangle, foreColor, flags);
        }
    }
}
