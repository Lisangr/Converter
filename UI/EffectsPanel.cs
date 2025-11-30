using Converter.Application.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq; // Added for OfType<T>()
using System.Text;
using System.Windows.Forms;

namespace Converter.UI
{
    public class EffectsPanel : Panel
    {
        private readonly VideoPlayerPanel _videoPlayer;
        private readonly CheckBox chkBlur;
        private readonly CheckBox chkGrayscale;
        private readonly CheckBox chkSepia;
        private readonly CheckBox chkVignette;

        private readonly CheckBox chkBrightness;
        private readonly TrackBar trackBrightness;
        private readonly Label lblBrightnessValue;

        private readonly CheckBox chkContrast;
        private readonly TrackBar trackContrast;
        private readonly Label lblContrastValue;

        private readonly CheckBox chkSaturation;
        private readonly TrackBar trackSaturation;
        private readonly Label lblSaturationValue;

        private readonly CheckBox chkGamma;
        private readonly TrackBar trackGamma;
        private readonly Label lblGammaValue;

        private VideoAdjustments _currentVideoAdjustments; // Added

        public event EventHandler? ApplyRequested; // New event
        public event Action<VideoAdjustments>? LiveEffectChanged; // New event

        private void TriggerLiveEffectChanged()
        {
            // Логика приоритетов цветов:
            // 1. Если Grayscale -> Насыщенность 0.
            // 2. Если Sepia -> Насыщенность пониженная (0.3), чтобы имитировать старину (цвет тонирования VLC не умеет делать сам).
            // 3. Иначе -> Значение со слайдера.

            float saturation = chkSaturation.Checked ? trackSaturation.Value / 100.0f : 1.0f;

            if (chkGrayscale.Checked)
            {
                saturation = 0f;
            }
            else if (chkSepia.Checked)
            {
                // Для превью сепии делаем картинку блеклой
                saturation = 0.3f;
            }

            _currentVideoAdjustments = new VideoAdjustments
            {
                Brightness = chkBrightness.Checked ? trackBrightness.Value / 100.0f : 0f,
                Contrast = chkContrast.Checked ? trackContrast.Value / 100.0f : 0f,
                Saturation = saturation,
                Gamma = chkGamma.Checked ? trackGamma.Value / 100.0f : 1.0f,
                IsGrayscale = chkGrayscale.Checked,
                IsSepia = chkSepia.Checked,
                IsBlur = chkBlur.Checked,
                IsVignette = chkVignette.Checked
            };

            LiveEffectChanged?.Invoke(_currentVideoAdjustments);
        }

        public bool HasEffects => chkBlur.Checked || chkGrayscale.Checked || chkSepia.Checked || chkVignette.Checked ||
                                  chkBrightness.Checked || chkContrast.Checked || chkSaturation.Checked || chkGamma.Checked;

        public EffectsPanel(VideoPlayerPanel player)
        {
            _videoPlayer = player;
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

            // --- Left Column Controls (Filters & Background) ---
            var leftFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 0, 6, 0)
            };
            mainLayout.Controls.Add(leftFlowPanel, 0, 0);

            // Group 1: Background
            var grpBackground = CreateGroupBox("Фон");
            leftFlowPanel.Controls.Add(grpBackground);

            chkBlur = new CheckBox
            {
                Text = "Размытие фона (вертикальное в 16:9)",
                Location = new Point(12, 20),
                AutoSize = true
            };
            // Add event handler for live preview
            chkBlur.CheckedChanged += (s, e) => TriggerLiveEffectChanged();
            grpBackground.Controls.Add(chkBlur);

            // Group 2: Stylization
            var grpStylization = CreateGroupBox("Стилизация");
            leftFlowPanel.Controls.Add(grpStylization);

            chkGrayscale = new CheckBox
            {
                Text = "Оттенки серого",
                Location = new Point(12, 20),
                AutoSize = true
            };
            // Add event handler for live preview
            chkGrayscale.CheckedChanged += (s, e) => TriggerLiveEffectChanged();
            grpStylization.Controls.Add(chkGrayscale);

            chkSepia = new CheckBox
            {
                Text = "Сепия",
                Location = new Point(12, 40),
                AutoSize = true
            };
            // Add event handler for live preview
            chkSepia.CheckedChanged += (s, e) => TriggerLiveEffectChanged();
            grpStylization.Controls.Add(chkSepia);

