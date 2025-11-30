using System;
using System.Drawing;
using System.Windows.Forms;

namespace Converter.UI
{
    public class CropPanel : Panel
    {
        private readonly CheckBox chkEnableCrop;
        private readonly NumericUpDown numX;
        private readonly NumericUpDown numY;
        private readonly NumericUpDown numWidth;
        private readonly NumericUpDown numHeight;
        private readonly ComboBox cmbPresets;

        public bool IsCropEnabled => chkEnableCrop.Checked;

        public CropPanel(VideoPlayerPanel player)
        {
            BackColor = Color.White;
            Padding = new Padding(15);
            AutoScroll = true;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true,
                Padding = new Padding(10)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            Controls.Add(mainLayout);

            chkEnableCrop = new CheckBox
            {
                Text = "Включить кадрирование",
                Dock = DockStyle.Top,
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 15)
            };
            chkEnableCrop.CheckedChanged += (_, _) => UpdateControlsState();
            mainLayout.Controls.Add(chkEnableCrop, 0, 0);
            mainLayout.SetColumnSpan(chkEnableCrop, 2);

            // Preset selector
            var lblPreset = new Label
            {
                Text = "Пресет:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            mainLayout.Controls.Add(lblPreset, 0, 1);

            cmbPresets = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Margin = new Padding(0, 5, 0, 5)
            };
            cmbPresets.Items.AddRange(new object[]
            {
                "1:1 (Instagram)",
                "9:16 (Stories/Reels)",
                "16:9 (YouTube)",
                "4:5 (Instagram Feed)",
                "Пользовательское"
            });
            cmbPresets.SelectedIndex = 4;
            cmbPresets.SelectedIndexChanged += (_, _) => ApplyPreset();
            mainLayout.Controls.Add(cmbPresets, 1, 1);

            var posPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 10)
            };
            mainLayout.Controls.Add(posPanel, 1, 2);

            var lblX = new Label { Text = "X:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 5, 10, 0) };
            posPanel.Controls.Add(lblX);

            numX = new NumericUpDown
            {
                Width = 80,
                Maximum = 9999,
                Enabled = false,
                Margin = new Padding(0, 0, 20, 0)
            };
            posPanel.Controls.Add(numX);

            var lblY = new Label { Text = "Y:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 5, 10, 0) };
            posPanel.Controls.Add(lblY);

            numY = new NumericUpDown
            {
                Width = 80,
                Maximum = 9999,
                Enabled = false
            };
            posPanel.Controls.Add(numY);

            var posLabel = new Label
            {
                Text = "Позиция:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            mainLayout.Controls.Add(posLabel, 0, 2);

            var sizePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 10)
            };
            mainLayout.Controls.Add(sizePanel, 1, 3);

            var lblWidth = new Label { Text = "Ширина:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 5, 10, 0) };
            sizePanel.Controls.Add(lblWidth);

            numWidth = new NumericUpDown
            {
                Width = 80,
                Maximum = 9999,
                Value = 1920,
                Enabled = false,
                Margin = new Padding(0, 0, 20, 0)
            };
            sizePanel.Controls.Add(numWidth);

            var lblHeight = new Label { Text = "Высота:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 5, 10, 0) };
            sizePanel.Controls.Add(lblHeight);

            numHeight = new NumericUpDown
            {
                Width = 80,
                Maximum = 9999,
                Value = 1080,
                Enabled = false
            };
            sizePanel.Controls.Add(numHeight);

            var sizeLabel = new Label
            {
                Text = "Размер:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };
            mainLayout.Controls.Add(sizeLabel, 0, 3);
        }

        private void UpdateControlsState()
        {
            var enabled = chkEnableCrop.Checked;
            cmbPresets.Enabled = enabled;
            numX.Enabled = enabled;
            numY.Enabled = enabled;
            numWidth.Enabled = enabled;
            numHeight.Enabled = enabled;
        }

        private void ApplyPreset()
        {
            switch (cmbPresets.SelectedIndex)
            {
                case 0:
                    numWidth.Value = 1080;
                    numHeight.Value = 1080;
                    break;
                case 1:
                    numWidth.Value = 1080;
                    numHeight.Value = 1920;
                    break;
                case 2:
                    numWidth.Value = 1920;
                    numHeight.Value = 1080;
                    break;
                case 3:
                    numWidth.Value = 1080;
                    numHeight.Value = 1350;
                    break;
            }
        }

        public (int X, int Y, int Width, int Height) GetCropData() =>
            ((int)numX.Value, (int)numY.Value, (int)numWidth.Value, (int)numHeight.Value);
    }
}
