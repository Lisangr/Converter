using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Services.UIServices;

namespace Converter.UI.Dialogs
{
    public class QueueDemoForm : Form
    {
        private readonly Label _lblTaskName;
        private readonly Label _lblStatus;
        private readonly ProgressBar _progressBar;
        private readonly System.Windows.Forms.Timer _animationTimer;

        private int _displayedProgress;
        private int _targetProgress;

        private readonly SimpleConversionQueue _queue;

        public QueueDemoForm()
        {
            Text = "Демо очереди конвертации";
            Size = new Size(500, 180);
            StartPosition = FormStartPosition.CenterParent;

            _lblTaskName = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(440, 20),
                Text = "Задача: --"
            };

            _lblStatus = new Label
            {
                Location = new Point(20, 45),
                Size = new Size(440, 20),
                Text = "Статус: ожидание"
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(20, 75),
                Size = new Size(440, 20),
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100
            };

            _animationTimer = new System.Windows.Forms.Timer
            {
                Interval = 30 // мс, ~33 FPS
            };
            _animationTimer.Tick += AnimationTimerOnTick;

            Controls.Add(_lblTaskName);
            Controls.Add(_lblStatus);
            Controls.Add(_progressBar);

            _queue = new SimpleConversionQueue();
            _queue.TaskStarted += QueueOnTaskStarted;
            _queue.TaskCompleted += QueueOnTaskCompleted;
            _queue.TaskProgressChanged += QueueOnTaskProgressChanged;

            Shown += async (_, _) => await StartDemoAsync();
        }

        private async Task StartDemoAsync()
        {
            var demoTask = new ConversionTask(
                "Демонстрационная конвертация",
                async progress =>
                {
                    for (var i = 0; i <= 100; i++)
                    {
                        progress.Report(i);
                        await Task.Delay(80).ConfigureAwait(false); // имитация работы
                    }
                });

            _queue.Enqueue(demoTask);
        }

        private void QueueOnTaskStarted(object? sender, ConversionTaskEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object?, ConversionTaskEventArgs>(QueueOnTaskStarted), sender, e);
                return;
            }

            _lblTaskName.Text = $"Задача: {e.Task.Name}";
            _lblStatus.Text = "Статус: обрабатывается";
            _targetProgress = 0;
            _displayedProgress = 0;
            _progressBar.Value = 0;
            _animationTimer.Start();
        }

        private void QueueOnTaskCompleted(object? sender, ConversionTaskEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object?, ConversionTaskEventArgs>(QueueOnTaskCompleted), sender, e);
                return;
            }

            _targetProgress = 100;
            _lblStatus.Text = "Статус: завершено";

            // Остановим анимацию чуть позже, когда прогресс дорисуется до 100
            var t = new System.Windows.Forms.Timer { Interval = 500 };
            t.Tick += (_, _) =>
            {
                t.Stop();
                t.Dispose();
                _animationTimer.Stop();
                _progressBar.Value = 100;
            };
            t.Start();
        }

        private void QueueOnTaskProgressChanged(object? sender, ConversionTaskProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object?, ConversionTaskProgressEventArgs>(QueueOnTaskProgressChanged), sender, e);
                return;
            }

            _targetProgress = e.Progress;
        }

        private void AnimationTimerOnTick(object? sender, EventArgs e)
        {
            if (_displayedProgress == _targetProgress)
            {
                return;
            }

            var step = 2; // скорость анимации
            if (_displayedProgress < _targetProgress)
            {
                _displayedProgress = Math.Min(_displayedProgress + step, _targetProgress);
            }
            else
            {
                _displayedProgress = Math.Max(_displayedProgress - step, _targetProgress);
            }

            _progressBar.Value = _displayedProgress;
            _lblStatus.Text = $"Статус: {_displayedProgress}%";
        }
    }
}
