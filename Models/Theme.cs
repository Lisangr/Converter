using System;
using System.Collections.Generic;
using System.Drawing;

namespace Converter.Models
{
    /// <summary>
    /// Расширенная модель цветовой темы приложения.
    /// </summary>
    public class Theme
    {
        public string Name { get; set; } = "light";
        public string DisplayName { get; set; } = "";
        public Dictionary<string, Color> Colors { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public Color this[string key] => Colors.TryGetValue(key, out var color) ? color : Color.Magenta;

        public static Theme Light => new Theme
        {
            Name = "light",
            DisplayName = "Светлая",
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["Background"] = Color.White,
                ["BackgroundSecondary"] = Color.FromArgb(240, 240, 240),
                ["Surface"] = Color.FromArgb(250, 250, 250),
                ["TextPrimary"] = Color.Black,
                ["TextSecondary"] = Color.FromArgb(100, 100, 100),
                ["Accent"] = Color.FromArgb(0, 120, 215),
                ["AccentHover"] = Color.FromArgb(0, 100, 195),
                ["Border"] = Color.FromArgb(200, 200, 200),
                ["Success"] = Color.FromArgb(16, 124, 16),
                ["Error"] = Color.FromArgb(232, 17, 35),
                ["Warning"] = Color.FromArgb(255, 185, 0),
                ["Info"] = Color.FromArgb(0, 120, 215)
            }
        };

        public static Theme Dark => new Theme
        {
            Name = "dark",
            DisplayName = "Темная",
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["Background"] = Color.FromArgb(25, 25, 25),
                ["BackgroundSecondary"] = Color.FromArgb(35, 35, 35),
                ["Surface"] = Color.FromArgb(45, 45, 45),
                ["TextPrimary"] = Color.White,
                ["TextSecondary"] = Color.FromArgb(180, 180, 180),
                ["Accent"] = Color.FromArgb(0, 120, 212),
                ["AccentHover"] = Color.FromArgb(0, 140, 232),
                ["Border"] = Color.FromArgb(60, 60, 60),
                ["Success"] = Color.FromArgb(16, 185, 16),
                ["Error"] = Color.FromArgb(255, 67, 54),
                ["Warning"] = Color.FromArgb(255, 193, 7),
                ["Info"] = Color.FromArgb(33, 150, 243)
            }
        };

        public static Theme Midnight => new Theme
        {
            Name = "midnight",
            DisplayName = "Полночь (AMOLED)",
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["Background"] = Color.FromArgb(0, 0, 0),
                ["BackgroundSecondary"] = Color.FromArgb(15, 15, 15),
                ["Surface"] = Color.FromArgb(25, 25, 25),
                ["TextPrimary"] = Color.FromArgb(220, 220, 220),
                ["TextSecondary"] = Color.FromArgb(150, 150, 150),
                ["Accent"] = Color.FromArgb(100, 181, 246),
                ["AccentHover"] = Color.FromArgb(120, 191, 255),
                ["Border"] = Color.FromArgb(50, 50, 50),
                ["Success"] = Color.FromArgb(76, 175, 80),
                ["Error"] = Color.FromArgb(244, 67, 54),
                ["Warning"] = Color.FromArgb(255, 235, 59),
                ["Info"] = Color.FromArgb(33, 150, 243)
            }
        };

        public static Theme NordLight => new Theme
        {
            Name = "nord_light",
            DisplayName = "Nord Light",
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["Background"] = Color.FromArgb(236, 239, 244),
                ["BackgroundSecondary"] = Color.FromArgb(229, 233, 240),
                ["Surface"] = Color.FromArgb(216, 222, 233),
                ["TextPrimary"] = Color.FromArgb(46, 52, 64),
                ["TextSecondary"] = Color.FromArgb(76, 86, 106),
                ["Accent"] = Color.FromArgb(94, 129, 172),
                ["AccentHover"] = Color.FromArgb(81, 115, 158),
                ["Border"] = Color.FromArgb(200, 207, 219),
                ["Success"] = Color.FromArgb(163, 190, 140),
                ["Error"] = Color.FromArgb(191, 97, 106),
                ["Warning"] = Color.FromArgb(235, 203, 139),
                ["Info"] = Color.FromArgb(136, 192, 208)
            }
        };

        public static Theme NordDark => new Theme
        {
            Name = "nord_dark",
            DisplayName = "Nord Dark",
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["Background"] = Color.FromArgb(46, 52, 64),
                ["BackgroundSecondary"] = Color.FromArgb(59, 66, 82),
                ["Surface"] = Color.FromArgb(67, 76, 94),
                ["TextPrimary"] = Color.FromArgb(236, 239, 244),
                ["TextSecondary"] = Color.FromArgb(216, 222, 233),
                ["Accent"] = Color.FromArgb(136, 192, 208),
                ["AccentHover"] = Color.FromArgb(143, 188, 187),
                ["Border"] = Color.FromArgb(76, 86, 106),
                ["Success"] = Color.FromArgb(163, 190, 140),
                ["Error"] = Color.FromArgb(191, 97, 106),
                ["Warning"] = Color.FromArgb(235, 203, 139),
                ["Info"] = Color.FromArgb(129, 161, 193)
            }
        };

        public static List<Theme> GetAllThemes()
        {
            return new List<Theme> { Light, Dark, Midnight, NordLight, NordDark };
        }
    }
}
