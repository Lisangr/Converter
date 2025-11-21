using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Converter.Domain.Models;
using Converter.UI.Dialogs;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Dialogs;

public class NotificationSettingsFormTests
{
    [Fact]
    public void NotificationSettingsForm_ShouldLoadPreferences()
    {
        RunSta(() =>
        {
            // Arrange
            var settings = new NotificationOptions
            {
                DesktopNotificationsEnabled = true,
                ShowProgressNotifications = true,
                SoundEnabled = true,
                UseCustomSound = true,
                CustomSoundPath = "custom.wav"
            };

            using var form = new NotificationSettingsForm(settings);

            // Assert
            GetField<CheckBox>(form, "_chkDesktopNotifications").Checked.Should().BeTrue();
            GetField<CheckBox>(form, "_chkProgressNotifications").Checked.Should().BeTrue();
            GetField<CheckBox>(form, "_chkSoundEnabled").Checked.Should().BeTrue();
            GetField<CheckBox>(form, "_chkCustomSound").Checked.Should().BeTrue();
            GetField<TextBox>(form, "_txtCustomSoundPath").Text.Should().Be("custom.wav");
        });
    }

    [Fact]
    public void NotificationSettingsForm_ShouldSavePreferences()
    {
        RunSta(() =>
        {
            // Arrange
            var settings = new NotificationOptions();
            using var form = new NotificationSettingsForm(settings);
            var desktop = GetField<CheckBox>(form, "_chkDesktopNotifications");
            var progress = GetField<CheckBox>(form, "_chkProgressNotifications");
            var sound = GetField<CheckBox>(form, "_chkSoundEnabled");
            var custom = GetField<CheckBox>(form, "_chkCustomSound");
            var customPath = GetField<TextBox>(form, "_txtCustomSoundPath");

            desktop.Checked = true;
            progress.Checked = true;
            sound.Checked = true;
            custom.Checked = true;
            customPath.Text = "tone.wav";

            // Act
            InvokeMethod(form, "SaveSettings");

            // Assert
            form.Settings.DesktopNotificationsEnabled.Should().BeTrue();
            form.Settings.ShowProgressNotifications.Should().BeTrue();
            form.Settings.SoundEnabled.Should().BeTrue();
            form.Settings.UseCustomSound.Should().BeTrue();
            form.Settings.CustomSoundPath.Should().Be("tone.wav");
        });
    }

    [Fact]
    public void NotificationSettingsForm_ShouldValidateInput()
    {
        RunSta(() =>
        {
            // Arrange
            var settings = new NotificationOptions();
            using var form = new NotificationSettingsForm(settings);
            var custom = GetField<CheckBox>(form, "_chkCustomSound");
            custom.Checked = true;
            form.DialogResult = DialogResult.OK;

            // Act
            InvokeMethod(form, "BtnSave_Click", form, EventArgs.Empty);

            // Assert
            form.DialogResult.Should().Be(DialogResult.None);
            form.Settings.UseCustomSound.Should().BeFalse();
        });
    }

    private static T GetField<T>(object target, string name)
    {
        return (T)(target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(target) ?? throw new InvalidOperationException());
    }

    private static void InvokeMethod(object target, string name, params object?[]? args)
    {
        target.GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?
            .Invoke(target, args);
    }

    private static void RunSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        })
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw exception;
        }
    }
}
