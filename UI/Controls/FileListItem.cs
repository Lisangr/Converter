using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Models;

namespace Converter.UI.Controls
{
    public class FileListItem : Panel
    {
        private PictureBox _thumbnail = null!;
        private Label _fileName = null!;
        private Label _fileSize = null!;
        private Label _duration = null!;
        private Button _removeButton = null!;
        private ProgressBar _progressBar = null!;
        private ContextMenuStrip _contextMenu = null!;
        private ToolStripMenuItem _refreshThumbnailItem = null!;
        private ToolStripMenuItem _thumbnailPositionItem = null!;
        
        private string _filePath;
        private bool _isConverting;
        private long _fileSizeBytes;
        
        public string FilePath 
        { 
            get => _filePath; 
            set 
            {
                _filePath = value;
                _fileName.Text = Path.GetFileName(value);
                UpdateFileInfo();
            }
        }

        public string FileName
        {
            get => _fileName.Text;
            set => _fileName.Text = value;
        }

        public long FileSize
        {
            get => _fileSizeBytes;
            set
            {
                _fileSizeBytes = value;
                UpdateFileInfo();
            }
        }

        public Action<string> OnRemoveClicked { get; set; }
        
        public Image Thumbnail 
        { 
            get => _thumbnail.Image; 
            set => _thumbnail.Image = value;
        }
        
        public int Progress 
        { 
            get => _progressBar.Value; 
            set 
            {
                _progressBar.Value = Math.Min(100, Math.Max(0, value));
                _progressBar.Visible = _isConverting;
            }
        }
        
        public bool IsConverting
        {
            get => _isConverting;
            set
            {
                _isConverting = value;
                _progressBar.Visible = value;
                _removeButton.Enabled = !value;
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateSelectionVisual();
            }
        }
        
        public event EventHandler<EventArgs>? RemoveClicked;
        public event EventHandler<EventArgs>? DoubleClicked;
        public event EventHandler<ThumbnailPositionEventArgs>? RefreshThumbnailRequested;
        public event EventHandler<EventArgs>? SelectionChanged;
        
        private readonly EventHandler<Theme> _themeChangedHandler;
        private readonly IThemeService _themeService;

        public FileListItem(string filePath, IThemeService themeService)
        {
            _filePath = filePath;
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _themeChangedHandler = (s, theme) => ApplyTheme(theme);
            InitializeComponents();
            UpdateFileInfo();
            ApplyTheme(_themeService.CurrentTheme);
            _themeService.ThemeChanged += _themeChangedHandler;
        }
        
