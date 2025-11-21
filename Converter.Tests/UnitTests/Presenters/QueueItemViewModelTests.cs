using System;
using System.Collections.Generic;
using System.ComponentModel;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Presenters;

public class QueueItemViewModelTests
{
    [Fact]
    public void FromModel_ShouldMapAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var item = new QueueItem
        {
            Id = id,
            FilePath = "C:/videos/test.mp4",
            FileSizeBytes = 12345,
            Status = ConversionStatus.Pending,
            Progress = 10,
            ErrorMessage = "err",
            OutputPath = "C:/videos/out.mp4",
            OutputFileSizeBytes = 999,
            IsStarred = true,
            Priority = 5
        };

        // Act
        var vm = QueueItemViewModel.FromModel(item);

        // Assert
        vm.Id.Should().Be(id);
        vm.FilePath.Should().Be(item.FilePath);
        vm.FileName.Should().Be("test.mp4");
        vm.FileSizeBytes.Should().Be(item.FileSizeBytes);
        vm.Status.Should().Be(item.Status);
        vm.Progress.Should().Be(item.Progress);
        vm.ErrorMessage.Should().Be(item.ErrorMessage);
        vm.OutputPath.Should().Be(item.OutputPath);
        vm.OutputFileSizeBytes.Should().Be(item.OutputFileSizeBytes);
        vm.IsStarred.Should().BeTrue();
        vm.Priority.Should().Be(item.Priority);
    }

    [Fact]
    public void FromModel_WithNullItem_ShouldThrow()
    {
        // Act
        Action act = () => QueueItemViewModel.FromModel(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateFromModel_ShouldCopyMutableProperties()
    {
        // Arrange
        var vm = new QueueItemViewModel();
        var item = new QueueItem
        {
            Status = ConversionStatus.Completed,
            Progress = 100,
            ErrorMessage = "done",
            OutputPath = "out.mp4",
            OutputFileSizeBytes = 1000,
            IsStarred = true,
            Priority = 10
        };

        // Act
        vm.UpdateFromModel(item);

        // Assert
        vm.Status.Should().Be(item.Status);
        vm.Progress.Should().Be(item.Progress);
        vm.ErrorMessage.Should().Be(item.ErrorMessage);
        vm.OutputPath.Should().Be(item.OutputPath);
        vm.OutputFileSizeBytes.Should().Be(item.OutputFileSizeBytes);
        vm.IsStarred.Should().Be(item.IsStarred);
        vm.Priority.Should().Be(item.Priority);
    }

    [Fact]
    public void PropertySetters_ShouldRaisePropertyChanged()
    {
        // Arrange
        var vm = new QueueItemViewModel();
        var changed = new List<string>();
        ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
            {
                changed.Add(e.PropertyName);
            }
        };

        // Act
        vm.IsSelected = true;
        vm.Status = ConversionStatus.Processing;
        vm.Progress = 50;
        vm.ErrorMessage = "x";
        vm.OutputPath = "out.mp4";
        vm.OutputFileSizeBytes = 123;
        vm.IsStarred = true;
        vm.Priority = 7;

        // Assert
        changed.Should().Contain(new[]
        {
            nameof(QueueItemViewModel.IsSelected),
            nameof(QueueItemViewModel.Status),
            nameof(QueueItemViewModel.Progress),
            nameof(QueueItemViewModel.ErrorMessage),
            nameof(QueueItemViewModel.OutputPath),
            nameof(QueueItemViewModel.OutputFileSizeBytes),
            nameof(QueueItemViewModel.IsStarred),
            nameof(QueueItemViewModel.Priority)
        });
    }
}
