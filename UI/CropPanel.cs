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
            chkEnableCrop = new CheckBox
            {
                Text = "Включить кадрирование",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            chkEnableCrop.CheckedChanged += (_, _) => UpdateControlsState();
            Controls.Add(chkEnableCrop);

            var lblPreset = new Label
            {
                Text = "Пресет:",
                Location = new Point(40, 55),
                AutoSize = true
            };
            Controls.Add(lblPreset);

            cmbPresets = new ComboBox
            {
                Location = new Point(110, 52),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
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
            Controls.Add(cmbPresets);

            numX = CreateNumericUpDown(70, 92);
            var lblX = new Label { Text = "X:", Location = new Point(40, 95), AutoSize = true };
            Controls.Add(lblX);
            Controls.Add(numX);

            numY = CreateNumericUpDown(200, 92);
            var lblY = new Label { Text = "Y:", Location = new Point(170, 95), AutoSize = true };
            Controls.Add(lblY);
            Controls.Add(numY);

            numWidth = CreateNumericUpDown(110, 132);
            numWidth.Value = 1920;
            var lblWidth = new Label { Text = "Ширина:", Location = new Point(40, 135), AutoSize = true };
            Controls.Add(lblWidth);
            Controls.Add(numWidth);

            numHeight = CreateNumericUpDown(280, 132);
            numHeight.Value = 1080;
            var lblHeight = new Label { Text = "Высота:", Location = new Point(210, 135), AutoSize = true };
            Controls.Add(lblHeight);
            Controls.Add(numHeight);
        }

        private NumericUpDown CreateNumericUpDown(int x, int y) => new NumericUpDown
        {
            Location = new Point(x, y),
            Width = 80,
            Maximum = 9999,
            Enabled = false
        };

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
