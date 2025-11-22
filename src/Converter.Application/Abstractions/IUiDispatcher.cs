using System;

namespace Converter.Application.Abstractions
{
    public interface IUiDispatcher
    {
        void Invoke(Action action);
    }
}
