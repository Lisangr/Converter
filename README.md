# Converter

A layered WinForms media converter built on top of FFmpeg with clear separation between domain, application, infrastructure and presentation concerns.

## Projects

- `Converter.Domain` — immutable domain models for conversion requests, queue items and progress.
- `Converter.Application` — abstractions, presenters and orchestrators that implement business rules.
- `Converter.Infrastructure` — FFmpeg integration, persistence, notifications and thumbnail generation.
- `Converter.WinForms` — thin UI shell that implements `IMainView` and wires up the presenter through `Host.CreateDefaultBuilder`.
- `Converter.Application.Tests` — xUnit tests covering presenters, command builder and queue service.

## Running

1. Ensure .NET 8.0 SDK is installed.
2. Restore dependencies: `dotnet restore Converter.sln`
3. Run the WinForms app: `dotnet run --project src/Converter.WinForms/Converter.WinForms.csproj`
4. Execute tests: `dotnet test Converter.sln`

FFmpeg binaries are downloaded at runtime via `Xabe.FFmpeg.Downloader` and cached under the application's data folder.
