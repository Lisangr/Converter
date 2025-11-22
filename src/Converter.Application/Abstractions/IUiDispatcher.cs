using System;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions
{
    public interface IUiDispatcher
    {
        void Invoke(Action action);
        Task InvokeAsync(Func<Task> action);
        Task<T> InvokeAsync<T>(Func<Task<T>> action);
    }
}
