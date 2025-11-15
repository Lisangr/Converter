namespace Converter.Application.ViewModels;

public sealed record QueueItemViewModel(Guid Id, string InputPath, string OutputPath, string StatusText);
