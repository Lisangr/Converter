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

            try
            {
                // Создаем Host через HostingExtensions и настраиваем все сервисы в одном контейнере
                var builder = HostingExtensions.CreateHostBuilder(args);

                builder.Services
                    .ConfigureApplicationServices(builder.Configuration)
                    .ConfigureHostedServices();

                host = builder.Build();
                var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

                // Обработка завершения через IHostApplicationLifetime
                lifetime.ApplicationStopping.Register(() =>
                {
                    cts.Cancel();
                });

                // Запускаем фоновые службы (включая FfmpegBootstrapService как IHostedService)
                host.StartAsync(cts.Token).GetAwaiter().GetResult();

                // Получаем основное представление и презентер из DI
                var presenter = host.Services.GetRequiredService<MainPresenter>();
                var mainView = host.Services.GetRequiredService<IMainView>() as Form1;

                if (mainView == null)
                {
                    MessageBox.Show("Failed to resolve main form from DI", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }

                // Устанавливаем ссылку на презентер в форме (для взаимодействия View 
                // с презентером через IMainView)
                mainView.SetMainPresenter(presenter);

                try
                {
                    presenter.InitializeAsync().GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("Application initialization was cancelled.",
                        "Initialization Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to initialize: {ex.GetBaseException().Message}",
                        "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }

                using (mainView)
                {
                    System.Windows.Forms.Application.Run(mainView);
                }

                // Graceful shutdown будет выполнен в блоке finally ниже
                return 0;
            }
            catch (OperationCanceledException)
            {
                return 0;
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
    }
}