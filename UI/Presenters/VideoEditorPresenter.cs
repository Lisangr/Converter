using Converter.Services;
using Converter.UI;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xabe.FFmpeg;

namespace Converter.UI.Presenters
{
    public class VideoEditorPresenter
    {
        private readonly IVideoEditorView _view;
        private readonly IVideoProcessingService _videoProcessingService;

        private string _originalVideoPath; // Store the original path
        private string _currentEditedVideoPath; // Path of the currently edited video (could be temp)
        private IMediaInfo? _mediaInfo;

        public VideoEditorPresenter(IVideoEditorView view, IVideoProcessingService videoProcessingService, string videoPath)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _videoProcessingService = videoProcessingService ?? throw new ArgumentNullException(nameof(videoProcessingService));
            _originalVideoPath = videoPath ?? throw new ArgumentNullException(nameof(videoPath));
            _currentEditedVideoPath = _originalVideoPath;

            _view.VideoLoaded += OnVideoLoaded;
            _view.TrimRequested += OnTrimRequested;
            _view.CropRequested += OnCropRequested;
            _view.ExportRequested += OnExportRequested;
            _view.PlayerPositionChanged += OnPlayerPositionChanged;

            // Set initial view state
            _view.CurrentVideoPath = _originalVideoPath;
            _view.IsExporting = false;
        }

        public async Task LoadInitialVideo()
        {
            try
            {
                _mediaInfo = await _videoProcessingService.GetMediaInfoAsync(_originalVideoPath);
                _view.MediaInfo = _mediaInfo;
                _view.LoadVideo(_originalVideoPath);
                _view.UpdateTrimPanel(_mediaInfo.Duration);
                _view.UpdateCropPanel(new Size(_mediaInfo.VideoStreams.FirstOrDefault()?.Width ?? 0, _mediaInfo.VideoStreams.FirstOrDefault()?.Height ?? 0));
                _view.SetCurrentCropRectangle(new Rectangle(0, 0, _mediaInfo.VideoStreams.FirstOrDefault()?.Width ?? 0, _mediaInfo.VideoStreams.FirstOrDefault()?.Height ?? 0));
            }
            catch (Exception ex)
            {
                _view.ShowMessage($"Ошибка загрузки видео: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnVideoLoaded(object? sender, string filePath)
        {
            // This event could be used if the VideoPlayerPanel itself notifies when it has loaded a new video.
            // For now, the presenter directly controls LoadVideo.
            // If the video player reloads content independently, this could be useful.
            _mediaInfo = await _videoProcessingService.GetMediaInfoAsync(filePath);
            _view.MediaInfo = _mediaInfo;
            _view.UpdateTrimPanel(_mediaInfo.Duration);
            _view.UpdateCropPanel(new Size(_mediaInfo.VideoStreams.FirstOrDefault()?.Width ?? 0, _mediaInfo.VideoStreams.FirstOrDefault()?.Height ?? 0));
            _view.SetCurrentCropRectangle(new Rectangle(0, 0, _mediaInfo.VideoStreams.FirstOrDefault()?.Width ?? 0, _mediaInfo.VideoStreams.FirstOrDefault()?.Height ?? 0));
        }

        private async void OnTrimRequested(object? sender, EventArgs e)
        {
            _view.ShowLoadingState();
            try
            {
                string newPath = await _videoProcessingService.TrimVideoAsync(
                    _originalVideoPath, // Always trim from the original for now, subsequent edits will chain
                    _view.TrimStartTime,
                    _view.TrimEndTime,
                    percentage => _view.UpdateProgress(percentage)
                );
                _currentEditedVideoPath = newPath;
                _mediaInfo = await _videoProcessingService.GetMediaInfoAsync(_currentEditedVideoPath);
                _view.MediaInfo = _mediaInfo; // Update MediaInfo in view
                _view.LoadVideo(_currentEditedVideoPath);
                _view.ShowMessage("Видео успешно обрезано для предпросмотра!", "Обрезка применена", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _view.ShowMessage($"Ошибка при обрезке видео: {ex.Message}", "Ошибка обрезки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Reset to original if trimming fails or previous successful trimmed video
                _currentEditedVideoPath = _originalVideoPath;
                _view.LoadVideo(_currentEditedVideoPath); // Reload original or last good state
            }
            finally
            {
                _view.HideLoadingState();
            }
        }

        private async void OnCropRequested(object? sender, EventArgs e)
        {
            _view.ShowLoadingState();
            try
            {
                string newPath = await _videoProcessingService.CropVideoAsync(
                    _currentEditedVideoPath, // Crop the currently edited video
                    _view.GetCurrentCropRectangle().Size,
                    _view.GetCurrentCropRectangle().Location,
                    percentage => _view.UpdateProgress(percentage)
                );
                _currentEditedVideoPath = newPath;
                _mediaInfo = await _videoProcessingService.GetMediaInfoAsync(_currentEditedVideoPath);
                _view.MediaInfo = _mediaInfo; // Update MediaInfo in view
                _view.LoadVideo(_currentEditedVideoPath);
                _view.ShowMessage("Кадрирование успешно применено для предпросмотра!", "Кадрирование применено", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _view.ShowMessage($"Ошибка при кадрировании видео: {ex.Message}", "Ошибка кадрирования", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Reset to original if cropping fails or previous successful trimmed video
                _currentEditedVideoPath = _originalVideoPath;
                _view.LoadVideo(_currentEditedVideoPath); // Reload original or last good state
            }
            finally
            {
                _view.HideLoadingState();
            }
        }

        private async void OnExportRequested(object? sender, EventArgs e)
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "MP4 Video|*.mp4|All Files|*.*",
                FileName = System.IO.Path.GetFileNameWithoutExtension(_originalVideoPath) + "_edited.mp4"
            };

            if (_view is Form form && saveDialog.ShowDialog(form) != DialogResult.OK) // Pass owner form to dialog
            {
                return;
            }

            _view.ShowLoadingState();
            try
            {
                var videoFilterGraph = _view.GetVideoEffectsFilter();
                await _videoProcessingService.ExportVideoAsync(
                    _currentEditedVideoPath,
                    saveDialog.FileName,
                    videoFilterGraph,
                    percentage => _view.UpdateProgress(percentage)
                );
                _view.ShowMessage("Видео успешно экспортировано!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _view.ShowMessage($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _view.HideLoadingState();
            }
        }

        private void OnPlayerPositionChanged(object? sender, TimeSpan position)
        {
            // The presenter can react to player position changes, e.g., to update a progress bar or other time-sensitive UI elements.
            // For this refactoring, we just pass it through, but it's here for future expansion.
            _view.SetPlayerPosition(position);
        }

        public void Cleanup()
        {
            _videoProcessingService.CleanupTemporaryFiles();
        }
    }
}