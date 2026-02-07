# KODA.md - Converter Project Documentation

## 1. Executive Summary

The Converter project is a Windows Forms application designed for video and audio file conversion. It leverages a modern .NET architecture, incorporating dependency injection, asynchronous operations, and a robust testing suite. The application aims to provide a user-friendly interface for managing conversion queues, applying presets, and monitoring conversion progress.

**Technologies Used:**

*   **Language:** C#
*   **Framework:** .NET 8 (Windows Forms)
*   **Core Libraries:** Microsoft Extensions (DI, Logging, Configuration), Xabe.FFmpeg, Newtonsoft.Json, System.Text.Json, LibVLCSharp.
*   **Testing:** xUnit, Moq, FluentAssertions.

## 2. Architecture Overview

The Converter project follows a layered architecture, with a strong emphasis on separation of concerns and testability. The primary architectural pattern appears to be a variation of **Model-View-ViewModel (MVVM)**, adapted for the Windows Forms environment, combined with **Dependency Injection** for managing service lifecycles and dependencies.

### Key Architectural Patterns:

*   **Dependency Injection:** The `Microsoft.Extensions.DependencyInjection` framework is heavily utilized, as evidenced by `Program.cs` and the `HostingExtensions.CreateHostBuilder` method. Services are registered and resolved through the `IServiceCollection`.
*   **MVVM (Model-View-ViewModel) Adaptation:**
    *   **View:** Primarily represented by WinForms controls and forms (e.g., `Form1.cs`, `UI/Controls/*`, `UI/Dialogs/*`). These are responsible for UI rendering and user interaction.
    *   **ViewModel:** Classes like `MainViewModel` and `QueueItemViewModel` expose data and commands to the View, often implementing `INotifyPropertyChanged` for data binding.
    *   **Model:** Domain entities like `QueueItem`, `AppSettings`, `ConversionProfile` reside in the `Domain` layer.
*   **Layered Architecture:**
    *   **Presentation (UI):** Contains the WinForms UI elements, views, and view models.
    *   **Application:** Houses the core application logic, use cases, presenters, and abstractions (interfaces). This layer acts as the orchestrator.
    *   **Domain:** Defines the core business entities and logic, independent of any framework or infrastructure.
    *   **Infrastructure:** Implements the interfaces defined in the Application layer, providing concrete implementations for persistence, external service integrations (like FFmpeg), and UI dispatching.
*   **Command Pattern:** Although not explicitly named, the use of interfaces like `IAddFilesCommand`, `IStartConversionCommand`, etc., suggests the Command pattern is employed for encapsulating actions.
*   **Asynchronous Programming:** Extensive use of `async`/`await` for non-blocking operations, especially for file processing, network operations, and UI updates.

### Role of Main Folders:

*   **`src/Converter.Application`**: The heart of the application logic. Contains abstractions (interfaces), use cases, presenters, and view models. It defines *what* the application does without specifying *how* it's done.
*   **`src/Converter.Domain`**: Contains the core business entities and rules. This layer is independent of any UI or infrastructure concerns.
*   **`src/Converter.Infrastructure`**: Provides concrete implementations for services defined in the Application layer. This includes data persistence (JSON stores, repositories), FFmpeg integration, notification services, and UI dispatching.
*   **`src/Converter.Presentation.WinForms`**: Contains the actual Windows Forms UI components, including forms, controls, and their associated logic.
*   **`Converter.Tests`**: Houses all unit, integration, and load tests for the application. Organized into `UnitTests`, `IntegrationTests`, and `LoadTests`.
*   **`Extensions`**: Contains extension methods for various functionalities, likely to enhance existing types or provide utility functions.
*   **`Interfaces`**: Likely contains shared interfaces used across different layers.
*   **`Models`**: Contains domain models and data transfer objects.
*   **`Presenters`**: Contains presenter classes that bridge the ViewModel and the View.
*   **`Presets`**: Stores XML files defining conversion presets.
*   **`Properties`**: Contains application settings and designer files.
*   **`Services`**: Contains concrete service implementations, often used by the Infrastructure layer.
*   **`UI`**: Contains UI-specific components and logic, potentially shared across different UI frameworks if the project were to evolve.

## 3. Key Components

Based on the provided file structure and snippets, here are some of the most important classes and their responsibilities:

