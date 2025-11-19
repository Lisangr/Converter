using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Converter.Application.ViewModels;

namespace Converter.UI.Controls;

public class QueueItemsListPanel : Panel
{
    private readonly FlowLayoutPanel _itemsFlowPanel;
    private readonly ScrollBar _scrollBar;
    private readonly Dictionary<Guid, QueueItemControl> _controls = new();

    public event EventHandler<Guid>? MoveUpClicked;
    public event EventHandler<Guid>? MoveDownClicked;
    public event EventHandler<Guid>? StarToggled;
    public event EventHandler<Guid>? CancelClicked;
    public event EventHandler<(Guid Id, int Priority)>? PriorityChanged;

    public QueueItemsListPanel()
    {
        Size = new Size(800, 400);
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(5);

        _itemsFlowPanel = new FlowLayoutPanel
        {
            Location = new Point(5, 5),
            Size = new Size(Width - 25, Height - 10),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BorderStyle = BorderStyle.None
        };

        Controls.Add(_itemsFlowPanel);
    }

    public void UpdateItems(IList<QueueItemViewModel> items)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => UpdateItems(items)));
            return;
        }

        // Remove controls that are no longer needed
        var currentIds = items.Select(x => x.Id).ToHashSet();
        var toRemove = _controls.Keys.Where(id => !currentIds.Contains(id)).ToList();
        
        foreach (var id in toRemove)
        {
            if (_controls.TryGetValue(id, out var control))
            {
                _itemsFlowPanel.Controls.Remove(control);
                control.Dispose();
                _controls.Remove(id);
            }
        }

        // Add or update controls
        foreach (var item in items)
        {
            if (!_controls.TryGetValue(item.Id, out var control))
            {
                control = new QueueItemControl(item);
                control.MoveUpClicked += (s, id) => MoveUpClicked?.Invoke(s, id);
                control.MoveDownClicked += (s, id) => MoveDownClicked?.Invoke(s, id);
                control.StarToggled += (s, id) => StarToggled?.Invoke(s, id);
                control.CancelClicked += (s, id) => CancelClicked?.Invoke(s, id);
                control.PriorityChanged += (s, args) => PriorityChanged?.Invoke(s, args);

                _controls[item.Id] = control;
                _itemsFlowPanel.Controls.Add(control);
            }
            else
            {
                control.UpdateDisplay();
            }
        }
    }

    public void ClearAll()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(ClearAll));
            return;
        }

        foreach (var control in _controls.Values)
        {
            control.Dispose();
        }
        _controls.Clear();
        _itemsFlowPanel.Controls.Clear();
    }
}