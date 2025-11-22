using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Converter;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.UI;

public class Form1RemoveSelectedFilesTests
{
    [Fact]
    public void OnRemoveSelectedFilesRequested_ShouldNotModifyQueueItemsBinding_WhenPresenterIsNull()
    {
        // Создаём Form1 без вызова конструктора, чтобы не тянуть DI/WinForms и не требовать STA
        var form = (Form1)FormatterServices.GetUninitializedObject(typeof(Form1));

        var items = new BindingList<QueueItemViewModel>
        {
            new() { Id = Guid.NewGuid(), FileName = "a.mp4", FilePath = "a.mp4", Status = ConversionStatus.Pending, IsSelected = true },
            new() { Id = Guid.NewGuid(), FileName = "b.mp4", FilePath = "b.mp4", Status = ConversionStatus.Pending, IsSelected = false }
        };

        // Устанавливаем приватное поле _queueItemsBinding через reflection
        var field = typeof(Form1).GetField("_queueItemsBinding", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.Should().NotBeNull();
        field!.SetValue(form, items);

        var method = typeof(Form1).GetMethod("OnRemoveSelectedFilesRequested", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var beforeIds = items.Select(i => i.Id).ToArray();

        // Act: вызываем обработчик с _mainPresenter == null (значение по умолчанию у неинициализированного объекта)
        method!.Invoke(form, new object?[] { null, EventArgs.Empty });

        // Assert: коллекция не изменилась
        items.Select(i => i.Id).Should().ContainInOrder(beforeIds);
        items.Count.Should().Be(2);
    }
}
