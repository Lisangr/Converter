using Converter.Application.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Интерфейс сервиса оценки конвертации видео.
    /// </summary>
    public interface IConversionEstimationService
    {
        /// <summary>
        /// Оценивает параметры конвертации видео.
        /// </summary>
        /// <param name="inputFilePath">Путь к исходному файлу</param>
        /// <param name="targetBitrateKbps">Целевой битрейт видео в кбит/с (0 - автоматический расчет)</param>
        /// <param name="targetWidth">Целевая ширина (null - без изменения)</param>
        /// <param name="targetHeight">Целевая высота (null - без изменения)</param>
        /// <param name="videoCodec">Кодек видео</param>
        /// <param name="includeAudio">Включать ли аудио</param>
        /// <param name="audioBitrateKbps">Битрейт аудио</param>
        /// <param name="crf">Параметр CRF (null - не используется)</param>
        /// <param name="audioCopy">Копировать ли аудио без перекодирования</param>
        /// <param name="ct">Токен отмены</param>
        /// <returns>Оценка конвертации</returns>
        Task<ConversionEstimate> EstimateConversion(
            string inputFilePath,
            int targetBitrateKbps,
            int? targetWidth,
            int? targetHeight,
            string videoCodec,
            bool includeAudio,
            int? audioBitrateKbps,
            int? crf = null,
            bool audioCopy = false,
            CancellationToken ct = default);
    }
}