using Converter.Application.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq; // Added for .OfType<T>()
using System.Threading.Tasks;
using System.Windows.Forms;
using Xabe.FFmpeg; // For IMediaInfo, IStream
using Converter.Services; // For IVideoProcessingService
using Converter.UI.Presenters; // For VideoEditorPresenter

namespace Converter.UI
{
    public partial class VideoEditorForm : Form, IVideoEditorView
    {
        // Контролы
        private readonly VideoPlayerPanel videoPlayer;
        private readonly TabControl editorTabs;
        private readonly SplitContainer mainSplitter;

        private readonly SubtitlesEditorPanel subtitlesPanel;
        private readonly CropPanel cropPanel;
        private readonly TrimPanel trimPanel;
        private readonly EffectsPanel effectsPanel;
        private readonly AudioPanel audioPanel; // <-- Добавить поле

        private readonly Button btnExport;
        private readonly Button btnCancel;

        private readonly string _initialVideoPath;

        private VideoEditorPresenter _presenter;
        private IVideoProcessingService _videoProcessingService;

        public VideoEditorForm(string videoPath)
        {
            _initialVideoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));

            Text = "Видео редактор";
            Size = new Size(1200, 900); // Increased height by 100
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
            btnExport.Click += (s, e) => ExportRequested?.Invoke(this, EventArgs.Empty);
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

            // --- 6. Эффекты ---
            effectsPanel = new EffectsPanel(videoPlayer)
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            editorTabs.TabPages.Add(new TabPage("🎨 Эффекты") { Controls = { effectsPanel } });

            // --- 7. Аудио ---
            audioPanel = new AudioPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            editorTabs.TabPages.Add(new TabPage("🔊 Аудио") { Controls = { audioPanel } });

            // Wire up panel events to view events
            trimPanel.TrimRequested += (s, e) => TrimRequested?.Invoke(this, EventArgs.Empty);
            audioPanel.ApplyRequested += (s, e) => AudioApplyRequested?.Invoke(this, e);
            effectsPanel.ApplyRequested += (s, e) => EffectsApplyRequested?.Invoke(this, e);
            effectsPanel.LiveEffectChanged += OnLiveEffectChanged;
            // Подписка на изменение эквалайзера
            audioPanel.LiveEqChanged += (enabled, preset) =>
            {
                videoPlayer.SetAudioEqualizer(enabled, preset);
            };

            effectsPanel.LiveEffectChanged += OnLiveEffectChanged;
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
            cropPanel.CropApplied += (s, cropRect) => CropRequested?.Invoke(this, EventArgs.Empty);
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

