using System;
using System.Drawing;
using System.Windows.Forms;
using Converter.Domain.Models;
using Converter.Models;

namespace Converter.UI.Controls;

public class QueueControlPanel : Panel
{
    private readonly Button _btnStart;
    private readonly Button _btnPause;
    private readonly Button _btnStop;
    private readonly Button _btnClearCompleted;
    private readonly Button _btnSortPriority;
    private readonly Button _btnSortSize;
    private readonly ComboBox _filterStatus;
    private readonly CheckBox _chkAutoStart;
    private readonly CheckBox _chkStopOnError;
    private readonly NumericUpDown _numConcurrent;
    private readonly Label _lblStats;

    public event EventHandler? StartClicked;
    public event EventHandler? PauseClicked;
    public event EventHandler? StopClicked;
    public event EventHandler? ClearCompletedClicked;
    public event EventHandler<string>? SortRequested;
    public event EventHandler<ConversionStatus?>? FilterChanged;
    public event EventHandler<bool>? AutoStartChanged;
    public event EventHandler<bool>? StopOnErrorChanged;
    public event EventHandler<int>? MaxConcurrentChanged;

    public QueueControlPanel()
    {
        Size = new Size(800, 60);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(5);

        _btnStart = CreateButton("▶ Старт", new Point(10, 10), Color.Green);
        _btnStart.Click += (_, _) => StartClicked?.Invoke(this, EventArgs.Empty);

        _btnPause = CreateButton("⏸ Пауза", new Point(90, 10), Color.Orange);
        _btnPause.Click += (_, _) => PauseClicked?.Invoke(this, EventArgs.Empty);

        _btnStop = CreateButton("⏹ Стоп", new Point(170, 10), Color.Red);
        _btnStop.Click += (_, _) => StopClicked?.Invoke(this, EventArgs.Empty);

        _btnClearCompleted = CreateButton("🗑 Очистить завершенные", new Point(250, 10), Color.Gray);
        _btnClearCompleted.Width = 180;
        _btnClearCompleted.Click += (_, _) => ClearCompletedClicked?.Invoke(this, EventArgs.Empty);

        var lblSort = new Label
        {
            Location = new Point(450, 15),
            Size = new Size(70, 20),
            Text = "Сортировка:"
        };

        _btnSortPriority = CreateSmallButton("Приоритет", new Point(520, 10));
        _btnSortPriority.Click += (_, _) => SortRequested?.Invoke(this, "priority");

        _btnSortSize = CreateSmallButton("Размер", new Point(600, 10));
        _btnSortSize.Click += (_, _) => SortRequested?.Invoke(this, "size");

        var lblFilter = new Label
        {
            Location = new Point(450, 40),
            Size = new Size(50, 20),
            Text = "Фильтр:"
        };

        _filterStatus = new ComboBox
        {
            Location = new Point(500, 37),
            Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _filterStatus.Items.AddRange(new object[] { "Все", "В очереди", "Обработка", "Завершено", "Ошибка" });
        _filterStatus.SelectedIndex = 0;
        _filterStatus.SelectedIndexChanged += (_, _) =>
        {
            ConversionStatus? status = _filterStatus.SelectedIndex switch
            {
                1 => ConversionStatus.Pending,
                2 => ConversionStatus.Processing,
                3 => ConversionStatus.Completed,
                4 => ConversionStatus.Failed,
                _ => null
            };
            FilterChanged?.Invoke(this, status);
        };

        _chkAutoStart = new CheckBox
        {
            Location = new Point(10, 35),
            Size = new Size(150, 20),
            Text = "Автозапуск",
            Checked = true
        };
        _chkAutoStart.CheckedChanged += (_, _) => AutoStartChanged?.Invoke(this, _chkAutoStart.Checked);

        _chkStopOnError = new CheckBox
        {
            Location = new Point(160, 35),
            Size = new Size(180, 20),
            Text = "Остановить при ошибке"
        };
        _chkStopOnError.CheckedChanged += (_, _) => StopOnErrorChanged?.Invoke(this, _chkStopOnError.Checked);

        var lblConcurrent = new Label
        {
            Location = new Point(340, 40),
            Size = new Size(100, 20),
            Text = "Параллельно:"
        };

        _numConcurrent = new NumericUpDown
        {
            Location = new Point(420, 37),
            Size = new Size(50, 25),
            Minimum = 1,
            Maximum = 8,
            Value = 2
        };
        _numConcurrent.ValueChanged += (_, _) => MaxConcurrentChanged?.Invoke(this, (int)_numConcurrent.Value);

        _lblStats = new Label
        {
            Location = new Point(650, 15),
            Size = new Size(140, 40),
            Font = new Font("Segoe UI", 8),
            Text = "Статистика:\n0 файлов"
        };

        Controls.AddRange(new Control[]
        {
            _btnStart, _btnPause, _btnStop, _btnClearCompleted,
            lblSort, _btnSortPriority, _btnSortSize,
            lblFilter, _filterStatus,
            _chkAutoStart, _chkStopOnError,
            lblConcurrent, _numConcurrent,
            _lblStats
        });
    }

    private Button CreateButton(string text, Point location, Color color)
    {
        return new Button
        {
            Location = location,
            Size = new Size(75, 25),
            Text = text,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
    }

    private Button CreateSmallButton(string text, Point location)
    {
        return new Button
        {
            Location = location,
            Size = new Size(75, 20),
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
    }

    public void UpdateStatistics(QueueStatistics stats)
    {
        _lblStats.Text = "Статистика:\n" +
                         $"{stats.CompletedItems}/{stats.TotalItems} завершено\n" +
                         $"Успех: {stats.SuccessRate}%";
    }
}
