using System;
using System.Drawing;
using System.Windows.Forms;
using Converter.Domain.Models;

namespace Converter.UI.Dialogs;

public class StatisticsDialog : Form
{
    public StatisticsDialog(QueueStatistics stats)
    {
        InitializeComponent(stats);
    }

    private void InitializeComponent(QueueStatistics stats)
    {
        Text = "Статистика конвертации";
        Size = new Size(500, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 12,
            Padding = new Padding(20)
        };

        AddStatRow(panel, 0, "📊 Всего файлов:", stats.TotalItems.ToString());
        AddStatRow(panel, 1, "✅ Завершено:", $"{stats.CompletedItems} ({stats.SuccessRate}%)");
        AddStatRow(panel, 2, "⏳ В очереди:", stats.PendingItems.ToString());
        AddStatRow(panel, 3, "🔄 Обработка:", stats.ProcessingItems.ToString());
        AddStatRow(panel, 4, "❌ Ошибок:", stats.FailedItems.ToString());

        panel.Controls.Add(new Label { Text = string.Empty, Height = 10 }, 0, 5);

        AddStatRow(panel, 6, "📦 Исходный размер:", FormatBytes(stats.TotalInputSize));
        AddStatRow(panel, 7, "📦 Итоговый размер:", FormatBytes(stats.TotalOutputSize));
        var savedPercent = stats.TotalInputSize > 0
            ? (1 - stats.CompressionRatio) * 100
            : 0;
        AddStatRow(panel, 8, "💾 Сэкономлено:", $"{FormatBytes(stats.SpaceSaved)} ({savedPercent:F1}%)");

        panel.Controls.Add(new Label { Text = string.Empty, Height = 10 }, 0, 9);

        AddStatRow(panel, 10, "⏱️ Время обработки:", FormatTimeSpan(stats.TotalProcessingTime));
        AddStatRow(panel, 11, "⚡ Средняя скорость:", $"{stats.AverageSpeed:F2} MB/сек");

        var btnClose = new Button
        {
            Text = "Закрыть",
            Size = new Size(100, 30),
            Dock = DockStyle.Bottom,
            DialogResult = DialogResult.OK
        };

        Controls.Add(panel);
        Controls.Add(btnClose);
        AcceptButton = btnClose;
    }

    private void AddStatRow(TableLayoutPanel panel, int row, string label, string value)
    {
        panel.Controls.Add(new Label
        {
            Text = label,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true
        }, 0, row);

        panel.Controls.Add(new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 10),
            AutoSize = true
        }, 1, row);
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = Math.Abs(bytes);
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        var sign = bytes < 0 ? "-" : string.Empty;
        return $"{sign}{len:0.##} {sizes[order]}";
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours} ч {ts.Minutes} мин";
        }

        if (ts.TotalMinutes >= 1)
        {
            return $"{(int)ts.TotalMinutes} мин {ts.Seconds} сек";
        }

        return $"{ts.Seconds} сек";
    }
}
