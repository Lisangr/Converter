namespace Converter.Application.Abstractions;

public interface INotificationGateway
{
    void Info(string title, string message);
    void Error(string title, string message);
    void Progress(string title, string message, int percent);
}
