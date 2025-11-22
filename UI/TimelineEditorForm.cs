using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Models;
using Converter.Domain.Models;
using Converter.Services;
using Xabe.FFmpeg;

namespace Converter.UI;

public class TimelineEditorForm : Form
{
    private readonly string _inputPath;
    private readonly List<TimelineSegment> _segments;
    private SegmentEditMode _editMode;
    private bool _lossless;

    private IMediaInfo? _mediaInfo;
    private TimeSpan _duration;

    private readonly VideoPlayerPanel videoPlayer;

    private readonly TableLayoutPanel rootLayout;

    private readonly Panel panelTimelineBar;
    private readonly Panel panelMarkers;

    private readonly Label lblMarkerA;
    private readonly Label lblMarkerB;
    private readonly Button btnSetMarkerA;
    private readonly Button btnSetMarkerB;
    private readonly Button btnSwapAB;
    private readonly Button btnAddSegmentFromAB;

    private readonly SplitContainer splitBottom;

    private readonly GroupBox gbSegments;
    private readonly DataGridView dgvSegments;
    private readonly BindingSource bsSegments;
    private readonly Panel panelSegmentButtons;
    private readonly Button btnSegmentAddFromAB;
    private readonly Button btnSegmentRemove;
    private readonly Button btnSegmentUp;
    private readonly Button btnSegmentDown;
    private readonly Button btnSegmentZoom;

    private readonly GroupBox gbOptions;
    private readonly GroupBox gbEditMode;
    private readonly RadioButton rbModeKeepOnly;
    private readonly RadioButton rbModeRemove;

    private readonly GroupBox gbOutputMode;
    private readonly CheckBox cbLossless;
    private readonly CheckBox cbSplitIntoFiles;
    private readonly CheckBox cbJoinAfterEdit;
    private readonly Label lblLosslessWarning;

    private readonly Panel panelBottom;
    private readonly Label lblSummary;
    private readonly Button btnReset;
    private readonly Button btnPreview;
    private readonly Button btnOk;
    private readonly Button btnCancel;

    private TimeSpan? _markerA;
    private TimeSpan? _markerB;

    public IReadOnlyList<TimelineSegment> ResultSegments => _segments;
    public SegmentEditMode ResultEditMode => _editMode;
    public bool ResultLossless => _lossless;

    public TimelineEditorForm(
        string inputPath,
        IEnumerable<TimelineSegment> segments,
        SegmentEditMode editMode,
        bool lossless)
    {
        _inputPath = inputPath ?? throw new ArgumentNullException(nameof(inputPath));
        _segments = segments?.Select(CloneSegment).ToList() ?? new List<TimelineSegment>();
        _editMode = editMode;
        _lossless = lossless;

        Text = "Редактор таймлайна";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1200, 800);

        // Row0: preview + controls (use existing VideoPlayerPanel)
        videoPlayer = new VideoPlayerPanel
        {
            Dock = DockStyle.Fill,
            Height = 260,
            BackColor = Color.Black
        };

        // Row1: timeline + markers
        panelTimelineBar = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 45,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        panelTimelineBar.Paint += PanelTimelineBar_Paint;
        panelTimelineBar.MouseDown += PanelTimelineBar_MouseDown;

