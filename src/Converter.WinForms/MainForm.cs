using Converter.Application.Interfaces;
using Converter.Application.ViewModels;
using Converter.Domain.Models;

namespace Converter.WinForms;

public partial class MainForm : Form, IMainView
{
    private readonly List<string> _inputFiles = new();
    private readonly Dictionary<Guid, ListViewItem> _items = new();
    private readonly Dictionary<Guid, string> _thumbnailKeys = new();

    public MainForm()
    {
        InitializeComponent();
    }

    public event EventHandler? ViewLoaded;
    public event EventHandler? AddFilesRequested;
    public event EventHandler? StartConversionRequested;
    public event EventHandler<Guid>? CancelConversionRequested;
    public event EventHandler<Guid>? RemoveItemRequested;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ViewLoaded?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<string> SelectedInputFiles => _inputFiles;
    public string? OutputDirectory => txtOutput.Text;
    public ConversionProfile? SelectedProfile => cmbProfiles.SelectedItem as ConversionProfile;

    private void OnAddFilesClicked()
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Media files|*.mp4;*.mkv;*.mov;*.avi|All files|*.*"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _inputFiles.Clear();
            _inputFiles.AddRange(dialog.FileNames);
            AddFilesRequested?.Invoke(this, EventArgs.Empty);
            ShowInfo($"Selected {_inputFiles.Count} file(s)");
        }
    }

    private void OnStartClicked()
    {
        StartConversionRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnBrowseClicked()
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtOutput.Text = dialog.SelectedPath;
        }
    }

    private void OnCancelSelected()
    {
        if (lvQueue.SelectedItems.Count == 0)
        {
            return;
        }

        var id = (Guid)lvQueue.SelectedItems[0].Tag;
        CancelConversionRequested?.Invoke(this, id);
    }

    private void OnRemoveSelected()
    {
        if (lvQueue.SelectedItems.Count == 0)
        {
            return;
        }

        var id = (Guid)lvQueue.SelectedItems[0].Tag;
        RemoveItemRequested?.Invoke(this, id);
        RemoveLocalItem(id);
    }

    public void SetAvailableProfiles(IReadOnlyList<ConversionProfile> profiles)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => SetAvailableProfiles(profiles)));
            return;
        }

        cmbProfiles.DataSource = profiles.ToList();
        cmbProfiles.DisplayMember = nameof(ConversionProfile.Name);
    }

    public void DisplayQueueItems(IReadOnlyList<QueueItemViewModel> items)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => DisplayQueueItems(items)));
            return;
        }

        lvQueue.BeginUpdate();
        lvQueue.Items.Clear();
        _items.Clear();
        foreach (var item in items)
        {
            var listItem = CreateListItem(item);
            lvQueue.Items.Add(listItem);
            _items[item.Id] = listItem;
        }

        lvQueue.EndUpdate();
    }

    public void UpdateProgress(Guid queueItemId, ConversionProgress progress)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateProgress(queueItemId, progress)));
            return;
        }

        if (_items.TryGetValue(queueItemId, out var item))
        {
            item.SubItems[3].Text = $"{progress.Percentage:F1}%";
        }
    }

    public void UpdateStatus(Guid queueItemId, string statusText)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateStatus(queueItemId, statusText)));
            return;
        }

        if (_items.TryGetValue(queueItemId, out var item))
        {
            item.SubItems[2].Text = statusText;
        }
        else
        {
            var model = new QueueItemViewModel(queueItemId, "", "", statusText);
            var listItem = CreateListItem(model);
            _items[queueItemId] = listItem;
            lvQueue.Items.Add(listItem);
        }
    }

    public void DisplayThumbnail(Guid queueItemId, Stream thumbnailStream)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => DisplayThumbnail(queueItemId, thumbnailStream)));
            return;
        }

        using var image = Image.FromStream(thumbnailStream);
        var key = queueItemId.ToString();
        thumbnails.Images.RemoveByKey(key);
        thumbnails.Images.Add(key, (Image)image.Clone());
        _thumbnailKeys[queueItemId] = key;
        if (_items.TryGetValue(queueItemId, out var item))
        {
            item.ImageKey = key;
        }
    }

    public void SetBusy(bool isBusy)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => SetBusy(isBusy)));
            return;
        }

        UseWaitCursor = isBusy;
    }

    public void ShowError(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => ShowError(message)));
            return;
        }

        MessageBox.Show(this, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public void ShowInfo(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => ShowInfo(message)));
            return;
        }

        MessageBox.Show(this, message, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RemoveLocalItem(Guid id)
    {
        if (_items.TryGetValue(id, out var item))
        {
            lvQueue.Items.Remove(item);
            _items.Remove(id);
            if (_thumbnailKeys.TryGetValue(id, out var key))
            {
                thumbnails.Images.RemoveByKey(key);
                _thumbnailKeys.Remove(id);
            }
        }
    }

    private static ListViewItem CreateListItem(QueueItemViewModel item)
    {
        var listItem = new ListViewItem(new[] { item.InputPath, item.OutputPath, item.StatusText, string.Empty })
        {
            Tag = item.Id
        };
        return listItem;
    }
}
