namespace Converter.Application.Interfaces;

public interface INotificationGateway
{
    Task ShowSuccessAsync(string title, string message, CancellationToken cancellationToken);
    Task ShowWarningAsync(string title, string message, CancellationToken cancellationToken);
    Task ShowErrorAsync(string title, string message, CancellationToken cancellationToken);
}
