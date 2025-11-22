using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using Converter.Application.Models;
using NotificationOptions = Converter.Domain.Models.NotificationOptions;
using Converter.Services;
using Converter.Services.UIServices;
using Converter.UI;
using Converter.UI.Controls;
using Converter.UI.Dialogs;
using Converter.Infrastructure;

namespace Converter
{
    public partial class Form1 : Form
    {
        private SplitContainer splitContainerMain = null!;
        private Panel panelLeftTop = null!;
        private FlowLayoutPanel filesPanel = null!;
        private Converter.Domain.Models.NotificationOptions _notificationSettings = new();

        // Button fields - initialized in UI building methods
        private Button btnAddFiles = null!;
        private Button btnRemoveSelected = null!;
        private Button btnClearAll = null!;
        private Button btnStart = null!;
        private Button btnStop = null!;
        private Button btnSavePreset = null!;
        private Button btnLoadPreset = null!;
        private Button _btnShare = null!;
        private Button _btnOpenEditor = null!;
        private Button _btnNotificationSettings = null!;

        private TabControl tabSettings = null!;
        private TabPage tabVideo = null!;
        private TabPage tabAudio = null!;
        private TabPage tabOutput = null!;
        private TabPage tabAdvanced = null!;
        private TabPage tabPresets = null!;
        private TabPage tabQueue = null!;

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
        private AudioProcessingPanel? _audioProcessingPanel;

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

        private GroupBox groupLog = null!;
        private TextBox txtLog = null!;

        // Services - получаем через DI
        private readonly IPresetService _presetService;
        private readonly IConversionEstimationService _estimationService;
        private PresetPanel? _presetPanel;
        private bool _presetsLoaded = false;

        private UI.Controls.EstimatePanel? _estimatePanel;

        // MVVM binding helpers
        private BindingSource? _queueBindingSource;
        private DataGridView? _queueGrid;

        // Presets binding (for future use)
        private BindingSource? _presetsBindingSource;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            BuildUi();
            SetDefaults();
            InitializeAdvancedTheming();
        }

        private void SyncQueueSelectionWithFileItem(FileListItem item)
        {
            if (_queueItemsBinding == null || _queueGrid == null)
            {
                return;
            }

            // Обновляем IsSelected у элементов очереди по пути файла
            foreach (var vm in _queueItemsBinding)
            {
                vm.IsSelected = string.Equals(vm.FilePath, item.FilePath, StringComparison.OrdinalIgnoreCase);
            }

            // Обновляем выделение строк в гриде очереди
            foreach (DataGridViewRow row in _queueGrid.Rows)
            {
                if (row.DataBoundItem is Converter.Application.ViewModels.QueueItemViewModel vm)
                {
                    row.Selected = vm.IsSelected;
                }
                else
                {
                    row.Selected = false;
                }
            }
        }

        private int EstimateVideoBitrateKbps(int height, string codec, int crf)
        {
            // very rough heuristic based on height, codec and CRF
            int baseKbps = height switch
            {
                >= 2160 => 45000,
                >= 1440 => 20000,
                >= 1080 => 8000,
                >= 720 => 5000,
                >= 480 => 1500,
                _ => 1000
            };
            // codec efficiency factors (lower bitrate needed for same quality)
            double codecEff = codec switch
            {
                var c when c.StartsWith("libx265", StringComparison.OrdinalIgnoreCase) => 0.6,
                var c when c.StartsWith("libvpx-vp9", StringComparison.OrdinalIgnoreCase) => 0.7,
                var c when c.StartsWith("libsvtav1", StringComparison.OrdinalIgnoreCase) => 0.5,
                _ => 1.0
            };
            // CRF influence (relative to CRF 23 baseline)
            double crfScale = Math.Pow(1.06, 23 - crf); // lower CRF => higher bitrate
            int kbps = (int)Math.Max(300, baseKbps * codecEff * crfScale);
            return kbps;
        }

        private void BuildUi()
        {
            this.Text = "Графический видеоконвертер Pro";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 650);
            this.ClientSize = new Size(1300, 750);
            this.BackColor = Color.FromArgb(240, 240, 245);
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
            //btnRemoveSelected = CreateStyledButton("➖ Удалить выбранные", 160);
            btnClearAll = CreateStyledButton("🗑 Очистить всё", 340);
            _btnShare = CreateStyledButton("📤 Поделиться", 520);
            _btnShare.Enabled = false;
            _btnShare.Click += OnShareButtonClick;

            _btnOpenEditor = CreateStyledButton("🎬 Открыть редактор", 680);
            _btnOpenEditor.Enabled = false;
            _btnOpenEditor.Click += OnOpenEditorClick;

            // IMainView: кнопка добавления файлов использует локальный диалог,
            // но далее синхронизирует очередь через async-событие FilesDroppedAsync.
            btnAddFiles.Click += btnAddFiles_Click;

            // File management buttons - делегируем в Presenter через IMainView async-события
            //btnRemoveSelected.Click += async (s, e) => await RaiseRemoveSelectedFilesRequestedAsync();
            btnClearAll.Click += async (s, e) =>
            {
                try
                {
                    // 1. Очищаем визуальные панели (filesPanel + DragDropPanel)
                    ClearAllFiles();

                    // 2. Сообщаем Presenter'у, что нужно очистить очередь
                    await RaiseClearAllFilesRequestedAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при очистке файлов: {ex.Message}");
                }
            };

            panelLeftTop.Controls.AddRange(new Control[]
            {
                btnAddFiles,
                btnRemoveSelected,
                btnClearAll,
                _btnShare,
                _btnOpenEditor
            });
            UpdateShareButtonState();
            UpdateEditorButtonState();