*   **`Program.cs`**: The entry point of the application. It sets up the host, configures dependency injection, handles global exception logging, hides the console window, and initializes the main UI form.
*   **`Form1.cs` (and related `Form1.*` files)**: The main Windows Form of the application. It serves as the primary View, hosting various controls and interacting with the `MainPresenter`.
*   **`MainPresenter.cs`**: Acts as the orchestrator between the `IMainView` and the ViewModels/Services. It handles user interactions, manages the queue, and coordinates conversion processes.
*   **`MainViewModel.cs`**: Represents the state and behavior of the main application window. It exposes collections of `QueueItemViewModel` and `ConversionProfile` to the View for data binding.
*   **`QueueItemViewModel.cs`**: A ViewModel for individual items in the conversion queue. It maps to the `QueueItem` domain model and handles UI-specific properties like selection status and progress display.
*   **`QueueItem.cs` (Domain Model)**: Represents a single item in the conversion queue, including its file path, status, progress, and output details.
*   **`AppSettings.cs` (Domain Model)**: Holds application-wide settings, such as FFmpeg path, concurrent conversion limits, and disk space checks.
*   **`IQueueRepository.cs` (Abstraction)**: Defines the contract for interacting with the conversion queue data.
*   **`QueueRepository.cs` (Infrastructure)**: Implements `IQueueRepository`, likely using an `IQueueStore` for persistence.
*   **`IQueueStore.cs` (Abstraction)**: Defines the contract for low-level storage operations for queue items.
*   **`JsonQueueStore.cs` (Infrastructure)**: Implements `IQueueStore` by persisting queue data to a JSON file.
*   **`FFmpegExecutor.cs` (Infrastructure)**: A crucial component responsible for executing FFmpeg commands. It likely handles process management, error capturing, and output parsing.
*   **`ConversionOrchestrator.cs` (Application)**: Manages the overall conversion process, coordinating tasks, and ensuring efficient use of resources.
*   **`ConversionUseCase.cs` (Application)**: Encapsulates specific conversion-related business logic, such as initiating a conversion or processing a single file.
*   **`IConversionEstimationService.cs` (Abstraction)**: Interface for estimating conversion times or resource requirements.
*   **`EstimationService.cs` (Application/Infrastructure)**: Provides implementations for conversion estimations.
*   **`PresetService.cs` (Application/Infrastructure)**: Manages loading, saving, and retrieving conversion presets. It likely uses an `IPresetRepository`.
*   **`JsonPresetRepository.cs` (Infrastructure)**: Implements `IPresetRepository` for loading presets from JSON or XML files.
*   **`IApplicationShutdownService.cs` (Abstraction)**: Provides a standardized way to request application shutdown, ensuring graceful termination.
*   **`MockQueueRepository.cs` (Test Base)**: A mock implementation of `IQueueRepository` used in load tests, designed to simulate repository behavior with events.

## 4. Setup & Build

Based on the provided `.csproj` and `.sln` files, the project is a standard .NET application.

**To build and run the application:**

1.  **Prerequisites:**
    *   .NET 8 SDK installed.
    *   Visual Studio (recommended for Windows Forms development) or VS Code with the C# extension.
2.  **Clone the Repository:** Obtain the project source code.
3.  **Open the Solution:** Open the `Converter.sln` file in Visual Studio or your preferred IDE.
4.  **Build:**
    *   In Visual Studio: Right-click on the `Converter` project (or the solution) and select "Build".
    *   Using .NET CLI: Navigate to the root directory of the solution in your terminal and run `dotnet build`.
5.  **Run:**
    *   In Visual Studio: Set the `Converter` project as the Startup Project and press F5 or click the "Start" button.
    *   Using .NET CLI: Navigate to the `Converter` project directory (where `Converter.csproj` is located) and run `dotnet run`.

**To run tests:**

1.  **Build the Solution:** Ensure the solution is built first.
2.  **Using Visual Studio Test Explorer:** Navigate to Test > Test Explorer. The tests from `Converter.Tests.csproj` should be discovered. You can then run them from the Test Explorer window.
3.  **Using .NET CLI:** Navigate to the root directory of the solution in your terminal and run `dotnet test`.

The `package.json` file suggests a potential frontend or web component, but given the `.csproj` and `.sln` files, it's likely not the primary build target for the desktop application. If it were a separate part of the project, its build/run commands would be found within that specific directory.

## 5. Tech Stack

*   **Language:** C#
*   **.NET Version:** .NET 8 (specifically `net8.0-windows`)
*   **UI Framework:** Windows Forms
*   **Core Libraries & Frameworks:**
    *   `Microsoft.Extensions.DependencyInjection`
    *   `Microsoft.Extensions.Hosting`
    *   `Microsoft.Extensions.Configuration` (including `Json`)
    *   `Microsoft.Extensions.Logging`
    *   `Xabe.FFmpeg` (for FFmpeg integration)
    *   `Xabe.FFmpeg.Downloader` (for FFmpeg auto-download)
    *   `Newtonsoft.Json` (for JSON serialization/deserialization)
    *   `System.Text.Json` (for JSON serialization/deserialization)
    *   `LibVLCSharp` and `LibVLCSharp.WinForms` (for video playback)
    *   `VideoLAN.LibVLC.Windows` (native LibVLC binaries)
    *   `Microsoft.Toolkit.Uwp.Notifications` (for Windows notifications)
    *   `System.Reactive` (for reactive extensions)
    *   `System.Threading.Tasks.Extensions`
    *   `Serilog` (for advanced logging)
*   **Testing Libraries:**
    *   `Microsoft.NET.Test.Sdk`
    *   `xunit`
    *   `xunit.runner.visualstudio`
    *   `coverlet.collector`
    *   `Moq` (for mocking)
    *   `FluentAssertions` (for fluent assertions)
*   **Other:**
    *   `System.Windows.Forms` (built-in .NET component)
    *   `System.Runtime.InteropServices` (for P/Invoke)