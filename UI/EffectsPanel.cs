using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace Converter.UI
{
    public class EffectsPanel : Panel
    {
        private readonly CheckBox chkBlur;
        private readonly CheckBox chkBrightness;
        private readonly CheckBox chkSaturation;
        private readonly TrackBar trackBrightness;
        private readonly TrackBar trackSaturation;

        public bool HasEffects => chkBlur.Checked || chkBrightness.Checked || chkSaturation.Checked;

        public EffectsPanel(VideoPlayerPanel player)
        {
            chkBlur = new CheckBox
            {
                Text = "Размытие краёв (для вертикального видео)",
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(chkBlur);

            chkBrightness = new CheckBox
            {
                Text = "Яркость:",
                Location = new Point(20, 60),
                AutoSize = true
            };
            Controls.Add(chkBrightness);

            trackBrightness = new TrackBar
            {
                Location = new Point(120, 55),
                Width = 300,
                Minimum = -100,
                Maximum = 100,
                Value = 0
            };
            Controls.Add(trackBrightness);

            chkSaturation = new CheckBox
            {
                Text = "Насыщенность:",
                Location = new Point(20, 110),
                AutoSize = true
            };
            Controls.Add(chkSaturation);

            trackSaturation = new TrackBar
            {
                Location = new Point(140, 105),
                Width = 300,
                Minimum = 0,
                Maximum = 200,
                Value = 100
            };
            Controls.Add(trackSaturation);
        }

        public string? GetVideoFilterGraph()
        {
            var builder = new StringBuilder();

            if (chkBlur.Checked)
            {
                builder.Append("split[original][copy];[copy]scale=ih*16/9:-1,crop=h=iw*9/16,gblur=sigma=20[blurred];[blurred][original]overlay=(main_w-overlay_w)/2:(main_h-overlay_h)/2");
            }

            var eqParameters = new List<string>();
            if (chkBrightness.Checked)
            {
                var brightness = (trackBrightness.Value / 100.0).ToString("0.00", CultureInfo.InvariantCulture);
                eqParameters.Add($"brightness={brightness}");
            }

            if (chkSaturation.Checked)
            {
                var saturation = (trackSaturation.Value / 100.0).ToString("0.00", CultureInfo.InvariantCulture);
                eqParameters.Add($"saturation={saturation}");
            }

            if (eqParameters.Count > 0)
            {
                var eqFilter = $"eq={string.Join(":", eqParameters)}";
                if (builder.Length > 0)
                {
                    builder.Append(',').Append(eqFilter);
                }
                else
                {
                    builder.Append(eqFilter);
                }
            }

            return builder.Length > 0 ? builder.ToString() : null;
        }
    }
}
