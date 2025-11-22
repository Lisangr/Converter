using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Application.Services;
using Converter.Infrastructure;

namespace Converter.UI.Controls
{
    /// <summary>
    /// Панель с пресетами без поиска.
    /// Просто рисует пресеты, которые отдаёт PresetService.
    /// </summary>
    public class PresetPanel : UserControl
    {
        public event Action<PresetProfile>? PresetSelected;

        private readonly IPresetService _service;
        private readonly FlowLayoutPanel _root = new FlowLayoutPanel();
        private readonly Dictionary<string, PresetButton> _buttons = new();
        private readonly List<GroupBox> _groupBoxes = new();

        public PresetPanel(IPresetService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Padding = new Padding(8);

            _root.Dock = DockStyle.Fill;
            _root.FlowDirection = FlowDirection.TopDown;
            _root.WrapContents = false;
            _root.AutoScroll = true;
            _root.SizeChanged += (_, __) => UpdateGroupBoxWidths();

            Controls.Add(_root);
        }

        /// <summary>Ручная загрузка списка пресетов (если понадобится).</summary>
        public void LoadPresets(IEnumerable<PresetProfile> presets)
        {
            var list = presets?.ToList() ?? new List<PresetProfile>();

            System.Diagnostics.Debug.WriteLine($"PresetPanel.LoadPresets: received {list.Count} presets");

            _root.SuspendLayout();
            _root.Controls.Clear();
            _buttons.Clear();
            _groupBoxes.Clear();

            if (list.Count == 0)
            {
                _root.Controls.Add(new Label
                {
                    Text = "Пресеты не найдены",
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                    Padding = new Padding(10),
                    Margin = new Padding(10)
                });
                _root.ResumeLayout();
                return;
            }

            var groups = list
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Прочее" : p.Category)
                .OrderBy(g => g.Key);

            System.Diagnostics.Debug.WriteLine($"PresetPanel: creating {groups.Count()} groups");

            int groupWidth = Math.Max(200, _root.ClientSize.Width - 25);

            foreach (var group in groups)
            {
                System.Diagnostics.Debug.WriteLine($"PresetPanel: creating group '{group.Key}' with {group.Count()} presets");

                var gb = new GroupBox
                {
                    Text = group.Key,
                    Width = groupWidth,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                    Padding = new Padding(10),
                    BackColor = Color.FromArgb(248, 249, 250)
                };

                // Используем FlowLayoutPanel для автоматического размещения кнопок
                var flowPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    BackColor = Color.Transparent,
                    Padding = new Padding(5)
                };

                foreach (var p in group)
                {
                    System.Diagnostics.Debug.WriteLine($"PresetPanel: creating button for preset '{p.Name}'");

                    var btn = new PresetButton
                    {
                        Width = 160,
                        Height = 90,
                        Margin = new Padding(5),
                        IconText = string.IsNullOrWhiteSpace(p.Icon) ? "⚙️" : p.Icon,
                        Title = p.Name,
                        Description = p.Description,
                        AccentColor = ParseColor(p.ColorHex) ?? Color.FromArgb(230, 230, 240),
                        Tag = p,
                        BackColor = Color.LightBlue  // Временный цвет для отладки
                    };

                    btn.SetTooltip(p.Description ?? "");
                    btn.Click += (_, __) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"PresetPanel: clicked preset '{p.Name}'");
                        if (btn.Tag is PresetProfile profile)
                            PresetSelected?.Invoke(profile);
                    };

                    if (!string.IsNullOrEmpty(p.Id))
                        _buttons[p.Id] = btn;

                    flowPanel.Controls.Add(btn);
                    
                    System.Diagnostics.Debug.WriteLine($"PresetPanel: added button '{p.Name}' with size {btn.Size} to flowPanel");
                }

                gb.Controls.Add(flowPanel);
                _root.Controls.Add(gb);
                _groupBoxes.Add(gb);
            }

            System.Diagnostics.Debug.WriteLine($"PresetPanel: created {_groupBoxes.Count} groups and {_buttons.Count} buttons");
            _root.ResumeLayout();
            UpdateGroupBoxWidths();
        }
        /// <summary>Подсветка выбранного пресета.</summary>
        public void Highlight(string presetId)
        {
            foreach (var kv in _buttons)
            {
                kv.Value.BackColor = kv.Key == presetId
                    ? Color.FromArgb(245, 248, 255)
                    : Color.White;
            }
        }

        private static Color? ParseColor(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            try { return ColorTranslator.FromHtml(hex); }
            catch { return null; }
        }

        private void UpdateGroupBoxWidths()
        {
            if (_groupBoxes.Count == 0) return;
            int width = Math.Max(200, _root.ClientSize.Width - 25);
            foreach (var gb in _groupBoxes)
                gb.Width = width;
        }
    }
}
