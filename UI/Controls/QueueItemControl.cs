using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using Converter.Extensions;

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
    private readonly System.Windows.Forms.Timer _animationTimer;
    private int _displayedProgress;
    private int _targetProgress;

    public QueueItemViewModel ViewModel { get; }

    public event EventHandler<Guid>? MoveUpClicked;
    public event EventHandler<Guid>? MoveDownClicked;
    public event EventHandler<Guid>? StarToggled;
    public event EventHandler<Guid>? CancelClicked;
    public event EventHandler<(Guid Id, int Priority)>? PriorityChanged;

    public QueueItemControl(QueueItemViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Size = new Size(800, 80);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(5);

        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = 30
        };
        _animationTimer.Tick += AnimationTimerOnTick;

        _statusIndicator = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(5, Height),
            BackColor = GetStatusColor(viewModel.Status)
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
            Text = viewModel.FileName
        };

        _status = new Label
        {
            Location = new Point(110, 35),
            Size = new Size(200, 18),
            Font = new Font("Segoe UI", 9),
            ForeColor = GetStatusColor(viewModel.Status),
            Text = GetStatusText(viewModel.Status)
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(110, 55),
            Size = new Size(300, 15),
            Value = Math.Clamp(viewModel.Progress, 0, 100),
            Style = ProgressBarStyle.Continuous
        };

        _eta = new Label
        {
            Location = new Point(420, 55),
            Size = new Size(100, 15),
            Font = new Font("Segoe UI", 8),
            Text = $"ETA: {GetEta()}"
        };

        _btnStar = new Button
        {
            Location = new Point(530, 10),
            Size = new Size(30, 30),
            Text = viewModel.IsStarred ? "★" : "☆",
            Font = new Font("Segoe UI", 14),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnStar.FlatAppearance.BorderSize = 0;
        _btnStar.Click += (_, _) => StarToggled?.Invoke(this, viewModel.Id);

        _priorityCombo = new ComboBox
        {
            Location = new Point(570, 10),
            Size = new Size(60, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _priorityCombo.Items.AddRange(new object[] { "1", "2", "3", "4", "5" });
        _priorityCombo.SelectedItem = viewModel.Priority.ToString();
        _priorityCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_updatingPriority)
            {
                return;
            }

            if (int.TryParse(_priorityCombo.SelectedItem?.ToString(), out var priority))
            {
                PriorityChanged?.Invoke(this, (viewModel.Id, priority));
            }
        };

        _btnMoveUp = CreateIconButton("▲", new Point(640, 10));
        _btnMoveUp.Click += (_, _) => MoveUpClicked?.Invoke(this, viewModel.Id);

        _btnMoveDown = CreateIconButton("▼", new Point(640, 45));
        _btnMoveDown.Click += (_, _) => MoveDownClicked?.Invoke(this, viewModel.Id);

        _btnCancel = CreateIconButton("✕", new Point(680, 25));
        _btnCancel.Click += (_, _) => CancelClicked?.Invoke(this, viewModel.Id);

        // Subscribe to ViewModel property changes
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _displayedProgress = Math.Clamp(viewModel.Progress, 0, 100);
        _targetProgress = _displayedProgress;

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

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnViewModelPropertyChanged(sender, e)));
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(QueueItemViewModel.Status):
                UpdateStatusDisplay();
                break;
            case nameof(QueueItemViewModel.Progress):
                UpdateProgressDisplay();
                break;
            case nameof(QueueItemViewModel.ErrorMessage):
                UpdateStatusDisplay();
                break;
            case nameof(QueueItemViewModel.IsStarred):
                _btnStar.Text = ViewModel.IsStarred ? "★" : "☆";
                break;
            case nameof(QueueItemViewModel.Priority):
                _updatingPriority = true;
                if (!_priorityCombo.Items.Contains(ViewModel.Priority.ToString()))
                {
                    _priorityCombo.Items.Add(ViewModel.Priority.ToString());
                }
                _priorityCombo.SelectedItem = ViewModel.Priority.ToString();
                _updatingPriority = false;
                break;
        }
    }

    private void UpdateStatusDisplay()
    {
        var statusColor = GetStatusColor(ViewModel.Status);
        _status.Text = GetStatusText(ViewModel.Status);
        _status.ForeColor = statusColor;
        _statusIndicator.BackColor = statusColor;
    }

    private void UpdateProgressDisplay()
    {
        _targetProgress = Math.Clamp(ViewModel.Progress, 0, 100);
        if (!_animationTimer.Enabled)
        {
            _animationTimer.Start();
        }
        _eta.Text = $"ETA: {GetEta()}";
    }

    public void UpdateDisplay()
    {
        UpdateStatusDisplay();
        UpdateProgressDisplay();
    }

    private static Color GetStatusColor(ConversionStatus status)
    {
        return status switch
        {
            ConversionStatus.Pending => Color.Gray,
            ConversionStatus.Processing => Color.Blue,
            ConversionStatus.Completed => Color.Green,
            ConversionStatus.Failed => Color.Red,
            ConversionStatus.Paused => Color.Orange,
            ConversionStatus.Cancelled => Color.DarkGray,
            _ => Color.Gray
        };
    }

    private static string GetStatusText(ConversionStatus status)
    {
        return status switch
        {
            ConversionStatus.Pending => "В очереди",
            ConversionStatus.Processing => "Обработка",
            ConversionStatus.Completed => "Завершено",
            ConversionStatus.Failed => "Ошибка",
            ConversionStatus.Paused => "Приостановлено",
            ConversionStatus.Cancelled => "Отменено",
            _ => "Неизвестно"
        };
    }

    private string GetEta()
    {
        if (ViewModel.Status != ConversionStatus.Processing)
            return "--:--";

        var remainingProgress = 100 - ViewModel.Progress;
        if (remainingProgress <= 0)
            return "00:00";

        // Простая оценка на основе прогресса (можно улучшить)
        var estimatedSeconds = remainingProgress * 2; // Примерная оценка
        var minutes = estimatedSeconds / 60;
        var seconds = estimatedSeconds % 60;
        return $"{minutes:D2}:{seconds:D2}";
    }

    private void AnimationTimerOnTick(object? sender, EventArgs e)
    {
        if (_displayedProgress == _targetProgress)
        {
            _animationTimer.Stop();
            return;
        }

        var step = 2;
        if (_displayedProgress < _targetProgress)
        {
            _displayedProgress = Math.Min(_displayedProgress + step, _targetProgress);
        }
        else
        {
            _displayedProgress = Math.Max(_displayedProgress - step, _targetProgress);
        }

        _progressBar.Value = _displayedProgress;
    }
}
