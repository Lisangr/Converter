using System;
using System.Drawing;
using System.Windows.Forms;
using Converter.Models;
using Converter.Services;

namespace Converter.UI.Controls
{
    public class EstimatePanel : UserControl
    {
        private Label lblTitle = new Label();
        private Label lblInput = new Label();
        private Label lblOutput = new Label();
        private Label lblSaved = new Label();
        private Label lblTime = new Label();
        private ProgressBar pbPerf = new ProgressBar();
        private Label lblPerf = new Label();
        private Panel perfPanel = new Panel();

        public bool ShowPerformanceBar
        {
            get => perfPanel.Visible;
            set => perfPanel.Visible = value;
        }

        private int _warningThreshold = 10; // minutes
        public int WarningThreshold
        {
            get => _warningThreshold;
            set => _warningThreshold = Math.Max(1, value);
        }

        private Theme CurrentTheme => ThemeManager.Instance.CurrentTheme;

        public EstimatePanel()
        {
            Dock = DockStyle.Top;
            Height = 110;
            BackColor = Color.White;
            Padding = new Padding(8);

            lblTitle.Text = "📊 Оценка конвертации";
            lblTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(6, 6);

            lblInput.AutoSize = true; lblInput.Location = new Point(10, 30);
            lblOutput.AutoSize = true; lblOutput.Location = new Point(10, 50);
            lblSaved.AutoSize = true; lblSaved.Location = new Point(10, 70);
            lblTime.AutoSize = true; lblTime.Location = new Point(10, 90);

            perfPanel.Dock = DockStyle.Right;
            perfPanel.Width = 220;
            perfPanel.Padding = new Padding(8);

            lblPerf.Text = "Производительность ПК";
            lblPerf.AutoSize = true;
            lblPerf.Location = new Point(0, 0);

            pbPerf.Width = 200;
            pbPerf.Location = new Point(0, 20);
            pbPerf.Minimum = 0;
            pbPerf.Maximum = 100;
            pbPerf.Value = 60;

            perfPanel.Controls.Add(pbPerf);
            perfPanel.Controls.Add(lblPerf);

            Controls.Add(lblTitle);
            Controls.Add(lblInput);
            Controls.Add(lblOutput);
            Controls.Add(lblSaved);
            Controls.Add(lblTime);
            Controls.Add(perfPanel);

            UpdateTheme(CurrentTheme);
        }

        public void ShowCalculating()
        {
            lblInput.Text = "Текущий размер: расчет...";
            lblOutput.Text = "После конвертации: ...";
            lblSaved.Text = "Экономия: ...";
            lblTime.Text = "⏱️ Примерное время: ...";
            lblTime.ForeColor = CurrentTheme["TextPrimary"];
        }

        public void UpdateEstimate(ConversionEstimate estimate)
        {
            lblInput.Text = $"Текущий размер: [{estimate.InputSizeFormatted}]";
            lblOutput.Text = $"После конвертации: [~{estimate.OutputSizeFormatted}]";
            lblSaved.Text = $"Экономия: [{estimate.SpaceSavedFormatted} ({estimate.SavingsPercent}%)]";
            lblTime.Text = $"⏱️ Примерное время: [~{estimate.DurationFormatted}]";

            // Simple perf mapping: shorter time => higher perf
            var mins = estimate.EstimatedDuration.TotalMinutes;
            int perf = (int)Math.Max(10, Math.Min(100, 100 - mins * 5));
            pbPerf.Value = Math.Max(pbPerf.Minimum, Math.Min(pbPerf.Maximum, perf));

            if (estimate.EstimatedDuration.TotalMinutes > WarningThreshold)
                lblTime.ForeColor = CurrentTheme["Warning"];
            else
                lblTime.ForeColor = CurrentTheme["TextPrimary"];
        }

        public void UpdateTheme(Theme theme)
        {
            BackColor = theme["BackgroundSecondary"];
            ForeColor = theme["TextPrimary"];
            lblTitle.ForeColor = theme["TextPrimary"];
            lblInput.ForeColor = theme["TextSecondary"];
            lblOutput.ForeColor = theme["TextSecondary"];
            lblSaved.ForeColor = theme["TextSecondary"];
            lblTime.ForeColor = theme["TextPrimary"];
            lblPerf.ForeColor = theme["TextPrimary"];
            perfPanel.BackColor = theme["Surface"];
            pbPerf.ForeColor = theme["Accent"];
            pbPerf.BackColor = theme["Border"];
            Invalidate();
        }
    }
}
