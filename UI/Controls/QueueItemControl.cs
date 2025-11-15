using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Models;

namespace Converter.UI.Controls;

public class QueueItemControl : Panel
{
    private readonly PictureBox _thumbnail;
    private readonly Label _fileName;
    private readonly Label _status;
    private readonly ProgressBar _progressBar;
    private readonly Label _eta;
    private readonly Button _btnMoveUp;
    private readonly Button _btnMoveDown;
    private readonly Button _btnStar;
    private readonly Button _btnCancel;
    private readonly ComboBox _priorityCombo;
    private readonly Panel _statusIndicator;
    private bool _updatingPriority;

    public QueueItem Item { get; }

    public event EventHandler<Guid>? MoveUpClicked;
    public event EventHandler<Guid>? MoveDownClicked;
    public event EventHandler<Guid>? StarToggled;
    public event EventHandler<Guid>? CancelClicked;
    public event EventHandler<(Guid Id, int Priority)>? PriorityChanged;

    public QueueItemControl(QueueItem item)
    {
        Item = item;
        Size = new Size(800, 80);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(5);

        _statusIndicator = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(5, Height),
            BackColor = item.StatusColor
        };

        _thumbnail = new PictureBox
        {
            Location = new Point(10, 10),
            Size = new Size(90, 60),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.LightGray
        };

        _fileName = new Label
        {
            Location = new Point(110, 10),
            Size = new Size(300, 20),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Text = item.FileName
        };

        _status = new Label
        {
            Location = new Point(110, 35),
            Size = new Size(200, 18),
            Font = new Font("Segoe UI", 9),
            ForeColor = item.StatusColor,
            Text = item.StatusText
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(110, 55),
            Size = new Size(300, 15),
            Value = Math.Clamp(item.Progress, 0, 100),
            Style = ProgressBarStyle.Continuous
        };

        _eta = new Label
        {
            Location = new Point(420, 55),
            Size = new Size(100, 15),
            Font = new Font("Segoe UI", 8),
            Text = $"ETA: {item.ETA}"
        };

        _btnStar = new Button
        {
            Location = new Point(530, 10),
            Size = new Size(30, 30),
            Text = item.IsStarred ? "★" : "☆",
            Font = new Font("Segoe UI", 14),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnStar.FlatAppearance.BorderSize = 0;
        _btnStar.Click += (_, _) => StarToggled?.Invoke(this, Item.Id);

        _priorityCombo = new ComboBox
        {
            Location = new Point(570, 10),
            Size = new Size(60, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _priorityCombo.Items.AddRange(new object[] { "1", "2", "3", "4", "5" });
        _priorityCombo.SelectedItem = Item.Priority.ToString();
        _priorityCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingPriority)
            {
                return;
            }

            if (int.TryParse(_priorityCombo.SelectedItem?.ToString(), out var priority))
            {
                PriorityChanged?.Invoke(this, (Item.Id, priority));
            }
        };

        _btnMoveUp = CreateIconButton("▲", new Point(640, 10));
        _btnMoveUp.Click += (_, _) => MoveUpClicked?.Invoke(this, Item.Id);

        _btnMoveDown = CreateIconButton("▼", new Point(640, 45));
        _btnMoveDown.Click += (_, _) => MoveDownClicked?.Invoke(this, Item.Id);

        _btnCancel = CreateIconButton("✕", new Point(680, 25));
        _btnCancel.Click += (_, _) => CancelClicked?.Invoke(this, Item.Id);

        Controls.AddRange(new Control[]
        {
            _statusIndicator, _thumbnail, _fileName, _status,
            _progressBar, _eta, _btnStar, _priorityCombo,
            _btnMoveUp, _btnMoveDown, _btnCancel
        });
    }

    private Button CreateIconButton(string text, Point location)
    {
        var btn = new Button
        {
            Location = location,
            Size = new Size(30, 30),
            Text = text,
            Font = new Font("Segoe UI", 10),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 1;
        return btn;
    }

    public void UpdateDisplay()
    {
        _status.Text = Item.StatusText;
        _status.ForeColor = Item.StatusColor;
        _statusIndicator.BackColor = Item.StatusColor;
        _progressBar.Value = Math.Clamp(Item.Progress, 0, 100);
        _eta.Text = $"ETA: {Item.ETA}";
        _btnStar.Text = Item.IsStarred ? "★" : "☆";
        if (!_priorityCombo.Items.Contains(Item.Priority.ToString()))
        {
            _priorityCombo.Items.Add(Item.Priority.ToString());
        }
        _updatingPriority = true;
        _priorityCombo.SelectedItem = Item.Priority.ToString();
        _updatingPriority = false;
    }
}
