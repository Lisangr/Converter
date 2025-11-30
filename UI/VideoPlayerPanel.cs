using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using Xabe.FFmpeg;

namespace Converter.UI
{
    public class VideoPlayerPanel : Panel
    {
        private readonly VideoView videoView;
        private readonly SubtitleOverlay subtitleOverlay;
        private readonly CropOverlayForm cropOverlayForm;
        private readonly TrackBar trackSeek;
        private readonly Button btnRewind5;
        private readonly Button btnPlay;
        private readonly Button btnPause;
        private readonly Button btnForward5;
        private readonly Button btnStop;
        private readonly Label lblTime;
        private readonly Label lblDuration;

        private readonly LibVLC libVlc;
        private readonly MediaPlayer mediaPlayer;
        private IMediaInfo? currentMediaInfo;
        private TimeSpan currentPosition = TimeSpan.Zero;
        private bool isSeeking;
        private readonly object subtitlesLock = new();
        private System.Collections.Generic.List<SubtitleItem> subtitles = new();

        private Color subtitleFontColor = Color.White;
        private Color subtitleBackgroundColor = Color.FromArgb(200, 0, 0, 0);
        private bool subtitleShowBackground = true;
        private bool subtitleShowOutline = true;
        private Font? subtitleFont;
        private string currentSubtitleText = string.Empty;
        private ContentAlignment subtitleAlignment = ContentAlignment.BottomCenter;

        public event EventHandler<TimeSpan>? PlaybackTimeChanged;
        public event EventHandler<Rectangle>? CropRectChanged;

        public int VideoWidth => currentMediaInfo?.VideoStreams.FirstOrDefault()?.Width ?? 0;
        public int VideoHeight => currentMediaInfo?.VideoStreams.FirstOrDefault()?.Height ?? 0;

        public VideoPlayerPanel()
        {
            BackColor = Color.Black;
            Core.Initialize();
            libVlc = new LibVLC();
            mediaPlayer = new MediaPlayer(libVlc);

            videoView = new VideoView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                MediaPlayer = mediaPlayer
            };
            Controls.Add(videoView);

            var controlPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(45, 45, 45),
                Padding = new Padding(10, 5, 10, 5)
            };
            Controls.Add(controlPanel);