            // Files FlowLayoutPanel with FileListItem controls
            filesPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(5)
                // AllowDrop will be set after handle creation
            };

            filesPanel.HandleCreated += (s, e) => {
                try
                {
                    if (!DesignMode)
                    {
                        filesPanel.AllowDrop = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not enable drag-drop for filesPanel: {ex.Message}");
                }
            };

            filesPanel.DragEnter += FilesPanel_DragEnter;
            filesPanel.DragDrop += FilesPanel_DragDrop;

            var leftContent = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            leftContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
            leftContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            leftContent.Controls.Add(filesPanel, 0, 0);
            leftContent.SetRowSpan(filesPanel, 2);

            splitContainerMain.Panel1.Controls.Add(leftContent);
            splitContainerMain.Panel1.Controls.Add(panelLeftTop);
        }

        private void OnDragDropPanelFilesAdded(object? sender, string[] files)
        {
            _ = OnDragDropPanelFilesAddedAsync(files);
        }

        private async Task OnDragDropPanelFilesAddedAsync(string[] files)
        {
            try
            {
                if (files != null && files.Length > 0)
                {
                    // 1. Обновляем визуальное представление файловой панели
                    AddFilesToList(files);

                    // 2. Сообщаем Presenter'у через IMainView, чтобы добавить файлы в очередь
                    await RaiseFilesDroppedAsync(files).ConfigureAwait(false);

                    // 3. Обновляем оценку времени
                    DebounceEstimate();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при добавлении файлов: {ex.Message}");
            }
        }

        private void OnDragDropPanelFileRemoved(object? sender, string filePath)
        {
            // Синхронизируем удаление только с визуальными элементами и делегируем
            // фактическое удаление из очереди презентеру через IMainView
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            // Удаляем из filesPanel если существует
            var fileItem = filesPanel.Controls.OfType<FileListItem>()
                .FirstOrDefault(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            if (fileItem != null)
            {
                // Используем единый путь удаления, который синхронизирует
                // выделение и инициирует сценарий удаления через Presenter
                RemoveFileFromList(fileItem);
            }

            // Обновляем состояние UI
            UpdateEditorButtonState();
            UpdateShareButtonState();
        }

        private void OnOpenEditorClick(object? sender, EventArgs e)
        {
            var files = filesPanel.Controls
                .OfType<FileListItem>()
                .Select(item => item.FilePath)
                .ToArray();

            if (files.Length == 0)
            {
                MessageBox.Show("Сначала добавьте видео файл!", "Внимание");
                return;
            }

            var firstFile = files[0];
            if (!File.Exists(firstFile))
            {
                MessageBox.Show("Выбранный файл не найден. Добавьте файл заново.", "Ошибка");
                return;
            }

            try
            {
                using var editorForm = new VideoEditorForm(firstFile);
                editorForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ShowError($"Не удалось открыть редактор: {ex.Message}");
            }
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

            tabPresets = new TabPage("⭐ Пресеты");
            tabVideo = new TabPage("🎬 Видео");
            tabAudio = new TabPage("🔊 Аудио");
            tabQueue = new TabPage("📋 Очередь");

            _ = BuildPresetsTabAsync();
            BuildVideoTab();
            BuildAudioTab();
            BuildQueueTab();

            tabSettings.TabPages.AddRange(new[] { tabPresets, tabVideo, tabAudio, tabQueue });
            panel.Controls.Add(tabSettings);
            splitContainerMain.Panel2.Controls.Add(panel);
        }

        private void BuildQueueTab()
        {
            var container = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            tabQueue.Controls.Clear();
            tabQueue.Controls.Add(container);
            InitializeQueueManagement(container);
        }

        private void InitializeQueueManagement(Control host)
        {
            // MVVM-based queue view: bind DataGridView to QueueItemsBinding (BindingList<QueueItemViewModel>)

            // Ensure binding source exists and is hooked up to the current binding list from IMainView
            _queueBindingSource ??= new BindingSource();

            // Если QueueItemsBinding уже установлен через IMainView/MainPresenter,
            // используем его как единый источник правды. Иначе создаём пустой список,
            // который впоследствии будет заполнен презентером.
            if (_queueItemsBinding == null)
            {
                _queueItemsBinding = new System.ComponentModel.BindingList<Converter.Application.ViewModels.QueueItemViewModel>();
            }

            var bindingList = _queueItemsBinding;
            _queueBindingSource.DataSource = bindingList;

            _queueGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true, // Allow selecting multiple rows
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                VirtualMode = false,
                AllowUserToOrderColumns = false,
                RowHeadersVisible = false,
                GridColor = Color.FromArgb(230, 230, 235),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            };

            // Handle selection changes to update IsSelected property
            _queueGrid.SelectionChanged += (sender, e) =>
            {
                if (_queueBindingSource?.DataSource is System.ComponentModel.BindingList<Converter.Application.ViewModels.QueueItemViewModel> bindingList)
                {
                    // Get currently selected rows
                    var selectedRows = _queueGrid.SelectedRows
                        .Cast<DataGridViewRow>()
                        .Where(row => row.DataBoundItem != null)
                        .Select(row => row.DataBoundItem as Converter.Application.ViewModels.QueueItemViewModel)
                        .Where(vm => vm != null)
                        .ToHashSet();

                    // Update IsSelected for all items based on grid selection
                    foreach (var item in bindingList)
                    {
                        var shouldBeSelected = selectedRows.Contains(item);
                        if (item.IsSelected != shouldBeSelected)
                        {
                            item.IsSelected = shouldBeSelected;
                        }
                    }
                }
            };

            _queueGrid.DataSource = _queueBindingSource;

            // Columns mapped to QueueItemViewModel
            _queueGrid.Columns.Clear();
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Converter.Application.ViewModels.QueueItemViewModel.FileName),
                HeaderText = "Файл",
                FillWeight = 40
            });
            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Converter.Application.ViewModels.QueueItemViewModel.Status),
                HeaderText = "Статус",
                FillWeight = 20
            });

            // Progress column with custom formatting
            var progressColumn = new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Converter.Application.ViewModels.QueueItemViewModel.Progress),
                HeaderText = "%",
                FillWeight = 10
            };
            _queueGrid.Columns.Add(progressColumn);

            _queueGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Converter.Application.ViewModels.QueueItemViewModel.ErrorMessage),
                HeaderText = "Ошибка",
                FillWeight = 30
            });

            // Force refresh when data changes
            if (bindingList is System.ComponentModel.BindingList<Converter.Application.ViewModels.QueueItemViewModel> bl)
            {
                bl.ListChanged += (s, e) =>
                {
                    if (e.ListChangedType == System.ComponentModel.ListChangedType.ItemChanged)
                    {
                        // Refresh the grid when individual items change
                        _queueGrid?.Refresh();
                    }
                };
            }

            // Layout: simple panel with padding, grid fills all available space inside tab
            host.Controls.Add(_queueGrid);
        }

        private async Task BuildPresetsTabAsync()
        {
            // Простейшая реализация: просто набор кнопок пресетов в FlowLayoutPanel
            tabPresets.Controls.Clear();

            // Вложенный TabControl с субвкладками по основным категориям
            var subTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F)
            };

            tabPresets.Controls.Add(subTabs);

            // Получаем пресеты через асинхронный интерфейс IPresetService
            var presets = await _presetService.GetAllPresetsAsync();
            AppendLog($"Загружено пресетов из сервиса: {presets.Count}");

            if (presets.Count == 0)
            {
                var emptyPage = new TabPage("Все")
                {
                    BackColor = Color.White
                };

                emptyPage.Controls.Add(new Label
                {
                    Text = "Пресеты не найдены",
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                    Padding = new Padding(10),
                    Margin = new Padding(10),
                    Dock = DockStyle.Top
                });

                subTabs.TabPages.Add(emptyPage);
                return;
            }

            // Готовим группы по категориям
            var groups = presets
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Прочее" : p.Category)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Name).ToList());

            // Основные категории, для которых нужны отдельные субвкладки
            var mainCategories = new[] { "Compression", "Social Media", "Video Platforms" };

            foreach (var cat in mainCategories)
            {
                if (groups.TryGetValue(cat, out var catPresets) && catPresets.Count > 0)
                {
                    CreatePresetCategoryTab(subTabs, cat, catPresets);
                }
            }

            // Остальные категории выводим в отдельной вкладке "Other", если они есть
            var otherPresets = presets
                .Where(p =>
                {
                    var c = string.IsNullOrWhiteSpace(p.Category) ? "Прочее" : p.Category;
                    return !mainCategories.Contains(c);
                })
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToList();

            if (otherPresets.Count > 0)
            {
                CreatePresetCategoryTab(subTabs, "Other", otherPresets);
            }

            if (subTabs.TabPages.Count == 0)
            {
                // На всякий случай, если фильтрация не создала ни одной вкладки
                CreatePresetCategoryTab(subTabs, "Все", presets.OrderBy(p => p.Name).ToList());
            }

            // Локальная функция для создания субвкладки категории с адаптивным одноколоночным списком кнопок
            void CreatePresetCategoryTab(TabControl parent, string title, IList<ConversionProfile> categoryPresets)
            {
                var page = new TabPage(title)
                {
                    BackColor = Color.White,
                    Padding = new Padding(10) // Используем отступы на самой вкладке
                };

                var flow = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false, // Важно для одноколоночного режима
                    BackColor = Color.White
                };

                // Этот обработчик растягивает кнопки на всю ширину FlowLayoutPanel,
                // что является обходным решением, т.к. Anchor не работает в FlowLayoutPanel.
                flow.SizeChanged += (_, __) =>
                {
                    // Используем ClientSize, чтобы учесть ширину возможного скроллбара
                    var targetWidth = flow.ClientSize.Width - flow.Padding.Horizontal;
                    foreach (var ctrl in flow.Controls.OfType<Button>())
                    {
                        ctrl.Width = targetWidth;
                    }
                };

                foreach (var preset in categoryPresets)
                {
                    var btn = new Button
                    {
                        AutoSize = false, // Обязательно для ручной установки размеров
                        Height = 40,
                        Margin = new Padding(0, 0, 0, 5), // Отступ только снизу
                        Padding = new Padding(10, 5, 10, 5),
                        TextAlign = ContentAlignment.MiddleLeft,
                        Text = $"{preset.Icon} {preset.Name}",
                        Tag = preset,
                        FlatStyle = FlatStyle.System
                        // Anchor здесь не работает и был убран для ясности
                    };

                    btn.Click += (_, __) =>
                    {
                        try
                        {
                            ApplyPresetToUi(preset);
                            AppendLog($"Выбран пресет: {preset.Name}");
                            DebounceEstimate();
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"Ошибка применения пресета: {ex.Message}");
                        }
                    };

                    flow.Controls.Add(btn);
                }

                // Добавляем FlowLayoutPanel напрямую на вкладку, убрав лишнюю панель-обертку
                page.Controls.Add(flow);
                parent.TabPages.Add(page);
            }
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
            cbNamingPattern.SelectedIndexChanged += (s, e) =>
            {
                NamingPattern = cbNamingPattern.SelectedItem?.ToString();
                DebounceEstimate();
            };
            panel.Controls.Add(cbNamingPattern);

            tabVideo.Controls.Add(panel);
        }

        private void BuildAudioTab()
        {
            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 1,
                RowCount = 2
            };
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var settingsPanel = new Panel { Dock = DockStyle.Top, Padding = new Padding(15), AutoSize = true };

            int y = 10;

            chkEnableAudio = new CheckBox
            {
                Left = 10,
                Top = y,
                Width = 220,
                Text = "Включить аудиодорожку",
                Checked = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            chkEnableAudio.CheckedChanged += (s, e) =>
            {
                var enabled = chkEnableAudio.Checked;
                cbAudioCodec.Enabled = enabled;
                cbAudioBitrate.Enabled = enabled;
                if (_audioProcessingPanel != null)
                {
                    _audioProcessingPanel.Enabled = enabled;
                }
            };
            settingsPanel.Controls.Add(chkEnableAudio);
            y += 50;

            settingsPanel.Controls.Add(CreateLabel("Аудио-кодек:", 10, y));
            cbAudioCodec = CreateComboBox(140, y, 180);
            settingsPanel.Controls.Add(cbAudioCodec);
            y += 40;

            settingsPanel.Controls.Add(CreateLabel("Битрейт:", 10, y));
            cbAudioBitrate = CreateComboBox(140, y, 180);
            cbAudioBitrate.Items.AddRange(new object[] { "96k", "128k", "160k", "192k", "256k", "320k" });
            settingsPanel.Controls.Add(cbAudioBitrate);

            _audioProcessingPanel = new AudioProcessingPanel
            {
                Dock = DockStyle.Fill
            };
            _audioProcessingPanel.Enabled = chkEnableAudio.Checked;

            container.Controls.Add(settingsPanel, 0, 0);
            container.Controls.Add(_audioProcessingPanel, 0, 1);

            tabAudio.Controls.Add(container);
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

            // Estimate panel (above buttons)
            _estimatePanel = new UI.Controls.EstimatePanel(_themeService)
            {
                Left = 0,
                Top = 95,
                Width = panelTop.Width - 20,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _estimatePanel.ShowPerformanceBar = true;
            panelTop.Controls.Add(_estimatePanel);

            // Buttons
            btnStart = CreateStyledButton("▶ Начать конвертацию", 0);
            btnStart.Top = _estimatePanel.Bottom + 10;
            btnStart.Width = 170;
            btnStart.Height = 35;
            btnStart.BackColor = Color.FromArgb(0, 120, 215);
            btnStart.ForeColor = Color.White;
            btnStart.FlatAppearance.BorderSize = 0;
            // IMainView: запуск конвертации
            btnStart.Click += async (s, e) =>
            {
                try
                {
                    await RaiseStartConversionRequestedAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при запуске конвертации: {ex.Message}");
                }
            };

            btnStop = CreateStyledButton("⏹ Остановить", 180);
            btnStop.Top = _estimatePanel.Bottom + 10;
            btnStop.Width = 120;
            btnStop.Height = 35;
            btnStop.BackColor = Color.FromArgb(180, 50, 50);
            btnStop.ForeColor = Color.White;
            btnStop.Enabled = false;
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 60, 60);
            btnStop.FlatAppearance.MouseDownBackColor = Color.FromArgb(160, 40, 40);
            // IMainView: отмена/остановка
            btnStop.Click += async (s, e) =>
            {
                try
                {
                    await RaiseCancelConversionRequestedAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при остановке конвертации: {ex.Message}");
                }
            };

            _btnNotificationSettings = CreateStyledButton("🔔 Уведомления", 310);
            _btnNotificationSettings.Top = _estimatePanel.Bottom + 10;
            _btnNotificationSettings.Width = 160;
            _btnNotificationSettings.Height = 35;
            _btnNotificationSettings.Click += BtnNotificationSettings_Click;

            btnSavePreset = CreateStyledButton("💾 Сохранить пресет", 890);
            btnSavePreset.Top = _estimatePanel.Bottom + 10;
            btnSavePreset.Width = 180;
            btnSavePreset.Height = 35;
            btnSavePreset.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSavePreset.Click += btnSavePreset_Click;

            btnLoadPreset = CreateStyledButton("📂 Загрузить пресет", 1080);
            btnLoadPreset.Top = _estimatePanel.Bottom + 10;
            btnLoadPreset.Width = 180;
            btnLoadPreset.Height = 35;
            btnLoadPreset.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLoadPreset.Click += btnLoadPreset_Click;

            panelTop.Controls.AddRange(new Control[] {
                lblStatusTotal, progressBarTotal, lblStatusCurrent, progressBarCurrent,
                btnStart, btnStop, _btnNotificationSettings!, btnSavePreset, btnLoadPreset
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

            // Resize handler to keep estimate panel width
            panelTop.Resize += (s, e) =>
            {
                if (_estimatePanel != null)
                {
                    _estimatePanel.Width = panelTop.Width - 20;
                    // Reposition buttons below estimate panel on resize
                    btnStart.Top = _estimatePanel.Bottom + 10;
                    btnStop.Top = _estimatePanel.Bottom + 10;
                    if (_btnNotificationSettings != null)
                    {
                        _btnNotificationSettings.Top = _estimatePanel.Bottom + 10;
                    }
                    btnSavePreset.Top = _estimatePanel.Bottom + 10;
                    btnLoadPreset.Top = _estimatePanel.Bottom + 10;
                }
            };
            InitEstimateDebounce();
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

            WireEstimateTriggers();
        }

        public void ApplyPresetToUi(PresetProfile preset)
        {
            // Format
            if (!string.IsNullOrWhiteSpace(preset.Format))
            {
                var fmt = preset.Format!.ToUpperInvariant();
                var idx = -1;
                for (int i = 0; i < cbFormat.Items.Count; i++)
                {
                    if (string.Equals(cbFormat.Items[i]?.ToString(), fmt, StringComparison.OrdinalIgnoreCase))
                    { idx = i; break; }
                }
                if (idx >= 0) cbFormat.SelectedIndex = idx; else { cbFormat.Items.Add(fmt); cbFormat.SelectedItem = fmt; }
            }

            // Refresh codec lists by format
            cbFormat_SelectedIndexChanged(null!, EventArgs.Empty);

            // Video codec
            if (!string.IsNullOrWhiteSpace(preset.VideoCodec))
            {
                for (int i = 0; i < cbVideoCodec.Items.Count; i++)
                {
                    var text = cbVideoCodec.Items[i]?.ToString() ?? string.Empty;
                    if (text.StartsWith(preset.VideoCodec!, StringComparison.OrdinalIgnoreCase))
                    { cbVideoCodec.SelectedIndex = i; break; }
                }
            }

            // Video Bitrate (if specified and not using CRF)
            if (preset.Bitrate.HasValue && !preset.CRF.HasValue)
            {
                // Try to find matching bitrate in quality dropdown
                for (int i = 0; i < cbQuality.Items.Count; i++)
                {
                    var text = cbQuality.Items[i]?.ToString() ?? string.Empty;
                    if (text.Contains($"{preset.Bitrate.Value}k") || text.Contains($"{preset.Bitrate.Value / 1000}M"))
                    { cbQuality.SelectedIndex = i; break; }
                }
            }

            // Quality (CRF)
            if (preset.CRF.HasValue)
            {
                for (int i = 0; i < cbQuality.Items.Count; i++)
                {
                    var text = cbQuality.Items[i]?.ToString() ?? string.Empty;
                    if (text.Contains($"CRF {preset.CRF.Value}"))
                    { cbQuality.SelectedIndex = i; break; }
                }
            }

            // Audio
            chkEnableAudio.Checked = preset.IncludeAudio;
            if (preset.IncludeAudio && !string.IsNullOrWhiteSpace(preset.AudioCodec))
            {
                for (int i = 0; i < cbAudioCodec.Items.Count; i++)
                {
                    var text = cbAudioCodec.Items[i]?.ToString() ?? string.Empty;
                    if (text.StartsWith(preset.AudioCodec!, StringComparison.OrdinalIgnoreCase))
                    { cbAudioCodec.SelectedIndex = i; break; }
                }
            }
            if (preset.IncludeAudio && preset.AudioBitrate.HasValue)
            {
                var target = preset.AudioBitrate.Value + "k";
                for (int i = 0; i < cbAudioBitrate.Items.Count; i++)
                {
                    var text = cbAudioBitrate.Items[i]?.ToString() ?? string.Empty;
                    if (string.Equals(text, target, StringComparison.OrdinalIgnoreCase))
                    { cbAudioBitrate.SelectedIndex = i; break; }
                }
            }

            // Resolution
            if (preset.Width.HasValue || preset.Height.HasValue)
            {
                rbUsePreset.Checked = true;
                var map = new Dictionary<int, string> { { 360, "360p" }, { 480, "480p" }, { 576, "576p" }, { 720, "720p" }, { 1080, "1080p" }, { 1440, "1440p" }, { 2160, "2160p (4K)" } };
                if (preset.Height.HasValue && map.TryGetValue(preset.Height.Value, out var label))
                {
                    for (int i = 0; i < cbPreset.Items.Count; i++)
                    {
                        if (string.Equals(cbPreset.Items[i]?.ToString(), label, StringComparison.Ordinal))
                        { cbPreset.SelectedIndex = i; break; }
                    }
                }
            }
            else
            {
                rbUsePercent.Checked = true;
                nudPercent.Value = 70;
            }
            DebounceEstimate();
        }

        private PresetProfile BuildPresetFromUi()
        {
            var fmt = cbFormat.SelectedItem?.ToString() ?? "mp4";
            var vcodec = ExtractCodecName(cbVideoCodec.SelectedItem?.ToString() ?? "libx264");
            var acodec = cbAudioCodec.SelectedItem?.ToString() ?? "aac";
            int? abitrate = null;
            if (chkEnableAudio.Checked && cbAudioBitrate.SelectedItem != null)
            {
                var s = cbAudioBitrate.SelectedItem.ToString() ?? "";
                if (s.EndsWith("k", StringComparison.OrdinalIgnoreCase)) s = s[..^1];
                if (int.TryParse(s, out var parsed))
                {
                    abitrate = parsed;
                }
            }

            var crf = ExtractCRF(cbQuality.SelectedItem?.ToString() ?? "CRF 23");
            var threads = nudThreads != null && nudThreads.Value > 0 ? (int?)nudThreads.Value : null;

            return new PresetProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Custom Preset",
                Category = "Custom",
                Icon = "⭐",
                Description = "Пользовательский пресет",
                Width = null,
                Height = null,
                VideoCodec = vcodec,
                Bitrate = null, // use CRF primarily; bitrate could be derived later
                CRF = crf,
                Format = fmt.ToLowerInvariant(),
                IncludeAudio = chkEnableAudio.Checked,
                AudioCodec = chkEnableAudio.Checked ? acodec : null,
                AudioBitrate = abitrate,
                ColorHex = "#4C7CF3"
            };
        }

        private void Form1_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private async void Form1_DragDrop(object? sender, DragEventArgs e)
        {
            await HandleDragDropAsync(e);
        }

        private async Task HandleDragDropAsync(DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
            {
                await AddFilesAndUpdateQueueAsync(files);
            }
        }

        private void FilesPanel_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private async void FilesPanel_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
            {
                await AddFilesAndUpdateQueueAsync(files);
            }
        }

        private async Task AddFilesAndUpdateQueueAsync(string[] files)
        {
            if (files == null || files.Length == 0) return;

            try
            {
                // Add files to the UI
                AddFilesToList(files);

                // Notify presenter about new files
                await RaiseFilesDroppedAsync(files).ConfigureAwait(false);

                // Update estimation
                DebounceEstimate();

                // Update UI state
                UpdateShareButtonState();
                UpdateEditorButtonState();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling drag and drop");
                ShowError($"Ошибка при добавлении файлов: {ex.Message}");
            }
        }

        private void btnAddFiles_Click(object? sender, EventArgs e)
        {
            _ = HandleAddFilesClickAsync();
        }

        private async Task HandleAddFilesClickAsync()
        {
            try
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "Видео файлы|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.mpg;*.mpeg|Все файлы|*.*",
                    Multiselect = true,
                    Title = "Выберите файлы для добавления"
                };

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    var filePaths = ofd.FileNames;
                    if (filePaths.Length > 0)
                    {
                        // Отключаем кнопку на время добавления файлов
                        btnAddFiles.Enabled = false;
                        try
                        {
                            // 1) Добавляем элементы на панель файлов (старый UI)
                            AddFilesToList(filePaths);

                            // 2) Сообщаем презентеру через FilesDroppedAsync, чтобы добавить в очередь
                            await RaiseFilesDroppedAsync(filePaths).ConfigureAwait(false);

                            // 3) Обновляем оценку
                            DebounceEstimate();
                        }
                        finally
                        {
                            btnAddFiles.Enabled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при добавлении файлов: {ex.Message}");
                _logger?.LogError(ex, "Ошибка в обработчике btnAddFiles_Click");
            }
        }

        private async Task AddFilesToList(string[] paths)
        {
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                    continue;

                // Check if file is already added
                if (filesPanel.Controls.OfType<FileListItem>().Any(item =>
                    string.Equals(item.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var fileItem = new FileListItem(path, _themeService)
                {
                    Width = filesPanel.Width - 24, // Account for padding and scrollbar
                    Margin = new Padding(0, 0, 0, 8)
                };

                // Use the new handler methods
                fileItem.RemoveClicked += OnFileItemRemoveClicked;
                fileItem.SelectionChanged += OnFileItemSelectionChanged;

                filesPanel.Controls.Add(fileItem);

                // Load thumbnail asynchronously
                _ = LoadThumbnailForFileItemAsync(fileItem, path);
            }

            // Update UI
            UpdateShareButtonState();
            UpdateEditorButtonState();
        }

        private void ClearAllFiles()
        {
            // Очищаем панель файлов
            foreach (Control control in filesPanel.Controls.OfType<FileListItem>().ToList())
            {
                control.Dispose();
            }
            filesPanel.Controls.Clear();
        }

        private async void RemoveFileFromList(FileListItem fileItem)
        {
            if (fileItem == null) return;

            try
            {
                // Get the file path before removing
                string filePath = fileItem.FilePath;
                if (string.IsNullOrEmpty(filePath)) return;

                // Remove event handlers
                fileItem.SelectionChanged -= OnFileItemSelectionChanged;
                fileItem.RemoveClicked -= OnFileItemRemoveClicked;

                // Remove from panel
                filesPanel.Controls.Remove(fileItem);

                // Notify presenter to remove from queue
                if (_mainPresenter != null)
                {
                    // Pass fromView: true to prevent circular reference
                    await _mainPresenter.RemoveFileFromQueue(filePath, fromView: true);
                }

                // Clean up resources
                fileItem.Dispose();

                // Update UI
                UpdateShareButtonState();
                UpdateEditorButtonState();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error removing file from list");
                ShowError($"Ошибка при удалении файла: {ex.Message}");
            }
        }

        private void OnFileItemRemoveClicked(object? sender, EventArgs e)
        {
            if (sender is FileListItem fileItem)
            {
                RemoveFileFromList(fileItem);
            }
        }

        private void OnFileItemSelectionChanged(object? sender, EventArgs e)
        {
            UpdateShareButtonState();
        }

        private void OpenVideoInPlayer(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    ShowError("Файл для просмотра не найден.");
                    return;
                }

                using var editorForm = new VideoEditorForm(filePath);
                editorForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ShowError($"Не удалось открыть видео: {ex.Message}");
            }
        }

        private Converter.Domain.Models.ConversionSettings CreateConversionSettings()
        {
            var format = (cbFormat.SelectedItem?.ToString() ?? "MP4").ToLowerInvariant();
            var videoCodec = ExtractCodecName(cbVideoCodec.SelectedItem?.ToString() ?? "libx264");
            var audioCodec = ExtractCodecName(cbAudioCodec.SelectedItem?.ToString() ?? "aac");
            int? audioBitrate = null;
            if (chkEnableAudio.Checked && cbAudioBitrate.SelectedItem != null)
            {
                var bitrateText = cbAudioBitrate.SelectedItem.ToString() ?? "";
                if (bitrateText.EndsWith("k", StringComparison.OrdinalIgnoreCase)) bitrateText = bitrateText[..^1];
                if (int.TryParse(bitrateText, out var parsed))
                {
                    audioBitrate = parsed;
                }
            }

            var crf = ExtractCRF(cbQuality.SelectedItem?.ToString() ?? "CRF 23");
            var threads = nudThreads != null && nudThreads.Value > 0 ? (int?)nudThreads.Value : null;

            return new Converter.Domain.Models.ConversionSettings
            {
                VideoCodec = videoCodec,
                AudioCodec = audioCodec,
                AudioBitrate = audioBitrate,
                PresetName = cbPreset.SelectedItem?.ToString(),
                ContainerFormat = format,
                Crf = crf,
                EnableAudio = chkEnableAudio.Checked,
                UseHardwareAcceleration = chkHardwareAccel?.Checked ?? false,
                Threads = threads,
                AudioProcessing = _audioProcessingPanel?.GetOptions().Clone()
            };
        }

        private static int? ParseBitrate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var sanitized = value.EndsWith("k", StringComparison.OrdinalIgnoreCase) ? value[..^1] : value;
            return int.TryParse(sanitized, out var parsed) ? parsed : null;
        }



        private void cbFormat_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var fmt = cbFormat.SelectedItem?.ToString() ?? "MP4";
            PopulateCodecsForFormat(fmt);
            DebounceEstimate();
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
            WireEstimateTriggers();
        }


        private void PromptShareResults(List<QueueItem> batchItems)
        {
            UpdateShareButtonState();

            var successfulItems = batchItems
                .Where(x => x.Status == ConversionStatus.Completed)
                .ToList();

            if (!successfulItems.Any())
            {
                return;
            }

            var savedBytes = successfulItems.Sum(x =>
                Math.Max(0, x.FileSizeBytes - (x.OutputFileSizeBytes ?? x.FileSizeBytes)));

            var message =
                $"Конвертация завершена!\n\n" +
                $"Файлов обработано: {successfulItems.Count}\n" +
                $"Сэкономлено места: {FormatFileSize(savedBytes)}\n\n" +
                "Хотите поделиться результатами?";

            if (MessageBox.Show(this, message, "Поздравляем!", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) == DialogResult.Yes)
            {
                ShowShareDialog(successfulItems);
            }
        }

        private void BtnNotificationSettings_Click(object? sender, EventArgs e)
        {
            var settingsCopy = new NotificationOptions
            {
                DesktopNotificationsEnabled = _notificationSettings.DesktopNotificationsEnabled,
                ShowProgressNotifications = _notificationSettings.ShowProgressNotifications,
                SoundEnabled = _notificationSettings.SoundEnabled,
                CustomSoundPath = _notificationSettings.CustomSoundPath
            };

            using var settingsForm = new NotificationSettingsForm(settingsCopy);
            if (settingsForm.ShowDialog(this) == DialogResult.OK)
            {
                _notificationSettings = settingsForm.Settings;
                _notificationService.UpdateSettings(_notificationSettings);
            }
        }

        private async Task SendCompletionNotificationAsync((int total, int ok, int failed, List<QueueItem> processedItems) result, DateTime startTime, CancellationToken cancellationToken = default)
        {
            if (_notificationService == null)
            {
                return;
            }

            var successfulItems = result.processedItems
                .Where(x => x.Status == ConversionStatus.Completed)
                .ToList();

            var summary = new NotificationSummary
            {
                SuccessCount = successfulItems.Count,
                FailedCount = result.failed,
                TotalSpaceSaved = CalculateSpaceSaved(successfulItems),
                TotalProcessingTime = DateTime.Now - startTime,
                Message = result.failed == 0
                    ? $"Успешно конвертировано: {successfulItems.Count} файлов"
                    : $"Успешно: {result.ok} из {result.total}. Ошибок: {result.failed}."
            };

            _notificationService.NotifyConversionComplete(summary);
        }

        private long CalculateSpaceSaved(IEnumerable<QueueItem> items)
        {
            long total = 0;
            foreach (var item in items)
            {
                var inputSize = item.FileSizeBytes;
                var outputSize = item.OutputFileSizeBytes ?? inputSize;
                if (outputSize < inputSize)
                {
                    total += inputSize - outputSize;
                }
            }

            return total;
        }

        private async Task<string?> GenerateNotificationThumbnailAsync(IEnumerable<QueueItem> items)
        {
            var videoPath = items
                .Select(i => i.OutputPath)
                .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));

            if (string.IsNullOrWhiteSpace(videoPath))
            {
                return null;
            }

            try
            {
                using var stream = await _thumbnailProvider.GetThumbnailAsync(videoPath!, 320, 180, CancellationToken.None);
                using var image = Image.FromStream(stream);

                var previewDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Converter",
                    "Notifications");
                Directory.CreateDirectory(previewDirectory);

                var previewPath = Path.Combine(previewDirectory, $"{Guid.NewGuid():N}.jpg");
                using (var bitmap = new Bitmap(image))
                {
                    bitmap.Save(previewPath, ImageFormat.Jpeg);
                }

                return previewPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Не удалось создать превью уведомления: {ex.Message}");
                return null;
            }
        }

        private void OnShareButtonClick(object? sender, EventArgs e)
        {
            _ = OnShareButtonClickAsync();
        }

        private async Task OnShareButtonClickAsync()
        {
            List<QueueItem> successfulItems = new List<QueueItem>();

            try
            {
                if (_mainPresenter != null)
                {
                    successfulItems = await _mainPresenter.GetCompletedItemsAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Не удалось получить результаты конвертации для шаринга: {ex.Message}");
                return;
            }

            if (!successfulItems.Any())
            {
                MessageBox.Show(this, "Еще нет завершённых конверсий для публикации.", "Нет данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateShareButtonState();
                return;
            }

            ShowShareDialog(successfulItems);
        }

        private void ShowShareDialog(List<QueueItem> completedItems)
        {
            var report = _shareService.GenerateReport(completedItems);
            if (report == null)
            {
                MessageBox.Show(this, "Не удалось собрать статистику для отчета.", "Нет данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var shareDialog = new ShareDialog(report);
            shareDialog.ShowDialog(this);
        }

        private void UpdateShareButtonState()
        {
            if (_btnShare == null)
            {
                return;
            }

            try
            {
                var hasCompleted = _queueItemsBinding != null &&
                                   _queueItemsBinding.Any(x => x.Status == ConversionStatus.Completed);
                _btnShare.Enabled = hasCompleted;
            }
            catch
            {
                _btnShare.Enabled = false;
            }
        }

        private void UpdateEditorButtonState()
        {
            if (_btnOpenEditor == null || filesPanel == null)
            {
                return;
            }

            _btnOpenEditor.Enabled = filesPanel.Controls.Count > 0;
        }

        private async Task ConvertFileAsync(string inputPath, string outputPath, string format, string vcodec,
            string acodec, string abitrate, int crf, CancellationToken cancellationToken, IProgress<double>? progressObserver = null)
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

                IMediaInfo? mediaInfo = null;
                string? scaleFilter = null;
                TimeSpan? estimatedDuration = null;
                try
                {
                    mediaInfo = await FFmpeg.GetMediaInfo(inputPath);
                    var v = mediaInfo.VideoStreams?.FirstOrDefault();

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

                        // Вычисляем оценку времени конвертации для отображения оставшегося времени
                        try
                        {
                            int? targetW = null;
                            int? targetH = null;
                            if (rbUsePreset.Checked)
                            {
                                targetH = PresetToHeight(cbPreset.SelectedItem?.ToString() ?? "720p");
                                if (targetH.HasValue && v.Height > 0)
                                {
                                    double scale = targetH.Value / (double)v.Height;
                                    targetW = Math.Max(2, (int)Math.Round(v.Width * scale));
                                    if (targetW % 2 != 0) targetW++;
                                    if (targetH.Value % 2 != 0) targetH++;
                                }
                            }
                            else if (rbUsePercent.Checked)
                            {
                                var pct = (int)nudPercent.Value;
                                if (v.Height > 0)
                                {
                                    targetH = Math.Max(2, (int)Math.Round(v.Height * pct / 100m));
                                    double scale = targetH.Value / (double)v.Height;
                                    targetW = Math.Max(2, (int)Math.Round(v.Width * scale));
                                    if (targetW % 2 != 0) targetW++;
                                    if (targetH.Value % 2 != 0) targetH++;
                                }
                            }

                            int estAudioKbps = 0;
                            bool estAudioCopy = false;
                            if (chkEnableAudio.Checked)
                            {
                                var selAudio = cbAudioCodec.SelectedItem?.ToString() ?? string.Empty;
                                if (selAudio.StartsWith("copy", StringComparison.OrdinalIgnoreCase))
                                {
                                    estAudioCopy = true;
                                }
                                else
                                {
                                    var s = cbAudioBitrate.SelectedItem?.ToString() ?? "128k";
                                    if (s.EndsWith("k", StringComparison.OrdinalIgnoreCase)) s = s[..^1];
                                    int.TryParse(s, out estAudioKbps);
                                    if (estAudioKbps == 0) estAudioKbps = 128;
                                }
                            }

                            var est = await _estimationService.EstimateConversion(
                                inputPath,
                                0,
                                targetW,
                                targetH,
                                vcodec,
                                chkEnableAudio.Checked,
                                estAudioKbps,
                                crf,
                                estAudioCopy,
                                cancellationToken);
                            estimatedDuration = est.EstimatedDuration;
                        }
                        catch
                        {
                            // Если не удалось вычислить оценку, используем null
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"⚠ Предупреждение при анализе: {ex.Message}");
                }

                var conv = FFmpeg.Conversions.New();
                var finalEstimatedDuration = estimatedDuration; // Захватываем для использования в замыкании

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
                        progressObserver?.Report(percent);

                        // Вычисляем оставшееся время на основе оценки
                        string timeDisplay;
                        if (finalEstimatedDuration.HasValue && percent > 0 && percent < 100)
                        {
                            var elapsed = finalEstimatedDuration.Value.TotalSeconds * (percent / 100.0);
                            var remaining = finalEstimatedDuration.Value.TotalSeconds - elapsed;
                            var remainingTimeSpan = TimeSpan.FromSeconds(Math.Max(0, remaining));
                            timeDisplay = remainingTimeSpan.TotalHours >= 1
                                ? $"{(int)remainingTimeSpan.TotalHours} ч {remainingTimeSpan.Minutes} мин"
                                : remainingTimeSpan.TotalMinutes >= 1
                                    ? $"{(int)remainingTimeSpan.TotalMinutes} мин {remainingTimeSpan.Seconds} сек"
                                    : $"{remainingTimeSpan.Seconds} сек";
                        }
                        else
                        {
                            // Если оценка недоступна, показываем исходную длительность
                            timeDisplay = args.TotalLength.ToString(@"hh\:mm\:ss");
                        }

                        lblStatusCurrent.Text = $"{fileName}: {percent:F1}% | {timeDisplay}";
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
                AudioProcessingOptions? audioProcessingOptions = null;
                string? audioFilterString = null;
                bool audioFiltersActive = false;
                if (!isGif && chkEnableAudio.Checked && _audioProcessingPanel != null)
                {
                    var snapshot = _audioProcessingPanel.GetOptions().Clone();
                    snapshot.TotalDuration = mediaInfo?.Duration.TotalSeconds ?? 0;
                    var builtFilters = AudioProcessingService.BuildAudioFilterString(snapshot);
                    if (!string.IsNullOrWhiteSpace(builtFilters))
                    {
                        audioProcessingOptions = snapshot;
                        audioFilterString = builtFilters;
                        audioFiltersActive = true;
                    }
                }

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
                    if (audioCopy && audioFiltersActive)
                    {
                        audioCopy = false;
                    }

                    if (audioCopy)
                    {
                        conv.AddParameter("-c:a copy");
                    }
                    else
                    {
                        conv.AddParameter($"-c:a {acodec} -b:a {abitrate}");
                    }

                    if (audioFiltersActive && audioProcessingOptions != null)
                    {
                        AudioProcessingService.ApplyAudioProcessing(conv, audioProcessingOptions, audioFilterString);
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

        private string GetSelectedPresetLabel()
        {
            if (rbUsePreset?.Checked == true)
            {
                return cbPreset?.SelectedItem?.ToString() ?? "Пресет";
            }

            if (rbUsePercent?.Checked == true)
            {
                return $"{nudPercent.Value}% масштаб";
            }

            return "Оригинал";
        }

        private void btnSavePreset_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "JSON Preset|*.json|Все файлы|*.*",
                Title = "Сохранить пресет",
                DefaultExt = "json",
                AddExtension = true
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var preset = BuildPresetFromUi();
                    _presetService.SavePresetToFile(preset, sfd.FileName);
                    AppendLog($"💾 Пресет сохранен: {System.IO.Path.GetFileName(sfd.FileName)}");
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка сохранения пресета: {ex.Message}");
                }
            }
        }

        private void btnLoadPreset_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "JSON Preset|*.json|Все файлы|*.*",
                Title = "Загрузить пресет"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var preset = _presetService.LoadPresetFromFile(ofd.FileName);
                    ApplyPresetToUi(preset);
                    AppendLog($"📂 Пресет загружен: {System.IO.Path.GetFileName(ofd.FileName)}");
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка загрузки пресета: {ex.Message}");
                }
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
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private class FileConversionInfo
        {
            public string FilePath { get; set; } = "";
            public TimeSpan Duration { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        // Debounce functionality for estimation updates
        private System.Windows.Forms.Timer _estimateDebounceTimer = new System.Windows.Forms.Timer();
        private bool _estimatePending = false;

        private void InitEstimateDebounce()
        {
            _estimateDebounceTimer.Interval = 500; // 500ms debounce
            _estimateDebounceTimer.Tick += EstimateDebounceTimer_Tick;
            _estimateDebounceTimer.Stop();
        }

        private void EstimateDebounceTimer_Tick(object? sender, EventArgs e)
        {
            _estimateDebounceTimer.Stop();
            _estimatePending = false;
            _ = UpdateEstimateAsync();
        }

        private void DebounceEstimate()
        {
            // Всегда выполняем логику дебаунса на UI-потоке, чтобы избежать
            // кросс-поточных обращений к WinForms Timer и контролам.
            if (this.InvokeRequired)
            {
                try
                {
                    this.BeginInvoke(new Action(DebounceEstimate));
                }
                catch
                {
                    // Если форма уже уничтожена - просто игнорируем
                }
                return;
            }

            if (_estimatePending)
            {
                _estimateDebounceTimer.Stop();
            }

            _estimatePending = true;
            _estimateDebounceTimer.Start();
        }

        private async Task UpdateEstimateAsync()
        {
            if (_estimatePanel == null || _mainPresenter == null)
                return;

            var files = GetCurrentFiles();
            if (files.Length == 0)
            {
                _estimatePanel.ShowCalculating();
                return;
            }

            try
            {
                // Собираем настройки из UI
                var settings = CreateConversionSettings();
                var format = (settings.ContainerFormat ?? "mp4").ToLowerInvariant();
                var videoCodec = settings.VideoCodec ?? "libx264";
                var audioCodec = settings.AudioCodec ?? "aac";
                var audioBitrate = settings.AudioBitrate ?? 128;
                var crf = settings.Crf ?? 23;

                int? targetWidth = null;
                int? targetHeight = null;

                if (rbUsePreset.Checked && cbPreset.SelectedItem is string preset)
                {
                    targetHeight = PresetToHeight(preset);
                }
                else if (rbUsePercent.Checked)
                {
                    targetHeight = null; // Будет рассчитано как процент
                }

                // Вызываем презентер для оценки
                var estimate = await _mainPresenter.EstimateConversionAsync(
                    files,
                    0, // Автоматический расчет битрейта
                    targetWidth,
                    targetHeight,
                    videoCodec,
                    chkEnableAudio.Checked,
                    audioBitrate,
                    crf,
                    false); // audioCopy

                if (files.Length > 0)
                {
                    _estimatePanel.UpdateEstimate(estimate);
                }
                else
                {
                    _estimatePanel.ShowCalculating();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка обновления оценки");
                _estimatePanel.ShowCalculating();
            }
        }

        private string[] GetCurrentFiles()
        {
            if (filesPanel != null)
            {
                return filesPanel.Controls
                    .OfType<FileListItem>()
                    .Select(item => item.FilePath)
                    .ToArray();
            }
            return Array.Empty<string>();
        }

        private void WireEstimateTriggers()
        {
            // Wire up all the controls that should trigger estimate updates
            var controls = new Control[]
            {
                cbFormat, cbVideoCodec, cbQuality, cbPreset, nudPercent,
                chkEnableAudio, cbAudioCodec, cbAudioBitrate,
                txtOutputFolder, chkCreateConvertedFolder, cbNamingPattern,
                rbUsePreset, rbUsePercent
            };

            foreach (var control in controls)
            {
                if (control is ComboBox comboBox)
                    comboBox.SelectedIndexChanged += (s, e) => DebounceEstimate();
                else if (control is NumericUpDown numericUpDown)
                    numericUpDown.ValueChanged += (s, e) => DebounceEstimate();
                else if (control is CheckBox checkBox)
                    checkBox.CheckedChanged += (s, e) => DebounceEstimate();
                else if (control is RadioButton radioButton)
                    radioButton.CheckedChanged += (s, e) => DebounceEstimate();
                else if (control is TextBox textBox)
                    textBox.TextChanged += (s, e) => DebounceEstimate();
            }
        }
    }
}
