using System.Collections.Generic;
using System.ComponentModel;
using Converter.Application.ViewModels;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Presenters;

public class BindingListAdapterTests
{
    private class NotifyingItem : INotifyPropertyChanged
    {
        private string _value = string.Empty;
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }
    }

    [Fact]
    public void Adapter_ShouldReflectUnderlyingCollection()
    {
        // Arrange
        var seed = new List<NotifyingItem> { new(), new() };

        // Act
        var adapter = new BindingListAdapter<NotifyingItem>(seed);
        adapter.Add(new NotifyingItem());

        // Assert
        seed.Should().HaveCount(3);
        adapter.Should().HaveCount(3);
    }

    [Fact]
    public void Adapter_ShouldNotifyOnChanges()
    {
        // Arrange
        var adapter = new BindingListAdapter<NotifyingItem>();
        ListChangedEventArgs? args = null;
        adapter.ListChanged += (_, e) => args = e;

        // Act
        adapter.Add(new NotifyingItem());

        // Assert
        args.Should().NotBeNull();
        args!.ListChangedType.Should().Be(ListChangedType.ItemAdded);
        args.NewIndex.Should().Be(0);
    }

    [Fact]
    public void Adapter_ShouldReactToItemPropertyChanges()
    {
        // Arrange
        var adapter = new BindingListAdapter<NotifyingItem>();
        var item = new NotifyingItem();
        adapter.Add(item);
        ListChangedEventArgs? args = null;
        adapter.ListChanged += (_, e) => args = e;

        // Act
        item.Value = "updated";

        // Assert
        args.Should().NotBeNull();
        args!.ListChangedType.Should().Be(ListChangedType.ItemChanged);
        args.NewIndex.Should().Be(0);
    }
}
