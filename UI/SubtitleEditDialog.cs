using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Converter.UI
{
    public class SubtitleEditDialog : Form
    {
        private MaskedTextBox txtStartTime = null!;
        private MaskedTextBox txtEndTime = null!;
        private TextBox txtText = null!;
        private readonly SubtitleItem subtitle;

        public SubtitleEditDialog(TimeSpan currentTime)
        {
            subtitle = new SubtitleItem
            {
                StartTime = currentTime,
                EndTime = currentTime.Add(TimeSpan.FromSeconds(3))
            };

            InitializeComponent();
            LoadSubtitle();
        }

        public SubtitleEditDialog(SubtitleItem existingSubtitle)
        {
            subtitle = new SubtitleItem
            {
                StartTime = existingSubtitle.StartTime,
                EndTime = existingSubtitle.EndTime,
                Text = existingSubtitle.Text
            };

            InitializeComponent();
            LoadSubtitle();
        }

        private void InitializeComponent()
        {
            Text = "Редактирование субтитра";
            Size = new Size(500, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;

            var lblStart = new Label
            {
                Text = "Время начала (чч:мм:сс.ммм):",
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(lblStart);

            txtStartTime = new MaskedTextBox
            {
                Location = new Point(20, 45),
                Width = 150,
                Mask = "00:00:00.000",
                Font = new Font("Consolas", 10)
            };
            Controls.Add(txtStartTime);

            var lblEnd = new Label
            {
                Text = "Время окончания (чч:мм:сс.ммм):",
                Location = new Point(200, 20),
                AutoSize = true
            };
            Controls.Add(lblEnd);

            txtEndTime = new MaskedTextBox
            {
                Location = new Point(200, 45),
                Width = 150,
                Mask = "00:00:00.000",
                Font = new Font("Consolas", 10)
            };
            Controls.Add(txtEndTime);

            var lblText = new Label
            {
                Text = "Текст субтитра:",
                Location = new Point(20, 85),
                AutoSize = true
            };
            Controls.Add(lblText);

            txtText = new TextBox
            {
                Location = new Point(20, 110),
                Size = new Size(450, 100),
                Multiline = true,
                Font = new Font("Segoe UI", 12)
            };
            Controls.Add(txtText);

            var btnOK = new Button
            {
                Text = "OK",
                Location = new Point(280, 220),
                Size = new Size(90, 30),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;
            Controls.Add(btnOK);

            var btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(380, 220),
                Size = new Size(90, 30),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancel);

            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }

        private void LoadSubtitle()
        {
            txtStartTime.Text = FormatTimeForEdit(subtitle.StartTime);
            txtEndTime.Text = FormatTimeForEdit(subtitle.EndTime);
            txtText.Text = subtitle.Text ?? string.Empty;
        }

        private static string FormatTimeForEdit(TimeSpan time) => time.ToString("hh\\:mm\\:ss\\.fff");

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            try
            {
                subtitle.StartTime = ParseTime(txtStartTime.Text);
                subtitle.EndTime = ParseTime(txtEndTime.Text);
                subtitle.Text = txtText.Text;

                if (subtitle.EndTime <= subtitle.StartTime)
                {
                    MessageBox.Show("Время окончания должно быть больше времени начала!", "Ошибка");
                    DialogResult = DialogResult.None;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
                DialogResult = DialogResult.None;
            }
        }

        private static TimeSpan ParseTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("Неверный формат времени");
            }

            // Нормализуем разделитель дробной части: заменяем запятую на точку
            var normalized = text.Trim().Replace(',', '.');

            if (TimeSpan.TryParseExact(normalized, "hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            throw new FormatException("Неверный формат времени");
        }

        public SubtitleItem GetSubtitle() => subtitle;
    }
}
