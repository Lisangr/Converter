using System.Diagnostics;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Converter
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            #if false
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                try
                {
                    var args = ToastArguments.Parse(toastArgs.Argument);
                    var action = args.Get("action");

                    if (string.Equals(action, "openFolder", StringComparison.OrdinalIgnoreCase))
                    {
                        var folder = args.Get("folder");
                        if (!string.IsNullOrWhiteSpace(folder))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = folder,
                                UseShellExecute = true
                            });
                        }
                    }
                }
                catch
                {
                    // Ignore activation errors.
                }
            };
            #endif

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}