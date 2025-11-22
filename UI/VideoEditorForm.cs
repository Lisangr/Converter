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
            StartPosition = FormStartPosition.CenterScreen;

            videoPlayer = new VideoPlayerPanel
            {
                Dock = DockStyle.Top,
                Height = 400
            };
            Controls.Add(videoPlayer);

            editorTabs = new TabControl
            {
                Location = new Point(0, 410),
                Size = new Size(1184, 310),
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            Controls.Add(editorTabs);

            subtitlesPanel = new SubtitlesEditorPanel(videoPlayer)
            {
                Dock = DockStyle.Fill
            };
            var tabSubtitles = new TabPage("📝 Субтитры");
            tabSubtitles.Controls.Add(subtitlesPanel);
            editorTabs.TabPages.Add(tabSubtitles);

            cropPanel = new CropPanel(videoPlayer)
            {
                Dock = DockStyle.Fill
            };
            var tabCrop = new TabPage("✂️ Кадрирование");
            tabCrop.Controls.Add(cropPanel);
            editorTabs.TabPages.Add(tabCrop);

            trimPanel = new TrimPanel(videoPlayer)
            {
                Dock = DockStyle.Fill
            };
            var tabTrim = new TabPage("⏱ Обрезка");
            tabTrim.Controls.Add(trimPanel);
            editorTabs.TabPages.Add(tabTrim);

            effectsPanel = new EffectsPanel(videoPlayer)
            {
                Dock = DockStyle.Fill
            };
            var tabEffects = new TabPage("✨ Эффекты");
            tabEffects.Controls.Add(effectsPanel);
            editorTabs.TabPages.Add(tabEffects);

            var btnY = 730;

            btnApply = new Button
            {
                Text = "👁 Предпросмотр",
                Location = new Point(800, btnY),
                Size = new Size(120, 35)
            };
            btnApply.Click += BtnApply_Click;
            Controls.Add(btnApply);

            btnExport = new Button
            {
                Text = "💾 Экспортировать",
                Location = new Point(930, btnY),
                Size = new Size(130, 35),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExport.Click += BtnExport_Click;
            Controls.Add(btnExport);

            btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(1070, btnY),
                Size = new Size(100, 35)
            };
            btnCancel.Click += (_, _) => Close();
            Controls.Add(btnCancel);

            Load += async (_, _) => await LoadVideoAsync();
        }

        private async Task LoadVideoAsync()
        {
            try
            {
                mediaInfo = await FFmpeg.GetMediaInfo(currentVideoPath).ConfigureAwait(true);
                await videoPlayer.LoadVideoAsync(currentVideoPath, mediaInfo).ConfigureAwait(true);
                subtitlesPanel.SetMediaInfo(mediaInfo);
                trimPanel.SetMediaInfo(mediaInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видео: {ex.Message}", "Ошибка");
                Close();
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
            conversion.AddParameter("-y");
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
