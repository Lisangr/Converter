// Удален - используйте Converter.Application.Services.PresetService

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Converter.Application.Models;

namespace Converter.Services
{
    /// <summary>
    /// Упрощённый фасад для работы с пресетами в WinForms-UI.
    /// Обеспечивает синхронные методы для получения пресетов и
    /// сохранения/загрузки их в JSON-файлы, поверх моделей Application.
    /// </summary>
    public class PresetService
    {
        /// <summary>
        /// Возвращает все доступные пресеты. На данном этапе возвращает
        /// только встроенные/пользовательские пресеты, загружаемые из
        /// конфигурации или внешних файлов (можно расширить позже).
        /// </summary>
        public List<PresetProfile> GetAllPresets()
        {
            try
            {
                var loader = new XmlPresetLoader();
                var presets = loader.LoadAllPresets();
                System.Diagnostics.Debug.WriteLine($"Loaded {presets.Count} presets from XML files");
                return presets;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading presets: {ex.Message}");
                return new List<PresetProfile>();
            }
        }

        /// <summary>
        /// Сохраняет указанный пресет в JSON-файл.
        /// </summary>
        public void SavePresetToFile(PresetProfile preset, string filePath)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required", nameof(filePath));

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(preset, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Загружает пресет из JSON-файла.
        /// </summary>
        public PresetProfile LoadPresetFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Preset file not found", filePath);

            var json = File.ReadAllText(filePath);
            var preset = JsonSerializer.Deserialize<PresetProfile>(json);

            if (preset == null)
            {
                throw new InvalidDataException("Не удалось десериализовать пресет из файла");
            }

            // Гарантируем наличие Id
            if (string.IsNullOrWhiteSpace(preset.Id))
            {
                preset.Id = Guid.NewGuid().ToString("N");
            }

            return preset;
        }
    }
}