using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xabe.FFmpeg;

namespace Converter.UI
{
    public interface IVideoEditorView
    {
        // Properties to expose UI state and data
        string CurrentVideoPath { get; set; }
        IMediaInfo MediaInfo { get; set; }
        TimeSpan TrimStartTime { get; set; }
        TimeSpan TrimEndTime { get; set; }

        // Methods for Crop Panel
        void UpdateCropPanel(Size videoSize);
        Rectangle GetCurrentCropRectangle();
        void SetCurrentCropRectangle(Rectangle rect);

        bool IsExporting { get; set; } // To show/hide loading states

        // Methods to update UI
        void LoadVideo(string filePath);
        void UpdateTrimPanel(TimeSpan duration);
        void ShowLoadingState();
        void HideLoadingState();
        void ShowMessage(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon);
        void EnableExportButton(bool enable);
        void UpdateProgress(int percentage);
        void SetPlayerPosition(TimeSpan position);


        // Новые методы для получения настроек
        Converter.Domain.Models.AudioProcessingOptions GetAudioOptions();
        string? GetVideoEffectsFilter();
        
        // ... (остальные методы)
        // Events for user actions
        event EventHandler<string> VideoLoaded;
        event EventHandler TrimRequested;
        event EventHandler CropRequested;
        event EventHandler ExportRequested;
        event EventHandler<TimeSpan> PlayerPositionChanged;

        // New events for applying audio and effect settings
        event EventHandler AudioApplyRequested;
        event EventHandler EffectsApplyRequested;
    }
}