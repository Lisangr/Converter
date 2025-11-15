using System;

namespace Converter.Models
{
    public class ConversionEstimate
    {
        public long InputFileSizeBytes { get; set; }
        public long EstimatedOutputSizeBytes { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public double CompressionRatio { get; set; }      // 0.0 - 1.0
        public long SpaceSavedBytes { get; set; }

        // UI helpers
        public string InputSizeFormatted => FormatBytes(InputFileSizeBytes);
        public string OutputSizeFormatted => FormatBytes(EstimatedOutputSizeBytes);
        public string SpaceSavedFormatted => FormatBytes(SpaceSavedBytes);
        public string DurationFormatted => FormatDuration(EstimatedDuration);
        public int CompressionPercent => (int)Math.Round(CompressionRatio * 100);
        public int SavingsPercent => 100 - CompressionPercent;

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024d;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours} ч {(ts.Minutes)} мин";
            if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes} мин {ts.Seconds} сек";
            return $"{ts.Seconds} сек";
        }
    }
}
