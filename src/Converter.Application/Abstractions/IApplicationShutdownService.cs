using System;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Высокоуровневый сервис для инициации корректного завершения работы приложения.
    /// Оборачивает сигнал остановки хоста и обеспечивает единую точку входа для shutdown.
    /// </summary>
    public interface IApplicationShutdownService
    {
        /// <summary>
        /// Запрашивает остановку приложения. Повторные вызовы должны быть идемпотентны.
        /// </summary>
        void RequestShutdown();
    }
}