            var layoutTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.FromArgb(45, 45, 45)
            };
            layoutTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            layoutTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            controlPanel.Controls.Add(layoutTable);

            trackSeek = new TrackBar
            {
                Dock = DockStyle.Fill,
                TickStyle = TickStyle.None,
                Maximum = 1000,
                Margin = new Padding(0, 5, 0, 0)
            };
            trackSeek.Scroll += TrackSeek_Scroll;
            layoutTable.Controls.Add(trackSeek, 0, 0);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Margin = new Padding(0)
            };
            layoutTable.Controls.Add(buttonPanel, 0, 1);

            btnRewind5 = new Button
            {
                Text = "<< 5s",
                Size = new Size(45, 30),
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 5, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            btnRewind5.FlatAppearance.BorderSize = 0;
            btnRewind5.Click += (_, _) => SeekByOffset(-5000);
            buttonPanel.Controls.Add(btnRewind5);

            btnPlay = new Button
            {
                Text = "▶",
                Size = new Size(45, 30),
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 5, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            btnPlay.FlatAppearance.BorderSize = 0;
            btnPlay.Click += BtnPlay_Click;
            buttonPanel.Controls.Add(btnPlay);

            btnPause = new Button
            {
                Text = "⏸",
                Size = new Size(45, 30),
                Font = new Font("Segoe UI", 10),
                Enabled = false,
                Margin = new Padding(0, 0, 15, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            btnPause.FlatAppearance.BorderSize = 0;
            btnPause.Click += BtnPause_Click;
            buttonPanel.Controls.Add(btnPause);

            btnForward5 = new Button
            {
                Text = "5s >>",
                Size = new Size(55, 30),
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 15, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            btnForward5.FlatAppearance.BorderSize = 0;
            btnForward5.Click += (_, _) => SeekByOffset(5000);
            buttonPanel.Controls.Add(btnForward5);

            btnStop = new Button
            {
                Text = "⏹",
                Size = new Size(45, 30),
                Font = new Font("Segoe UI", 10),
                Enabled = false,
                Margin = new Padding(0, 0, 15, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Click += BtnStop_Click;
            buttonPanel.Controls.Add(btnStop);

            lblTime = new Label
            {
                Text = "00:00:00",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Consolas", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, 5, 0)
            };
            buttonPanel.Controls.Add(lblTime);

            var lblSeparator = new Label
            {
                Text = "/",
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Consolas", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 5, 5, 0)
            };
            buttonPanel.Controls.Add(lblSeparator);

            lblDuration = new Label
            {
                Text = "00:00:00",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Consolas", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 5, 0, 0)
            };
            buttonPanel.Controls.Add(lblDuration);

            subtitleOverlay = new SubtitleOverlay(this);
            Controls.Add(subtitleOverlay);
            subtitleOverlay.BringToFront();

            cropOverlayForm = new CropOverlayForm();
            cropOverlayForm.CropRectChanged += (s, rect) => CropRectChanged?.Invoke(this, rect);

            subtitleFont = new Font("Segoe UI", 18, FontStyle.Bold);

            mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            mediaPlayer.EndReached += MediaPlayer_EndReached;

            videoView.SizeChanged += (_, _) =>
            {
                UpdateOverlayVideoMetrics();
                UpdateOverlayBounds();
            };
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            var form = FindForm();
            if (form != null)
            {
                form.Move -= OwnerFormOnMoveOrResize;
                form.Resize -= OwnerFormOnMoveOrResize;

                form.Move += OwnerFormOnMoveOrResize;
                form.Resize += OwnerFormOnMoveOrResize;
            }

            UpdateOverlayBounds();
        }

        private void OwnerFormOnMoveOrResize(object? sender, EventArgs e)
        {
            UpdateOverlayBounds();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateOverlayBounds();
        }

        private void UpdateOverlayVideoMetrics()
        {
            if (VideoWidth > 0 && VideoHeight > 0)
            {
                cropOverlayForm.VideoSize = new Size(VideoWidth, VideoHeight);
                cropOverlayForm.ContainerSize = videoView.ClientSize;
            }
        }

        private void UpdateOverlayBounds()
        {
            if (!IsHandleCreated || videoView.IsDisposed)
            {
                return;
            }

            var screenLocation = videoView.PointToScreen(Point.Empty);
            cropOverlayForm.Bounds = new Rectangle(screenLocation, videoView.ClientSize);
        }

        public Task LoadVideoAsync(string videoPath, IMediaInfo mediaInfo)
        {
            currentMediaInfo = mediaInfo;
            lblDuration.Text = FormatTime(mediaInfo.Duration);
            currentPosition = TimeSpan.Zero;
            UpdateUI();

            mediaPlayer.Stop();
            mediaPlayer.Media?.Dispose();
            var media = new Media(libVlc, new Uri(videoPath));
            mediaPlayer.Media = media;

            btnPlay.Enabled = true;
            btnPause.Enabled = false;
            btnStop.Enabled = false;
            trackSeek.Value = 0;
            UpdateSubtitleOverlay(TimeSpan.Zero);

            if (VideoWidth > 0 && VideoHeight > 0)
            {
                UpdateOverlayVideoMetrics();
                UpdateOverlayBounds();
            }

            return Task.CompletedTask;
        }

        public void ShowCropOverlay()
        {
            UpdateOverlayVideoMetrics();
            UpdateOverlayBounds();

            var owner = FindForm();
            if (owner == null)
            {
                return;
            }

            if (!cropOverlayForm.Visible)
            {
                cropOverlayForm.Show(owner);
            }
        }

        public void HideCropOverlay()
        {
            if (cropOverlayForm.Visible)
            {
                cropOverlayForm.Hide();
            }
        }

        public void SetCropRect(Rectangle rect)
        {
            cropOverlayForm.CropRect = rect;
            cropOverlayForm.Invalidate();
        }

        public Rectangle GetCropRect()
        {
            return cropOverlayForm.CropRect;
        }

        private void BtnPlay_Click(object? sender, EventArgs e)
        {
            if (mediaPlayer.Media == null)
            {
                return;
            }

            mediaPlayer.Play();
            btnPlay.Enabled = false;
            btnPause.Enabled = true;
            btnStop.Enabled = true;
        }

        private void BtnPause_Click(object? sender, EventArgs e)
        {
            if (mediaPlayer.Media == null)
            {
                return;
            }

            mediaPlayer.Pause();
            btnPlay.Enabled = true;
            btnPause.Enabled = false;
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            Stop();
        }

        public void Stop()
        {
            mediaPlayer.Stop();
            currentPosition = TimeSpan.Zero;
            btnPlay.Enabled = mediaPlayer.Media != null;
            btnPause.Enabled = false;
            btnStop.Enabled = false;
            UpdateUI();
            UpdateSubtitleOverlay(TimeSpan.Zero);
        }

        private void MediaPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            var time = TimeSpan.FromMilliseconds(e.Time);
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnTimeChangedUi(time)));
            }
            else
            {
                OnTimeChangedUi(time);
            }
        }

        private void OnTimeChangedUi(TimeSpan time)
        {
            currentPosition = time;
            UpdateUI();
            UpdateSubtitleOverlay(time);
            PlaybackTimeChanged?.Invoke(this, time);
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(Stop));
            }
            else
            {
                Stop();
            }
        }

        private void TrackSeek_Scroll(object? sender, EventArgs e)
        {
            if (mediaPlayer.Media == null)
            {
                return;
            }

            isSeeking = true;
            var lengthMs = mediaPlayer.Length;
            if (lengthMs <= 0)
            {
                isSeeking = false;
                return;
            }

            var percent = trackSeek.Value / (double)trackSeek.Maximum;
            var targetMs = (long)(lengthMs * percent);
            mediaPlayer.Time = targetMs;
            isSeeking = false;
        }

        private void UpdateUI()
        {
            lblTime.Text = FormatTime(currentPosition);

            var totalSeconds = currentMediaInfo?.Duration.TotalSeconds ?? (mediaPlayer.Length > 0 ? mediaPlayer.Length / 1000.0 : 0);
            if (totalSeconds > 0 && !isSeeking)
            {
                var percent = currentPosition.TotalSeconds / totalSeconds;
                trackSeek.Value = Math.Clamp((int)(percent * trackSeek.Maximum), 0, trackSeek.Maximum);
            }
        }

        private void UpdateSubtitleOverlay(TimeSpan time)
        {
            SubtitleItem? active = null;
            lock (subtitlesLock)
            {
                foreach (var s in subtitles)
                {
                    if (time >= s.StartTime && time <= s.EndTime)
                    {
                        active = s;
                        break;
                    }
                }
            }

            currentSubtitleText = active?.Text ?? string.Empty;

            subtitleOverlay.UpdateSubtitleDisplay(
                currentSubtitleText,
                subtitleFont!,
                subtitleFontColor,
                subtitleBackgroundColor,
                subtitleShowBackground,
                subtitleShowOutline,
                subtitleAlignment,
                videoView.Size
            );
        }

        private static string FormatTime(TimeSpan time) => time.ToString("hh\\:mm\\:ss");

        public TimeSpan GetCurrentTime() => currentPosition;

        public void SeekTo(TimeSpan time)
        {
            if (mediaPlayer.Media == null)
            {
                return;
            }

            mediaPlayer.Time = (long)time.TotalMilliseconds;
        }

        public Rectangle UiToVideoCoordinates(Rectangle uiRect)
        {
            return cropOverlayForm.UiToVideoCoordinates(uiRect);
        }

        public Rectangle VideoToUiCoordinates(Rectangle videoRect)
        {
            return cropOverlayForm.VideoToUiCoordinates(videoRect);
        }

        private void SeekByOffset(int offsetMs)
        {
            if (mediaPlayer.Media == null)
            {
                return;
            }

            var length = mediaPlayer.Length;
            if (length <= 0)
            {
                return;
            }

            var current = mediaPlayer.Time;
            var target = current + offsetMs;
            if (target < 0)
            {
                target = 0;
            }
            else if (target > length)
            {
                target = length;
            }

            mediaPlayer.Time = target;
        }

        public void UpdateSubtitles(System.Collections.Generic.List<SubtitleItem> items)
        {
            if (items == null)
            {
                items = new System.Collections.Generic.List<SubtitleItem>();
            }

            lock (subtitlesLock)
            {
                subtitles = new System.Collections.Generic.List<SubtitleItem>(items);
            }
            UpdateSubtitleOverlay(currentPosition);
        }

        public void SetSubtitleStyle(Color fontColor, Color backgroundColor, bool showBackground, bool showOutline, ContentAlignment alignment, Font? font = null)
        {
            subtitleFontColor = fontColor;
            subtitleBackgroundColor = backgroundColor;
            subtitleShowBackground = showBackground;
            subtitleShowOutline = showOutline;
            subtitleAlignment = alignment;

            if (font != null)
            {
                subtitleFont?.Dispose();
                subtitleFont = font;
            }

            UpdateSubtitleOverlay(currentPosition);
        }

        private class CropOverlayForm : Form
        {
            private const int HandleSize = 10;
            private const int MinCropSize = 50;

            private Rectangle _cropRect; // Stored in UI coordinates
            private bool _isDragging = false;
            private Point _dragStartPoint;
            private HitTestRegion _hitRegion = HitTestRegion.None;

            public event EventHandler<Rectangle>? CropRectChanged;

            public Size VideoSize { get; set; } // Actual video resolution (e.g., 1920x1080)
            public Size ContainerSize { get; set; } // Size of the videoView control (e.g., 800x450)

            public Rectangle CropRect
            {
                get => _cropRect;
                set
                {
                    Rectangle videoDisplayRect = GetVideoDisplayRect();

                    if (videoDisplayRect.IsEmpty || value.Width <= 0 || value.Height <= 0)
                    {
                        _cropRect = Rectangle.Empty;
                    }
                    else
                    {
                        int x = Math.Max(videoDisplayRect.X, value.X);
                        int y = Math.Max(videoDisplayRect.Y, value.Y);
                        int width = Math.Min(value.Width, videoDisplayRect.Width - (x - videoDisplayRect.X));
                        int height = Math.Min(value.Height, videoDisplayRect.Height - (y - videoDisplayRect.Y));

                        if (width < MinCropSize) width = MinCropSize;
                        if (height < MinCropSize) height = MinCropSize;

                        if (x + width > videoDisplayRect.Right) x = videoDisplayRect.Right - width;
                        if (y + height > videoDisplayRect.Bottom) y = videoDisplayRect.Bottom - height;
                        if (x < videoDisplayRect.X) x = videoDisplayRect.X;
                        if (y < videoDisplayRect.Y) y = videoDisplayRect.Y;

                        _cropRect = new Rectangle(x, y, width, height);
                    }

                    if (_cropRect != value)
                    {
                        Invalidate();
                        CropRectChanged?.Invoke(this, _cropRect);
                    }
                }
            }

            [Flags]
            private enum HitTestRegion
            {
                None = 0,
                Move = 1,
                TopLeft = 2,
                Top = 4,
                TopRight = 8,
                Left = 16,
                Right = 32,
                BottomLeft = 64,
                Bottom = 128,
                BottomRight = 256
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    unchecked
                    {
                        cp.Style |= (int)0x80000000; // WS_POPUP
                    }
                    return cp;
                }
            }

            public CropOverlayForm()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;

                BackColor = Color.Magenta;
                TransparencyKey = Color.Magenta;

                DoubleBuffered = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

                CropRect = Rectangle.Empty;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                if (_cropRect.IsEmpty || !Visible || VideoSize.Width == 0 || VideoSize.Height == 0 || ClientSize.Width == 0 || ClientSize.Height == 0)
                {
                    return;
                }

                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Rectangle videoDisplayRect = GetVideoDisplayRect();

                using (SolidBrush dimBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    if (_cropRect.Y > videoDisplayRect.Y)
                        g.FillRectangle(dimBrush, videoDisplayRect.X, videoDisplayRect.Y, videoDisplayRect.Width, _cropRect.Y - videoDisplayRect.Y);

                    if (_cropRect.Bottom < videoDisplayRect.Bottom)
                        g.FillRectangle(dimBrush, videoDisplayRect.X, _cropRect.Bottom, videoDisplayRect.Width, videoDisplayRect.Bottom - _cropRect.Bottom);

                    if (_cropRect.X > videoDisplayRect.X)
                        g.FillRectangle(dimBrush, videoDisplayRect.X, _cropRect.Y, _cropRect.X - videoDisplayRect.X, _cropRect.Height);

                    if (_cropRect.Right < videoDisplayRect.Right)
                        g.FillRectangle(dimBrush, _cropRect.Right, _cropRect.Y, videoDisplayRect.Right - _cropRect.Right, _cropRect.Height);
                }

                using (Pen outlinePen = new Pen(Color.Black, 3))
                using (Pen innerPen = new Pen(Color.White, 1))
                {
                    g.DrawRectangle(outlinePen, _cropRect);
                    g.DrawRectangle(innerPen, _cropRect);
                }

                using (Pen gridPen = new Pen(Color.FromArgb(150, 255, 255, 255), 1))
                {
                    gridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

                    g.DrawLine(gridPen, _cropRect.X + _cropRect.Width / 3, _cropRect.Y, _cropRect.X + _cropRect.Width / 3, _cropRect.Bottom);
                    g.DrawLine(gridPen, _cropRect.X + 2 * _cropRect.Width / 3, _cropRect.Y, _cropRect.X + 2 * _cropRect.Width / 3, _cropRect.Bottom);

                    g.DrawLine(gridPen, _cropRect.X, _cropRect.Y + _cropRect.Height / 3, _cropRect.Right, _cropRect.Y + _cropRect.Height / 3);
                    g.DrawLine(gridPen, _cropRect.X, _cropRect.Y + 2 * _cropRect.Height / 3, _cropRect.Right, _cropRect.Y + 2 * _cropRect.Height / 3);
                }

                using (Pen crosshairPen = new Pen(Color.White, 1))
                {
                    int centerX = _cropRect.X + _cropRect.Width / 2;
                    int centerY = _cropRect.Y + _cropRect.Height / 2;
                    int crossSize = 10;
                    g.DrawLine(crosshairPen, centerX - crossSize, centerY, centerX + crossSize, centerY);
                    g.DrawLine(crosshairPen, centerX, centerY - crossSize, centerX, centerY + crossSize);
                }

                using (SolidBrush handleBrush = new SolidBrush(Color.White))
                {
                    foreach (HitTestRegion region in Enum.GetValues(typeof(HitTestRegion)))
                    {
                        if (region != HitTestRegion.None && region != HitTestRegion.Move)
                        {
                            Rectangle handleRect = GetHandleRect(region);
                            g.FillRectangle(handleBrush, handleRect);
                            g.DrawRectangle(Pens.Black, handleRect);
                        }
                    }
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left)
                {
                    _hitRegion = HitTest(e.Location);
                    if (_hitRegion != HitTestRegion.None)
                    {
                        _isDragging = true;
                        _dragStartPoint = e.Location;
                        Capture = true;
                    }
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);

                if (_isDragging)
                {
                    int dx = e.X - _dragStartPoint.X;
                    int dy = e.Y - _dragStartPoint.Y;

                    Rectangle newRect = _cropRect;
                    Rectangle videoDisplayRect = GetVideoDisplayRect();

                    if (videoDisplayRect.IsEmpty) return;

                    EventHandler<Rectangle>? tempHandler = CropRectChanged;
                    CropRectChanged = null;

                    switch (_hitRegion)
                    {
                        case HitTestRegion.Move:
                            newRect.X += dx;
                            newRect.Y += dy;
                            break;
                        case HitTestRegion.TopLeft:
                            newRect.X += dx;
                            newRect.Y += dy;
                            newRect.Width -= dx;
                            newRect.Height -= dy;
                            break;
                        case HitTestRegion.Top:
                            newRect.Y += dy;
                            newRect.Height -= dy;
                            break;
                        case HitTestRegion.TopRight:
                            newRect.Y += dy;
                            newRect.Width += dx;
                            newRect.Height -= dy;
                            break;
                        case HitTestRegion.Left:
                            newRect.X += dx;
                            newRect.Width -= dx;
                            break;
                        case HitTestRegion.Right:
                            newRect.Width += dx;
                            break;
                        case HitTestRegion.BottomLeft:
                            newRect.X += dx;
                            newRect.Width -= dx;
                            newRect.Height += dy;
                            break;
                        case HitTestRegion.Bottom:
                            newRect.Height += dy;
                            break;
                        case HitTestRegion.BottomRight:
                            newRect.Width += dx;
                            newRect.Height += dy;
                            break;
                    }

                    if (newRect.Width < MinCropSize) { if (_hitRegion.HasFlag(HitTestRegion.Left)) newRect.X -= (MinCropSize - newRect.Width); newRect.Width = MinCropSize; }
                    if (newRect.Height < MinCropSize) { if (_hitRegion.HasFlag(HitTestRegion.Top)) newRect.Y -= (MinCropSize - newRect.Height); newRect.Height = MinCropSize; }

                    int clampedX = Math.Max(videoDisplayRect.X, newRect.X);
                    int clampedY = Math.Max(videoDisplayRect.Y, newRect.Y);
                    int clampedRight = Math.Min(videoDisplayRect.Right, newRect.Right);
                    int clampedBottom = Math.Min(videoDisplayRect.Bottom, newRect.Bottom);

                    newRect = new Rectangle(clampedX, clampedY, clampedRight - clampedX, clampedBottom - clampedY);

                    if (_hitRegion == HitTestRegion.Move)
                    {
                        if (newRect.X < videoDisplayRect.X) newRect.X = videoDisplayRect.X;
                        if (newRect.Y < videoDisplayRect.Y) newRect.Y = videoDisplayRect.Y;
                        if (newRect.Right > videoDisplayRect.Right) newRect.X = videoDisplayRect.Right - newRect.Width;
                        if (newRect.Bottom > videoDisplayRect.Bottom) newRect.Y = videoDisplayRect.Bottom - newRect.Height;
                    }

                    _cropRect = newRect;

                    Invalidate();

                    _dragStartPoint = e.Location;

                    CropRectChanged = tempHandler;
                    CropRectChanged?.Invoke(this, _cropRect);
                }
                else
                {
                    Cursor = GetCursor(HitTest(e.Location));
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                _isDragging = false;
                _hitRegion = HitTestRegion.None;
                Capture = false;
                Cursor = Cursors.Default;
            }

            public Rectangle UiToVideoCoordinates(Rectangle uiRect)
            {
                if (VideoSize.Width == 0 || VideoSize.Height == 0) return Rectangle.Empty;
                Rectangle videoDisplayRect = GetVideoDisplayRect();

                if (videoDisplayRect.Width == 0 || videoDisplayRect.Height == 0) return Rectangle.Empty;

                double scaleX = (double)VideoSize.Width / videoDisplayRect.Width;
                double scaleY = (double)VideoSize.Height / videoDisplayRect.Height;

                int x = (int)Math.Round((uiRect.X - videoDisplayRect.X) * scaleX);
                int y = (int)Math.Round((uiRect.Y - videoDisplayRect.Y) * scaleY);
                int width = (int)Math.Round(uiRect.Width * scaleX);
                int height = (int)Math.Round(uiRect.Height * scaleY);

                x = Math.Max(0, Math.Min(x, VideoSize.Width));
                y = Math.Max(0, Math.Min(y, VideoSize.Height));
                width = Math.Max(0, Math.Min(width, VideoSize.Width - x));
                height = Math.Max(0, Math.Min(height, VideoSize.Height - y));

                return new Rectangle(x, y, width, height);
            }

            public Rectangle VideoToUiCoordinates(Rectangle videoRect)
            {
                if (VideoSize.Width == 0 || VideoSize.Height == 0) return Rectangle.Empty;
                Rectangle videoDisplayRect = GetVideoDisplayRect();

                if (videoDisplayRect.Width == 0 || videoDisplayRect.Height == 0) return Rectangle.Empty;

                double scaleX = (double)videoDisplayRect.Width / VideoSize.Width;
                double scaleY = (double)videoDisplayRect.Height / VideoSize.Height;

                int x = (int)Math.Round(videoRect.X * scaleX + videoDisplayRect.X);
                int y = (int)Math.Round(videoRect.Y * scaleY + videoDisplayRect.Y);
                int width = (int)Math.Round(videoRect.Width * scaleX);
                int height = (int)Math.Round(videoRect.Height * scaleY);

                x = Math.Max(videoDisplayRect.X, Math.Min(x, videoDisplayRect.Right));
                y = Math.Max(videoDisplayRect.Y, Math.Min(y, videoDisplayRect.Bottom));
                width = Math.Max(0, Math.Min(width, videoDisplayRect.Right - x));
                height = Math.Max(0, Math.Min(height, videoDisplayRect.Bottom - y));

                return new Rectangle(x, y, width, height);
            }

            private Rectangle GetVideoDisplayRect()
            {
                if (VideoSize.Width == 0 || VideoSize.Height == 0 || ClientSize.Width == 0 || ClientSize.Height == 0)
                {
                    return Rectangle.Empty;
                }

                float containerAspectRatio = (float)ClientSize.Width / ClientSize.Height;
                float videoAspectRatio = (float)VideoSize.Width / VideoSize.Height;

                int displayWidth, displayHeight;
                int x = 0, y = 0;

                if (videoAspectRatio > containerAspectRatio)
                {
                    displayWidth = ClientSize.Width;
                    displayHeight = (int)Math.Round(displayWidth / videoAspectRatio);
                    y = (ClientSize.Height - displayHeight) / 2;
                }
                else
                {
                    displayHeight = ClientSize.Height;
                    displayWidth = (int)Math.Round(displayHeight * videoAspectRatio);
                    x = (ClientSize.Width - displayWidth) / 2;
                }

                return new Rectangle(x, y, displayWidth, displayHeight);
            }

            private HitTestRegion HitTest(Point p)
            {
                if (_cropRect.IsEmpty) return HitTestRegion.None;

                Rectangle topLeftHandle = GetHandleRect(HitTestRegion.TopLeft);
                if (topLeftHandle.Contains(p)) return HitTestRegion.TopLeft;
                Rectangle topRightHandle = GetHandleRect(HitTestRegion.TopRight);
                if (topRightHandle.Contains(p)) return HitTestRegion.TopRight;
                Rectangle bottomLeftHandle = GetHandleRect(HitTestRegion.BottomLeft);
                if (bottomLeftHandle.Contains(p)) return HitTestRegion.BottomLeft;
                Rectangle bottomRightHandle = GetHandleRect(HitTestRegion.BottomRight);
                if (bottomRightHandle.Contains(p)) return HitTestRegion.BottomRight;

                Rectangle topHandle = GetHandleRect(HitTestRegion.Top);
                if (topHandle.Contains(p)) return HitTestRegion.Top;
                Rectangle bottomHandle = GetHandleRect(HitTestRegion.Bottom);
                if (bottomHandle.Contains(p)) return HitTestRegion.Bottom;
                Rectangle leftHandle = GetHandleRect(HitTestRegion.Left);
                if (leftHandle.Contains(p)) return HitTestRegion.Left;
                Rectangle rightHandle = GetHandleRect(HitTestRegion.Right);
                if (rightHandle.Contains(p)) return HitTestRegion.Right;

                if (_cropRect.Contains(p)) return HitTestRegion.Move;

                return HitTestRegion.None;
            }

            private Rectangle GetHandleRect(HitTestRegion region)
            {
                switch (region)
                {
                    case HitTestRegion.TopLeft: return new Rectangle(_cropRect.X - HandleSize / 2, _cropRect.Y - HandleSize / 2, HandleSize, HandleSize);
                    case HitTestRegion.Top: return new Rectangle(_cropRect.X + _cropRect.Width / 2 - HandleSize / 2, _cropRect.Y - HandleSize / 2, HandleSize, HandleSize);
                    case HitTestRegion.TopRight: return new Rectangle(_cropRect.Right - HandleSize / 2, _cropRect.Y - HandleSize / 2, HandleSize, HandleSize);
                    case HitTestRegion.Left: return new Rectangle(_cropRect.X - HandleSize / 2, _cropRect.Y + _cropRect.Height / 2 - HandleSize / 2, HandleSize, HandleSize);
                    case HitTestRegion.Right: return new Rectangle(_cropRect.Right - HandleSize / 2, _cropRect.Y + _cropRect.Height / 2 - HandleSize / 2, HandleSize, HandleSize);
                    case HitTestRegion.BottomLeft: return new Rectangle(_cropRect.X - HandleSize / 2, _cropRect.Bottom - HandleSize / 2, HandleSize, HandleSize);
                    case HitTestRegion.Bottom: return new Rectangle(_cropRect.X + _cropRect.Width / 2 - HandleSize / 2, _cropRect.Bottom - HandleSize / 2, HandleSize, HandleSize);
                    case HitTestRegion.BottomRight: return new Rectangle(_cropRect.Right - HandleSize / 2, _cropRect.Bottom - HandleSize / 2, HandleSize, HandleSize);
                    default: return Rectangle.Empty;
                }
            }

            private Cursor GetCursor(HitTestRegion region)
            {
                switch (region)
                {
                    case HitTestRegion.TopLeft: return Cursors.SizeNWSE;
                    case HitTestRegion.Top: return Cursors.SizeNS;
                    case HitTestRegion.TopRight: return Cursors.SizeNESW;
                    case HitTestRegion.Left: return Cursors.SizeWE;
                    case HitTestRegion.Right: return Cursors.SizeWE;
                    case HitTestRegion.BottomLeft: return Cursors.SizeNESW;
                    case HitTestRegion.Bottom: return Cursors.SizeNS;
                    case HitTestRegion.BottomRight: return Cursors.SizeNWSE;
                    case HitTestRegion.Move: return Cursors.SizeAll;
                    default: return Cursors.Default;
                }
            }
        }

        private class SubtitleOverlay : Control
        {
            private string _currentText = string.Empty;
            private Font? _currentFont;
            private Color _currentForeColor = Color.White;
            private Color _currentBackColor = Color.FromArgb(200, 0, 0, 0);
            private bool _currentShowBackground = true;
            private bool _currentShowOutline = true;
            private ContentAlignment _currentAlignment = ContentAlignment.BottomCenter;

            public SubtitleOverlay(VideoPlayerPanel parent)
            {
                SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
                DoubleBuffered = true;
                AutoSize = false;
            }

            public void UpdateSubtitleDisplay(string text, Font font, Color foreColor, Color backColor, bool showBackground, bool showOutline, ContentAlignment alignment, Size parentSize)
            {
                _currentText = text;
                _currentFont = font;
                _currentForeColor = foreColor;
                _currentBackColor = backColor;
                _currentShowBackground = showBackground;
                _currentShowOutline = showOutline;
                _currentAlignment = alignment;

                if (string.IsNullOrWhiteSpace(_currentText) || _currentFont == null)
                {
                    this.Visible = false;
                    return;
                }

                this.Visible = true;

                using (var g = CreateGraphics())
                {
                    const int padding = 10;
                    var textSize = g.MeasureString(_currentText, _currentFont);

                    var rectWidth = (int)Math.Ceiling(textSize.Width) + padding * 2;
                    var rectHeight = (int)Math.Ceiling(textSize.Height) + padding * 2;

                    this.Size = new Size(rectWidth, rectHeight);

                    int x = (parentSize.Width - rectWidth) / 2;
                    int y;

                    switch (_currentAlignment)
                    {
                        case ContentAlignment.TopCenter:
                            y = padding;
                            break;
                        case ContentAlignment.MiddleCenter:
                            y = (parentSize.Height - rectHeight) / 2;
                            break;
                        case ContentAlignment.BottomCenter:
                        default:
                            y = parentSize.Height - rectHeight - padding;
                            break;
                    }
                    this.Location = new Point(x, y);
                }
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                if (string.IsNullOrWhiteSpace(_currentText) || _currentFont == null)
                {
                    return;
                }

                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var client = ClientRectangle;
                const int padding = 10;

                var textRect = new RectangleF(padding, padding, client.Width - padding * 2, client.Height - padding * 2);

                if (_currentShowBackground)
                {
                    using (var bgBrush = new SolidBrush(_currentBackColor))
                    {
                        g.FillRectangle(bgBrush, client);
                    }
                }

                if (_currentShowOutline)
                {
                    using (var outlineBrush = new SolidBrush(Color.Black))
                    {
                        var outlineOffset = 2;
                        g.DrawString(_currentText, _currentFont, outlineBrush, textRect.X - outlineOffset, textRect.Y - outlineOffset);
                        g.DrawString(_currentText, _currentFont, outlineBrush, textRect.X + outlineOffset, textRect.Y - outlineOffset);
                        g.DrawString(_currentText, _currentFont, outlineBrush, textRect.X - outlineOffset, textRect.Y + outlineOffset);
                        g.DrawString(_currentText, _currentFont, outlineBrush, textRect.X + outlineOffset, textRect.Y + outlineOffset);
                        g.DrawString(_currentText, _currentFont, outlineBrush, textRect.X - outlineOffset, textRect.Y);
                        g.DrawString(_currentText, _currentFont, outlineBrush, textRect.X + outlineOffset, textRect.Y);
                        g.DrawString(_currentText, _currentFont, outlineBrush, textRect.X, textRect.Y - outlineOffset);
                        g.DrawString(_currentText, _currentFont, outlineBrush, textRect.X, textRect.Y + outlineOffset);
                    }
                }

                using (var textBrush = new SolidBrush(_currentForeColor))
                {
                    g.DrawString(_currentText, _currentFont, textBrush, textRect);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                mediaPlayer.TimeChanged -= MediaPlayer_TimeChanged;
                mediaPlayer.EndReached -= MediaPlayer_EndReached;
                mediaPlayer.Dispose();
                libVlc.Dispose();
                subtitleFont?.Dispose();

                //cropOverlayForm.CropRectChanged = null;
                cropOverlayForm.Hide();
                cropOverlayForm.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}