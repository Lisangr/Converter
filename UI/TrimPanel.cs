using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Xabe.FFmpeg;

namespace Converter.UI
{
    public class TrimPanel : Panel
    {
        private readonly VideoPlayerPanel videoPlayer;
        private readonly CheckBox chkEnableTrim;
        private readonly MaskedTextBox txtStartTime;
        private readonly MaskedTextBox txtEndTime;
        private readonly Button btnSetStart;
        private readonly Button btnSetEnd;
        private IMediaInfo? mediaInfo;

        public bool IsTrimEnabled => chkEnableTrim.Checked;

        public TrimPanel(VideoPlayerPanel player)
        {
            videoPlayer = player;

            chkEnableTrim = new CheckBox
            {
                Text = "Включить обрезку",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            chkEnableTrim.CheckedChanged += (_, _) => UpdateControlsState();
            Controls.Add(chkEnableTrim);

            var lblStart = new Label
            {
                Text = "Начало:",
                Location = new Point(40, 60),
                AutoSize = true
            };
            Controls.Add(lblStart);

            txtStartTime = new MaskedTextBox
            {
                Location = new Point(110, 57),
                Width = 120,
                Mask = "00:00:00",
                Font = new Font("Consolas", 10),
                Enabled = false
            };
            Controls.Add(txtStartTime);

            btnSetStart = new Button
            {
                Text = "⏱ Текущее время",
                Location = new Point(240, 55),
                Size = new Size(130, 25),
                Enabled = false
            };
            btnSetStart.Click += (_, _) => txtStartTime.Text = FormatTime(videoPlayer.GetCurrentTime());
            Controls.Add(btnSetStart);

            var lblEnd = new Label
            {
                Text = "Конец:",
                Location = new Point(40, 100),
                AutoSize = true
            };
            Controls.Add(lblEnd);

            txtEndTime = new MaskedTextBox
            {
                Location = new Point(110, 97),
                Width = 120,
                Mask = "00:00:00",
                Font = new Font("Consolas", 10),
                Enabled = false
            };
            Controls.Add(txtEndTime);

            btnSetEnd = new Button
            {
                Text = "⏱ Текущее время",
                Location = new Point(240, 95),
                Size = new Size(130, 25),
                Enabled = false
            };
            btnSetEnd.Click += (_, _) => txtEndTime.Text = FormatTime(videoPlayer.GetCurrentTime());
            Controls.Add(btnSetEnd);
        }

        private void UpdateControlsState()
        {
            var enabled = chkEnableTrim.Checked;
            txtStartTime.Enabled = enabled;
            txtEndTime.Enabled = enabled;
            btnSetStart.Enabled = enabled;
            btnSetEnd.Enabled = enabled;
        }

        public void SetMediaInfo(IMediaInfo info)
        {
            mediaInfo = info;
            txtStartTime.Text = "00:00:00";
            txtEndTime.Text = FormatTime(info.Duration);
        }

        public (TimeSpan StartTime, TimeSpan Duration) GetTrimData()
        {
            var start = ParseTime(txtStartTime.Text);
            var end = ParseTime(txtEndTime.Text);
            if (mediaInfo != null)
            {
                end = TimeSpan.FromSeconds(Math.Min(end.TotalSeconds, mediaInfo.Duration.TotalSeconds));
            }

            if (end <= start)
            {
                end = start.Add(TimeSpan.FromSeconds(1));
            }

            return (start, end - start);
        }

        private static TimeSpan ParseTime(string text)
        {
            if (TimeSpan.TryParseExact(text, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            throw new FormatException("Неверный формат времени");
        }

        private static string FormatTime(TimeSpan time) => time.ToString("hh\\:mm\\:ss");
    }
}
