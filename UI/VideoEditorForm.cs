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
        private readonly VideoPlayerPanel videoPlayer;
        private readonly TabControl editorTabs;
        private readonly SplitContainer mainLayout;
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
            Size = new Size(1200, 850);
            MinimumSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 245, 250)
            };
            Controls.Add(bottomPanel);

            mainLayout = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.None,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            mainLayout.SplitterDistance = 380;

            Controls.Add(mainLayout);

            videoPlayer = new VideoPlayerPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            mainLayout.Panel1.Controls.Add(videoPlayer);

            editorTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            mainLayout.Panel2.Controls.Add(editorTabs);

            subtitlesPanel = new SubtitlesEditorPanel(videoPlayer) { Dock = DockStyle.Fill };
            var tabSubtitles = new TabPage("📝 Субтитры");
            tabSubtitles.Controls.Add(subtitlesPanel);
            editorTabs.TabPages.Add(tabSubtitles);

            cropPanel = new CropPanel(videoPlayer) { Dock = DockStyle.Fill };
            var tabCrop = new TabPage("✂️ Кадрирование");
            tabCrop.Controls.Add(cropPanel);
            editorTabs.TabPages.Add(tabCrop);

            trimPanel = new TrimPanel(videoPlayer) { Dock = DockStyle.Fill };
            var tabTrim = new TabPage("⏱ Обрезка");
            tabTrim.Controls.Add(trimPanel);
            editorTabs.TabPages.Add(tabTrim);

            effectsPanel = new EffectsPanel(videoPlayer) { Dock = DockStyle.Fill };
            var tabEffects = new TabPage("✨ Эффекты");
            tabEffects.Controls.Add(effectsPanel);
            editorTabs.TabPages.Add(tabEffects);

            var buttonsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            bottomPanel.Controls.Add(buttonsFlow);

            btnApply = new Button
            {
                Text = "👁 Предпросмотр",
                AutoSize = true,
                Margin = new Padding(0, 0, 10, 0),
                Height = 35
            };
            btnApply.Click += BtnApply_Click;
            buttonsFlow.Controls.Add(btnApply);

            btnExport = new Button
            {
                Text = "💾 Экспортировать",
                AutoSize = true,
                Margin = new Padding(0, 0, 10, 0),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Height = 35
            };
            btnExport.Click += BtnExport_Click;
            buttonsFlow.Controls.Add(btnExport);

            btnCancel = new Button
            {
                Text = "Отмена",
                AutoSize = true,
                Margin = new Padding(0),
                Height = 35
            };
            btnCancel.Click += (_, _) => Close();
            buttonsFlow.Controls.Add(btnCancel);

            Load += (_, _) => LoadVideo();
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
            // Живой предпросмотр: просто переходим к началу (или началу трима),
            // субтитры рисуются поверх видео в VideoPlayerPanel без перекодирования.
            var start = TimeSpan.Zero;
            if (trimPanel.IsTrimEnabled)
            {
                var trim = trimPanel.GetTrimData();
                start = trim.StartTime;
            }

            videoPlayer.SeekTo(start);
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
                    videoFilters.Add(subtitlesFilter);
                }
            }

            if (videoFilters.Count > 0)
            {
                conversion.AddParameter($"-vf \"{string.Join(",", videoFilters)}\"");
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