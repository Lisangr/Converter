using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.UI.Dialogs
{
    public class NotificationSettingsForm : Form
    {
        private Converter.Domain.Models.NotificationOptions _settings;

        private CheckBox _chkDesktopNotifications = null!;
        private CheckBox _chkProgressNotifications = null!;
        private GroupBox _grpSound = null!;
        private CheckBox _chkSoundEnabled = null!;
        private CheckBox _chkCustomSound = null!;
        private TextBox _txtCustomSoundPath = null!;
        private Button _btnBrowseSound = null!;
        private Button _btnTestSound = null!;
        private Button _btnTestNotification = null!;
        private Button _btnSave = null!;
        private Button _btnCancel = null!;

        public Converter.Domain.Models.NotificationOptions Settings => _settings;

        public NotificationSettingsForm(Converter.Domain.Models.NotificationOptions settings)
        {
            _settings = settings ?? new Converter.Domain.Models.NotificationOptions();
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Настройки уведомлений";
            Size = new Size(520, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            var yPos = 20;

            _chkDesktopNotifications = new CheckBox
            {
                Text = "✅ Включить Windows уведомления",
                Location = new Point(20, yPos),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };
            Controls.Add(_chkDesktopNotifications);
            yPos += 35;

            _chkProgressNotifications = new CheckBox
            {
                Text = "Показывать промежуточные уведомления о прогрессе",
                Location = new Point(40, yPos),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            Controls.Add(_chkProgressNotifications);
            yPos += 40;

            _grpSound = new GroupBox
            {
                Text = "🔊 Звуковые уведомления",
                Location = new Point(10, yPos),
                Size = new Size(480, 150),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            Controls.Add(_grpSound);

            _chkSoundEnabled = new CheckBox
            {
                Text = "Включить звуковой сигнал",
                Location = new Point(10, 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
            _chkSoundEnabled.CheckedChanged += (s, e) => UpdateSoundControls();
            _grpSound.Controls.Add(_chkSoundEnabled);

            _chkCustomSound = new CheckBox
            {
                Text = "Использовать свой звук:",
                Location = new Point(30, 55),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            _chkCustomSound.CheckedChanged += (s, e) => UpdateSoundControls();
            _grpSound.Controls.Add(_chkCustomSound);

            _txtCustomSoundPath = new TextBox
            {
                Location = new Point(30, 80),
                Width = 320,
                ReadOnly = true
            };
            _grpSound.Controls.Add(_txtCustomSoundPath);

            _btnBrowseSound = new Button
            {
                Text = "Обзор...",
                Location = new Point(360, 78),
                Width = 90
            };
            _btnBrowseSound.Click += BtnBrowseSound_Click;
            _grpSound.Controls.Add(_btnBrowseSound);

            _btnTestSound = new Button
            {
                Text = "🔊 Тест звука",
                Location = new Point(30, 110),
                Width = 120
            };
            _btnTestSound.Click += BtnTestSound_Click;
            _grpSound.Controls.Add(_btnTestSound);

            yPos += 170;

            _btnTestNotification = new Button
            {
                Text = "🧪 Тестовое уведомление",
                Location = new Point(20, yPos),
                Size = new Size(200, 35),
                Font = new Font("Segoe UI", 9)
            };
            _btnTestNotification.Click += BtnTestNotification_Click;
            Controls.Add(_btnTestNotification);

            yPos += 50;

            _btnSave = new Button
            {
                Text = "Сохранить",
                Location = new Point(300, yPos),
                Size = new Size(90, 35),
                DialogResult = DialogResult.OK
            };
            _btnSave.Click += BtnSave_Click;
            Controls.Add(_btnSave);

            _btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(400, yPos),
                Size = new Size(90, 35),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(_btnCancel);

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
        }

        private void LoadSettings()
        {
            _chkDesktopNotifications.Checked = _settings.DesktopNotificationsEnabled;
            _chkProgressNotifications.Checked = _settings.ShowProgressNotifications;
            _chkSoundEnabled.Checked = _settings.SoundEnabled;
            _chkCustomSound.Checked = _settings.UseCustomSound;
            _txtCustomSoundPath.Text = _settings.CustomSoundPath;
            UpdateSoundControls();
        }

        private void UpdateSoundControls()
        {
            var soundEnabled = _chkSoundEnabled.Checked;
            var customSoundEnabled = soundEnabled && _chkCustomSound.Checked;

            _chkCustomSound.Enabled = soundEnabled;
            _txtCustomSoundPath.Enabled = customSoundEnabled;
            _btnBrowseSound.Enabled = customSoundEnabled;
            _btnTestSound.Enabled = soundEnabled;
        }

        private void BtnBrowseSound_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "WAV файлы (*.wav)|*.wav|Все файлы (*.*)|*.*",
                Title = "Выберите звуковой файл"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _txtCustomSoundPath.Text = openFileDialog.FileName;
            }
        }

        private void BtnTestSound_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!_chkSoundEnabled.Checked)
                {
                    SystemSounds.Beep.Play();
                    return;
                }

                if (_chkCustomSound.Checked && !string.IsNullOrWhiteSpace(_txtCustomSoundPath.Text) && File.Exists(_txtCustomSoundPath.Text))
                {
                    using var player = new SoundPlayer(_txtCustomSoundPath.Text);
                    player.Play();
                }
                else
                {
                    SystemSounds.Asterisk.Play();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка воспроизведения: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTestNotification_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_chkDesktopNotifications.Checked)
                {
                    MessageBox.Show(
                        this,
                        "Тестовое уведомление о завершении конвертации\n\nФайлов обработано: 5\nСэкономлено: 150 МБ",
                        "Тестовое уведомление",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                // Одновременно тестируем звук
                if (_chkSoundEnabled.Checked)
                {
                    if (_chkCustomSound.Checked && !string.IsNullOrWhiteSpace(_txtCustomSoundPath.Text) && File.Exists(_txtCustomSoundPath.Text))
                    {
                        using var player = new SoundPlayer(_txtCustomSoundPath.Text);
                        player.Play();
                    }
                    else
                    {
                        SystemSounds.Asterisk.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка тестового уведомления: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (_chkCustomSound.Checked && string.IsNullOrWhiteSpace(_txtCustomSoundPath.Text))
            {
                MessageBox.Show("Выберите WAV файл со звуком", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            if (_chkCustomSound.Checked && !string.IsNullOrWhiteSpace(_txtCustomSoundPath.Text) && !File.Exists(_txtCustomSoundPath.Text))
            {
                MessageBox.Show("Файл со звуком не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.None;
                return;
            }

            SaveSettings();
        }

        private void SaveSettings()
        {
            _settings.DesktopNotificationsEnabled = _chkDesktopNotifications.Checked;
            _settings.ShowProgressNotifications = _chkProgressNotifications.Checked;
            _settings.SoundEnabled = _chkSoundEnabled.Checked;
            _settings.UseCustomSound = _chkCustomSound.Checked;
            _settings.CustomSoundPath = _chkCustomSound.Checked ? _txtCustomSoundPath.Text : null;
        }
    }
}
