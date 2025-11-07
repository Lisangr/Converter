using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace Converter
{
    public partial class Form1 : Form
    {
        private SplitContainer splitContainerMain = null!;
        private Panel panelLeftTop = null!;
        private Button btnAddFiles = null!;
        private Button btnRemoveSelected = null!;
        private Button btnClearAll = null!;
        private ListView lvFiles = null!;
        private ColumnHeader colName = new();
        private ColumnHeader colPath = new();
        private ColumnHeader colFormat = new();
        private ColumnHeader colResolution = new();
        private ColumnHeader colDuration = new();
        private ColumnHeader colSize = new();
        private ColumnHeader colStatus = new();

        private TabControl tabSettings = null!;
        private TabPage tabVideo = null!;
        private TabPage tabAudio = null!;
        private TabPage tabOutput = null!;
        private TabPage tabAdvanced = null!;

        private ComboBox cbFormat = null!;
        private ComboBox cbVideoCodec = null!;
        private RadioButton rbUsePreset = null!;
        private RadioButton rbUsePercent = null!;
        private ComboBox cbPreset = null!;
        private NumericUpDown nudPercent = null!;
        private ComboBox cbQuality = null!;

        private CheckBox chkEnableAudio = null!;
        private ComboBox cbAudioCodec = null!;
        private ComboBox cbAudioBitrate = null!;

        private TextBox txtOutputFolder = null!;
        private CheckBox chkCreateConvertedFolder = null!;
        private ComboBox cbNamingPattern = null!;

        private TextBox txtFfmpegPath = null!;
        private NumericUpDown nudThreads = null!;
        private CheckBox chkHardwareAccel = null!;

        private Panel panelBottom = null!;
        private ProgressBar progressBarTotal = null!;
        private ProgressBar progressBarCurrent = null!;
        private Label lblStatusTotal = null!;
        private Label lblStatusCurrent = null!;
        private Button btnStart = null!;
        private Button btnStop = null!;
        private Button btnSavePreset = null!;
        private Button btnLoadPreset = null!;

        private GroupBox groupLog = null!;
        private TextBox txtLog = null!;

        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isProcessing = false;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            BuildUi();
            SetDefaults();
            _ = EnsureFfmpegAsync();
        }

        private void BuildUi()
        {
            this.Text = "Графический видеоконвертер Pro";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 650);
            this.ClientSize = new Size(1300, 750);
            this.BackColor = Color.FromArgb(240, 240, 245);
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

            splitContainerMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 700,
                BackColor = Color.FromArgb(200, 200, 210)
            };
            this.Controls.Add(splitContainerMain);

            // Left panel - File list
            BuildLeftPanel();

            // Right panel - Settings
            BuildRightPanel();

            // Bottom panel - Progress and controls
            BuildBottomPanel();
        }

        private void BuildLeftPanel()
        {
            // Top toolbar
            panelLeftTop = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 50,
                BackColor = Color.FromArgb(250, 250, 255),
                Padding = new Padding(10, 10, 10, 5)
            };

            btnAddFiles = CreateStyledButton("➕ Добавить файлы", 0);
            btnRemoveSelected = CreateStyledButton("➖ Удалить выбранные", 160);
            btnClearAll = CreateStyledButton("🗑 Очистить всё", 340);

            btnAddFiles.Click += btnAddFiles_Click;
            btnRemoveSelected.Click += btnRemoveSelected_Click;
            btnClearAll.Click += (s, e) => lvFiles.Items.Clear();

            panelLeftTop.Controls.AddRange(new Control[] { btnAddFiles, btnRemoveSelected, btnClearAll });

            // Files ListView with modern styling
            lvFiles = new ListView
            {
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                View = View.Details,
                MultiSelect = true,
                AllowDrop = true,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            lvFiles.DragEnter += ListView_DragEnter;
            lvFiles.DragDrop += ListView_DragDrop;

            colName.Text = "Имя файла"; colName.Width = 150;
            colPath.Text = "Путь"; colPath.Width = 220;
            colFormat.Text = "Формат"; colFormat.Width = 70;
            colResolution.Text = "Разрешение"; colResolution.Width = 100;
            colDuration.Text = "Длительность"; colDuration.Width = 100;
            colSize.Text = "Размер"; colSize.Width = 90;
            colStatus.Text = "Статус"; colStatus.Width = 120;

            lvFiles.Columns.AddRange(new[] { colName, colPath, colFormat, colResolution, colDuration, colSize, colStatus });

            splitContainerMain.Panel1.Controls.Add(lvFiles);
            splitContainerMain.Panel1.Controls.Add(panelLeftTop);
        }

        private void BuildRightPanel()
        {
            var panel = new Panel 
            { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(10),
                BackColor = Color.FromArgb(250, 250, 255)
            };

            tabSettings = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F)
            };

            tabVideo = new TabPage("🎬 Видео");
            tabAudio = new TabPage("🔊 Аудио");

            BuildVideoTab();
            BuildAudioTab();

            tabSettings.TabPages.AddRange(new[] { tabVideo, tabAudio });
            panel.Controls.Add(tabSettings);
            splitContainerMain.Panel2.Controls.Add(panel);
        }

        private void BuildVideoTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15), AutoScroll = true };

            int y = 10;

            // Format
            panel.Controls.Add(CreateLabel("Формат вывода:", 10, y));
            cbFormat = CreateComboBox(140, y, 180);
            cbFormat.Items.AddRange(new object[] { "MP4", "MKV", "AVI", "MOV", "WEBM", "FLV", "TS", "M4V", "3GP", "OGV", "WMV", "GIF" });
            cbFormat.SelectedIndexChanged += cbFormat_SelectedIndexChanged;
            panel.Controls.Add(cbFormat);
            y += 40;

            // Video Codec
            panel.Controls.Add(CreateLabel("Видео-кодек:", 10, y));
            cbVideoCodec = CreateComboBox(140, y, 180);
            panel.Controls.Add(cbVideoCodec);
            y += 40;

            // Quality
            panel.Controls.Add(CreateLabel("Качество:", 10, y));
            cbQuality = CreateComboBox(140, y, 180);
            cbQuality.Items.AddRange(new object[] { "Высокое (CRF 18)", "Хорошее (CRF 23)", "Среднее (CRF 28)", "Низкое (CRF 32)" });
            panel.Controls.Add(cbQuality);
            y += 50;

            // Resolution
            var groupRes = new GroupBox { Left = 10, Top = y, Width = 470, Height = 110, Text = "Разрешение" };
            
            rbUsePreset = new RadioButton { Left = 15, Top = 25, Width = 180, Text = "Стандартное разрешение", Checked = true };
            rbUsePercent = new RadioButton { Left = 15, Top = 55, Width = 200, Text = "Масштаб (% от оригинала)" };
            
            cbPreset = CreateComboBox(230, 22, 120);
            cbPreset.Items.AddRange(new object[] { "360p", "480p", "576p", "720p", "1080p", "1440p", "2160p (4K)" });
            
            nudPercent = new NumericUpDown { Left = 230, Top = 52, Width = 80, Minimum = 10, Maximum = 200, Value = 100, Enabled = false };

            rbUsePreset.CheckedChanged += (s, e) => 
            {
                cbPreset.Enabled = rbUsePreset.Checked;
                nudPercent.Enabled = !rbUsePreset.Checked;
            };

            groupRes.Controls.AddRange(new Control[] { rbUsePreset, rbUsePercent, cbPreset, nudPercent });
            panel.Controls.Add(groupRes);
            y += groupRes.Height + 20;

            // Output section (moved from 'Вывод')
            panel.Controls.Add(CreateLabel("Папка сохранения:", 10, y));
            txtOutputFolder = new TextBox { Left = 10, Top = y + 25, Width = 380, Font = new Font("Segoe UI", 9F) };
            var btnBrowseOut = CreateStyledButton("📁 Обзор", 400);
            btnBrowseOut.Top = y + 23;
            btnBrowseOut.Width = 80;
            btnBrowseOut.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtOutputFolder.Text = fbd.SelectedPath;
            };
            panel.Controls.AddRange(new Control[] { txtOutputFolder, btnBrowseOut });
            y += 70;

            chkCreateConvertedFolder = new CheckBox
            {
                Left = 10,
                Top = y,
                Width = 300,
                Text = "Создать подпапку 'Converted'",
                Checked = true
            };
            panel.Controls.Add(chkCreateConvertedFolder);
            y += 40;

            panel.Controls.Add(CreateLabel("Шаблон имени:", 10, y));
            cbNamingPattern = CreateComboBox(140, y, 250);
            cbNamingPattern.Items.AddRange(new object[]
            {
                "{original}",
                "{original}_converted",
                "{original}_{format}",
                "{original}_{codec}_{resolution}"
            });
            if (cbNamingPattern.Items.Count > 1) cbNamingPattern.SelectedIndex = 1;
            panel.Controls.Add(cbNamingPattern);

            tabVideo.Controls.Add(panel);
        }

        private void BuildAudioTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15), AutoScroll = true };

            int y = 10;

            chkEnableAudio = new CheckBox 
            { 
                Left = 10, 
                Top = y, 
                Width = 200, 
                Text = "Включить аудиодорожку", 
                Checked = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            chkEnableAudio.CheckedChanged += (s, e) => 
            {
                cbAudioCodec.Enabled = chkEnableAudio.Checked;
                cbAudioBitrate.Enabled = chkEnableAudio.Checked;
            };
            panel.Controls.Add(chkEnableAudio);
            y += 50;

            panel.Controls.Add(CreateLabel("Аудио-кодек:", 10, y));
            cbAudioCodec = CreateComboBox(140, y, 180);
            panel.Controls.Add(cbAudioCodec);
            y += 40;

            panel.Controls.Add(CreateLabel("Битрейт:", 10, y));
            cbAudioBitrate = CreateComboBox(140, y, 180);
            cbAudioBitrate.Items.AddRange(new object[] { "96k", "128k", "160k", "192k", "256k", "320k" });
            panel.Controls.Add(cbAudioBitrate);

            tabAudio.Controls.Add(panel);
        }

        private void BuildOutputTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15), AutoScroll = true };

            int y = 10;

            panel.Controls.Add(CreateLabel("Папка сохранения:", 10, y));
            txtOutputFolder = new TextBox { Left = 10, Top = y + 25, Width = 380, Font = new Font("Segoe UI", 9F) };
            var btnBrowse = CreateStyledButton("📁 Обзор", 400);
            btnBrowse.Top = y + 23;
            btnBrowse.Width = 80;
            btnBrowse.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtOutputFolder.Text = fbd.SelectedPath;
            };
            panel.Controls.AddRange(new Control[] { txtOutputFolder, btnBrowse });
            y += 70;

            chkCreateConvertedFolder = new CheckBox 
            { 
                Left = 10, 
                Top = y, 
                Width = 300, 
                Text = "Создать подпапку 'Converted'",
                Checked = true
            };
            panel.Controls.Add(chkCreateConvertedFolder);
            y += 40;

            panel.Controls.Add(CreateLabel("Шаблон имени:", 10, y));
            cbNamingPattern = CreateComboBox(140, y, 250);
            cbNamingPattern.Items.AddRange(new object[] 
            { 
                "{original}", 
                "{original}_converted", 
                "{original}_{format}",
                "{original}_{codec}_{resolution}"
            });
            cbNamingPattern.SelectedIndex = 1;
            panel.Controls.Add(cbNamingPattern);

            tabOutput.Controls.Add(panel);
        }

        private void BuildAdvancedTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15), AutoScroll = true };

            int y = 10;

            panel.Controls.Add(CreateLabel("Путь к FFmpeg:", 10, y));
            txtFfmpegPath = new TextBox { Left = 10, Top = y + 25, Width = 380, Font = new Font("Segoe UI", 9F) };
            var btnBrowse = CreateStyledButton("📁 Обзор", 400);
            btnBrowse.Top = y + 23;
            btnBrowse.Width = 80;
            btnBrowse.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtFfmpegPath.Text = fbd.SelectedPath;
            };
            panel.Controls.AddRange(new Control[] { txtFfmpegPath, btnBrowse });
            y += 70;

            panel.Controls.Add(CreateLabel("Потоков кодирования:", 10, y));
            nudThreads = new NumericUpDown 
            { 
                Left = 180, 
                Top = y, 
                Width = 80, 
                Minimum = 0, 
                Maximum = 16, 
                Value = 0 
            };
            panel.Controls.Add(nudThreads);
            y += 40;

            chkHardwareAccel = new CheckBox 
            { 
                Left = 10, 
                Top = y, 
                Width = 300, 
                Text = "Аппаратное ускорение (если доступно)"
            };
            panel.Controls.Add(chkHardwareAccel);

            tabAdvanced.Controls.Add(panel);
        }

        private void BuildBottomPanel()
        {
            panelBottom = new Panel 
            { 
                Dock = DockStyle.Bottom, 
                Height = 300, // Increased height to accommodate both sections
                BackColor = Color.FromArgb(250, 250, 255),
                Padding = new Padding(10)
            };

            // Main split container for progress and log sections
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 120, // Initial position of the splitter
                SplitterWidth = 8,
                BackColor = Color.FromArgb(200, 200, 210)
            };

            // Top panel - Progress section
            var panelTop = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            // Progress section
            lblStatusTotal = new Label { Left = 0, Top = 5, Width = 600, Text = "Готов к конвертации", Font = new Font("Segoe UI", 9F) };
            progressBarTotal = new ProgressBar { Left = 0, Top = 25, Width = panelTop.Width - 20, Height = 20, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };

            lblStatusCurrent = new Label { Left = 0, Top = 50, Width = 600, Text = "Ожидание...", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.Gray };
            progressBarCurrent = new ProgressBar { Left = 0, Top = 70, Width = panelTop.Width - 20, Height = 15, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };

            // Buttons
            btnStart = CreateStyledButton("▶ Начать конвертацию", 0);
            btnStart.Top = 95;
            btnStart.Width = 170;
            btnStart.Height = 35;
            btnStart.BackColor = Color.FromArgb(0, 120, 215);
            btnStart.ForeColor = Color.White;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += btnStart_Click;

            btnStop = CreateStyledButton("⏹ Остановить", 180);
            btnStop.Top = 95;
            btnStop.Width = 120;
            btnStop.Height = 35;
            btnStop.BackColor = Color.FromArgb(180, 50, 50);
            btnStop.ForeColor = Color.White;
            btnStop.Enabled = false;
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Click += (s, e) => _cancellationTokenSource?.Cancel();

            btnSavePreset = CreateStyledButton("💾 Сохранить пресет", 890);
            btnSavePreset.Top = 95;
            btnSavePreset.Width = 180;
            btnSavePreset.Height = 35;
            btnSavePreset.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSavePreset.Click += btnSavePreset_Click;

            btnLoadPreset = CreateStyledButton("📂 Загрузить пресет", 1080);
            btnLoadPreset.Top = 95;
            btnLoadPreset.Width = 180;
            btnLoadPreset.Height = 35;
            btnLoadPreset.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoadPreset.Click += btnLoadPreset_Click;

            panelTop.Controls.AddRange(new Control[] { 
                lblStatusTotal, progressBarTotal, lblStatusCurrent, progressBarCurrent,
                btnStart, btnStop, btnSavePreset, btnLoadPreset 
            });

            // Bottom panel - Log section
            var panelBottomLog = new Panel 
            { 
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5)
            };

            groupLog = new GroupBox 
            { 
                Dock = DockStyle.Fill, 
                Text = "📋 Журнал операций",
                Padding = new Padding(5)
            };
            
            txtLog = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                Multiline = true, 
                ScrollBars = ScrollBars.Both,
                BackColor = Color.FromArgb(245, 245, 250),
                Font = new Font("Consolas", 8.5F),
                ReadOnly = true,
                WordWrap = false
            };
            groupLog.Controls.Add(txtLog);
            panelBottomLog.Controls.Add(groupLog);

            // Add panels to split container
            splitContainer.Panel1.Controls.Add(panelTop);
            splitContainer.Panel2.Controls.Add(panelBottomLog);

            // Add split container to main panel
            panelBottom.Controls.Add(splitContainer);
            this.Controls.Add(panelBottom);
        }

        private Button CreateStyledButton(string text, int left)
        {
            return new Button
            {
                Text = text,
                Left = left,
                Top = 8,
                Width = 150,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(230, 230, 240),
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
        }

        private Label CreateLabel(string text, int left, int top)
        {
            return new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = 120,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private ComboBox CreateComboBox(int left, int top, int width)
        {
            return new ComboBox
            {
                Left = left,
                Top = top,
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
        }

        private void SetDefaults()
        {
            cbFormat.SelectedIndex = 0;
            cbPreset.SelectedIndex = 3;
            cbAudioBitrate.SelectedIndex = 3;
            cbQuality.SelectedIndex = 1;
            // Ensure Output defaults
            if (cbNamingPattern != null && cbNamingPattern.Items.Count > 1 && cbNamingPattern.SelectedIndex < 0)
                cbNamingPattern.SelectedIndex = 1;
            // Initialize hidden Advanced defaults to keep logic working without UI tab
            if (txtFfmpegPath == null) txtFfmpegPath = new TextBox();
            if (nudThreads == null) nudThreads = new NumericUpDown { Value = 0 };
            if (chkHardwareAccel == null) chkHardwareAccel = new CheckBox { Checked = false };
        }

        private void Form1_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
                AddFilesToList(files);
        }

        private void ListView_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private void ListView_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
                AddFilesToList(files);
        }

        private async void btnAddFiles_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Видео файлы|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.mpg;*.mpeg|Все файлы|*.*",
                Multiselect = true
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                AddFilesToList(ofd.FileNames);
            }
        }

        private void AddFilesToList(string[] paths)
        {
            foreach (var path in paths)
            {
                if (!System.IO.File.Exists(path)) continue;

                // Check if already added
                if (lvFiles.Items.Cast<ListViewItem>().Any(i => i.SubItems[1].Text == path))
                    continue;

                var fi = new System.IO.FileInfo(path);
                var item = new ListViewItem(new[]
                {
                    fi.Name,
                    fi.FullName,
                    fi.Extension.Trim('.').ToUpperInvariant(),
                    "-",
                    "-",
                    FormatFileSize(fi.Length),
                    "В очереди"
                });
                item.Tag = new FileConversionInfo { FilePath = path };
                lvFiles.Items.Add(item);

                _ = ProbeFileAsync(item, path);
            }

            AppendLog($"Добавлено файлов: {paths.Length}");
        }

        private async Task ProbeFileAsync(ListViewItem item, string path)
        {
            try
            {
                await EnsureFfmpegAsync();
                var info = await FFmpeg.GetMediaInfo(path);
                var v = info.VideoStreams?.FirstOrDefault();
                
                this.BeginInvoke(new Action(() =>
                {
                    item.SubItems[3].Text = v != null ? $"{v.Width}x{v.Height}" : "-";
                    item.SubItems[4].Text = info.Duration.ToString(@"hh\:mm\:ss");
                    
                    if (item.Tag is FileConversionInfo fileInfo)
                    {
                        fileInfo.Duration = info.Duration;
                        fileInfo.Width = v?.Width ?? 0;
                        fileInfo.Height = v?.Height ?? 0;
                    }

                    if (cbVideoCodec.Items.Count == 0)
                        PopulateCodecsForFormat(cbFormat.SelectedItem?.ToString() ?? "MP4");
                    if (cbAudioCodec.Items.Count == 0)
                    {
                        cbAudioCodec.Items.AddRange(new object[] { "aac", "libmp3lame", "libopus", "ac3" });
                        cbAudioCodec.SelectedIndex = 0;
                    }
                }));
            }
            catch (Exception ex)
            {
                AppendLog($"Ошибка анализа {System.IO.Path.GetFileName(path)}: {ex.Message}");
            }
        }

        private void btnRemoveSelected_Click(object? sender, EventArgs e)
        {
            foreach (ListViewItem item in lvFiles.SelectedItems)
                lvFiles.Items.Remove(item);
            
            AppendLog($"Удалено выбранных файлов: {lvFiles.SelectedItems.Count}");
        }

        private void cbFormat_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var fmt = cbFormat.SelectedItem?.ToString() ?? "MP4";
            PopulateCodecsForFormat(fmt);
        }

        private void PopulateCodecsForFormat(string format)
        {
            cbVideoCodec.Items.Clear();
            cbAudioCodec.Items.Clear();

            switch (format.ToUpperInvariant())
            {
                case "MP4":
                case "M4V":
                    cbVideoCodec.Items.AddRange(new object[] { "copy (без перекодирования)", "libx264 (H.264)", "libx265 (HEVC)", "libsvtav1 (AV1)" });
                    cbAudioCodec.Items.AddRange(new object[] { "copy", "aac", "libmp3lame" });
                    break;
                case "MKV":
                    cbVideoCodec.Items.AddRange(new object[] { "copy (без перекодирования)", "libx264 (H.264)", "libx265 (HEVC)", "libvpx-vp9 (VP9)", "libsvtav1 (AV1)", "mpeg2video" });
                    cbAudioCodec.Items.AddRange(new object[] { "copy", "aac", "libmp3lame", "libopus", "ac3", "flac" });
                    break;
                case "WEBM":
                    cbVideoCodec.Items.AddRange(new object[] { "copy (без перекодирования)", "libvpx (VP8)", "libvpx-vp9 (VP9)", "libsvtav1 (AV1)" });
                    cbAudioCodec.Items.AddRange(new object[] { "copy", "libopus", "libvorbis" });
                    break;
                case "AVI":
                    cbVideoCodec.Items.AddRange(new object[] { "copy (без перекодирования)", "mpeg4", "libx264 (H.264)" });
                    cbAudioCodec.Items.AddRange(new object[] { "copy", "mp2", "libmp3lame" });
                    break;
                case "MOV":
                    cbVideoCodec.Items.AddRange(new object[] { "copy (без перекодирования)", "libx264 (H.264)", "prores_ks (ProRes)" });
                    cbAudioCodec.Items.AddRange(new object[] { "copy", "aac", "alac" });
                    break;
                case "FLV":
                    cbVideoCodec.Items.AddRange(new object[] { "copy (без перекодирования)", "flv1", "libx264 (H.264)" });
                    cbAudioCodec.Items.AddRange(new object[] { "copy", "aac", "libmp3lame" });
                    break;
                case "TS":
                    cbVideoCodec.Items.AddRange(new object[] { "copy (без перекодирования)", "libx264 (H.264)", "libx265 (HEVC)", "mpeg2video" });
                    cbAudioCodec.Items.AddRange(new object[] { "copy", "aac", "mp2", "ac3" });
                    break;
                case "3GP":
                    cbVideoCodec.Items.AddRange(new object[] { "libx264 (H.264)", "mpeg4" });
                    cbAudioCodec.Items.AddRange(new object[] { "aac" });
                    break;
                case "OGV":
                    cbVideoCodec.Items.AddRange(new object[] { "libtheora" });
                    cbAudioCodec.Items.AddRange(new object[] { "libvorbis", "libopus" });
                    break;
                case "WMV":
                    cbVideoCodec.Items.AddRange(new object[] { "wmv2", "libx264 (H.264)" });
                    cbAudioCodec.Items.AddRange(new object[] { "wmav2", "libmp3lame" });
                    break;
                case "GIF":
                    cbVideoCodec.Items.AddRange(new object[] { "gif" });
                    cbAudioCodec.Items.AddRange(new object[] { });
                    break;
                default:
                    cbVideoCodec.Items.AddRange(new object[] { "libx264 (H.264)" });
                    cbAudioCodec.Items.AddRange(new object[] { "aac" });
                    break;
            }

            if (cbVideoCodec.Items.Count > 0) cbVideoCodec.SelectedIndex = 0;
            if (cbAudioCodec.Items.Count > 0) cbAudioCodec.SelectedIndex = 0;
        }

        private async void btnStart_Click(object? sender, EventArgs e)
        {
            if (_isProcessing)
            {
                MessageBox.Show(this, "Конвертация уже выполняется", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lvFiles.Items.Count == 0)
            {
                MessageBox.Show(this, "Добавьте файлы для конвертации", "Нет файлов", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
            {
                MessageBox.Show(this, "Пожалуйста, укажите папку для сохранения файлов", "Папка не выбрана", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputFolder.Text))
            {
                MessageBox.Show(this, "Укажите папку для сохранения", "Не указана папка", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isProcessing = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await EnsureFfmpegAsync();
                var result = await ProcessAllFilesAsync(_cancellationTokenSource.Token);
                
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (result.failed == 0)
                    {
                        AppendLog("✅ Все файлы успешно обработаны!");
                        MessageBox.Show(this, "Конвертация завершена успешно!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        AppendLog($"⚠ Завершено с ошибками: успешно {result.ok} из {result.total}, ошибок {result.failed}");
                        MessageBox.Show(this, $"Конвертация завершена с ошибками.\nУспешно: {result.ok}/{result.total}", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    
                    if (MessageBox.Show(this, "Открыть папку с файлами?", "Готово", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = txtOutputFolder.Text,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    AppendLog("⚠ Конвертация отменена пользователем");
                }
            }
            catch (OperationCanceledException)
            {
                AppendLog("⚠ Конвертация отменена");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Критическая ошибка: {ex.Message}");
                MessageBox.Show(this, $"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                progressBarTotal.Value = 0;
                progressBarCurrent.Value = 0;
                lblStatusTotal.Text = "Готов к конвертации";
                lblStatusCurrent.Text = "Ожидание...";
            }
        }

        private async Task<(int total, int ok, int failed)> ProcessAllFilesAsync(CancellationToken cancellationToken)
        {
            var format = (cbFormat.SelectedItem?.ToString() ?? "MP4").ToLowerInvariant();
            var vcodec = ExtractCodecName(cbVideoCodec.SelectedItem?.ToString() ?? "libx264");
            var acodec = chkEnableAudio.Checked ? ExtractCodecName(cbAudioCodec.SelectedItem?.ToString() ?? "aac") : "none";
            var abitrate = chkEnableAudio.Checked ? (cbAudioBitrate.SelectedItem?.ToString() ?? "192k") : "0k";
            var crf = ExtractCRF(cbQuality.SelectedItem?.ToString() ?? "Хорошее (CRF 23)");

            var items = lvFiles.Items.Cast<ListViewItem>().ToList();
            var totalFiles = items.Count;
            var processedFiles = 0;
            var failedFiles = 0;

            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                processedFiles++;
                var inputPath = item.SubItems[1].Text;
                var fileName = System.IO.Path.GetFileNameWithoutExtension(inputPath);

                // Update status
                this.BeginInvoke(new Action(() =>
                {
                    item.SubItems[6].Text = "Обработка...";
                    item.BackColor = Color.LightYellow;
                    lblStatusTotal.Text = $"Обработка файла {processedFiles} из {totalFiles}";
                    progressBarTotal.Value = (int)((processedFiles - 1) * 100.0 / totalFiles);
                    progressBarCurrent.Value = 0;
                }));

                try
                {
                    var outputPath = GenerateOutputPath(inputPath, format);
                    await ConvertFileAsync(inputPath, outputPath, format, vcodec, acodec, abitrate, crf, cancellationToken);

                    this.BeginInvoke(new Action(() =>
                    {
                        item.SubItems[6].Text = "✅ Готово";
                        item.BackColor = Color.LightGreen;
                    }));
                }
                catch (OperationCanceledException)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        item.SubItems[6].Text = "⚠ Отменено";
                        item.BackColor = Color.LightGray;
                    }));
                    throw;
                }
                catch (Exception ex)
                {
                    failedFiles++;
                    AppendLog($"❌ Ошибка обработки {fileName}: {ex.Message}");
                    this.BeginInvoke(new Action(() =>
                    {
                        item.SubItems[6].Text = "❌ Ошибка";
                        item.BackColor = Color.LightCoral;
                    }));
                }
            }

            this.BeginInvoke(new Action(() =>
            {
                progressBarTotal.Value = 100;
                lblStatusTotal.Text = $"Завершено: {processedFiles} из {totalFiles}";
            }));

            return (totalFiles, totalFiles - failedFiles, failedFiles);
        }

        private async Task ConvertFileAsync(string inputPath, string outputPath, string format, string vcodec, 
            string acodec, string abitrate, int crf, CancellationToken cancellationToken)
        {
            var fileName = System.IO.Path.GetFileName(inputPath);
            AppendLog($"🎬 Начало: {fileName} -> {System.IO.Path.GetFileName(outputPath)}");

            try 
            {
                // Ensure input file exists
                if (!System.IO.File.Exists(inputPath))
                {
                    throw new FileNotFoundException($"Входной файл не найден: {inputPath}");
                }

                // Ensure output directory exists
                var outputDir = System.IO.Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !System.IO.Directory.Exists(outputDir))
                {
                    System.IO.Directory.CreateDirectory(outputDir);
                }

                string? scaleFilter = null;
                try
                {
                    var info = await FFmpeg.GetMediaInfo(inputPath);
                    var v = info.VideoStreams?.FirstOrDefault();
                    
                    if (v != null)
                    {
                        if (rbUsePreset.Checked)
                        {
                            int newHeight = PresetToHeight(cbPreset.SelectedItem?.ToString() ?? "720p");
                            scaleFilter = $"scale=-2:{newHeight}";
                        }
                        else
                        {
                            var pct = (int)nudPercent.Value;
                            scaleFilter = $"scale=trunc(iw*{pct}/100/2)*2:trunc(ih*{pct}/100/2)*2";
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"⚠ Предупреждение при анализе: {ex.Message}");
                }

                var conv = FFmpeg.Conversions.New();
                
                conv.OnProgress += (s, args) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // FFmpeg will handle cancellation
                        return;
                    }

                    this.BeginInvoke(new Action(() =>
                    {
                        var percent = Math.Clamp(args.Percent, 0, 100);
                        progressBarCurrent.Value = (int)percent;
                        lblStatusCurrent.Text = $"{fileName}: {percent:F1}% | {args.TotalLength:hh\\:mm\\:ss}";
                    }));
                };

                conv.OnDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        AppendLog($"FFmpeg: {args.Data}");
                    }
                };

                conv.AddParameter("-loglevel verbose");
                conv.AddParameter($"-i \"{inputPath}\"");

                bool isGif = string.Equals(format, "gif", StringComparison.OrdinalIgnoreCase);
                bool videoCopy = string.Equals(vcodec, "copy", StringComparison.OrdinalIgnoreCase);
                bool audioCopy = chkEnableAudio.Checked && string.Equals(acodec, "copy", StringComparison.OrdinalIgnoreCase);

                if (chkHardwareAccel.Checked)
                {
                    conv.AddParameter("-hwaccel auto");
                }

                // GIF не содержит аудио
                if (isGif)
                {
                    conv.AddParameter("-an");
                }
                else if (!chkEnableAudio.Checked)
                {
                    conv.AddParameter("-an");
                }

                // Видео кодирование/копирование
                if (videoCopy)
                {
                    conv.AddParameter("-c:v copy");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(scaleFilter))
                    {
                        conv.AddParameter($"-vf {scaleFilter}");
                    }
                    conv.AddParameter($"-c:v {vcodec}");
                    if (!isGif)
                    {
                        conv.AddParameter("-pix_fmt yuv420p");
                    }
                    if (vcodec.IndexOf("x264", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        vcodec.IndexOf("x265", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        conv.AddParameter($"-crf {crf} -preset medium");
                    }
                }

                var threads = (int)nudThreads.Value;
                if (threads > 0)
                {
                    conv.AddParameter($"-threads {threads}");
                }

                // Аудио кодирование/копирование (если не GIF и аудио включено)
                if (!isGif && chkEnableAudio.Checked)
                {
                    if (audioCopy)
                    {
                        conv.AddParameter("-c:a copy");
                    }
                    else
                    {
                        conv.AddParameter($"-c:a {acodec} -b:a {abitrate}");
                    }
                }

                if (string.Equals(format, "mp4", StringComparison.OrdinalIgnoreCase))
                {
                    conv.AddParameter("-movflags +faststart");
                }

                conv.AddParameter($"\"{outputPath}\"");

                try
                {
                    AppendLog("FFmpeg cmd: " + conv.Build());
                }
                catch { }

                await conv.Start(cancellationToken);
                AppendLog($"✅ Завершено: {fileName}");
            }
            catch (OperationCanceledException)
            {
                AppendLog($"⏹ Прервано: {fileName}");
                throw;
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Ошибка при обработке {fileName}: {ex.Message}");
                throw new Exception($"Не удалось обработать {fileName}: {ex.Message}", ex);
            }
        }

        private string GenerateOutputPath(string inputPath, string format)
        {
            var outputFolder = txtOutputFolder.Text.Trim();
            
            // If output folder is not set, use the input file's directory
            if (string.IsNullOrEmpty(outputFolder))
            {
                outputFolder = System.IO.Path.GetDirectoryName(inputPath) ?? string.Empty;
            }

            if (chkCreateConvertedFolder.Checked)
            {
                outputFolder = System.IO.Path.Combine(outputFolder, "Converted");
                System.IO.Directory.CreateDirectory(outputFolder);
            }

            var originalName = System.IO.Path.GetFileNameWithoutExtension(inputPath);
            var pattern = cbNamingPattern.SelectedItem?.ToString() ?? "{original}_converted";
            
            var outputName = pattern
                .Replace("{original}", originalName)
                .Replace("{format}", format.ToUpperInvariant())
                .Replace("{codec}", ExtractCodecName(cbVideoCodec.SelectedItem?.ToString() ?? ""))
                .Replace("{resolution}", cbPreset.SelectedItem?.ToString() ?? "");

            var outputFile = $"{outputName}.{format}";
            var fullPath = System.IO.Path.Combine(outputFolder, outputFile);

            // Ensure the directory exists
            var dir = System.IO.Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            return fullPath;
        }

        private void btnSavePreset_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Preset файл|*.preset|Все файлы|*.*",
                DefaultExt = "preset"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var preset = new Dictionary<string, string>
                    {
                        ["Format"] = cbFormat.SelectedItem?.ToString() ?? "",
                        ["VideoCodec"] = cbVideoCodec.SelectedItem?.ToString() ?? "",
                        ["AudioCodec"] = cbAudioCodec.SelectedItem?.ToString() ?? "",
                        ["AudioBitrate"] = cbAudioBitrate.SelectedItem?.ToString() ?? "",
                        ["Quality"] = cbQuality.SelectedItem?.ToString() ?? "",
                        ["Preset"] = cbPreset.SelectedItem?.ToString() ?? "",
                        ["Percent"] = nudPercent.Value.ToString(),
                        ["UsePreset"] = rbUsePreset.Checked.ToString(),
                        ["EnableAudio"] = chkEnableAudio.Checked.ToString(),
                        ["NamingPattern"] = cbNamingPattern.SelectedItem?.ToString() ?? "",
                        ["Threads"] = nudThreads.Value.ToString(),
                        ["HardwareAccel"] = chkHardwareAccel.Checked.ToString()
                    };

                    var lines = preset.Select(kvp => $"{kvp.Key}={kvp.Value}");
                    System.IO.File.WriteAllLines(sfd.FileName, lines);
                    
                    AppendLog($"💾 Пресет сохранен: {System.IO.Path.GetFileName(sfd.FileName)}");
                    MessageBox.Show(this, "Пресет успешно сохранен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnLoadPreset_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Preset файл|*.preset|Все файлы|*.*"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var lines = System.IO.File.ReadAllLines(ofd.FileName);
                    var preset = lines
                        .Select(line => line.Split('='))
                        .Where(parts => parts.Length == 2)
                        .ToDictionary(parts => parts[0], parts => parts[1]);

                    if (preset.TryGetValue("Format", out var format))
                        cbFormat.SelectedItem = format;
                    if (preset.TryGetValue("VideoCodec", out var vcodec))
                        cbVideoCodec.SelectedItem = vcodec;
                    if (preset.TryGetValue("AudioCodec", out var acodec))
                        cbAudioCodec.SelectedItem = acodec;
                    if (preset.TryGetValue("AudioBitrate", out var abitrate))
                        cbAudioBitrate.SelectedItem = abitrate;
                    if (preset.TryGetValue("Quality", out var quality))
                        cbQuality.SelectedItem = quality;
                    if (preset.TryGetValue("Preset", out var res))
                        cbPreset.SelectedItem = res;
                    if (preset.TryGetValue("Percent", out var pct))
                        nudPercent.Value = decimal.Parse(pct);
                    if (preset.TryGetValue("UsePreset", out var usePreset))
                        rbUsePreset.Checked = bool.Parse(usePreset);
                    if (preset.TryGetValue("EnableAudio", out var enableAudio))
                        chkEnableAudio.Checked = bool.Parse(enableAudio);
                    if (preset.TryGetValue("NamingPattern", out var naming))
                        cbNamingPattern.SelectedItem = naming;
                    if (preset.TryGetValue("Threads", out var threads))
                        nudThreads.Value = int.Parse(threads);
                    if (preset.TryGetValue("HardwareAccel", out var hwaccel))
                        chkHardwareAccel.Checked = bool.Parse(hwaccel);

                    AppendLog($"📂 Пресет загружен: {System.IO.Path.GetFileName(ofd.FileName)}");
                    MessageBox.Show(this, "Пресет успешно загружен!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task EnsureFfmpegAsync()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtFfmpegPath.Text) && System.IO.Directory.Exists(txtFfmpegPath.Text))
                {
                    FFmpeg.SetExecutablesPath(txtFfmpegPath.Text);
                    return;
                }

                var baseDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "Converter", 
                    "ffmpeg"
                );
                
                var ffmpegExe = System.IO.Path.Combine(baseDir, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

                if (!System.IO.File.Exists(ffmpegExe))
                {
                    AppendLog("⏳ Загрузка FFmpeg...");
                    System.IO.Directory.CreateDirectory(baseDir);
                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, baseDir);
                    AppendLog("✅ FFmpeg загружен успешно");
                }

                FFmpeg.SetExecutablesPath(baseDir);
                
                if (txtFfmpegPath != null && string.IsNullOrWhiteSpace(txtFfmpegPath.Text))
                {
                    this.BeginInvoke(new Action(() => txtFfmpegPath.Text = baseDir));
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Ошибка настройки FFmpeg: {ex.Message}");
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private static int PresetToHeight(string preset)
        {
            return preset switch
            {
                "360p" => 360,
                "480p" => 480,
                "576p" => 576,
                "720p" => 720,
                "1080p" => 1080,
                "1440p" => 1440,
                "2160p (4K)" => 2160,
                _ => 720
            };
        }

        private static string ExtractCodecName(string codecDisplay)
        {
            // Extract codec name from display text like "libx264 (H.264)"
            var parts = codecDisplay.Split(' ');
            return parts[0];
        }

        private static int ExtractCRF(string qualityDisplay)
        {
            // Extract CRF value from display text like "Хорошее (CRF 23)"
            var match = System.Text.RegularExpressions.Regex.Match(qualityDisplay, @"\d+");
            return match.Success ? int.Parse(match.Value) : 23;
        }

        private void AppendLog(string message)
        {
            try
            {
                if (txtLog?.InvokeRequired == true)
                {
                    txtLog.BeginInvoke(new Action(() => AppendLog(message)));
                    return;
                }
                
                if (txtLog != null)
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss");
                    txtLog.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
                }
            }
            catch { }
        }

        private class FileConversionInfo
        {
            public string FilePath { get; set; } = "";
            public TimeSpan Duration { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}
