using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Services;

namespace Converter.UI
{
    public class AudioProcessingPanel : Panel
    {
        private EqualizerPreset _visualPreset = EqualizerPreset.None;
        private Dictionary<int, double> _visualCustomBands = new();

        private GroupBox grpNormalization = null!;
        private CheckBox chkNormalizeVolume = null!;
        private ComboBox cmbNormalizationMode = null!;
        private Label lblNormalizationInfo = null!;

        private GroupBox grpNoiseReduction = null!;
        private CheckBox chkNoiseReduction = null!;
        private TrackBar trackNoiseStrength = null!;
        private Label lblNoiseStrength = null!;
        private Label lblNoiseInfo = null!;

        private GroupBox grpEqualizer = null!;
        private CheckBox chkUseEqualizer = null!;
        private ComboBox cmbEqualizerPreset = null!;
        private Button btnCustomEqualizer = null!;
        private Panel pnlVisualization = null!;

        private GroupBox grpFade = null!;
        private CheckBox chkFadeIn = null!;
        private NumericUpDown numFadeIn = null!;
        private CheckBox chkFadeOut = null!;
        private NumericUpDown numFadeOut = null!;

        public AudioProcessingOptions Options { get; private set; }

        public AudioProcessingPanel()
        {
            Options = new AudioProcessingOptions();
            DoubleBuffered = true;
            InitializeComponents();
            UpdateOptions();
        }

