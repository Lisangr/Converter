using System;
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
    }
}