        private void InitializeComponents()
        {
            // Layout:
            // [Thumbnail 120x90] [FileName    ] [Remove]
            //                     [FileSize    ] [Progress]
            //                     [Duration    ]
            
            this.Size = new Size(600, 100);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Padding = new Padding(5);
            this.BackColor = Color.White;
            this.Cursor = Cursors.Hand;
            
            // Thumbnail
            _thumbnail = new PictureBox
            {
                Location = new Point(5, 5),
                Size = new Size(120, 90),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            _thumbnail.DoubleClick += (s, e) => DoubleClicked?.Invoke(this, e);
            
            // File name
            _fileName = new Label
            {
                Location = new Point(135, 10),
                Size = new Size(350, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Text = Path.GetFileName(_filePath)
            };
            
            // File size
            _fileSize = new Label
            {
                Location = new Point(135, 35),
                Size = new Size(200, 18),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(102, 102, 102)
            };
            
            // Duration
            _duration = new Label
            {
                Location = new Point(135, 55),
                Size = new Size(200, 18),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(102, 102, 102)
            };
            
            // Progress bar
            _progressBar = new ProgressBar
            {
                Location = new Point(135, 75),
                Size = new Size(350, 15),
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };
            
            // Remove button
            _removeButton = new Button
            {
                Location = new Point(560, 35),
                Size = new Size(30, 30),
                Text = "✕",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            _removeButton.FlatAppearance.BorderSize = 0;
            _removeButton.Click += (s, e) => 
            {
                if (OnRemoveClicked != null)
                {
                    OnRemoveClicked(_filePath);
                }
                else
                {
                    RemoveClicked?.Invoke(this, e);
                }
            };
            
            // Context menu
            _contextMenu = new ContextMenuStrip();
            _refreshThumbnailItem = new ToolStripMenuItem("Обновить превью");
            _refreshThumbnailItem.Click += (s, e) => RefreshThumbnailRequested?.Invoke(this, new ThumbnailPositionEventArgs(TimeSpan.Zero));
            
            _thumbnailPositionItem = new ToolStripMenuItem("Выбрать кадр для превью");
            _thumbnailPositionItem.DropDownItems.Add("0%", null, (s, e) => RefreshThumbnailRequested?.Invoke(this, new ThumbnailPositionEventArgs(TimeSpan.Zero)));
            _thumbnailPositionItem.DropDownItems.Add("10%", null, (s, e) => RefreshThumbnailRequested?.Invoke(this, new ThumbnailPositionEventArgs(TimeSpan.Zero)));
            _thumbnailPositionItem.DropDownItems.Add("25%", null, (s, e) => RefreshThumbnailRequested?.Invoke(this, new ThumbnailPositionEventArgs(TimeSpan.Zero)));
            _thumbnailPositionItem.DropDownItems.Add("50%", null, (s, e) => RefreshThumbnailRequested?.Invoke(this, new ThumbnailPositionEventArgs(TimeSpan.Zero)));
            _thumbnailPositionItem.DropDownItems.Add("75%", null, (s, e) => RefreshThumbnailRequested?.Invoke(this, new ThumbnailPositionEventArgs(TimeSpan.Zero)));
            _thumbnailPositionItem.DropDownItems.Add("90%", null, (s, e) => RefreshThumbnailRequested?.Invoke(this, new ThumbnailPositionEventArgs(TimeSpan.Zero)));
            
            _contextMenu.Items.Add(_refreshThumbnailItem);
            _contextMenu.Items.Add(_thumbnailPositionItem);
            
            this.ContextMenuStrip = _contextMenu;
            
            // Add controls
            this.Controls.AddRange(new Control[] { 
                _thumbnail, _fileName, _fileSize, _duration, _removeButton, _progressBar 
            });
            
            // Double click event for the whole panel
            this.DoubleClick += (s, e) => DoubleClicked?.Invoke(this, e);

            // Click event for selection
            this.Click += (s, e) => ToggleSelection();
        }
        
        private void UpdateFileInfo()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var fileInfo = new FileInfo(_filePath);
                    var fileSize = FormatFileSize(fileInfo.Length);
                    _fileSize.Text = $"Размер: {fileSize}";
                    
                    // Try to get video duration (this would require FFmpeg analysis)
                    // For now, just show file extension
                    var extension = Path.GetExtension(_filePath).ToUpper();
                    _duration.Text = $"Тип: {extension}";
                }
                else
                {
                    _fileSize.Text = "Файл не найден";
                    _duration.Text = "";
                }
            }
            catch
            {
                _fileSize.Text = "Ошибка чтения файла";
                _duration.Text = "";
            }
        }
        
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        
        public void SetVideoDuration(TimeSpan duration)
        {
            _duration.Text = $"Длительность: {FormatDuration(duration)}";

            // Update context menu items with actual duration
            if (duration.TotalSeconds > 0)   
            {
                var items = _thumbnailPositionItem.DropDownItems;
                items[1].Tag = duration.TotalSeconds * 0.1; // 10%
                items[2].Tag = duration.TotalSeconds * 0.25; // 25%
                items[3].Tag = duration.TotalSeconds * 0.5; // 50%
                items[4].Tag = duration.TotalSeconds * 0.75; // 75%
                items[5].Tag = duration.TotalSeconds * 0.9; // 90%
                
                // Update click handlers
                for (int i = 1; i < items.Count; i++)
                {
                    var item = items[i];
                    item.Click -= (s, e) => RefreshThumbnailRequested?.Invoke(this, new ThumbnailPositionEventArgs(TimeSpan.Zero));
                    item.Click += (s, e) => {
                        if (item.Tag is double seconds)
                        {
                            RefreshThumbnailRequested?.Invoke(this, new ThumbnailPositionEventArgs(TimeSpan.FromSeconds(seconds)));
                        }
                    };
                }
            }
        }
        
        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            else
                return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw selection border if selected
            if (_isSelected)
            {
                using var pen = new Pen(Color.FromArgb(0, 120, 215), 2);
                e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 2, this.Height - 2);
            }
            // Draw subtle border when mouse is over (only if not selected)
            else if (this.ClientRectangle.Contains(this.PointToClient(Cursor.Position)))
            {
                using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        private void ToggleSelection()
        {
            IsSelected = !_isSelected;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateSelectionVisual()
        {
            // Update visual appearance for selection
            this.Invalidate(); // Force redraw
        }
        
        public void ApplyTheme(Theme theme)
        {
            BackColor = theme["Surface"];
            ForeColor = theme["TextPrimary"];
            _fileName.ForeColor = theme["TextPrimary"];
            _fileSize.ForeColor = theme["TextSecondary"];
            _duration.ForeColor = theme["TextSecondary"];
            _progressBar.ForeColor = theme["Accent"];
            _thumbnail.BackColor = theme["BackgroundSecondary"];
            _thumbnail.BorderStyle = BorderStyle.FixedSingle;
            _removeButton.BackColor = theme["Error"];
            _removeButton.ForeColor = Color.White;
            _removeButton.FlatStyle = FlatStyle.Flat;
            _removeButton.FlatAppearance.BorderSize = 0;
            _progressBar.BackColor = theme["Border"];
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _themeService.ThemeChanged -= _themeChangedHandler;
                _thumbnail?.Image?.Dispose();
                _contextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    
    public class ThumbnailPositionEventArgs : EventArgs
    {
        public TimeSpan Position { get; }
        
        public ThumbnailPositionEventArgs(TimeSpan position)
        {
            Position = position;
        }
    }
}
