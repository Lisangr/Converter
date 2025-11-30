using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Converter.UI
{
    public class CropPanel : Panel
    {
        private readonly CheckBox chkEnableCrop;
        private readonly NumericUpDown numX;
        private readonly NumericUpDown numY;
        private readonly NumericUpDown numWidth;
        private readonly NumericUpDown numHeight;
        private readonly FlowLayoutPanel pnlPresets;
        private readonly Button btnApplyCrop;

        private int _videoWidth;
        private int _videoHeight;
        private bool _suppressNumericEvents;
        private bool _suppressRectChangedEvent;

        public event EventHandler<Rectangle>? CropRectChangedByUser;
        public event EventHandler<Rectangle>? CropApplied;
        public event EventHandler<bool>? CropEnabledChanged; // New event

        public bool IsCropEnabled => chkEnableCrop.Checked;

        // Enum for aspect ratio presets
        private enum CropAspectRatioPreset
        {
            Free,
            InstagramPost_1_1,
            InstagramStory_9_16,
            YouTube_16_9,
            FacebookPortrait_4_5,
            FacebookLandscape_5_4,
            Facebook360_2_1,
            LinkedIn_1_2_4,
            Pinterest_2_3
        }

        public CropPanel()
        {
            BackColor = Color.White;
            Padding = new Padding(15);
            AutoScroll = true;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Padding = new Padding(10)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F)); // Left column fixed width
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Right column takes remaining space
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Single row takes all vertical space

            Controls.Add(mainLayout);

            // --- Left Section: Parameters ---
            var leftControlsFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0),
                Padding = new Padding(0, 0, 10, 0)
            };
            mainLayout.Controls.Add(leftControlsFlowPanel, 0, 0);

            chkEnableCrop = new CheckBox
            {
                Text = "Включить кадрирование",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 15)
            };
            chkEnableCrop.CheckedChanged += (_, _) => UpdateControlsState();
            leftControlsFlowPanel.Controls.Add(chkEnableCrop);

            // Position controls
            var lblPosition = new Label
            {
                Text = "Позиция (X, Y):",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 5)
            };
            leftControlsFlowPanel.Controls.Add(lblPosition);

            var posPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };
            leftControlsFlowPanel.Controls.Add(posPanel);

            var lblX = new Label { Text = "X:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 5, 5, 0) };
            posPanel.Controls.Add(lblX);
            numX = new NumericUpDown
            {
                Width = 80,
                Maximum = 9999,
                Enabled = false,
                Margin = new Padding(0, 0, 10, 0)
            };
            numX.ValueChanged += NumericUpDown_ValueChanged;
            posPanel.Controls.Add(numX);

            var lblY = new Label { Text = "Y:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 5, 5, 0) };
            posPanel.Controls.Add(lblY);
            numY = new NumericUpDown
            {
                Width = 80,
                Maximum = 9999,
                Enabled = false
            };
            numY.ValueChanged += NumericUpDown_ValueChanged;
            posPanel.Controls.Add(numY);

            // Size controls
            var lblSize = new Label
            {
                Text = "Размер (Ш, В):",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 5)
            };
            leftControlsFlowPanel.Controls.Add(lblSize);

            var sizePanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };
            leftControlsFlowPanel.Controls.Add(sizePanel);

            var lblWidth = new Label { Text = "Ширина:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 5, 5, 0) };
            sizePanel.Controls.Add(lblWidth);
            numWidth = new NumericUpDown
            {
                Width = 80,
                Maximum = 9999,
                Value = 1920,
                Enabled = false,
                Margin = new Padding(0, 0, 10, 0)
            };
            numWidth.ValueChanged += NumericUpDown_ValueChanged;
            sizePanel.Controls.Add(numWidth);

            var lblHeight = new Label { Text = "Высота:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 5, 5, 0) };
            sizePanel.Controls.Add(lblHeight);
            numHeight = new NumericUpDown
            {
                Width = 80,
                Maximum = 9999,
                Value = 1080,
                Enabled = false
            };
            numHeight.ValueChanged += NumericUpDown_ValueChanged;
            sizePanel.Controls.Add(numHeight);

            // Apply button
            btnApplyCrop = new Button
            {
                Text = "Применить кадрирование",
                Height = 40,
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(0, 15, 0, 0)
            };
            btnApplyCrop.FlatAppearance.BorderSize = 0;
            btnApplyCrop.Click += BtnApplyCrop_Click;
            leftControlsFlowPanel.Controls.Add(btnApplyCrop);


            // --- Right Section: Presets ---
            var rightPresetsFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(100, 0, 0, 0) // Moved 100 pixels to the right
            };
            mainLayout.Controls.Add(rightPresetsFlowPanel, 1, 0);

            // Presets Label
            var lblPresets = new Label
            {
                Text = "Пресеты соотношения сторон:",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 5)
            };
            rightPresetsFlowPanel.Controls.Add(lblPresets);

            // Presets FlowLayoutPanel
            pnlPresets = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown, // Changed to TopDown
                WrapContents = false, // Changed to false
                Margin = new Padding(0)
            };
            rightPresetsFlowPanel.Controls.Add(pnlPresets);
            LoadAspectRatioPresets();

            UpdateControlsState();
        }

        private void LoadAspectRatioPresets()
        {
            // Define presets with their display text and ratio (Width / Height)
            var presets = new Dictionary<CropAspectRatioPreset, (string text, double ratio)>()
            {
                { CropAspectRatioPreset.Free, ("Свободное", 0.0) }, // 0.0 indicates free aspect ratio
                { CropAspectRatioPreset.InstagramPost_1_1, ("1:1 (Instagram Post)", 1.0 / 1.0) },
                { CropAspectRatioPreset.InstagramStory_9_16, ("9:16 (Instagram & Snap story / TikTok)", 9.0 / 16.0) },
                { CropAspectRatioPreset.YouTube_16_9, ("16:9 (YouTube / Widescreen)", 16.0 / 9.0) },
                { CropAspectRatioPreset.FacebookPortrait_4_5, ("4:5 (Facebook / Twitter Portrait)", 4.0 / 5.0) },
                { CropAspectRatioPreset.FacebookLandscape_5_4, ("5:4 (Facebook / Twitter Landscape)", 5.0 / 4.0) },
                { CropAspectRatioPreset.Facebook360_2_1, ("2:1 (Facebook 360 Video)", 2.0 / 1.0) },
                { CropAspectRatioPreset.LinkedIn_1_2_4, ("1:2.4 (LinkedIn)", 1.0 / 2.4) },
                { CropAspectRatioPreset.Pinterest_2_3, ("2:3 (Pinterest)", 2.0 / 3.0) }
            };

            foreach (var preset in presets)
            {
                var radioButton = new RadioButton
                {
                    Text = preset.Value.text,
                    Tag = preset.Key, // Store the enum value in Tag
                    AutoSize = true,
                    Enabled = false,
                    Margin = new Padding(0, 0, 10, 5)
                };
                radioButton.CheckedChanged += PresetRadioButton_CheckedChanged;
                pnlPresets.Controls.Add(radioButton);
            }

            // Select 'Free' by default
            if (pnlPresets.Controls.OfType<RadioButton>().FirstOrDefault(r => (CropAspectRatioPreset)r.Tag == CropAspectRatioPreset.Free) is RadioButton defaultRadio)
            {
                defaultRadio.Checked = true;
            }
        }

        private void PresetRadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton rb && rb.Checked)
            {
                ApplyPreset((CropAspectRatioPreset)rb.Tag);
            }
        }

        private void ApplyPreset(CropAspectRatioPreset preset)
        {
            if (_videoWidth == 0 || _videoHeight == 0) return; // Cannot apply preset without video dimensions

            Rectangle newRect = GetCropData(); // Initialize with current data, will be overwritten if a specific preset is applied
            double targetRatio = 0.0;

            switch (preset)
            {
                case CropAspectRatioPreset.Free:
                    // For Free preset, use current values as the base, clamped to bounds.
                    // UpdateCropRectNumericControls will handle clamping and then invoke CropRectChangedByUser.
                    // No explicit ratio calculation needed, so targetRatio remains 0.0.
                    newRect = new Rectangle((int)numX.Value, (int)numY.Value, (int)numWidth.Value, (int)numHeight.Value);
                    break;
                case CropAspectRatioPreset.InstagramPost_1_1: targetRatio = 1.0 / 1.0; break;
                case CropAspectRatioPreset.InstagramStory_9_16: targetRatio = 9.0 / 16.0; break;
                case CropAspectRatioPreset.YouTube_16_9: targetRatio = 16.0 / 9.0; break;
                case CropAspectRatioPreset.FacebookPortrait_4_5: targetRatio = 4.0 / 5.0; break;
                case CropAspectRatioPreset.FacebookLandscape_5_4: targetRatio = 5.0 / 4.0; break;
                case CropAspectRatioPreset.Facebook360_2_1: targetRatio = 2.0 / 1.0; break;
                case CropAspectRatioPreset.LinkedIn_1_2_4: targetRatio = 1.0 / 2.4; break;
                case CropAspectRatioPreset.Pinterest_2_3: targetRatio = 2.0 / 3.0; break;
            }

            if (targetRatio > 0) // If a specific ratio is selected
            {
                // Calculate maximum possible crop rectangle for the given aspect ratio
                int calculatedWidth = _videoWidth;
                int calculatedHeight = _videoHeight;

                double videoRatio = (double)_videoWidth / _videoHeight;

                if (videoRatio > targetRatio) // Video is wider than target ratio
                {
                    calculatedWidth = (int)(_videoHeight * targetRatio);
                    calculatedHeight = _videoHeight;
                }
                else // Video is taller than target ratio
                {
                    calculatedWidth = _videoWidth;
                    calculatedHeight = (int)(_videoWidth / targetRatio);
                }
                
                // Center the new rectangle
                int newX = (_videoWidth - calculatedWidth) / 2;
                int newY = (_videoHeight - calculatedHeight) / 2;
                newRect = new Rectangle(newX, newY, calculatedWidth, calculatedHeight);
            }
            
            UpdateCropRectNumericControls(newRect);
            // Explicitly invoke the event as per requirements, if crop is enabled and not suppressed
            if (IsCropEnabled && !_suppressRectChangedEvent)
            {
                CropRectChangedByUser?.Invoke(this, newRect);
            }
        }

        private void UpdateCropRectNumericControls(Rectangle rect)
        {
            _suppressNumericEvents = true;

            numX.Value = Math.Max(0, Math.Min(rect.X, _videoWidth));
            numY.Value = Math.Max(0, Math.Min(rect.Y, _videoHeight));
            numWidth.Value = Math.Max(0, Math.Min(rect.Width, _videoWidth - numX.Value));
            numHeight.Value = Math.Max(0, Math.Min(rect.Height, _videoHeight - numY.Value));

            _suppressNumericEvents = false;

            if (!_suppressRectChangedEvent)
            {
                CropRectChangedByUser?.Invoke(this, GetCropData());
            }
        }

        private void NumericUpDown_ValueChanged(object? sender, EventArgs e)
        {
            if (_suppressNumericEvents)
            {
                return;
            }

            _suppressNumericEvents = true;

            // Ensure crop rectangle stays within video bounds and is valid
            int x = (int)numX.Value;
            int y = (int)numY.Value;
            int width = (int)numWidth.Value;
            int height = (int)numHeight.Value;

            // Clamp values to prevent going out of bounds or negative sizes
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (width < 1) width = 1;
            if (height < 1) height = 1;

            // Adjust width/height if they exceed video dimensions from current x,y
            if (x + width > _videoWidth && _videoWidth > 0) width = _videoWidth - x;
            if (y + height > _videoHeight && _videoHeight > 0) height = _videoHeight - y;

            // Adjust x/y if they exceed video dimensions
            if (x > _videoWidth && _videoWidth > 0) x = _videoWidth - width;
            if (y > _videoHeight && _videoHeight > 0) y = _videoHeight - height;
            
            // Update values back to controls if they were clamped
            numX.Value = x;
            numY.Value = y;
            numWidth.Value = width;
            numHeight.Value = height;

            // If a fixed aspect ratio is selected, adjust other dimension
            if (pnlPresets.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked && (CropAspectRatioPreset)r.Tag != CropAspectRatioPreset.Free) is RadioButton selectedRatioButton)
            {
                double targetRatio = 0.0;
                switch ((CropAspectRatioPreset)selectedRatioButton.Tag)
                {
                    case CropAspectRatioPreset.InstagramPost_1_1: targetRatio = 1.0 / 1.0; break;
                    case CropAspectRatioPreset.InstagramStory_9_16: targetRatio = 9.0 / 16.0; break;
                    case CropAspectRatioPreset.YouTube_16_9: targetRatio = 16.0 / 9.0; break;
                    case CropAspectRatioPreset.FacebookPortrait_4_5: targetRatio = 4.0 / 5.0; break;
                    case CropAspectRatioPreset.FacebookLandscape_5_4: targetRatio = 5.0 / 4.0; break;
                    case CropAspectRatioPreset.Facebook360_2_1: targetRatio = 2.0 / 1.0; break;
                    case CropAspectRatioPreset.LinkedIn_1_2_4: targetRatio = 1.0 / 2.4; break;
                    case CropAspectRatioPreset.Pinterest_2_3: targetRatio = 2.0 / 3.0; break;
                }

                if (targetRatio > 0)
                {
                    if (sender == numWidth) // Width changed, adjust Height
                    {
                        double widthValue = (double)numWidth.Value;
                        int adjustedHeight = (int)(widthValue / targetRatio);
                        int maxHeight = _videoHeight - (int)numY.Value;
                        if (maxHeight < 0) maxHeight = 0;
                        if (adjustedHeight > maxHeight) adjustedHeight = maxHeight;
                        if (adjustedHeight < 0) adjustedHeight = 0;
                        numHeight.Value = adjustedHeight;
                    }
                    else if (sender == numHeight) // Height changed, adjust Width
                    {
                        double heightValue = (double)numHeight.Value;
                        int adjustedWidth = (int)(heightValue * targetRatio);
                        int maxWidth = _videoWidth - (int)numX.Value;
                        if (maxWidth < 0) maxWidth = 0;
                        if (adjustedWidth > maxWidth) adjustedWidth = maxWidth;
                        if (adjustedWidth < 0) adjustedWidth = 0;
                        numWidth.Value = adjustedWidth;
                    }
                }
            }

            _suppressNumericEvents = false;

            if (!_suppressRectChangedEvent)
            {
                CropRectChangedByUser?.Invoke(this, GetCropData());
            }
        }

        private void BtnApplyCrop_Click(object? sender, EventArgs e)
        {
            CropApplied?.Invoke(this, GetCropData());
        }

        private void UpdateControlsState()
        {
            var enabled = chkEnableCrop.Checked;
            btnApplyCrop.Enabled = enabled;
            numX.Enabled = enabled;
            numY.Enabled = enabled;
            numWidth.Enabled = enabled;
            numHeight.Enabled = enabled;

            foreach (RadioButton rb in pnlPresets.Controls.OfType<RadioButton>())
            {
                rb.Enabled = enabled;
            }

            // If crop is enabled, ensure a preset is applied to initialize the values.
            if (enabled)
            {
                if (pnlPresets.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked) is RadioButton selectedRadio)
                {
                    ApplyPreset((CropAspectRatioPreset)selectedRadio.Tag);
                } else { // If nothing is checked, default to Free
                    if (pnlPresets.Controls.OfType<RadioButton>().FirstOrDefault(r => (CropAspectRatioPreset)r.Tag == CropAspectRatioPreset.Free) is RadioButton freeRadio) {
                        freeRadio.Checked = true;
                    }
                }
            }
            else
            {
                // When disabled, reset values or make them irrelevant
                _suppressNumericEvents = true;
                _suppressRectChangedEvent = true;
                numX.Value = 0;
                numY.Value = 0;
                numWidth.Value = 0;
                numHeight.Value = 0;
                _suppressNumericEvents = false;
                _suppressRectChangedEvent = false;
            }
            CropEnabledChanged?.Invoke(this, enabled);
        }
        public void SetVideoDimensions(int width, int height)
        {
            _videoWidth = width;
            _videoHeight = height;

            // Update maximums for numeric up-down controls
            numX.Maximum = width > 0 ? width : 9999;
            numY.Maximum = height > 0 ? height : 9999;
            numWidth.Maximum = width > 0 ? width : 9999;
            numHeight.Maximum = height > 0 ? height : 9999;

            // When video dimensions are set, re-apply the current preset to fit new dimensions
            if (chkEnableCrop.Checked)
            {
                if (pnlPresets.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked) is RadioButton selectedRadio)
                {
                    ApplyPreset((CropAspectRatioPreset)selectedRadio.Tag);
                }
                else
                { // If nothing is checked, default to Free
                    if (pnlPresets.Controls.OfType<RadioButton>().FirstOrDefault(r => (CropAspectRatioPreset)r.Tag == CropAspectRatioPreset.Free) is RadioButton freeRadio)
                    {
                        freeRadio.Checked = true;
                    }
                }
            }
        }

        public void SetCropRect(Rectangle rect)
        {
            _suppressRectChangedEvent = true;
            UpdateCropRectNumericControls(rect);
            _suppressRectChangedEvent = false;
        }

        public Rectangle GetCropData() =>
            new Rectangle((int)numX.Value, (int)numY.Value, (int)numWidth.Value, (int)numHeight.Value);
    }
}