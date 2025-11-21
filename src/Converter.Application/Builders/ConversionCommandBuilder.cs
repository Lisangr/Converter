using System;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.Application.Builders;

/// <summary>
/// Построитель команд конвертации FFmpeg.
/// </summary>
public interface IConversionCommandBuilder
{
    /// <summary>
    /// Строит команду FFmpeg для конвертации.
    /// </summary>
    /// <param name="request">Запрос на конвертацию</param>
    /// <returns>Строка команды FFmpeg</returns>
    string Build(ConversionRequest request);
}

/// <summary>
/// Реализация построителя команд конвертации.
/// </summary>
public class ConversionCommandBuilder : IConversionCommandBuilder
{
    public string Build(ConversionRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var args = "-i \"" + request.InputPath + "\" ";
        
        // Добавляем видео настройки
        if (!string.IsNullOrEmpty(request.Profile.VideoCodec))
        {
            args += "-c:v " + request.Profile.VideoCodec + " ";
        }
        
        if (request.Profile.CRF.HasValue)
        {
            args += "-crf " + request.Profile.CRF.Value + " ";
        }
        
        // Добавляем аудио настройки
        if (!string.IsNullOrEmpty(request.Profile.AudioCodec))
        {
            args += "-c:a " + request.Profile.AudioCodec + " ";
        }
        
        if (request.Profile.AudioBitrate.HasValue)
        {
            args += "-b:a " + request.Profile.AudioBitrate.Value + "k ";
        }
        
        args += "\"" + request.OutputPath + "\"";
        return args.Trim();
    }
}