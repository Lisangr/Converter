using System;
using System.Windows.Forms;

namespace Converter.Extensions
{
    public static class ControlExtensions
    {
        public static void InvokeIfRequired(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                // Use BeginInvoke for better responsiveness, as Invoke can block the UI thread.
                control.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        // Overload for asynchronous actions using Task
        public static void InvokeIfRequired(this Control control, Func<Task> function)
        {
            if (control.InvokeRequired)
            {
                control.BeginInvoke((Action)(async () => await function()));
            }
            else
            {
                // If not required, execute directly. Consider making this async if the function is long-running.
                // For simplicity here, we assume it can be called directly. If it needs await, this needs adjustment.
                // However, BeginInvoke handles async callbacks, so this path is generally for immediate execution.
                // For true async execution on the UI thread without blocking, Task.Run might be needed, but BeginInvoke is preferred for UI updates.
                _ = function(); // Fire and forget for direct execution path
            }
        }
    }
}