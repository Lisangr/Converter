using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Converter.UI
{
    public static class SubtitleParser
    {
        public static List<SubtitleItem> ParseSRT(string filePath)
        {
            var subtitles = new List<SubtitleItem>();
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            SubtitleItem? currentSubtitle = null;
            var textLines = new List<string>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (currentSubtitle != null && textLines.Count > 0)
                    {
                        currentSubtitle.Text = string.Join("\n", textLines);
                        subtitles.Add(currentSubtitle);
                        currentSubtitle = null;
                        textLines.Clear();
                    }
                    continue;
                }

                if (line.Contains("-->", StringComparison.Ordinal))
                {
                    var times = line.Split(new[] { "-->" }, StringSplitOptions.None);
                    if (times.Length == 2)
                    {
                        currentSubtitle = new SubtitleItem
                        {
                            StartTime = ParseSRTTime(times[0].Trim()),
                            EndTime = ParseSRTTime(times[1].Trim())
                        };
                    }
                }
                else if (currentSubtitle != null && !int.TryParse(line, out _))
                {
                    textLines.Add(line);
                }
            }

            if (currentSubtitle != null && textLines.Count > 0)
            {
                currentSubtitle.Text = string.Join("\n", textLines);
                subtitles.Add(currentSubtitle);
            }

            return subtitles;
        }

        private static TimeSpan ParseSRTTime(string time)
        {
            var normalized = time.Replace(',', '.');
            if (TimeSpan.TryParseExact(normalized, "hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            throw new FormatException($"Неверный формат времени: {time}");
        }

        public static void ExportToSRT(List<SubtitleItem> subtitles, string filePath)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < subtitles.Count; i++)
            {
                var sub = subtitles[i];
                sb.AppendLine((i + 1).ToString(CultureInfo.InvariantCulture));
                sb.AppendLine($"{FormatSRTTime(sub.StartTime)} --> {FormatSRTTime(sub.EndTime)}");
                sb.AppendLine(sub.Text);
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static string FormatSRTTime(TimeSpan time) => time.ToString("hh\\:mm\\:ss\\,fff", CultureInfo.InvariantCulture);
    }
}