        panelMarkers = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 35
        };

        lblMarkerA = new Label { Text = "A: —", AutoSize = true, Location = new Point(10, 10) };
        lblMarkerB = new Label { Text = "B: —", AutoSize = true, Location = new Point(120, 10) };
        btnSetMarkerA = new Button { Text = "Установить A", Width = 120, Location = new Point(230, 6) };
        btnSetMarkerB = new Button { Text = "Установить B", Width = 120, Location = new Point(360, 6) };
        btnSwapAB = new Button { Text = "Поменять A/B", Width = 130, Location = new Point(490, 6) };
        btnAddSegmentFromAB = new Button { Text = "Добавить сегмент A–B", Width = 180, Location = new Point(630, 6) };

        btnSetMarkerA.Click += (_, _) => SetMarkerA(GetCurrentTime());
        btnSetMarkerB.Click += (_, _) => SetMarkerB(GetCurrentTime());
        btnSwapAB.Click += (_, _) => SwapAB();
        btnAddSegmentFromAB.Click += BtnAddSegmentFromAB_Click;

        panelMarkers.Controls.AddRange(new Control[]
        {
            lblMarkerA, lblMarkerB, btnSetMarkerA, btnSetMarkerB, btnSwapAB, btnAddSegmentFromAB
        });

        var panelTimeline = new Panel { Dock = DockStyle.Fill, Height = 80 };
        panelTimeline.Controls.Add(panelTimelineBar);
        panelTimeline.Controls.Add(panelMarkers);

        // Row2: split container (segments left, options right)
        splitBottom = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 650
        };

        // Left: segments
        gbSegments = new GroupBox { Text = "Сегменты", Dock = DockStyle.Fill };
        dgvSegments = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false };
        bsSegments = new BindingSource();

        panelSegmentButtons = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        btnSegmentAddFromAB = new Button { Text = "+ A–B", Width = 80, Left = 5, Top = 6 };
        btnSegmentRemove = new Button { Text = "Удалить", Width = 80, Left = 90, Top = 6 };
        btnSegmentUp = new Button { Text = "↑", Width = 40, Left = 175, Top = 6 };
        btnSegmentDown = new Button { Text = "↓", Width = 40, Left = 220, Top = 6 };
        btnSegmentZoom = new Button { Text = "Показать", Width = 90, Left = 265, Top = 6 };

        btnSegmentAddFromAB.Click += BtnAddSegmentFromAB_Click;
        btnSegmentRemove.Click += (_, _) => RemoveSelectedSegment();
        btnSegmentUp.Click += (_, _) => MoveSelectedSegment(-1);
        btnSegmentDown.Click += (_, _) => MoveSelectedSegment(1);
        btnSegmentZoom.Click += (_, _) => ZoomToSelectedSegment();

        panelSegmentButtons.Controls.AddRange(new Control[]
        {
            btnSegmentAddFromAB, btnSegmentRemove, btnSegmentUp, btnSegmentDown, btnSegmentZoom
        });

        InitSegmentsGrid();
        gbSegments.Controls.Add(dgvSegments);
        gbSegments.Controls.Add(panelSegmentButtons);

        splitBottom.Panel1.Controls.Add(gbSegments);

        // Right: options
        gbOptions = new GroupBox { Text = "Опции редактирования", Dock = DockStyle.Fill };

        gbEditMode = new GroupBox { Text = "Режим", Dock = DockStyle.Top, Height = 80 };
        rbModeKeepOnly = new RadioButton { Text = "Оставить только эти сегменты", Left = 10, Top = 20, AutoSize = true };
        rbModeRemove = new RadioButton { Text = "Вырезать эти сегменты", Left = 10, Top = 45, AutoSize = true };
        rbModeKeepOnly.CheckedChanged += (_, _) => { if (rbModeKeepOnly.Checked) _editMode = SegmentEditMode.KeepOnly; };
        rbModeRemove.CheckedChanged += (_, _) => { if (rbModeRemove.Checked) _editMode = SegmentEditMode.Remove; };
        gbEditMode.Controls.AddRange(new Control[] { rbModeKeepOnly, rbModeRemove });

        gbOutputMode = new GroupBox { Text = "Вывод", Dock = DockStyle.Top, Height = 120 };
        cbLossless = new CheckBox { Text = "Быстрая обрезка без перекодирования (если возможно)", Left = 10, Top = 20, AutoSize = true };
        cbSplitIntoFiles = new CheckBox { Text = "Создать отдельный файл для каждого сегмента", Left = 10, Top = 45, AutoSize = true };
        cbJoinAfterEdit = new CheckBox { Text = "Склеить сегменты в один файл", Left = 10, Top = 70, AutoSize = true };
        lblLosslessWarning = new Label
        {
            Text = "Lossless использует -c copy, границы по keyframe, точки могут слегка \"плавать\".",
            Left = 10,
            Top = 95,
            AutoSize = true,
            ForeColor = Color.Gray
        };
        cbLossless.CheckedChanged += (_, _) => _lossless = cbLossless.Checked;
        gbOutputMode.Controls.AddRange(new Control[] { cbLossless, cbSplitIntoFiles, cbJoinAfterEdit, lblLosslessWarning });

        gbOptions.Controls.Add(gbOutputMode);
        gbOptions.Controls.Add(gbEditMode);

        splitBottom.Panel2.Controls.Add(gbOptions);

        // Bottom bar
        panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 45 };
        lblSummary = new Label { Text = "Сегментов: 0, суммарно: 00:00:00", AutoSize = true, Left = 10, Top = 14 };
        btnReset = new Button { Text = "Сбросить", Left = 300, Top = 8, Width = 90 };
        btnPreview = new Button { Text = "Черновой предпросмотр", Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Left = 800, Top = 8, Width = 160 };
        btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Left = 970, Top = 8, Width = 80 };
        btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, Left = 1060, Top = 8, Width = 90 };

        btnReset.Click += (_, _) => ResetSegments();
        btnPreview.Click += async (_, _) => await DoPreviewAsync().ConfigureAwait(true);
        btnOk.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        panelBottom.Controls.AddRange(new Control[] { lblSummary, btnReset, btnPreview, btnOk, btnCancel });

        // Root layout
        rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // preview
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // timeline
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // segments + options

        rootLayout.Controls.Add(videoPlayer, 0, 0);
        rootLayout.Controls.Add(panelTimeline, 0, 1);
        rootLayout.Controls.Add(splitBottom, 0, 2);

        Controls.Add(rootLayout);
        Controls.Add(panelBottom);

        // Bindings
        bsSegments.DataSource = _segments;
        dgvSegments.DataSource = bsSegments;
        rbModeKeepOnly.Checked = (_editMode == SegmentEditMode.KeepOnly);
        rbModeRemove.Checked = (_editMode == SegmentEditMode.Remove);
        cbLossless.Checked = _lossless;

        // Key bindings for A/B
        KeyPreview = true;
        KeyDown += TimelineEditorForm_KeyDown;

        Load += async (_, _) => await LoadMediaInfoAsync().ConfigureAwait(true);

        UpdateSummary();
    }
