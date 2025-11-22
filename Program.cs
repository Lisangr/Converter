using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Converter.Application.Abstractions;
using Converter.Application.Presenters;
using Converter.Application.Services;
using Converter.Application.Builders;
using Converter.Application.ViewModels;
using Converter.Infrastructure;
using Converter.Infrastructure.Ffmpeg;
using Converter.Services;
using Converter.UI;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Converter
{
    internal static class Program
    {
        // Windows API для скрытия консольного окна
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        [System.STAThread]
        static int Main(string[] args)
        {
            // Скрываем консольное окно при старте (если оно есть)
            HideConsoleWindow();

            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            // Весь пайплайн запускаем синхронно в STA-потоке,
            // чтобы Form1 и диалоги всегда создавались и вызывались из STA
            try
            {
                return RunApplication(args);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error: {ex.Message}\n\n{ex.StackTrace}",
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static void HideConsoleWindow()
        {
            try
            {
                var consoleWindow = GetConsoleWindow();
                if (consoleWindow != IntPtr.Zero)
                {
                    ShowWindow(consoleWindow, SW_HIDE);
                }
            }
            catch
            {
                // Игнорируем ошибки - консольного окна может и не быть
            }
        }


        private static int RunApplication(string[] args)
        {
            using var cts = new CancellationTokenSource();
            IHost? host = null;
            ILogger? logger = null;

            try
            {
                                var builder = HostingExtensions.CreateHostBuilder(args);
                
                                // Configure services using extension methods
                                builder.Services
                                    .ConfigureApplicationServices(builder.Configuration)
                                    .ConfigureHostedServices();
                

                host = builder.Build();
                var loggerFactory = host.Services.GetService<ILoggerFactory>();
                logger = loggerFactory?.CreateLogger("Program");
                
                logger?.LogInformation("Host built successfully");
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

                lifetime.ApplicationStopping.Register(() =>
                {
                    logger?.LogInformation("Application is stopping, cancelling operations...");
                    cts.Cancel();
                });

                // Запускаем хост в фоновом режиме без ожидания
                logger?.LogInformation("Starting host in background...");
                _ = host.StartAsync(cts.Token);
                logger?.LogInformation("Host started in background, continuing with UI initialization");

                // Продолжаем с инициализацией UI
                return RunUi(host, cts.Token);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Fatal error during application startup");
                return 1;
            }
            finally
            {
                try
                {
                    if (host != null)
                    {
                        using (var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                        {
                            host.StopAsync(shutdownCts.Token).GetAwaiter().GetResult();
                        }
                        host.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Host shutdown error: {ex.Message}");
                }
            }
        }

        private static int RunUi(IHost host, CancellationToken cancellationToken)
        {
            var loggerFactory = host.Services.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("Program") ?? host.Services.GetService<ILogger<object>>();
            
            try 
            {
                logger?.LogInformation("Resolving MainPresenter...");
                var presenter = host.Services.GetRequiredService<MainPresenter>();
                
                logger?.LogInformation("Resolving IMainView...");
                if (host.Services.GetRequiredService<IMainView>() is not Form1 mainView)
                {
                    logger?.LogError("Failed to resolve main form from DI");
                    return 1;
                }

                // Устанавливаем ссылку на презентер в форме (для взаимодействия View 
                // с презентером через IMainView)
                logger?.LogInformation("Wiring MainPresenter into Form1 via SetMainPresenter");
                mainView.SetMainPresenter(presenter);

                logger?.LogInformation("Initializing MainPresenter...");
                presenter.InitializeAsync().GetAwaiter().GetResult();
                
                logger?.LogInformation("Starting UI message loop...");
                System.Windows.Forms.Application.Run(mainView);
                
                return 0;
            }
            catch (OperationCanceledException)
            {
                logger?.LogWarning("Application initialization was cancelled (OperationCanceledException)");
                return 0;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during UI initialization");
                return 1;
            }
        }
    }
}