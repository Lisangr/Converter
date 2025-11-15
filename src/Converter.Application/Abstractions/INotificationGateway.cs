namespace Converter.Application.Abstractions;

public interface INotificationGateway : IAsyncDisposable
{
    Task ShowInfoAsync(string title, string message, CancellationToken cancellationToken);
    Task ShowErrorAsync(string title, string message, CancellationToken cancellationToken);
}