private void SwapAB()
{
    // Swap marker A and B
    var temp = _markerA;
    _markerA = _markerB;
    _markerB = temp;

    // Update UI
    UpdateMarkerLabels();
    panelTimelineBar.Invalidate();
}

private void UpdateMarkerLabels()
{
    lblMarkerA.Text = _markerA.HasValue ? $"A: {_markerA.Value:hh\\:mm\\:ss\\.fff}" : "A: —";
    lblMarkerB.Text = _markerB.HasValue ? $"B: {_markerB.Value:hh\\:mm\\:ss\\.fff}" : "B: —";
}
    private TimelineSegment CloneSegment(TimelineSegment s) => new()
    {
        Start = s.Start,
        End = s.End,
        Enabled = s.Enabled,
        Label = s.Label
    };

    private async Task LoadMediaInfoAsync()
    {
        _mediaInfo = await FFmpeg.GetMediaInfo(_inputPath).ConfigureAwait(true);
        _duration = _mediaInfo.Duration;
        await videoPlayer.LoadVideoAsync(_inputPath, _mediaInfo).ConfigureAwait(true);
        UpdateSummary();
        panelTimelineBar.Invalidate();
    }

    private void InitSegmentsGrid()
    {
        dgvSegments.Columns.Clear();

        var colEnabled = new DataGridViewCheckBoxColumn { DataPropertyName = nameof(TimelineSegment.Enabled), HeaderText = "✓", Width = 30 };
        var colStart = new DataGridViewTextBoxColumn { DataPropertyName = nameof(TimelineSegment.Start), HeaderText = "Начало", Width = 120 };
        var colEnd = new DataGridViewTextBoxColumn { DataPropertyName = nameof(TimelineSegment.End), HeaderText = "Конец", Width = 120 };
        var colDuration = new DataGridViewTextBoxColumn { HeaderText = "Длительность", Width = 120, ReadOnly = true };
        var colLabel = new DataGridViewTextBoxColumn { DataPropertyName = nameof(TimelineSegment.Label), HeaderText = "Название", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };

        dgvSegments.Columns.AddRange(colEnabled, colStart, colEnd, colDuration, colLabel);

        dgvSegments.CellFormatting += (s, e) =>
        {
            if (e.RowIndex >= 0 && dgvSegments.Columns[e.ColumnIndex].HeaderText == "Длительность")
            {
                var row = dgvSegments.Rows[e.RowIndex];
                if (row.DataBoundItem is TimelineSegment seg)
                {
                    e.Value = seg.Duration.ToString();
                }
            }
        };

        dgvSegments.CellEndEdit += DgvSegments_CellEndEdit;
        dgvSegments.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0 && dgvSegments.Rows[e.RowIndex].DataBoundItem is TimelineSegment seg)
            {
                SeekTo(seg.Start);
                SetMarkerA(seg.Start);
                SetMarkerB(seg.End);
            }
        };
    }

    private void DgvSegments_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var seg = dgvSegments.Rows[e.RowIndex].DataBoundItem as TimelineSegment;
        if (seg == null) return;

        try
        {
            // Attempt normalization and validation
            seg.Validate();
            bsSegments.ResetBindings(false);
            UpdateSummary();
            panelTimelineBar.Invalidate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка в сегменте: {ex.Message}");
        }
    }

    private void BtnAddSegmentFromAB_Click(object? sender, EventArgs e)
    {
        if (!_markerA.HasValue || !_markerB.HasValue)
        {
            MessageBox.Show("Сначала установите маркеры A и B.");
            return;
        }
        if (_markerB <= _markerA)
        {
            MessageBox.Show("Маркер B должен быть позже A.");
            return;
        }

        var segment = new TimelineSegment
        {
            Start = _markerA.Value,
            End = _markerB.Value,
            Enabled = true,
            Label = "A–B"
        };

        _segments.Add(segment);
        bsSegments.ResetBindings(false);
        UpdateSummary();
        panelTimelineBar.Invalidate();
    }

    private void RemoveSelectedSegment()
    {
        if (dgvSegments.CurrentRow?.DataBoundItem is TimelineSegment seg)
        {
            _segments.Remove(seg);
            bsSegments.ResetBindings(false);
            UpdateSummary();
            panelTimelineBar.Invalidate();
        }
    }

    private void MoveSelectedSegment(int direction)
    {
        if (dgvSegments.CurrentRow == null) return;
        int index = dgvSegments.CurrentRow.Index;
        int newIndex = index + direction;
        if (newIndex < 0 || newIndex >= _segments.Count) return;
        var item = _segments[index];
        _segments.RemoveAt(index);
        _segments.Insert(newIndex, item);
        bsSegments.ResetBindings(false);
        dgvSegments.CurrentCell = dgvSegments.Rows[newIndex].Cells[0];
        UpdateSummary();
        panelTimelineBar.Invalidate();
    }

    private void ZoomToSelectedSegment()
    {
        if (dgvSegments.CurrentRow?.DataBoundItem is TimelineSegment seg)
        {
            SetMarkerA(seg.Start);
            SetMarkerB(seg.End);
            SeekTo(seg.Start);
        }
    }

    private void TimelineEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.I)
        {
            SetMarkerA(GetCurrentTime());
        }
        else if (e.KeyCode == Keys.O)
        {
            SetMarkerB(GetCurrentTime());
        }
        else if (e.KeyCode == Keys.Enter)
        {
            BtnAddSegmentFromAB_Click(sender, EventArgs.Empty);
        }
    }

    private void SetMarkerA(TimeSpan time)
    {
        _markerA = time;
        lblMarkerA.Text = $"A: {_markerA}";
    }

    private void SetMarkerB(TimeSpan time)
    {
        if (_markerA.HasValue && time <= _markerA.Value)
        {
            MessageBox.Show("Маркер B должен быть позже A.");
            return;
        }
        _markerB = time;
        lblMarkerB.Text = $"B: {_markerB}";
    }

    private TimeSpan GetCurrentTime() => videoPlayer.GetCurrentTime();
    private void SeekTo(TimeSpan t) => videoPlayer.SeekTo(t);

    private void ResetSegments()
    {
        _segments.Clear();
        _markerA = null;
        _markerB = null;
        lblMarkerA.Text = "A: —";
        lblMarkerB.Text = "B: —";
        bsSegments.ResetBindings(false);
        UpdateSummary();
        panelTimelineBar.Invalidate();
    }

    private void UpdateSummary()
    {
        var active = _segments.Where(s => s.Enabled).ToList();
        var sumDuration = active.Aggregate(TimeSpan.Zero, (acc, s) => acc + s.Duration);
        lblSummary.Text = $"Сегментов: {active.Count}, суммарно: {sumDuration}";
    }

    private async Task DoPreviewAsync()
    {
        try
        {
            var output = Path.Combine(Path.GetTempPath(), $"timeline_preview_{Guid.NewGuid():N}.mp4");
            var normalized = TimelineUtils.Normalize(_segments);

            if (cbSplitIntoFiles.Checked)
            {
                await TimelineSplitService.SplitBySegmentsAsync(_inputPath, Path.GetDirectoryName(output)!, normalized, cbLossless.Checked).ConfigureAwait(true);
                MessageBox.Show("Готово: разбиение по сегментам завершено.");
                return;
            }

            if (_lossless)
            {
                // For quick preview in lossless mode perform KeepOnly concat via filter if possible
                await TimelineEditingService.CutToSingleFileAsync(_inputPath, output, normalized, SegmentEditMode.KeepOnly).ConfigureAwait(true);
            }
            else
            {
                await TimelineEditingService.CutToSingleFileAsync(_inputPath, output, normalized, _editMode).ConfigureAwait(true);
            }

            if (File.Exists(output))
            {
                var info = await FFmpeg.GetMediaInfo(output).ConfigureAwait(true);
                await videoPlayer.LoadVideoAsync(output, info).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка предпросмотра: {ex.Message}");
        }
    }

    private void PanelTimelineBar_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(panelTimelineBar.BackColor);
        using var penScale = new Pen(Color.Gray, 1);
        using var brushSeg = new SolidBrush(Color.FromArgb(100, Color.DeepSkyBlue));
        using var brushPos = new SolidBrush(Color.Red);

        if (_duration <= TimeSpan.Zero)
            return;

        // draw ticks every N seconds
        double totalSec = _duration.TotalSeconds;
        double tick = ChooseTick(totalSec);
        for (double t = 0; t <= totalSec; t += tick)
        {
            int x = (int)(t / totalSec * panelTimelineBar.Width);
            g.DrawLine(penScale, x, 0, x, panelTimelineBar.Height);
        }

        // draw segments
        foreach (var s in _segments.Where(s => s.Enabled))
        {
            int x0 = (int)(s.Start.TotalSeconds / totalSec * panelTimelineBar.Width);
            int x1 = (int)(s.End.TotalSeconds / totalSec * panelTimelineBar.Width);
            var rect = new Rectangle(Math.Min(x0, x1), 10, Math.Max(2, Math.Abs(x1 - x0)), panelTimelineBar.Height - 20);
            g.FillRectangle(brushSeg, rect);
        }

        // draw current position from player
        var pos = GetCurrentTime();
        int xpos = (int)(pos.TotalSeconds / totalSec * panelTimelineBar.Width);
        g.FillRectangle(brushPos, new Rectangle(xpos - 1, 0, 2, panelTimelineBar.Height));
    }

    private void PanelTimelineBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_duration <= TimeSpan.Zero) return;
        double percent = e.X / (double)Math.Max(1, panelTimelineBar.Width);
        var t = TimeSpan.FromSeconds(_duration.TotalSeconds * percent);
        SeekTo(t);
        panelTimelineBar.Invalidate();
    }

    private static double ChooseTick(double totalSeconds)
    {
        double[] options = { 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300 };
        foreach (var o in options)
        {
            if (totalSeconds / o <= 20) return o;
        }
        return 600;
    }
}
