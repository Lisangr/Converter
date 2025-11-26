using System.Text;
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

        private Font? currentFont;

        private readonly List<SubtitleItem> subtitles = new();
        private IMediaInfo? mediaInfo;

        public bool HasSubtitles => subtitles.Count > 0;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of the cached font
                currentFont?.Dispose();
                currentFont = null; // Help GC

                // Unsubscribe from events to prevent memory leaks if the player is still alive
                videoPlayer.PositionChanged -= OnPlayerPositionChanged;
            }
            base.Dispose(disposing);
        }

        public SubtitlesEditorPanel(VideoPlayerPanel player)
        {
            videoPlayer = player;
            BackColor = Color.White;
            Padding = new Padding(10);
            AutoScroll = true;

            // 1. Нижняя панель стиля субтитров
            var grpStyle = new GroupBox
            {
                Text = "🎨 Стиль субтитров",
                Dock = DockStyle.Bottom,
                Height = 140,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(10)
            };
            Controls.Add(grpStyle);

            var styleFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(5),
                AutoScroll = true
            };
            grpStyle.Controls.Add(styleFlow);

            // Параметры стиля (как в присланном коде)
            var fontPanel = new Panel { Height = 30, Width = 200, Margin = new Padding(5) };
            var lblFont = new Label { Text = "Шрифт:", Location = new Point(0, 5), AutoSize = true };
            fontPanel.Controls.Add(lblFont);
            cmbFont = new ComboBox
            {
                Location = new Point(60, 2),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFont.Items.AddRange(new[] { "Arial", "Impact", "Bebas", "Montserrat", "Roboto" });
            cmbFont.SelectedIndex = 0;
            fontPanel.Controls.Add(cmbFont);
            styleFlow.Controls.Add(fontPanel);

            var sizePanel = new Panel { Height = 30, Width = 140, Margin = new Padding(5) };
            var lblSize = new Label { Text = "Размер:", Location = new Point(0, 5), AutoSize = true };
            sizePanel.Controls.Add(lblSize);
            numFontSize = new NumericUpDown
            {
                Location = new Point(65, 2),
                Width = 70,
                Minimum = 12,
                Maximum = 120,
                Value = 48
            };
            sizePanel.Controls.Add(numFontSize);
            styleFlow.Controls.Add(sizePanel);

            btnFontColor = new Button
            {
                Text = "Цвет текста",
                Size = new Size(110, 30),
                BackColor = Color.White,
                Margin = new Padding(5)
            };
            btnFontColor.Click += BtnFontColor_Click;
            styleFlow.Controls.Add(btnFontColor);

            btnBackgroundColor = new Button
            {
                Text = "Цвет фона",
                Size = new Size(110, 30),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Margin = new Padding(5)
            };
            btnBackgroundColor.Click += BtnBackgroundColor_Click;
            styleFlow.Controls.Add(btnBackgroundColor);

            var posPanel = new Panel { Height = 30, Width = 160, Margin = new Padding(5) };
            var lblPos = new Label { Text = "Позиция:", Location = new Point(0, 5), AutoSize = true };
            posPanel.Controls.Add(lblPos);
            cmbPosition = new ComboBox
            {
                Location = new Point(65, 2),
                Width = 90,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPosition.Items.AddRange(new[] { "Внизу", "Вверху", "По центру" });
            cmbPosition.SelectedIndex = 0;
            posPanel.Controls.Add(cmbPosition);
            styleFlow.Controls.Add(posPanel);

            chkBold = new CheckBox
            {
                Text = "Жирный",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(5, 8, 5, 5)
            };
            styleFlow.Controls.Add(chkBold);

            chkOutline = new CheckBox
            {
                Text = "Обводка",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(5, 8, 5, 5)
            };
            styleFlow.Controls.Add(chkOutline);

            // 2. Главная панель сверху
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 0, 10)
            };
            Controls.Add(mainPanel);

            // Сначала правая панель с кнопками
            var rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 160,
                Padding = new Padding(5, 0, 0, 0)
            };
            mainPanel.Controls.Add(rightPanel);

            int buttonY = 0;
            var btnAdd = new Button
            {
                Text = "➕ Добавить",
                Location = new Point(0, buttonY),
                Size = new Size(155, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnAdd.Click += BtnAdd_Click;
            rightPanel.Controls.Add(btnAdd);

            buttonY += 40;
            btnEdit = new Button
            {
                Text = "✏️ Изменить",
                Location = new Point(0, buttonY),
                Size = new Size(155, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Enabled = false
            };
            btnEdit.Click += (_, _) => EditSelectedSubtitle();
            rightPanel.Controls.Add(btnEdit);

            buttonY += 40;
            btnDelete = new Button
            {
                Text = "🗑 Удалить",
                Location = new Point(0, buttonY),
                Size = new Size(155, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;
            rightPanel.Controls.Add(btnDelete);

            buttonY += 45;
            var btnImport = new Button
            {
                Text = "📂 Импорт SRT",
                Location = new Point(0, buttonY),
                Size = new Size(155, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnImport.Click += BtnImport_Click;
            rightPanel.Controls.Add(btnImport);

            buttonY += 40;
            var btnExport = new Button
            {
                Text = "💾 Экспорт SRT",
                Location = new Point(0, buttonY),
                Size = new Size(155, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnExport.Click += BtnExport_Click;
            rightPanel.Controls.Add(btnExport);

            // Затем левая панель со списком субтитров
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 5, 0)
            };
            mainPanel.Controls.Add(leftPanel);

            var lblList = new Label
            {
                Text = "Субтитры:",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            leftPanel.Controls.Add(lblList);

            lstSubtitles = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                IntegralHeight = false
            };
            lstSubtitles.SelectedIndexChanged += LstSubtitles_SelectedIndexChanged;
            lstSubtitles.DoubleClick += (_, _) => EditSelectedSubtitle();
            leftPanel.Controls.Add(lstSubtitles);

            // Живой предпросмотр субтитров по позиции плеера
            videoPlayer.PositionChanged += OnPlayerPositionChanged;
        }

        private void OnPlayerPositionChanged(TimeSpan position)
        {
            if (subtitles.Count == 0)
            {
                videoPlayer.SetSubtitleOverlay(null, null, null, null, ContentAlignment.BottomCenter);
                return;
            }

            SubtitleItem? current = null;
            foreach (var s in subtitles)
            {
                if (position >= s.StartTime && position <= s.EndTime)
                {
                    current = s;
                    break;
                }
            }

            if (current == null)
            {
                videoPlayer.SetSubtitleOverlay(null, null, null, null, ContentAlignment.BottomCenter);
                return;
            }

            // Настройки стиля для live-просмотра (упрощённо на основе текущих контролов)
            var fontStyle = chkBold.Checked ? FontStyle.Bold : FontStyle.Regular;
            var newFont = new Font(cmbFont.Text, (float)numFontSize.Value, fontStyle);

            // Dispose of the old font if it exists and is different from the new one
            if (currentFont != null && !currentFont.Equals(newFont))
            {
                currentFont.Dispose();
                currentFont = null;
            }

            // Cache the new font if it's not already cached
            if (currentFont == null)
            {
                currentFont = newFont;
            }
            else
            {
                // If the font is the same, dispose of the newly created one to avoid leak
                newFont.Dispose();
            }

            var fore = btnFontColor.BackColor;
            var back = btnBackgroundColor.BackColor;

            var align = ContentAlignment.BottomCenter;
            if (cmbPosition.SelectedIndex >= 0 && cmbPosition.SelectedIndex <= 2)
            {
                align = cmbPosition.SelectedIndex switch
                {
                    0 => ContentAlignment.BottomCenter,
                    1 => ContentAlignment.TopCenter,
                    2 => ContentAlignment.MiddleCenter,
                    _ => ContentAlignment.BottomCenter // Default fallback, though index should be valid
                };
            }
            var outlineThickness = chkOutline.Checked ? 2 : 0;
            videoPlayer.SetSubtitleOverlay(current.Text, currentFont, fore, back, align, outlineThickness);
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

            // Для FFmpeg под Windows рекомендуемый формат пути в фильтрах:
            // C\\:/path/file.ass  (экранируем двоеточие после буквы диска)
            var normalizedPath = assPath.Replace("\\", "/");
            if (normalizedPath.Length >= 2 && normalizedPath[1] == ':')
            {
                normalizedPath = normalizedPath[0] + "\\:" + normalizedPath.Substring(2);
            }

            normalizedPath = normalizedPath.Replace("'", "\\'");

            // Используем фильтр subtitles, он тоже принимает ASS-файл
            return $"subtitles='{normalizedPath}'";
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
            var outline = chkOutline.Checked ? 3 : 0;

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
