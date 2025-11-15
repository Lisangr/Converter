using System.Drawing;

namespace Converter.Models
{
    /// <summary>
    /// Модель цветовой темы приложения.
    /// </summary>
    public class Theme
    {
        public string Name { get; set; } = "Light";
        public Color BackgroundPrimary { get; set; }
        public Color BackgroundSecondary { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextSecondary { get; set; }
        public Color Accent { get; set; }
        public Color Border { get; set; }
        public Color Success { get; set; }
        public Color Error { get; set; }
        public Color Warning { get; set; }

        public static Theme Light => new Theme
        {
            Name = "Light",
            BackgroundPrimary = Color.White,
            BackgroundSecondary = Color.FromArgb(240, 240, 240),
            TextPrimary = Color.Black,
            TextSecondary = Color.FromArgb(100, 100, 100),
            Accent = Color.FromArgb(0, 120, 215),
            Border = Color.FromArgb(200, 200, 200),
            Success = Color.FromArgb(16, 124, 16),
            Error = Color.FromArgb(232, 17, 35),
            Warning = Color.FromArgb(255, 185, 0)
        };

        public static Theme Dark => new Theme
        {
            Name = "Dark",
            BackgroundPrimary = Color.FromArgb(32, 32, 32),
            BackgroundSecondary = Color.FromArgb(45, 45, 45),
            TextPrimary = Color.White,
            TextSecondary = Color.FromArgb(180, 180, 180),
            Accent = Color.FromArgb(0, 120, 212),
            Border = Color.FromArgb(60, 60, 60),
            Success = Color.FromArgb(16, 185, 16),
            Error = Color.FromArgb(255, 67, 54),
            Warning = Color.FromArgb(255, 193, 7)
        };
    }
}

