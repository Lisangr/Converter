using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xabe.FFmpeg;

namespace Converter.UI
{
    public class VideoEditorForm : Form
    {
        // Контролы
        private readonly VideoPlayerPanel videoPlayer;
        private readonly TabControl editorTabs;
        private readonly SplitContainer mainSplitter; // Объявляем тут

        private readonly SubtitlesEditorPanel subtitlesPanel;
        private readonly CropPanel cropPanel;
        private readonly TrimPanel trimPanel;
        private readonly EffectsPanel effectsPanel;

        private readonly Button btnApply;
        private readonly Button btnExport;
        private readonly Button btnCancel;

        private readonly string currentVideoPath;
        private IMediaInfo? mediaInfo;

        public VideoEditorForm(string videoPath)
        {
            currentVideoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));

            Text = "Видео редактор";
            Size = new Size(1200, 800);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;

            // --- 1. Нижняя панель с кнопками ---
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                Padding = new Padding(10, 8, 10, 8),
                BackColor = Color.FromArgb(245, 245, 250)
            };
            Controls.Add(bottomPanel);

            bottomPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(220, 220, 220)))
                {
                    e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
                }
            };

            var buttonsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 0, 5, 0),
                Margin = new Padding(0)
            };
            bottomPanel.Controls.Add(buttonsFlow);

            btnApply = new Button
            {
                Text = "👁 Предпросмотр",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(10, 0, 0, 0),
                Height = 36,
                MinimumSize = new Size(120, 36),
                Padding = new Padding(12, 0, 12, 0)
            };
            btnApply.Click += BtnApply_Click;
            buttonsFlow.Controls.Add(btnApply);

            btnExport = new Button
            {
                Text = "💾 Экспортировать",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(10, 0, 0, 0),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Height = 36,
                MinimumSize = new Size(140, 36),
                Padding = new Padding(12, 0, 12, 0)
            };
            btnExport.Click += BtnExport_Click;
            buttonsFlow.Controls.Add(btnExport);

            btnCancel = new Button
            {
                Text = "Отмена",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(10, 0, 0, 0),
                Height = 36,
                MinimumSize = new Size(100, 36),
                Padding = new Padding(12, 0, 12, 0),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnCancel.Click += (_, _) => Close();
            buttonsFlow.Controls.Add(btnCancel);

            // --- 2. SplitContainer (Безопасная инициализация) ---
            mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 8,
                BackColor = SystemColors.Control,
                FixedPanel = FixedPanel.Panel1,
                // ВАЖНО: Ставим маленькие минимумы при создании, чтобы избежать ошибки при инициализации
                Panel1MinSize = 25,
                Panel2MinSize = 25
            };

            Controls.Add(mainSplitter);
            mainSplitter.BringToFront();

            // --- 3. Верхняя панель (Плеер) ---
            videoPlayer = new VideoPlayerPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.Black
            };
            mainSplitter.Panel1.Controls.Add(videoPlayer);

            // --- 4. Нижняя панель (Табы) ---
            var tabContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 0)
            };

            editorTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Point(8, 4),
                ItemSize = new Size(0, 28)
            };
            tabContainer.Controls.Add(editorTabs);
            mainSplitter.Panel2.Controls.Add(tabContainer);

            // --- 5. Вкладки ---
            subtitlesPanel = new SubtitlesEditorPanel(videoPlayer)
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            editorTabs.TabPages.Add(new TabPage("📝 Субтитры") { Controls = { subtitlesPanel } });

            cropPanel = new CropPanel(videoPlayer)
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            editorTabs.TabPages.Add(new TabPage("✂️ Кадрирование") { Controls = { cropPanel } });

            trimPanel = new TrimPanel(videoPlayer)
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            editorTabs.TabPages.Add(new TabPage("⏱ Обрезка") { Controls = { trimPanel } });

            effectsPanel = new EffectsPanel(videoPlayer)
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            editorTabs.TabPages.Add(new TabPage("✨ Эффекты") { Controls = { effectsPanel } });

            // Подписка на событие Load для безопасной настройки размеров
            Load += VideoEditorForm_Load;
        }

        private void VideoEditorForm_Load(object? sender, EventArgs e)
        {
            // Настраиваем сплиттер только когда форма загрузилась и имеет размеры
            try
            {
                // Рассчитываем 60% высоты под видео, но не меньше 200px
                int totalHeight = mainSplitter.Height;
                int desiredSplit = (int)(totalHeight * 0.6);

                // Проверка на безопасность границ
                if (desiredSplit < 200) desiredSplit = 200;
                if (desiredSplit > totalHeight - 200) desiredSplit = totalHeight - 200;

                // 1. Сначала ставим позицию
                mainSplitter.SplitterDistance = desiredSplit;

                // 2. Теперь, когда позиция корректна, включаем жесткие ограничения
                mainSplitter.Panel1MinSize = 200;
                mainSplitter.Panel2MinSize = 200;
            }
            catch
            {
                // Если размеры совсем маленькие (глюк системы), оставляем дефолт
            }

            LoadVideo();
        }

        private void LoadVideo()
        {
            try
            {
                mediaInfo = FFmpeg.GetMediaInfo(currentVideoPath).GetAwaiter().GetResult();
                videoPlayer.LoadVideoAsync(currentVideoPath, mediaInfo).GetAwaiter().GetResult();
                subtitlesPanel.SetMediaInfo(mediaInfo);
                trimPanel.SetMediaInfo(mediaInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видео: {ex.Message}", "Ошибка");
            }
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            _ = BtnApplyAsync();
        }

        private async Task BtnApplyAsync()
        {
            var tempOutput = Path.Combine(Path.GetTempPath(), $"preview_{Guid.NewGuid():N}.mp4");
            try
            {
                await ApplyEditsAndExport(tempOutput, isPreview: true).ConfigureAwait(true);

                if (File.Exists(tempOutput))
                {
                    var previewInfo = await FFmpeg.GetMediaInfo(tempOutput).ConfigureAwait(true);
                    await videoPlayer.LoadVideoAsync(tempOutput, previewInfo).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подготовить предпросмотр: {ex.Message}", "Ошибка");
            }
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            _ = BtnExportAsync();
        }

        private async Task BtnExportAsync()
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "MP4 Video|*.mp4|All Files|*.*",
                FileName = Path.GetFileNameWithoutExtension(currentVideoPath) + "_edited.mp4"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            btnExport.Enabled = false;
            btnExport.Text = "Экспорт...";

            try
            {
                await ApplyEditsAndExport(saveDialog.FileName, isPreview: false).ConfigureAwait(true);
                MessageBox.Show("Видео успешно экспортировано!", "Готово");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
            }
            finally
            {
                btnExport.Enabled = true;
                btnExport.Text = "💾 Экспортировать";
            }
        }

        private async Task ApplyEditsAndExport(string outputPath, bool isPreview)
        {
            var conversion = FFmpeg.Conversions.New();
            conversion.AddParameter($"-i \"{currentVideoPath}\"");

            var videoFilters = new List<string>();
            var complexFilters = new List<string>();

            if (trimPanel.IsTrimEnabled)
            {
                var trimData = trimPanel.GetTrimData();
                conversion.AddParameter($"-ss {trimData.StartTime}");
                conversion.AddParameter($"-t {trimData.Duration}");
            }

            if (cropPanel.IsCropEnabled)
            {
                var cropData = cropPanel.GetCropData();
                videoFilters.Add($"crop={cropData.Width}:{cropData.Height}:{cropData.X}:{cropData.Y}");
            }

            var effectsFilter = effectsPanel.GetVideoFilterGraph();
            if (!string.IsNullOrWhiteSpace(effectsFilter))
            {
                videoFilters.Add(effectsFilter);
            }

            if (subtitlesPanel.HasSubtitles)
            {
                var subtitlesFilter = subtitlesPanel.BuildSubtitlesFilter();
                if (!string.IsNullOrEmpty(subtitlesFilter))
                {
                    complexFilters.Add(subtitlesFilter);
                }
            }

            if (videoFilters.Count > 0)
            {
                conversion.AddParameter($"-vf \"{string.Join(",", videoFilters)}\"");
            }

            if (complexFilters.Count > 0)
            {
                conversion.AddParameter($"-filter_complex \"{string.Join(";", complexFilters)}\"");
            }

            if (isPreview)
            {
                conversion.AddParameter("-c:v libx264 -preset ultrafast -crf 28");
            }
            else
            {
                conversion.AddParameter("-c:v libx264 -preset medium -crf 23");
            }

            conversion.AddParameter("-c:a copy");
            conversion.SetOutput(outputPath);

            await conversion.Start().ConfigureAwait(true);
        }
    }
}