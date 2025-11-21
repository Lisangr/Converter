using System;
using System.IO;

namespace Converter.Domain.Models;

/// <summary>
/// Элемент очереди конвертации.
/// </summary>
public class QueueItem
{
    /// <summary>
    /// Уникальный идентификатор элемента.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Путь к исходному файлу.
    /// </summary>
    public string InputPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Путь к выходному файлу.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Статус обработки элемента.
    /// </summary>
    public ConversionStatus Status { get; set; } = ConversionStatus.Pending;
    
    /// <summary>
    /// Прогресс выполнения в процентах (0-100).
    /// </summary>
    public int Progress { get; set; }
    
    /// <summary>
    /// Время создания элемента.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Время начала обработки.
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// Время завершения обработки.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Сообщение об ошибке (если есть).
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Размер выходного файла в байтах.
    /// </summary>
    public long? OutputFileSizeBytes { get; set; }
    
    /// <summary>
    /// Идентификатор профиля конвертации.
    /// </summary>
    public string ProfileId { get; set; } = string.Empty;

    // Дополнительные свойства для совместимости с существующим кодом
    
    /// <summary>
    /// Путь к файлу (алиас для InputPath).
    /// </summary>
    public string FilePath 
    { 
        get => InputPath; 
        set => InputPath = value; 
    }
    
    /// <summary>
    /// Размер исходного файла в байтах.
    /// </summary>
    public long FileSizeBytes { get; set; }
    
    /// <summary>
    /// Имя файла без пути.
    /// </summary>
    public string FileName => Path.GetFileName(InputPath);
    
    /// <summary>
    /// Время добавления в очередь (алиас для CreatedAt).
    /// </summary>
    public DateTime AddedAt 
    { 
        get => CreatedAt; 
        set => CreatedAt = value; 
    }
    
    /// <summary>
    /// Настройки конвертации для данного элемента.
    /// </summary>
    public ConversionSettings? Settings { get; set; }
    
    /// <summary>
    /// Время выполнения конвертации.
    /// </summary>
    public TimeSpan? ConversionDuration 
    { 
        get 
        { 
            if (StartedAt.HasValue && CompletedAt.HasValue) 
            { 
                return CompletedAt.Value - StartedAt.Value; 
            } 
            return null; 
        } 
    }
    
    /// <summary>
    /// Директория для выходного файла.
    /// </summary>
    public string? OutputDirectory { get; set; }
    
    /// <summary>
    /// Помечен ли элемент как избранный.
    /// </summary>
    public bool IsStarred { get; set; }
    
    /// <summary>
    /// Приоритет элемента в очереди.
    /// </summary>
    public int Priority { get; set; }
}