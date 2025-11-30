using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Domain.Models; // Убедитесь, что namespace совпадает с вашим проектом

namespace Converter.UI
{
    public class AudioPanel : Panel
    {
        private readonly CheckBox chkNormalize;
        private readonly ComboBox cmbNormMode;

        private readonly CheckBox chkNoiseReduction;
        private readonly ComboBox cmbNoiseStrength;

        private readonly NumericUpDown numFadeIn;
        private readonly NumericUpDown numFadeOut;

        private readonly CheckBox chkEqualizer;
        private readonly ComboBox cmbEqPreset;
        private readonly Button btnCustomEq;

        // Храним кастомные настройки эквалайзера
        private Dictionary<int, double> _customEqBands = new();

        public event EventHandler? ApplyRequested; // New event for apply button

        public AudioPanel()
        {
            BackColor = Color.White;
            Padding = new Padding(8, 8, 8, 4);
            AutoScroll = true;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Controls.Add(mainLayout);

            // --- Left Column Controls ---
            var leftFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 0, 6, 0) // Compact padding to separate columns
            };
            mainLayout.Controls.Add(leftFlowPanel, 0, 0);

            // --- 1. Громкость и Нормализация ---
            var grpVolume = CreateGroupBox("Громкость и Нормализация");
            leftFlowPanel.Controls.Add(grpVolume);

            chkNormalize = new CheckBox
            {
                Text = "Нормализация громкости",
                AutoSize = true,
                Location = new Point(12, 20) // Relative to GroupBox (more compact)
            };
            chkNormalize.CheckedChanged += (s, e) => cmbNormMode.Enabled = chkNormalize.Checked;
            grpVolume.Controls.Add(chkNormalize);

            var lblNormMode = new Label { Text = "Режим:", Location = new Point(12, 42), AutoSize = true };
            grpVolume.Controls.Add(lblNormMode);

            cmbNormMode = new ComboBox
            {
                Location = new Point(75, 39),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            // Заполняем Enum
            cmbNormMode.DataSource = Enum.GetValues(typeof(VolumeNormalizationMode));
            grpVolume.Controls.Add(cmbNormMode);

            // --- 3. Фейдинг (Затухание) ---
            var grpFade = CreateGroupBox("Плавное появление/затухание");
            leftFlowPanel.Controls.Add(grpFade);

            var lblFadeIn = new Label { Text = "Fade In (сек):", Location = new Point(12, 22), AutoSize = true };
            grpFade.Controls.Add(lblFadeIn);

            numFadeIn = new NumericUpDown
            {
                Location = new Point(105, 20),
                Width = 80,
                DecimalPlaces = 1,
                Increment = 0.5M,
                Maximum = 60
            };
            grpFade.Controls.Add(numFadeIn);

            var lblFadeOut = new Label { Text = "Fade Out (сек):", Location = new Point(12, 44), AutoSize = true };
            grpFade.Controls.Add(lblFadeOut);

            numFadeOut = new NumericUpDown
            {
                Location = new Point(105, 42),
                Width = 80,
                DecimalPlaces = 1,
                Increment = 0.5M,
                Maximum = 60
            };
            grpFade.Controls.Add(numFadeOut);

            // --- Right Column Controls ---
            var rightFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(6, 0, 0, 0) // Compact padding to separate columns
            };
            mainLayout.Controls.Add(rightFlowPanel, 1, 0);

            // --- 2. Шумоподавление ---
            var grpNoise = CreateGroupBox("Шумоподавление");
            rightFlowPanel.Controls.Add(grpNoise);

            chkNoiseReduction = new CheckBox
            {
                Text = "Подавление шума",
                AutoSize = true,
                Location = new Point(12, 20)
            };
            chkNoiseReduction.CheckedChanged += (s, e) => cmbNoiseStrength.Enabled = chkNoiseReduction.Checked;
            grpNoise.Controls.Add(chkNoiseReduction);

            var lblNoiseStr = new Label { Text = "Сила:", Location = new Point(12, 42), AutoSize = true };
            grpNoise.Controls.Add(lblNoiseStr);

