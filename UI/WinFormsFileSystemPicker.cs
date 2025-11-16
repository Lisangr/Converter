using System;
using System.Windows.Forms;
using Converter.Application.Abstractions;

namespace Converter.UI
{
    public sealed class WinFormsFilePicker : IFilePicker
    {
        public string[] PickFiles(string title, string filter)
        {
            using var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = true
            };

            return dlg.ShowDialog() == DialogResult.OK
                ? dlg.FileNames
                : Array.Empty<string>();
        }
    }

    public sealed class WinFormsFolderPicker : IFolderPicker
    {
        public string? PickFolder(string description)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            return dlg.ShowDialog() == DialogResult.OK
                ? dlg.SelectedPath
                : null;
        }
    }
}
