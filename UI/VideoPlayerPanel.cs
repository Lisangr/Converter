using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Xabe.FFmpeg;
using System.Threading.Tasks;

namespace Converter.UI
{
    public class VideoPlayerPanel : Panel
    {
        private readonly PictureBox pictureBox;
        private readonly TrackBar trackSeek;
        private readonly Button btnPlay;
        private readonly Button btnPause;
        private readonly Label lblTime;
        private readonly Label lblDuration;

        private IMediaInfo? currentMedia;
        private readonly System.Windows.Forms.Timer playbackTimer;
        private TimeSpan currentPosition = TimeSpan.Zero;
        private bool isPlaying;

        public VideoPlayerPanel()
        {
            BackColor = Color.Black;

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };
            Controls.Add(pictureBox);

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
            trackSeek.ValueChanged += TrackSeek_ValueChanged;
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

            btnPlay = new Button
            {
                Text = "▶",
                Size = new Size(45, 30),
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(0, 0, 5, 0)
            };
            btnPlay.Click += BtnPlay_Click;
            buttonPanel.Controls.Add(btnPlay);

            btnPause = new Button
            {
                Text = "⏸",
                Size = new Size(45, 30),
                Font = new Font("Segoe UI", 10),
                Enabled = false,
                Margin = new Padding(0, 0, 15, 0)
            };
            btnPause.Click += BtnPause_Click;
            buttonPanel.Controls.Add(btnPause);

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

            playbackTimer = new System.Windows.Forms.Timer { Interval = 100 };
            playbackTimer.Tick += PlaybackTimer_Tick;
        }

        public async Task LoadVideoAsync(string videoPath, IMediaInfo mediaInfo)
        {
            currentMedia = mediaInfo;
            lblDuration.Text = FormatTime(mediaInfo.Duration);
            currentPosition = TimeSpan.Zero;
            UpdateUI();

            var thumbnailPath = Path.Combine(Path.GetTempPath(), $"video_preview_{Guid.NewGuid():N}.jpg");

            try
            {
                var conversion = FFmpeg.Conversions.New()
                    .AddParameter($"-i \"{videoPath}\"")
                    .AddParameter("-vframes 1")
                    .AddParameter("-ss 00:00:01")
                    .SetOutput(thumbnailPath);

                await conversion.Start().ConfigureAwait(true);

                if (File.Exists(thumbnailPath))
                {
                    using var fs = new FileStream(thumbnailPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    pictureBox.Image = Image.FromStream(fs);
                }
            }
            catch
            {
                // ignore thumbnail errors
            }
            finally
            {
                if (File.Exists(thumbnailPath))
                {
                    try
                    {
                        File.Delete(thumbnailPath);
                    }
                    catch
                    {
                        // ignore cleanup errors
                    }
                }
            }
        }

        private void BtnPlay_Click(object? sender, EventArgs e)
        {
            isPlaying = true;
            btnPlay.Enabled = false;
            btnPause.Enabled = true;
            playbackTimer.Start();
        }

        private void BtnPause_Click(object? sender, EventArgs e)
        {
            isPlaying = false;
            btnPlay.Enabled = true;
            btnPause.Enabled = false;
            playbackTimer.Stop();
        }

        private void PlaybackTimer_Tick(object? sender, EventArgs e)
        {
            if (currentMedia == null)
            {
                return;
            }

            currentPosition = currentPosition.Add(TimeSpan.FromMilliseconds(100));

            if (currentPosition >= currentMedia.Duration)
            {
                currentPosition = currentMedia.Duration;
                BtnPause_Click(null, EventArgs.Empty);
            }

            UpdateUI();
        }

        private void TrackSeek_ValueChanged(object? sender, EventArgs e)
        {
            if (currentMedia == null)
            {
                return;
            }

            var percent = trackSeek.Value / (double)trackSeek.Maximum;
            currentPosition = TimeSpan.FromSeconds(currentMedia.Duration.TotalSeconds * percent);
            UpdateUI();
        }

        private void UpdateUI()
        {
            lblTime.Text = FormatTime(currentPosition);

            if (currentMedia != null)
            {
                var percent = currentPosition.TotalSeconds / currentMedia.Duration.TotalSeconds;
                trackSeek.Value = Math.Clamp((int)(percent * trackSeek.Maximum), 0, trackSeek.Maximum);
            }
        }

        private static string FormatTime(TimeSpan time) => time.ToString("hh\\:mm\\:ss");

        public TimeSpan GetCurrentTime() => currentPosition;

        public void SeekTo(TimeSpan time)
        {
            currentPosition = time;
            UpdateUI();
        }
    }
}
