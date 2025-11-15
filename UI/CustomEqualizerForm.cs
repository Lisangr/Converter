using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Converter.UI
{
    public class CustomEqualizerForm : Form
    {
        private readonly int[] _frequencies = { 32, 64, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
        private readonly Dictionary<int, TrackBar> _frequencyBands = new();
        private readonly Dictionary<int, Label> _valueLabels = new();

        public Dictionary<int, double> EQBands { get; }

        public CustomEqualizerForm(Dictionary<int, double>? existingBands)
        {
            EQBands = existingBands != null
                ? new Dictionary<int, double>(existingBands)
                : new Dictionary<int, double>();

            InitializeComponent();
            LoadExistingValues();
        }

        private void InitializeComponent()
        {
            Text = "Настройка эквалайзера";
            Size = new Size(720, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var xPos = 20;
            const int spacing = 65;

            foreach (var freq in _frequencies)
            {
                var label = new Label
                {
                    Text = freq >= 1000 ? $"{freq / 1000} кГц" : $"{freq} Гц",
                    Location = new Point(xPos, 15),
                    Width = 55,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8)
                };
                Controls.Add(label);

                var trackBar = new TrackBar
                {
                    Location = new Point(xPos, 40),
                    Height = 210,
                    Orientation = Orientation.Vertical,
                    Minimum = -12,
                    Maximum = 12,
                    Value = 0,
                    TickFrequency = 3,
                    TickStyle = TickStyle.Both
                };
                trackBar.ValueChanged += (s, e) => UpdateLabel(freq, trackBar);
                _frequencyBands[freq] = trackBar;
                Controls.Add(trackBar);

                var valueLabel = new Label
                {
                    Location = new Point(xPos, 255),
                    Width = 55,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8, FontStyle.Bold)
                };
                _valueLabels[freq] = valueLabel;
                Controls.Add(valueLabel);

                UpdateLabel(freq, trackBar);
                xPos += spacing;
            }

            var btnReset = new Button
            {
                Text = "Сброс",
                Location = new Point(20, 290),
                Size = new Size(100, 32)
            };
            btnReset.Click += (s, e) => ResetAllBands();
            Controls.Add(btnReset);

            var btnOk = new Button
            {
                Text = "OK",
                Location = new Point(500, 290),
                Size = new Size(90, 32),
                DialogResult = DialogResult.OK
            };
            btnOk.Click += BtnOk_Click;
            Controls.Add(btnOk);

            var btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(600, 290),
                Size = new Size(90, 32),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void LoadExistingValues()
        {
            foreach (var freq in _frequencies)
            {
                if (_frequencyBands.TryGetValue(freq, out var track) && EQBands.TryGetValue(freq, out var value))
                {
                    var clamped = (int)Math.Clamp(Math.Round(value), track.Minimum, track.Maximum);
                    track.Value = clamped;
                    UpdateLabel(freq, track);
                }
            }
        }

        private void UpdateLabel(int frequency, TrackBar track)
        {
            if (_valueLabels.TryGetValue(frequency, out var label))
            {
                label.Text = $"{track.Value:+0;-0;0} dB";
            }
        }

        private void ResetAllBands()
        {
            foreach (var track in _frequencyBands.Values)
            {
                track.Value = 0;
            }
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            EQBands.Clear();
            foreach (var band in _frequencyBands)
            {
                if (band.Value.Value != 0)
                {
                    EQBands[band.Key] = band.Value.Value;
                }
            }
        }
    }
}
