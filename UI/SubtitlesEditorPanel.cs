using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Xabe.FFmpeg;

namespace Converter.UI
{
    public class SubtitlesEditorPanel : Panel
    {
        private readonly VideoPlayerPanel videoPlayer;
        private readonly ListBox lstSubtitles;
        private readonly Button btnEdit;
        private readonly Button btnDelete;
        private readonly Button btnFontColor;
        private readonly Button btnBackgroundColor;
        private readonly ComboBox cmbFont;
        private readonly NumericUpDown numFontSize;
        private readonly ComboBox cmbPosition;
        private readonly CheckBox chkBold;
        private readonly CheckBox chkOutline;

        private readonly List<SubtitleItem> subtitles = new();
        private IMediaInfo? mediaInfo;

        public bool HasSubtitles => subtitles.Count > 0;

        public SubtitlesEditorPanel(VideoPlayerPanel player)
        {
            videoPlayer = player;
            BackColor = Color.White;

            var lblList = new Label
            {
                Text = "Субтитры:",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            Controls.Add(lblList);

            lstSubtitles = new ListBox
            {
                Location = new Point(10, 35),
                Size = new Size(500, 180),
                Font = new Font("Consolas", 9)
            };
            lstSubtitles.SelectedIndexChanged += LstSubtitles_SelectedIndexChanged;
            lstSubtitles.DoubleClick += (_, _) => EditSelectedSubtitle();
            Controls.Add(lstSubtitles);

            var btnAdd = new Button
            {
                Text = "➕ Добавить",
                Location = new Point(520, 35),
                Size = new Size(120, 30)
            };
            btnAdd.Click += BtnAdd_Click;
            Controls.Add(btnAdd);

            btnEdit = new Button
            {
                Text = "✏️ Изменить",
                Location = new Point(520, 70),
                Size = new Size(120, 30),
                Enabled = false
            };
            btnEdit.Click += (_, _) => EditSelectedSubtitle();
            Controls.Add(btnEdit);

            btnDelete = new Button
            {
                Text = "🗑 Удалить",
                Location = new Point(520, 105),
                Size = new Size(120, 30),
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;
            Controls.Add(btnDelete);

            var btnImport = new Button
            {
                Text = "📂 Импорт SRT",
                Location = new Point(520, 145),
                Size = new Size(120, 30)
            };
            btnImport.Click += BtnImport_Click;
            Controls.Add(btnImport);

            var btnExport = new Button
            {
                Text = "💾 Экспорт SRT",
                Location = new Point(520, 180),
                Size = new Size(120, 30)
            };
            btnExport.Click += BtnExport_Click;
            Controls.Add(btnExport);

            var grpStyle = new GroupBox
            {
                Text = "🎨 Стиль субтитров",
                Location = new Point(10, 225),
                Size = new Size(630, 90),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            Controls.Add(grpStyle);

            var lblFont = new Label
            {
                Text = "Шрифт:",
                Location = new Point(10, 25),
                AutoSize = true
            };
            grpStyle.Controls.Add(lblFont);

            cmbFont = new ComboBox
            {
                Location = new Point(60, 22),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFont.Items.AddRange(new[] { "Arial", "Impact", "Bebas", "Montserrat", "Roboto" });
            cmbFont.SelectedIndex = 0;
            grpStyle.Controls.Add(cmbFont);

            var lblSize = new Label
            {
                Text = "Размер:",
                Location = new Point(190, 25),
                AutoSize = true
            };
            grpStyle.Controls.Add(lblSize);

            numFontSize = new NumericUpDown
            {
                Location = new Point(250, 22),
                Width = 60,
                Minimum = 12,
                Maximum = 120,
                Value = 48
            };
            grpStyle.Controls.Add(numFontSize);

            btnFontColor = new Button
            {
                Text = "Цвет текста",
                Location = new Point(320, 20),
                Size = new Size(100, 25),
                BackColor = Color.White
            };
            btnFontColor.Click += BtnFontColor_Click;
            grpStyle.Controls.Add(btnFontColor);

            btnBackgroundColor = new Button
            {
                Text = "Цвет фона",
                Location = new Point(430, 20),
                Size = new Size(100, 25),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            btnBackgroundColor.Click += BtnBackgroundColor_Click;
            grpStyle.Controls.Add(btnBackgroundColor);

            var lblPos = new Label
            {
                Text = "Позиция:",
                Location = new Point(540, 25),
                AutoSize = true
            };
            grpStyle.Controls.Add(lblPos);

            cmbPosition = new ComboBox
            {
                Location = new Point(540, 45),
                Width = 80,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPosition.Items.AddRange(new[] { "Внизу", "Вверху", "По центру" });
            cmbPosition.SelectedIndex = 0;
            grpStyle.Controls.Add(cmbPosition);

            chkBold = new CheckBox
            {
                Text = "Жирный",
                Location = new Point(10, 55),
                AutoSize = true,
                Checked = true
            };
            grpStyle.Controls.Add(chkBold);

            chkOutline = new CheckBox
            {
                Text = "Обводка",
                Location = new Point(100, 55),
                AutoSize = true,
                Checked = true
            };
            grpStyle.Controls.Add(chkOutline);
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            var currentTime = videoPlayer.GetCurrentTime();
            using var dialog = new SubtitleEditDialog(currentTime);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                subtitles.Add(dialog.GetSubtitle());
                subtitles.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                RefreshSubtitlesList();
            }
        }

        private void EditSelectedSubtitle()
        {
            if (lstSubtitles.SelectedIndex < 0)
            {
                return;
            }

            var subtitle = subtitles[lstSubtitles.SelectedIndex];
            using var dialog = new SubtitleEditDialog(subtitle);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                subtitles[lstSubtitles.SelectedIndex] = dialog.GetSubtitle();
                RefreshSubtitlesList();
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (lstSubtitles.SelectedIndex < 0)
            {
                return;
            }

            subtitles.RemoveAt(lstSubtitles.SelectedIndex);
            RefreshSubtitlesList();
        }

        private void BtnImport_Click(object? sender, EventArgs e)
        {
            using var openDialog = new OpenFileDialog
            {
                Filter = "SRT файлы (*.srt)|*.srt|Все файлы|*.*"
            };

            if (openDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                var imported = SubtitleParser.ParseSRT(openDialog.FileName);
                subtitles.AddRange(imported);
                subtitles.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                RefreshSubtitlesList();
                MessageBox.Show($"Импортировано {imported.Count} субтитров", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка");
            }
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            if (subtitles.Count == 0)
            {
                MessageBox.Show("Нет субтитров для экспорта", "Внимание");
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "SRT файлы (*.srt)|*.srt"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                SubtitleParser.ExportToSRT(subtitles, saveDialog.FileName);
                MessageBox.Show("Субтитры экспортированы", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
            }
        }

        private void BtnFontColor_Click(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                btnFontColor.BackColor = colorDialog.Color;
            }
        }

        private void BtnBackgroundColor_Click(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                btnBackgroundColor.BackColor = colorDialog.Color;
                btnBackgroundColor.ForeColor = GetContrastColor(colorDialog.Color);
            }
        }

        private void LstSubtitles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var hasSelection = lstSubtitles.SelectedIndex >= 0;
            btnEdit.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;

            if (hasSelection)
            {
                var subtitle = subtitles[lstSubtitles.SelectedIndex];
                videoPlayer.SeekTo(subtitle.StartTime);
            }
        }

        private void RefreshSubtitlesList()
        {
            lstSubtitles.Items.Clear();
            foreach (var sub in subtitles)
            {
                lstSubtitles.Items.Add($"{FormatTime(sub.StartTime)} → {FormatTime(sub.EndTime)} | {sub.Text}");
            }
        }

        private static string FormatTime(TimeSpan time) => time.ToString("hh\\:mm\\:ss\\.fff");

        public void SetMediaInfo(IMediaInfo info)
        {
            mediaInfo = info;
        }

        public string? BuildSubtitlesFilter()
        {
            if (subtitles.Count == 0)
            {
                return null;
            }

            var assPath = Path.Combine(Path.GetTempPath(), $"subtitles_{Guid.NewGuid():N}.ass");
            GenerateASSFile(assPath);
            return $"ass='{assPath.Replace("\\", "/").Replace(":", "\\\\:")}'";
        }

        private void GenerateASSFile(string outputPath)
        {
            var ass = new StringBuilder();
            ass.AppendLine("[Script Info]");
            ass.AppendLine("ScriptType: v4.00+");
            ass.AppendLine("PlayResX: 1920");
            ass.AppendLine("PlayResY: 1080");
            ass.AppendLine();

            ass.AppendLine("[V4+ Styles]");
            ass.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");

            var fontColor = ColorToAss(btnFontColor.BackColor);
            var bgColor = ColorToAss(btnBackgroundColor.BackColor);
            var alignment = GetAlignment();
            var bold = chkBold.Checked ? -1 : 0;
            var outline = chkOutline.Checked ? 2 : 0;

            ass.AppendLine($"Style: Default,{cmbFont.Text},{numFontSize.Value},{fontColor},&H00000000,&H00000000,{bgColor},{bold},0,0,0,100,100,0,0,1,{outline},1,{alignment},10,10,10,1");
            ass.AppendLine();

            ass.AppendLine("[Events]");
            ass.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

            foreach (var subtitle in subtitles)
            {
                var start = FormatTimeForASS(subtitle.StartTime);
                var end = FormatTimeForASS(subtitle.EndTime);
                var text = subtitle.Text.Replace("\n", "\\N");
                ass.AppendLine($"Dialogue: 0,{start},{end},Default,,0,0,0,,{text}");
            }

            File.WriteAllText(outputPath, ass.ToString(), Encoding.UTF8);
        }

        private static string ColorToAss(Color color) => $"&H{color.A:X2}{color.B:X2}{color.G:X2}{color.R:X2}";

        private int GetAlignment() => cmbPosition.SelectedIndex switch
        {
            0 => 2,
            1 => 8,
            2 => 5,
            _ => 2
        };

        private static string FormatTimeForASS(TimeSpan time) => $"{time.Hours}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}";

        private static Color GetContrastColor(Color color)
        {
            var d = (int)Math.Sqrt(color.R * color.R * 0.299 + color.G * color.G * 0.587 + color.B * color.B * 0.114);
            return d > 130 ? Color.Black : Color.White;
        }
    }

    public class SubtitleItem
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
