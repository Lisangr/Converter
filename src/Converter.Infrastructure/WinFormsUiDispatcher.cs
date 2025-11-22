using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Extensions;

namespace Converter.Infrastructure
{
    public sealed class WinFormsUiDispatcher : IUiDispatcher
    {
        private readonly IMainView _view;

        public WinFormsUiDispatcher(IMainView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Invoke(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            _view.RunOnUiThread(action);
        }

        public Task InvokeAsync(Func<Task> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var tcs = new TaskCompletionSource();
            _view.RunOnUiThread(async () =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        public Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var tcs = new TaskCompletionSource<T>();
            _view.RunOnUiThread(async () =>
            {
                try
                {
                    T result = await action().ConfigureAwait(false);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
