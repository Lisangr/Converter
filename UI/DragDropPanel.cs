using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Converter.Application.Abstractions;

namespace Converter.UI
{
    /// <summary>
    /// Панель с поддержкой Drag & Drop и отображением миниатюр.
    /// </summary>
    public class DragDropPanel : FlowLayoutPanel
    {
        private readonly Label _placeholderLabel;
        private readonly List<VideoThumbnailControl> _thumbnails = new();
        private readonly IThemeService _themeService;

        public event EventHandler<string[]>? FilesAdded;
        public event EventHandler<string>? FileRemoved;

        public DragDropPanel(IThemeService themeService)
        {
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            AllowDrop = true;
            AutoScroll = true;
            BackColor = Color.FromArgb(240, 240, 240);
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(10);
            WrapContents = true;
            FlowDirection = FlowDirection.LeftToRight;

            _placeholderLabel = new Label
            {
                Text = "📁 Перетащите видео файлы сюда\nили нажмите для выбора",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = Color.Gray,
                Cursor = Cursors.Hand
            };
            _placeholderLabel.Click += (_, _) => SelectFiles();
            Controls.Add(_placeholderLabel);

            DragEnter += OnDragEnter;
            DragOver += OnDragOver;
            DragLeave += (_, _) => BackColor = Color.FromArgb(240, 240, 240);
            DragDrop += OnDragDrop;
        }

        public void AddFiles(IEnumerable<string> filePaths, bool notify = true)
        {
            var addedFiles = new List<string>();

            foreach (var filePath in filePaths)
            {
                if (!IsVideoFile(filePath))
                {
                    continue;
                }

                if (_thumbnails.Any(t => string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var thumbnail = new VideoThumbnailControl(filePath, _themeService);
                thumbnail.RemoveRequested += (_, _) => RemoveThumbnail(thumbnail);
                _thumbnails.Add(thumbnail);
                Controls.Add(thumbnail);
                addedFiles.Add(filePath);
            }

            UpdatePlaceholderVisibility();

            if (notify && addedFiles.Count > 0)
            {
                FilesAdded?.Invoke(this, addedFiles.ToArray());
            }
        }

        public void RemoveFile(string filePath, bool notify = false)
        {
            var thumbnail = _thumbnails.FirstOrDefault(t =>
                string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            if (thumbnail != null)
            {
                RemoveThumbnail(thumbnail, notify);
            }
        }

        public void ClearFiles(bool notify = false)
        {
            foreach (var thumbnail in _thumbnails.ToList())
            {
                RemoveThumbnail(thumbnail, notify);
            }
        }

        public string[] GetFilePaths() => _thumbnails.Select(t => t.FilePath).ToArray();

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
                BackColor = Color.FromArgb(220, 240, 255);
            }
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void OnDragDrop(object? sender, DragEventArgs e)
        {
            BackColor = Color.FromArgb(240, 240, 240);

            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
            {
                var videoFiles = files.Where(IsVideoFile).ToArray();
                if (videoFiles.Length > 0)
                {
                    AddFiles(videoFiles);
                }
            }
        }

        private void SelectFiles()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.3gp;*.ts;*.ogv|All Files|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                AddFiles(openFileDialog.FileNames);
            }
        }

        private void RemoveThumbnail(VideoThumbnailControl thumbnail, bool notify = true)
        {
            Controls.Remove(thumbnail);
            _thumbnails.Remove(thumbnail);
            thumbnail.Dispose();

            if (notify)
            {
                FileRemoved?.Invoke(this, thumbnail.FilePath);
            }

            UpdatePlaceholderVisibility();
        }

        private void UpdatePlaceholderVisibility()
        {
            _placeholderLabel.Visible = _thumbnails.Count == 0;
        }

        private static bool IsVideoFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            string[] videoExtensions =
            {
                ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".3gp", ".ts", ".ogv"
            };

            var extension = Path.GetExtension(filePath);
            return videoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}