            // Initialize services and presenter
            _videoProcessingService = new VideoProcessingService();
            _presenter = new VideoEditorPresenter(this, _videoProcessingService, _initialVideoPath);
        }

        private async void VideoEditorForm_Load(object? sender, EventArgs e)
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
            await _presenter.LoadInitialVideo();
        }

        // IVideoEditorView Implementation
        public string CurrentVideoPath { get; set; }
        public IMediaInfo MediaInfo { get; set; }
        public TimeSpan TrimStartTime { get => trimPanel.StartTime; set => trimPanel.StartTime = value; }
        public TimeSpan TrimEndTime { get => trimPanel.EndTime; set => trimPanel.EndTime = value; }

        // Implementation for Crop Panel methods
        public Rectangle GetCurrentCropRectangle() => cropPanel.GetCropData();
        public void SetCurrentCropRectangle(Rectangle rect) => cropPanel.SetCropRect(rect);

        // Реализация метода интерфейса (см. шаг 4)
        public Converter.Domain.Models.AudioProcessingOptions GetAudioOptions()
        {
             // Если нужно, здесь можно добавить TotalDuration видео в опции,
             // чтобы логика могла проверить валидность FadeOut
             var opts = audioPanel.GetAudioOptions();
             if (MediaInfo != null)
             {
                 opts.TotalDuration = MediaInfo.Duration.TotalSeconds;
             }
             return opts;
        }
        
        public string? GetVideoEffectsFilter()
        {
            return effectsPanel.GetVideoFilterGraph();
        }

        public bool IsExporting 
        {
            get => btnExport.Enabled == false; 
            set 
            {
                btnExport.Enabled = !value;
                btnExport.Text = value ? "Экспорт..." : "💾 Экспортировать";
            } 
        }

        public void LoadVideo(string filePath)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => LoadVideo(filePath)));
                return;
            }

            videoPlayer.LoadVideoAsync(filePath, MediaInfo).GetAwaiter().GetResult(); // Synchronous wait for UI update
            subtitlesPanel.SetMediaInfo(MediaInfo);
            trimPanel.SetMediaInfo(MediaInfo);
            cropPanel.SetVideoDimensions(videoPlayer.VideoWidth, videoPlayer.VideoHeight);

            // Set initial crop rect to full video size, in UI coordinates
            Rectangle initialCropRect = videoPlayer.VideoToUiCoordinates(new Rectangle(0, 0, videoPlayer.VideoWidth, videoPlayer.VideoHeight));
            videoPlayer.SetCropRect(initialCropRect);
            cropPanel.SetCropRect(initialCropRect);
            VideoLoaded?.Invoke(this, filePath);
        }

        public void UpdateTrimPanel(TimeSpan duration)
        {
            // Assuming TrimPanel needs IMediaInfo, this might need adjustment if it only needs duration
            // For now, setting a dummy MediaInfo with just duration
            var dummyMediaInfo = new DummyMediaInfo { Duration = duration };
            trimPanel.SetMediaInfo(dummyMediaInfo);
        }

        public void UpdateCropPanel(Size videoSize)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateCropPanel(videoSize)));
                return;
            }
            cropPanel.SetVideoDimensions(videoSize.Width, videoSize.Height);
            // Set initial crop rect to full video size
            Rectangle initialCropRect = new Rectangle(0, 0, videoSize.Width, videoSize.Height);
            cropPanel.SetCropRect(initialCropRect);
        }

        public void ShowLoadingState()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ShowLoadingState));
                return;
            }

            Cursor = Cursors.WaitCursor;
            IsExporting = true;
        }

        public void HideLoadingState()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(HideLoadingState));
                return;
            }

            Cursor = Cursors.Default;
            IsExporting = false;
        }

        public void ShowMessage(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowMessage(message, title, buttons, icon)));
                return;
            }
            
            MessageBox.Show(this, message, title, buttons, icon);
        }

        public void EnableExportButton(bool enable)
        {
            btnExport.Enabled = enable;
        }

        public void UpdateProgress(int percentage)
        {
            // TODO: Implement a progress bar in the UI and update it here
            Console.WriteLine($"Export Progress: {percentage}%");
        }
        
        public void SetPlayerPosition(TimeSpan position)
        {
            videoPlayer.SetPosition(position);
        }

        public event EventHandler<string> VideoLoaded;
        public event EventHandler TrimRequested;
        public event EventHandler CropRequested;
        public event EventHandler ExportRequested;
        public event EventHandler<TimeSpan> PlayerPositionChanged;
        public event EventHandler AudioApplyRequested;
        public event EventHandler EffectsApplyRequested;

        // Dummy MediaInfo class for UpdateTrimPanel if it strictly requires IMediaInfo
        private class DummyMediaInfo : IMediaInfo
        {
            public TimeSpan Duration { get; set; }
            public IEnumerable<IAudioStream> AudioStreams => Enumerable.Empty<IAudioStream>();
            public IEnumerable<IVideoStream> VideoStreams => Enumerable.Empty<IVideoStream>();
            public IEnumerable<ISubtitleStream> SubtitleStreams => Enumerable.Empty<ISubtitleStream>();
            public IEnumerable<IStream> Streams => Enumerable.Empty<IStream>();
            public string Path => string.Empty;
            public string Format => string.Empty;
            public TimeSpan Start => TimeSpan.Zero;
            public long Size => 0;
            public double Bitrate => 0;
            public int Chapters => 0;
            public Dictionary<string, string> Metadata => new Dictionary<string, string>();
            public DateTime? CreationTime => DateTime.MinValue;
        }
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _presenter.Cleanup();
        }

        private void OnLiveEffectChanged(VideoAdjustments adjustments)
        {
            videoPlayer.SetVideoAdjustments(adjustments);
        }
    }
}