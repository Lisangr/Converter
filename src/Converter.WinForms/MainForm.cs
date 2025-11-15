using System.IO;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.WinForms;

public sealed class MainForm : Form, IMainView
{
    private readonly Button _addFilesButton = new() { Text = "Add Files" };
    private readonly Button _startButton = new() { Text = "Start" };
    private readonly Button _cancelButton = new() { Text = "Cancel", Enabled = false };
    private readonly Button _browseButton = new() { Text = "Browse" };
    private readonly ComboBox _profilesComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _outputFolderTextBox = new() { PlaceholderText = "Output folder" };
    private readonly ProgressBar _progressBar = new() { Dock = DockStyle.Fill };
    private readonly ListView _queueView = new() { View = View.Details, FullRowSelect = true, Dock = DockStyle.Fill };
    private readonly Label _statusLabel = new() { Text = "Idle" };

    public event EventHandler? AddFilesRequested;
    public event EventHandler? StartConversionRequested;
    public event EventHandler? CancelRequested;
    public event EventHandler? BrowseOutputFolderRequested;
    public event EventHandler<ConversionProfile>? ProfileChanged;

    public MainForm()
    {
        Text = "Converter";
        Width = 900;
        Height = 600;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            AutoSize = true
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        layout.Controls.Add(_profilesComboBox, 0, 0);
        layout.SetColumnSpan(_profilesComboBox, 2);
        layout.Controls.Add(_addFilesButton, 2, 0);
        layout.Controls.Add(_startButton, 3, 0);

        layout.Controls.Add(_outputFolderTextBox, 0, 1);
        layout.SetColumnSpan(_outputFolderTextBox, 3);
        layout.Controls.Add(_browseButton, 3, 1);

        _queueView.Columns.Add("File", 400);
        _queueView.Columns.Add("Profile", 150);
        layout.Controls.Add(_queueView, 0, 2);
        layout.SetColumnSpan(_queueView, 4);

        layout.Controls.Add(_progressBar, 0, 3);
        layout.SetColumnSpan(_progressBar, 3);
        layout.Controls.Add(_cancelButton, 3, 3);

        Controls.Add(layout);
        Controls.Add(_statusLabel);
        _statusLabel.Dock = DockStyle.Bottom;

        _addFilesButton.Click += (s, e) => AddFilesRequested?.Invoke(this, EventArgs.Empty);
        _startButton.Click += (s, e) => StartConversionRequested?.Invoke(this, EventArgs.Empty);
        _cancelButton.Click += (s, e) => CancelRequested?.Invoke(this, EventArgs.Empty);
        _browseButton.Click += (s, e) => BrowseOutputFolderRequested?.Invoke(this, EventArgs.Empty);
        _profilesComboBox.SelectedIndexChanged += (s, e) =>
        {
            if (_profilesComboBox.SelectedItem is ConversionProfile profile)
            {
                ProfileChanged?.Invoke(this, profile);
            }
        };
    }

    public ConversionRequest? BuildConversionRequest(string inputFile)
    {
        if (_profilesComboBox.SelectedItem is not ConversionProfile profile)
        {
            ShowError("Profile", "Select profile first");
            return null;
        }

        var outputFolder = _outputFolderTextBox.Text;
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            ShowError("Output", "Output folder is required");
            return null;
        }

        return new ConversionRequest(inputFile, outputFolder!, profile);
    }

    public string? RequestInputFile()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Media files|*.mp4;*.mkv;*.mov;*.avi;*.mp3;*.wav|All files|*.*"
        };
        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? SelectOutputFolder()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputFolderTextBox.Text = dialog.SelectedPath;
            return dialog.SelectedPath;
        }

        return null;
    }

    public void BindProfiles(IEnumerable<ConversionProfile> profiles)
    {
        InvokeIfRequired(() =>
        {
            _profilesComboBox.DataSource = profiles.ToList();
            _profilesComboBox.DisplayMember = nameof(ConversionProfile.Name);
        });
    }

    public void UpdateQueue(IEnumerable<QueueItem> items)
    {
        InvokeIfRequired(() =>
        {
            _queueView.BeginUpdate();
            _queueView.Items.Clear();
            foreach (var item in items)
            {
                var listViewItem = new ListViewItem(new[]
                {
                    Path.GetFileName(item.Request.InputPath),
                    item.Request.Profile.Name
                });
                _queueView.Items.Add(listViewItem);
            }

            _queueView.EndUpdate();
        });
    }

    public void UpdateProgress(ConversionProgress progress)
    {
        InvokeIfRequired(() =>
        {
            _progressBar.Value = (int)Math.Clamp(progress.Percentage, 0, 100);
            _statusLabel.Text = progress.CurrentFile is null
                ? $"Progress: {_progressBar.Value}%"
                : $"{progress.CurrentFile} - {_progressBar.Value}%";
        });
    }

    public void SetBusyState(bool isBusy)
    {
        InvokeIfRequired(() =>
        {
            _startButton.Enabled = !isBusy;
            _cancelButton.Enabled = isBusy;
        });
    }

    public void ShowError(string title, string message)
    {
        InvokeIfRequired(() => MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error));
    }

    public void ShowInfo(string title, string message)
    {
        InvokeIfRequired(() => MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information));
    }

    private void InvokeIfRequired(Action action)
    {
        if (InvokeRequired)
        {
            BeginInvoke(action);
        }
        else
        {
            action();
        }
    }
}
