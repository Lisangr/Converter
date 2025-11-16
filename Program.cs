using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Converter.Application.Abstractions;
using Converter.Application.Presenters;
using Converter.Application.Services;
using Converter.Application.ViewModels;
using Converter.Infrastructure;
using Converter.UI;

namespace Converter
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection()
                .AddLogging(configure => 
                    configure.AddDebug()
                            .SetMinimumLevel(LogLevel.Information))
                .AddSingleton<IMainView, Form1>()
                .AddSingleton<MainPresenter>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<IQueueRepository, QueueRepository>()
                .AddSingleton<IConversionUseCase, ConversionUseCase>()
                .AddSingleton<IProfileProvider, ProfileProvider>()
                .AddSingleton<IOutputPathBuilder, OutputPathBuilder>()
                .AddSingleton<IProgressReporter, UiProgressReporter>()
                .AddSingleton<IQueueProcessor, QueueProcessor>()
                .AddSingleton<IFilePicker, WinFormsFilePicker>()
                .AddSingleton<IFolderPicker, WinFormsFolderPicker>()
                .AddInfrastructureServices()
                .BuildServiceProvider();

            try
            {
                var mainForm = services.GetRequiredService<IMainView>() as Form;
                var presenter = services.GetRequiredService<MainPresenter>();
                
                // Initialize the presenter asynchronously
                presenter.InitializeAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        MessageBox.Show($"Failed to initialize: {t.Exception?.GetBaseException().Message}", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());

                System.Windows.Forms.Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error: {ex.Message}\n\n{ex.StackTrace}", 
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}