            cmbNoiseStrength = new ComboBox
            {
                Location = new Point(75, 39),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cmbNoiseStrength.DataSource = Enum.GetValues(typeof(NoiseReductionStrength));
            cmbNoiseStrength.SelectedItem = NoiseReductionStrength.Medium; // Default
            grpNoise.Controls.Add(cmbNoiseStrength);

            // --- 4. Эквалайзер ---
            var grpEq = CreateGroupBox("Эквалайзер");
            rightFlowPanel.Controls.Add(grpEq);

            chkEqualizer = new CheckBox
            {
                Text = "Включить эквалайзер",
                AutoSize = true,
                Location = new Point(12, 20)
            };
            chkEqualizer.CheckedChanged += (s, e) => UpdateEqControls();
            grpEq.Controls.Add(chkEqualizer);

            var lblPreset = new Label { Text = "Пресет:", Location = new Point(12, 42), AutoSize = true };
            grpEq.Controls.Add(lblPreset);

            cmbEqPreset = new ComboBox
            {
                Location = new Point(75, 39),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cmbEqPreset.DataSource = Enum.GetValues(typeof(EqualizerPreset));
            cmbEqPreset.SelectedIndexChanged += (s, e) => UpdateEqControls();
            grpEq.Controls.Add(cmbEqPreset);

            btnCustomEq = new Button
            {
                Text = "Настроить...",
                Location = new Point(285, 38),
                Size = new Size(90, 24),
                Enabled = false
            };
            btnCustomEq.Click += BtnCustomEq_Click;
            grpEq.Controls.Add(btnCustomEq);

            // --- Apply Button (Footer) ---
            var pnlFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 0) // Compact top padding to separate from groups
            };
            Controls.Add(pnlFooter);
            pnlFooter.BringToFront(); // Ensure it's on top of other controls

            var btnApply = new Button
            {
                Text = "Применить аудио настройки",
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3),
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += (s, e) => ApplyRequested?.Invoke(this, EventArgs.Empty);
            pnlFooter.Controls.Add(btnApply);

            UpdateEqControls();
        }

        private GroupBox CreateGroupBox(string title)
        {
            return new GroupBox
            {
                Text = title,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(8, 18, 8, 6),
                Margin = new Padding(0, 0, 0, 6)
            };
        }
        public event Action<bool, EqualizerPreset>? LiveEqChanged; // Новое событие

        private void UpdateEqControls()
        {
            var enabled = chkEqualizer.Checked;
            cmbEqPreset.Enabled = enabled;

            var isCustom = false;
            if (cmbEqPreset.SelectedItem is EqualizerPreset preset)
            {
                isCustom = preset == EqualizerPreset.Custom;
            }
            btnCustomEq.Enabled = enabled && isCustom;

            // Вызываем событие для обновления звука в плеере
            if (cmbEqPreset.SelectedItem is EqualizerPreset currentPreset)
            {
                LiveEqChanged?.Invoke(enabled, currentPreset);
            }
        }

        private void BtnCustomEq_Click(object? sender, EventArgs e)
        {
            using var dlg = new CustomEqualizerForm(_customEqBands);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _customEqBands = new Dictionary<int, double>(dlg.EQBands);
                // При ручной настройке переключаем пресет в Custom, если он есть в enum
                if (Enum.IsDefined(typeof(EqualizerPreset), EqualizerPreset.Custom))
                {
                    cmbEqPreset.SelectedItem = EqualizerPreset.Custom;
                }
            }
        }

        public AudioProcessingOptions GetAudioOptions()
        {
            var options = new AudioProcessingOptions
            {
                NormalizeVolume = chkNormalize.Checked,
                NormalizationMode = cmbNormMode.Enabled && cmbNormMode.SelectedItem is VolumeNormalizationMode mode
                    ? mode
                    : VolumeNormalizationMode.None,
                NoiseReduction = chkNoiseReduction.Checked,
                NoiseReductionStrength = cmbNoiseStrength.Enabled && cmbNoiseStrength.SelectedItem is NoiseReductionStrength nr
                    ? nr
                    : NoiseReductionStrength.None,
                FadeInDuration = (double)numFadeIn.Value,
                FadeOutDuration = (double)numFadeOut.Value,
                UseEqualizer = chkEqualizer.Checked,
                EqualizerPreset = cmbEqPreset.Enabled && cmbEqPreset.SelectedItem is EqualizerPreset eq
                    ? eq
                    : EqualizerPreset.None,
                CustomEQBands = new Dictionary<int, double>(_customEqBands)
            };

            return options;
        }
    }
}
