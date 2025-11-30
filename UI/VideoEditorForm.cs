using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq; // Added for .OfType<T>()
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
        private readonly SplitContainer mainSplitter;

        private readonly SubtitlesEditorPanel subtitlesPanel;
        private readonly CropPanel cropPanel;
        private readonly TrimPanel trimPanel;
        private readonly EffectsPanel effectsPanel;

        private readonly Button btnExport;
        private readonly Button btnCancel;

        private readonly string currentVideoPath;
        private readonly string _originalVideoPath; // Store original path
        private IMediaInfo? mediaInfo;

        public VideoEditorForm(string videoPath)
        {
            currentVideoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));
            _originalVideoPath = currentVideoPath; // Store the original path

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

            // --- 2. SplitContainer ---
            mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 8,
                BackColor = SystemColors.Control,
                FixedPanel = FixedPanel.Panel1,
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

            cropPanel = new CropPanel()
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

            trimPanel.TrimRequested += OnTrimRequested;

            effectsPanel = new EffectsPanel(videoPlayer)
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            editorTabs.TabPages.Add(new TabPage("✨ Эффекты") { Controls = { effectsPanel } });

            // Wire up crop events
            // CropPanel оперирует в координатах исходного видео, а CropOverlay в координатах UI (оверлея)
            cropPanel.CropRectChangedByUser += (s, videoRect) =>
            {
                // Пользователь изменил значения в NumericUpDown (videoRect в координатах видео)
                var uiRect = videoPlayer.VideoToUiCoordinates(videoRect);
                videoPlayer.SetCropRect(uiRect);
            };

            videoPlayer.CropRectChanged += (s, uiRect) =>
            {
                // Пользователь изменил рамку мышью в плеере (uiRect в координатах UI)
                var videoRect = videoPlayer.UiToVideoCoordinates(uiRect);
                cropPanel.SetCropRect(videoRect);
            };
            cropPanel.CropApplied += HandleCropApplied;
            cropPanel.CropEnabledChanged += (s, enabled) => 
            {
                if (enabled)
                {
                    videoPlayer.ShowCropOverlay();
                    // Initialize crop overlay with full video dimensions when enabling
                    if (videoPlayer.VideoWidth > 0 && videoPlayer.VideoHeight > 0)
                    {
                        var fullVideoRect = new Rectangle(0, 0, videoPlayer.VideoWidth, videoPlayer.VideoHeight);
                        // В плеер передаём UI-координаты, в панель — координаты видео
                        videoPlayer.SetCropRect(videoPlayer.VideoToUiCoordinates(fullVideoRect));
                        cropPanel.SetCropRect(fullVideoRect);
                    }
                }
                else
                {
                    videoPlayer.HideCropOverlay();
                }
            };

            Load += VideoEditorForm_Load;
        }

        private string? _trimmedVideoTempPath;
        private string? _croppedVideoTempPath;

        private void VideoEditorForm_Load(object? sender, EventArgs e)
        {
            try
            {
                int totalHeight = mainSplitter.Height;
                int desiredSplit = (int)(totalHeight * 0.6);

                if (desiredSplit < 200) desiredSplit = 200;
                if (desiredSplit > totalHeight - 200) desiredSplit = totalHeight - 200;

                mainSplitter.SplitterDistance = desiredSplit;
                mainSplitter.Panel1MinSize = 200;
                mainSplitter.Panel2MinSize = 200;
            }
            catch
            {
                // Игнорируем ошибки размеров при инициализации
            }

            LoadVideo();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            CleanupTempFiles();
        }

        private void CleanupTempFiles()
        {
            if (!string.IsNullOrEmpty(_trimmedVideoTempPath) && File.Exists(_trimmedVideoTempPath))
            {
                try
                {
                    File.Delete(_trimmedVideoTempPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting temporary trimmed video file: {ex.Message}");
                } 
            }
            if (!string.IsNullOrEmpty(_croppedVideoTempPath) && File.Exists(_croppedVideoTempPath))
            {
                try
                {
                    File.Delete(_croppedVideoTempPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting temporary cropped video file: {ex.Message}");
                }
            }
        }

        // --- ИСПРАВЛЕННЫЙ МЕТОД ---
        private async void OnTrimRequested(object? sender, TrimPanel.TrimRequestedEventArgs e)
        {
            // 1. Подготовка (UI поток)
            CleanupTempFiles();
            var tempPath = Path.Combine(Path.GetTempPath(), $"preview_trim_{Guid.NewGuid()}.mp4");
            var inputPath = _originalVideoPath;
            var start = e.StartTime;
            var duration = e.Duration;

            try
            {
                // 2. Фоновая работа (Background Thread)
                // Запускаем через Task.Run, чтобы не блокировать UI и иметь чистый контекст
                await Task.Run(async () =>
                {
                    var conversion = FFmpeg.Conversions.New();
                    conversion.AddParameter($"-ss {start} -i \"{inputPath}\" -t {duration} -c copy");
                    conversion.SetOutput(tempPath);
                    
                    // Запуск FFmpeg
                    await conversion.Start();

                    // Получение инфо о новом файле (тоже IO операция)
                    var newInfo = await FFmpeg.GetMediaInfo(tempPath);

                    // 3. Обновление UI (UI Thread)
                    // Используем Invoke для гарантированного выполнения в главном потоке
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        this.Invoke(new MethodInvoker(() =>
                        {
                            _trimmedVideoTempPath = tempPath;
                            mediaInfo = newInfo;

                            // Обновление плеера (LoadVideoAsync меняет Label.Text, поэтому строго в UI потоке)
                            videoPlayer.LoadVideoAsync(_trimmedVideoTempPath, mediaInfo);
                            
                            // Обновление панели тримминга
                            trimPanel.SetMediaInfo(mediaInfo);

                            MessageBox.Show("Видео успешно обрезано для предпросмотра!", "Обрезка применена");
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                // Обработка ошибок в UI потоке
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        MessageBox.Show($"Ошибка при обрезке видео: {ex.Message}", "Ошибка обрезки");
                        _trimmedVideoTempPath = null;
                    }));
                }
            }
        }

        private async void HandleCropApplied(object? sender, Rectangle cropRect)
        {
            CleanupTempFiles();
            var tempPath = Path.Combine(Path.GetTempPath(), $"preview_crop_{Guid.NewGuid()}.mp4");
            var inputPath = _trimmedVideoTempPath ?? _originalVideoPath;

            btnExport.Enabled = false;
            btnExport.Text = "Применение кадрирования...";

            try
            {
                await Task.Run(async () =>
                {
                    var conversion = FFmpeg.Conversions.New();
                    conversion.AddParameter($"-i \"{inputPath}\" -vf \"crop={cropRect.Width}:{cropRect.Height}:{cropRect.X}:{cropRect.Y}\" -c:a copy");
                    conversion.SetOutput(tempPath);
                    
                    await conversion.Start();

                    var newInfo = await FFmpeg.GetMediaInfo(tempPath);

                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        this.Invoke(new MethodInvoker(() =>
                        {
                            _croppedVideoTempPath = tempPath;
                            mediaInfo = newInfo;
                            videoPlayer.LoadVideoAsync(_croppedVideoTempPath, mediaInfo);
                            trimPanel.SetMediaInfo(mediaInfo);
                            cropPanel.SetVideoDimensions(mediaInfo.VideoStreams.FirstOrDefault().Width, mediaInfo.VideoStreams.FirstOrDefault().Height);
                            MessageBox.Show("Кадрирование успешно применено для предпросмотра!", "Кадрирование применено");
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        MessageBox.Show($"Ошибка при кадрировании видео: {ex.Message}", "Ошибка кадрирования");
                        _croppedVideoTempPath = null;
                    }));
                }
            }
            finally
            {
                if (!this.IsDisposed && this.IsHandleCreated)
                {
                    this.Invoke(new MethodInvoker(() =>
                    {
                        btnExport.Enabled = true;
                        btnExport.Text = "💾 Экспортировать";
                    }));
                }
            }
        }

        private void LoadVideo()
        {
            try
            {
                mediaInfo = FFmpeg.GetMediaInfo(currentVideoPath).GetAwaiter().GetResult();
                videoPlayer.LoadVideoAsync(currentVideoPath, mediaInfo).GetAwaiter().GetResult();
                subtitlesPanel.SetMediaInfo(mediaInfo);
                trimPanel.SetMediaInfo(mediaInfo);
                cropPanel.SetVideoDimensions(videoPlayer.VideoWidth, videoPlayer.VideoHeight);

                // Set initial crop rect to full video size, in UI coordinates
                Rectangle initialCropRect = videoPlayer.VideoToUiCoordinates(new Rectangle(0, 0, videoPlayer.VideoWidth, videoPlayer.VideoHeight));
                videoPlayer.SetCropRect(initialCropRect);
                cropPanel.SetCropRect(initialCropRect);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видео: {ex.Message}", "Ошибка");
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
            string actualInputPath = currentVideoPath;

            if (trimPanel.IsTrimEnabled && !string.IsNullOrEmpty(_trimmedVideoTempPath) && File.Exists(_trimmedVideoTempPath))
            {
                actualInputPath = _trimmedVideoTempPath;
            }

            conversion.AddParameter($"-i \"{actualInputPath}\"");

            var videoFilters = new List<string>();

            if (trimPanel.IsTrimEnabled && string.IsNullOrEmpty(_trimmedVideoTempPath))
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