        private void InitializeComponents()
        {
            AutoScroll = true;
            Dock = DockStyle.Fill;
            Padding = new Padding(10);

            var yPos = 10;

            grpNormalization = new GroupBox
            {
                Text = "🔊 Нормализация громкости",
                Location = new Point(10, yPos),
                Size = new Size(740, 110),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            Controls.Add(grpNormalization);

            chkNormalizeVolume = new CheckBox
            {
                Text = "Включить нормализацию громкости",
                Location = new Point(10, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            chkNormalizeVolume.CheckedChanged += (s, e) =>
            {
                cmbNormalizationMode.Enabled = chkNormalizeVolume.Checked;
                UpdateOptions();
            };
            grpNormalization.Controls.Add(chkNormalizeVolume);

            var lblMode = new Label
            {
                Text = "Режим:",
                Location = new Point(30, 55),
                AutoSize = true
            };
            grpNormalization.Controls.Add(lblMode);

            cmbNormalizationMode = new ComboBox
            {
                Location = new Point(90, 52),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cmbNormalizationMode.Items.AddRange(new object[]
            {
                "Отключено",
                "Peak (EBU R128)",
                "RMS (YouTube)",
                "Spotify (-14 LUFS)",
                "Пользовательская"
            });
            cmbNormalizationMode.SelectedIndex = (int)VolumeNormalizationMode.Peak;
            cmbNormalizationMode.SelectedIndexChanged += (s, e) => UpdateOptions();
            grpNormalization.Controls.Add(cmbNormalizationMode);

            lblNormalizationInfo = new Label
            {
                Text = "ℹ Выравнивает громкость до стандартного уровня",
                Location = new Point(320, 55),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };
            grpNormalization.Controls.Add(lblNormalizationInfo);

            yPos += 120;

            grpNoiseReduction = new GroupBox
            {
                Text = "🎤 Удаление шума",
                Location = new Point(10, yPos),
                Size = new Size(740, 130),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            Controls.Add(grpNoiseReduction);

            chkNoiseReduction = new CheckBox
            {
                Text = "Включить шумоподавление",
                Location = new Point(10, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            chkNoiseReduction.CheckedChanged += (s, e) =>
            {
                trackNoiseStrength.Enabled = chkNoiseReduction.Checked;
                UpdateOptions();
            };
            grpNoiseReduction.Controls.Add(chkNoiseReduction);

            var lblStrength = new Label
            {
                Text = "Сила:",
                Location = new Point(30, 60),
                AutoSize = true
            };
            grpNoiseReduction.Controls.Add(lblStrength);

            trackNoiseStrength = new TrackBar
            {
                Location = new Point(80, 55),
                Width = 320,
                Minimum = (int)NoiseReductionStrength.Light,
                Maximum = (int)NoiseReductionStrength.VeryStrong,
                TickFrequency = 1,
                TickStyle = TickStyle.BottomRight,
                Value = (int)NoiseReductionStrength.Medium,
                Enabled = false
            };
            trackNoiseStrength.ValueChanged += TrackNoiseStrength_ValueChanged;
            grpNoiseReduction.Controls.Add(trackNoiseStrength);

            lblNoiseStrength = new Label
            {
                Location = new Point(410, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            grpNoiseReduction.Controls.Add(lblNoiseStrength);

            lblNoiseInfo = new Label
            {
                Text = "ℹ Снижает фоновый шум. Высокие значения могут повлиять на качество",
                Location = new Point(30, 90),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };
            grpNoiseReduction.Controls.Add(lblNoiseInfo);
            UpdateNoiseStrengthLabel();

            yPos += 140;

            grpEqualizer = new GroupBox
            {
                Text = "🎛 Эквалайзер",
                Location = new Point(10, yPos),
                Size = new Size(740, 200),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            Controls.Add(grpEqualizer);

            chkUseEqualizer = new CheckBox
            {
                Text = "Включить эквалайзер",
                Location = new Point(10, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            chkUseEqualizer.CheckedChanged += (s, e) =>
            {
                var enabled = chkUseEqualizer.Checked;
                cmbEqualizerPreset.Enabled = enabled;
                btnCustomEqualizer.Enabled = enabled;
                UpdateOptions();
            };
            grpEqualizer.Controls.Add(chkUseEqualizer);

            var lblPreset = new Label
            {
                Text = "Пресет:",
                Location = new Point(30, 60),
                AutoSize = true
            };
            grpEqualizer.Controls.Add(lblPreset);

            cmbEqualizerPreset = new ComboBox
            {
                Location = new Point(90, 57),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cmbEqualizerPreset.Items.AddRange(new object[]
            {
                "Без изменений",
                "Пользовательский",
                "Басы",
                "Высокие",
                "Поп",
                "Рок",
                "Классика",
                "Джаз",
                "Голос/Подкаст",
                "Кино"
            });
            cmbEqualizerPreset.SelectedIndex = (int)EqualizerPreset.None;
            cmbEqualizerPreset.SelectedIndexChanged += CmbEqualizerPreset_SelectedIndexChanged;
            grpEqualizer.Controls.Add(cmbEqualizerPreset);

            btnCustomEqualizer = new Button
            {
                Text = "⚙ Настроить вручную",
                Location = new Point(320, 56),
                Size = new Size(170, 27),
                Enabled = false
            };
            btnCustomEqualizer.Click += BtnCustomEqualizer_Click;
            grpEqualizer.Controls.Add(btnCustomEqualizer);

            pnlVisualization = new Panel
            {
                Location = new Point(30, 95),
                Size = new Size(680, 85),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            pnlVisualization.Paint += VisualizationPaint;
            grpEqualizer.Controls.Add(pnlVisualization);

            yPos += 210;

            grpFade = new GroupBox
            {
                Text = "🎵 Плавное появление/затухание",
                Location = new Point(10, yPos),
                Size = new Size(740, 90),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            Controls.Add(grpFade);

            chkFadeIn = new CheckBox
            {
                Text = "Fade In (сек):",
                Location = new Point(10, 35),
                AutoSize = true
            };
            chkFadeIn.CheckedChanged += (s, e) =>
            {
                numFadeIn.Enabled = chkFadeIn.Checked;
                UpdateOptions();
            };
            grpFade.Controls.Add(chkFadeIn);

            numFadeIn = new NumericUpDown
            {
                Location = new Point(120, 32),
                Width = 80,
                Minimum = 0,
                Maximum = 10,
                DecimalPlaces = 1,
                Increment = 0.5M,
                Enabled = false
            };
            numFadeIn.ValueChanged += (s, e) => UpdateOptions();
            grpFade.Controls.Add(numFadeIn);

            chkFadeOut = new CheckBox
            {
                Text = "Fade Out (сек):",
                Location = new Point(240, 35),
                AutoSize = true
            };
            chkFadeOut.CheckedChanged += (s, e) =>
            {
                numFadeOut.Enabled = chkFadeOut.Checked;
                UpdateOptions();
            };
            grpFade.Controls.Add(chkFadeOut);

            numFadeOut = new NumericUpDown
            {
                Location = new Point(360, 32),
                Width = 80,
                Minimum = 0,
                Maximum = 10,
                DecimalPlaces = 1,
                Increment = 0.5M,
                Enabled = false
            };
            numFadeOut.ValueChanged += (s, e) => UpdateOptions();
            grpFade.Controls.Add(numFadeOut);
        }

        private void TrackNoiseStrength_ValueChanged(object? sender, EventArgs e)
        {
            UpdateNoiseStrengthLabel();
            UpdateOptions();
        }

        private void UpdateNoiseStrengthLabel()
        {
            var strength = (NoiseReductionStrength)Math.Max((int)NoiseReductionStrength.Light, trackNoiseStrength.Value);
            lblNoiseStrength.Text = strength switch
            {
                NoiseReductionStrength.Light => "Лёгкое",
                NoiseReductionStrength.Medium => "Среднее",
                NoiseReductionStrength.Strong => "Сильное",
                NoiseReductionStrength.VeryStrong => "Очень сильное",
                _ => "Среднее"
            };
        }

        private void CmbEqualizerPreset_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var preset = (EqualizerPreset)Math.Max(0, cmbEqualizerPreset.SelectedIndex);
            if (preset == EqualizerPreset.Custom && Options.CustomEQBands.Count > 0)
            {
                DrawCustomEqualizerVisualization(Options.CustomEQBands);
            }
            else
            {
                DrawEqualizerVisualization(preset);
            }

            UpdateOptions();
        }

        private void BtnCustomEqualizer_Click(object? sender, EventArgs e)
        {
            var dialogBands = new Dictionary<int, double>(Options.CustomEQBands);
            using var customForm = new CustomEqualizerForm(dialogBands);
            if (customForm.ShowDialog() == DialogResult.OK)
            {
                Options.CustomEQBands = new Dictionary<int, double>(customForm.EQBands);
                cmbEqualizerPreset.SelectedIndex = (int)EqualizerPreset.Custom;
                DrawCustomEqualizerVisualization(Options.CustomEQBands);
                UpdateOptions();
            }
        }

        private void VisualizationPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.FromArgb(240, 240, 240));

            using var pen = new Pen(Color.Gray, 1);
            var centerY = pnlVisualization.Height / 2;
            g.DrawLine(pen, 0, centerY, pnlVisualization.Width, centerY);

            IEnumerable<double> gains;
            if (_visualPreset == EqualizerPreset.Custom && _visualCustomBands.Count > 0)
            {
                gains = _visualCustomBands
                    .OrderBy(pair => pair.Key)
                    .Select(pair => pair.Value);
            }
            else if (_visualPreset != EqualizerPreset.None)
            {
                gains = GetPresetGains(_visualPreset);
            }
            else
            {
                return;
            }

            var gainArray = gains.ToArray();
            if (gainArray.Length == 0)
            {
                return;
            }

            var barWidth = Math.Max(10, pnlVisualization.Width / gainArray.Length);
            for (var i = 0; i < gainArray.Length; i++)
            {
                var gain = gainArray[i];
                var height = (int)(gain * 3);
                var barHeight = Math.Abs(height);
                var x = i * barWidth + 5;
                var y = height >= 0 ? centerY - barHeight : centerY;

                using var brush = new SolidBrush(Color.FromArgb(100, 150, 255));
                g.FillRectangle(brush, x, y, barWidth - 10, barHeight);
            }
        }

        private void DrawEqualizerVisualization(EqualizerPreset preset)
        {
            _visualPreset = preset;
            _visualCustomBands = new Dictionary<int, double>();
            pnlVisualization.Invalidate();
        }

        private void DrawCustomEqualizerVisualization(Dictionary<int, double> bands)
        {
            _visualPreset = EqualizerPreset.Custom;
            _visualCustomBands = new Dictionary<int, double>(bands);
            pnlVisualization.Invalidate();
        }

        private static double[] GetPresetGains(EqualizerPreset preset)
        {
            return preset switch
            {
                EqualizerPreset.Bass => new[] { 8.0, 5.0, 2.0, 0.0, -1.0, -1.0, 0.0 },
                EqualizerPreset.Treble => new[] { -1.0, 0.0, 1.0, 3.0, 6.0, 8.0, 8.0 },
                EqualizerPreset.Pop => new[] { -1.0, 0.0, 2.0, 4.0, 3.0, 1.0, -1.0 },
                EqualizerPreset.Rock => new[] { 5.0, 3.0, 2.0, 0.0, 1.0, 3.0, 4.0 },
                EqualizerPreset.Classical => new[] { 3.0, 2.0, 1.0, 0.0, 0.0, 4.0, 2.0 },
                EqualizerPreset.Jazz => new[] { 4.0, 1.0, -2.0, 0.0, 2.0, 3.0, 1.0 },
                EqualizerPreset.Vocal => new[] { -2.0, 0.0, 2.0, 4.0, 5.0, 3.0, 0.0 },
                EqualizerPreset.Cinema => new[] { 6.0, 3.0, 0.0, 0.0, -2.0, -1.0, 0.0 },
                _ => Array.Empty<double>()
            };
        }

        public AudioProcessingOptions GetOptions()
        {
            UpdateOptions();
            return Options;
        }

        public void ApplyOptions(AudioProcessingOptions options)
        {
            Options = options ?? new AudioProcessingOptions();
            chkNormalizeVolume.Checked = Options.NormalizeVolume;
            cmbNormalizationMode.SelectedIndex = (int)Options.NormalizationMode;

            chkNoiseReduction.Checked = Options.NoiseReduction;
            trackNoiseStrength.Value = (int)Math.Clamp((int)Options.NoiseReductionStrength, trackNoiseStrength.Minimum, trackNoiseStrength.Maximum);

            chkUseEqualizer.Checked = Options.UseEqualizer;
            cmbEqualizerPreset.SelectedIndex = (int)Options.EqualizerPreset;
            if (Options.EqualizerPreset == EqualizerPreset.Custom && Options.CustomEQBands.Count > 0)
            {
                DrawCustomEqualizerVisualization(Options.CustomEQBands);
            }

            if (Options.FadeInDuration > 0)
            {
                chkFadeIn.Checked = true;
                numFadeIn.Value = (decimal)Math.Min((double)numFadeIn.Maximum, Options.FadeInDuration);
            }
            else
            {
                chkFadeIn.Checked = false;
            }

            if (Options.FadeOutDuration > 0)
            {
                chkFadeOut.Checked = true;
                numFadeOut.Value = (decimal)Math.Min((double)numFadeOut.Maximum, Options.FadeOutDuration);
            }
            else
            {
                chkFadeOut.Checked = false;
            }

            UpdateOptions();
        }

        private void UpdateOptions()
        {
            Options.NormalizeVolume = chkNormalizeVolume.Checked;
            Options.NormalizationMode = (VolumeNormalizationMode)Math.Max(0, cmbNormalizationMode.SelectedIndex);

            Options.NoiseReduction = chkNoiseReduction.Checked;
            Options.NoiseReductionStrength = chkNoiseReduction.Checked
                ? (NoiseReductionStrength)Math.Max((int)NoiseReductionStrength.Light, trackNoiseStrength.Value)
                : NoiseReductionStrength.None;

            Options.UseEqualizer = chkUseEqualizer.Checked;
            Options.EqualizerPreset = chkUseEqualizer.Checked
                ? (EqualizerPreset)Math.Max(0, cmbEqualizerPreset.SelectedIndex)
                : EqualizerPreset.None;

            if (Options.EqualizerPreset != EqualizerPreset.Custom)
            {
                Options.CustomEQBands = Options.CustomEQBands ?? new Dictionary<int, double>();
                if (Options.CustomEQBands.Count > 0)
                {
                    Options.CustomEQBands.Clear();
                }
            }

            Options.FadeInDuration = chkFadeIn.Checked ? (double)numFadeIn.Value : 0;
            Options.FadeOutDuration = chkFadeOut.Checked ? (double)numFadeOut.Value : 0;
        }
    }
}