            chkVignette = new CheckBox
            {
                Text = "Виньетка",
                Location = new Point(12, 60),
                AutoSize = true
            };
            // Add event handler for live preview
            chkVignette.CheckedChanged += (s, e) => TriggerLiveEffectChanged();
            grpStylization.Controls.Add(chkVignette);

            chkVignette = new CheckBox
            {
                Text = "Виньетка",
                Location = new Point(12, 60),
                AutoSize = true
            };
            // Add event handler for live preview
            chkVignette.CheckedChanged += (s, e) => TriggerLiveEffectChanged();
            grpStylization.Controls.Add(chkVignette);

            // Создаем и настраиваем всплывающие подсказки
            var tooltip = new ToolTip();
            tooltip.AutoPopDelay = 5000;
            tooltip.InitialDelay = 500;
            tooltip.ReshowDelay = 500;
            tooltip.ShowAlways = true;

            tooltip.SetToolTip(chkBlur, "Размытие фона — сложный эффект.\nОн будет применен только в итоговом файле при экспорте.\nВ предпросмотре не отображается.");
            tooltip.SetToolTip(chkVignette, "Виньетка будет видна только в итоговом файле при экспорте.\nВ предпросмотре не отображается.");

            // --- Right Column Controls (Color Correction) ---
            var rightFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(6, 0, 0, 0)
            };
            mainLayout.Controls.Add(rightFlowPanel, 1, 0);

            // Group 3: Adjustments
            var grpAdjustments = CreateGroupBox("Настройки цвета");
            rightFlowPanel.Controls.Add(grpAdjustments);

            // Brightness
            (chkBrightness, trackBrightness, lblBrightnessValue) = CreateAdjustmentControl(
                grpAdjustments, "Яркость", -100, 100, 0, UpdateBrightnessValue, 20);

            // Contrast
            (chkContrast, trackContrast, lblContrastValue) = CreateAdjustmentControl(
                grpAdjustments, "Контраст", -200, 200, 0, UpdateContrastValue, 56);

            // Saturation
            (chkSaturation, trackSaturation, lblSaturationValue) = CreateAdjustmentControl(
                grpAdjustments, "Насыщенность", 0, 300, 100, UpdateSaturationValue, 92);

            // Gamma
            (chkGamma, trackGamma, lblGammaValue) = CreateAdjustmentControl(
                grpAdjustments, "Гамма", 10, 400, 100, UpdateGammaValue, 128);


            // --- Apply Button (Footer) ---
            var pnlFooter = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 4, 0, 0)
            };
            Controls.Add(pnlFooter);
            pnlFooter.BringToFront();

            var btnApply = new Button
            {
                Text = "Применить эффекты",
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

            UpdateControlsState();

            // Initialize the current adjustments and trigger the event
            // This ensures the initial state is sent if needed
            TriggerLiveEffectChanged();
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

        private (CheckBox chk, TrackBar track, Label lbl) CreateAdjustmentControl(
            GroupBox parent,
            string text,
            int min,
            int max,
            int value,
            Action updateLabel,
            int yOffset)
        {
            var chk = new CheckBox
            {
                Text = text,
                Location = new Point(12, yOffset),
                AutoSize = true
            };

            var track = new TrackBar
            {
                Location = new Point(130, yOffset - 4), // Compact Y alignment with checkbox
                Width = 230,
                Minimum = min,
                Maximum = max,
                Value = value,
                TickStyle = TickStyle.None,
                Enabled = false,
                AutoSize = false,
                Height = 24,
                SmallChange = 5,
                LargeChange = 10
            };

            chk.CheckedChanged += (s, e) => { track.Enabled = chk.Checked; TriggerLiveEffectChanged(); };
            track.ValueChanged += (s, e) => { updateLabel(); TriggerLiveEffectChanged(); };

            var lbl = new Label
            {
                Text = (value / 100.0).ToString("0.00", CultureInfo.InvariantCulture),
                Location = new Point(368, yOffset + 1),
                AutoSize = true
            };

            parent.Controls.Add(chk);
            parent.Controls.Add(track);
            parent.Controls.Add(lbl);

            return (chk, track, lbl);
        }

        private void UpdateBrightnessValue() => lblBrightnessValue.Text = $"{trackBrightness.Value / 100.0:0.00}";
        private void UpdateContrastValue() => lblContrastValue.Text = $"{trackContrast.Value / 100.0:0.00}";
        private void UpdateSaturationValue() => lblSaturationValue.Text = $"{trackSaturation.Value / 100.0:0.00}";
        private void UpdateGammaValue() => lblGammaValue.Text = $"{trackGamma.Value / 100.0:0.00}";


        private void UpdateControlsState()
        {
            trackBrightness.Enabled = chkBrightness.Checked;
            trackContrast.Enabled = chkContrast.Checked;
            trackSaturation.Enabled = chkSaturation.Checked;
            trackGamma.Enabled = chkGamma.Checked;
            // No specific state needed for grayscale, sepia, vignette checkboxes themselves
            // The logic for applying them is in GetVideoFilterGraph
        }
        public string? GetVideoFilterGraph()
        {
            var filters = new List<string>();
            var eqParams = new List<string>();

            // 1. Сначала собираем "Линейные" фильтры (Цвет, Стилизация)

            // Оттенки серого
            if (chkGrayscale.Checked)
            {
                filters.Add("format=gray");
            }

            // Сепия
            if (chkSepia.Checked)
            {
                // ИСПРАВЛЕНО: добавлено ":0" в конце. Всего должно быть 12 значений (rr:rg:rb:ra:gr:gg:gb:ga:br:bg:bb:ba)
                filters.Add("colorchannelmixer=.393:.769:.189:0:.349:.686:.168:0:.272:.534:.131:0");
            }

            // Виньетка
            if (chkVignette.Checked)
            {
                // ИСПРАВЛЕНО: убраны одинарные кавычки, они могут ломать парсинг аргументов
                filters.Add("vignette=angle=PI/5:max_mode=linear:min_mode=linear:d=0.8:s=0.5");
            }

            // Настройки цвета (EQ)
            if (chkBrightness.Checked)
                eqParams.Add($"brightness={(trackBrightness.Value / 100.0).ToString("0.00", CultureInfo.InvariantCulture)}");

            if (chkContrast.Checked)
                eqParams.Add($"contrast={(trackContrast.Value / 100.0).ToString("0.00", CultureInfo.InvariantCulture)}");

            if (chkSaturation.Checked)
                eqParams.Add($"saturation={(trackSaturation.Value / 100.0).ToString("0.00", CultureInfo.InvariantCulture)}");

            if (chkGamma.Checked)
                eqParams.Add($"gamma={(trackGamma.Value / 100.0).ToString("0.00", CultureInfo.InvariantCulture)}");

            if (eqParams.Count > 0)
            {
                filters.Add($"eq={string.Join(":", eqParams)}");
            }

            // Объединяем простые фильтры в одну цепочку через запятую
            string linearChain = string.Join(",", filters);

            // 2. Обрабатываем сложный фильтр (Размытие фона)
            if (chkBlur.Checked)
            {
                // Для размытия фона нужен Complex Filter Graph
                StringBuilder blurBuilder = new StringBuilder();

                // Входной поток разделяем на [original] и [copy]
                blurBuilder.Append("split[original][copy];");

                // [copy] масштабируем, обрезаем, размываем -> получаем [blurred]
                // scale=ih*16/9:-1 -> пытаемся заполнить 16:9
                blurBuilder.Append("[copy]scale=ih*16/9:-1,crop=h=iw*9/16,gblur=sigma=20[blurred];");

                // Накладываем оригинал поверх размытого фона
                blurBuilder.Append("[blurred][original]overlay=(main_w-overlay_w)/2:(main_h-overlay_h)/2");

                // Если есть линейные фильтры (сепия, гамма и т.д.), применяем их к результату оверлея
                if (!string.IsNullOrEmpty(linearChain))
                {
                    blurBuilder.Append($",{linearChain}");
                }

                // Возвращаем сложный граф
                return blurBuilder.ToString();
            }
            else
            {
                // Если размытия нет, возвращаем просто цепочку линейных фильтров
                return string.IsNullOrEmpty(linearChain) ? null : linearChain;
            }
        }
    }
}