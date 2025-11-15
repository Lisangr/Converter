# Техническая документация: Converter (2024 refactor)

## 1. Архитектура высокого уровня
- **Converter.Domain** — неизменяемые record-типы (`ConversionProfile`, `ConversionRequest`, `ConversionResult`, `ConversionProgress`, `QueueItem`, `MediaInfo`, `ThumbnailRequest`, `ConversionCommand`). Не содержит зависимости от UI/инфраструктуры.
- **Converter.Application** — интерфейсы и бизнес-логика (MVP-презентер, очередь, построитель команд, оркестратор). Здесь находятся:
  - `IMainView`, `IConversionOrchestrator`, `IFFmpegExecutor`, `IQueueService`, `INotificationGateway`, `IThumbnailProvider`, `ISettingsStore`, `IPresetRepository`.
  - `MainPresenter` — единственная точка координации пользовательских сценариев.
  - `QueueService` — канал-ориентированная очередь с фоновой задачей, `CancellationToken` на элемент и потокобезопасными событиями.
  - `ConversionCommandBuilder` и `ConversionOrchestrator` — построение/выполнение команд FFmpeg без доступа к UI.
- **Converter.Infrastructure** — реализации интерфейсов: `FfmpegExecutor`, `ThumbnailProvider`, `NotificationGateway`, `SettingsStore`, `PresetRepository`. Каждый класс отвечает за конкретный адаптер и соблюдает `IAsyncDisposable` там, где есть unmanaged-ресурсы.
- **Converter.WinForms** — минимальная реализация `IMainView` (`MainForm` + `MainForm.Designer`). Форма только отображает состояние и возбуждает события. Композиция выполняется через `Microsoft.Extensions.Hosting` в `Program.cs`.

## 2. MVP-поток данных
1. `Program` запускает `Host`, регистрирует все сервисы и создаёт `MainForm` (как `IMainView`) и `MainPresenter` (singleton).
2. `MainPresenter` подписывается на события представления (`ViewLoaded`, `AddFilesRequested`, `StartConversionRequested`, `CancelConversionRequested`, `RemoveItemRequested`). В обработчиках используется собственный pipeline (цепочка `Task`), чтобы ни одна операция не была fire-and-forget.
3. Пользователь выбирает файлы → `MainForm` обновляет `_inputFiles` и вызывает событие. Презентер синхронизируется с очередью, загружает пресеты через `ISettingsStore` + `IPresetRepository`, обновляет UI по `SynchronizationContext`.
4. При старте конверсии `MainPresenter` валидирует профиль/папку, сохраняет настройки, собирает `ConversionRequest` и вызывает `IQueueService.EnqueueAsync` для каждого файла.
5. `QueueService` создаёт `QueueItem`, сохраняет его в snapshot, публикует событие `ItemQueued`, затем в фоновой задаче создаёт линкованный `CancellationToken` и вызывает `IConversionOrchestrator.ExecuteAsync`. Прогресс проксируется через `IProgress<ConversionProgress>`.
6. `ConversionOrchestrator` → `IFFmpegExecutor.ProbeAsync` (Xabe wrapper) → `ConversionCommandBuilder.Build` → `IFFmpegExecutor.ExecuteAsync`. Все исключения логируются и трансформируются в `ConversionResult.Failure`.
7. По завершении `QueueService` возбуждает `ItemCompleted`, а презентер обновляет UI и уведомляет пользователя через `INotificationGateway`. Миниатюры подгружаются через `IThumbnailProvider` (stream-based API) и отображаются в `MainForm` с безопасным marshaling на UI-поток.

## 3. Управление ресурсами и потоками
- `QueueService` использует `Channel<QueueItem>` + `CancellationTokenSource _shutdown`. Каждый элемент получает собственный `CancellationTokenSource`, который освобождается после завершения задачи.
- `MainPresenter` хранит `_lifetimeCts` и `_pipeline`. В `DisposeAsync` ожидается завершение всех операций.
- `FfmpegExecutor` и `ThumbnailProvider` реализуют `IAsyncDisposable` и освобождают `SemaphoreSlim`/`MemoryCache`/`FFmpegDownloader` ресурсы.
- WinForms обращение к контролам идёт через `SynchronizationContext` внутри `MainPresenter` или через `InvokeRequired` внутри `MainForm`.

## 4. FFmpeg-пайплайн
- `ConversionCommandBuilder` строит CLI-параметры: `-y`, `-i`, кодеки, битрейты, пользовательские аргументы и путь вывода.
- `FfmpegExecutor` гарантирует наличие двоичных файлов (через `FFmpegDownloader`), получает `MediaInfo` и запускает конверсию через `FFmpeg.Conversions.New()` с подпиской на `OnProgress`.
- `ThumbnailProvider` использует ту же библиотеку для извлечения одного кадра, хранит байты в `MemoryCache` с ограничением размера.

## 5. Настройки и пресеты
- `SettingsStore` сериализует JSON (`settings.json`) в `%AppData%/Converter/Settings/`. Содержит последнее расположение папки и пользовательские профили.
- `PresetRepository` возвращает встроенный список `ConversionProfile`. Для добавления новых достаточно расширить статическую коллекцию или подключить другое хранилище.

## 6. Уведомления
- `NotificationGateway` строит toast-уведомления (success/warning/error) через `Microsoft.Toolkit.Uwp.Notifications`. Никаких `MessageBox`/WinForms API внутри инфраструктуры.
- UI слой использует `ShowInfo/ShowError` только для локальных ошибок в `MainForm` (например, валидация).

## 7. DI-композиция
```
services.AddSingleton<IMainView, MainForm>();
services.AddSingleton<MainPresenter>();
services.AddSingleton<IQueueService, QueueService>();
services.AddSingleton<IConversionOrchestrator, ConversionOrchestrator>();
services.AddSingleton<ConversionCommandBuilder>();
services.AddSingleton<IFFmpegExecutor, FfmpegExecutor>();
services.AddSingleton<IThumbnailProvider, ThumbnailProvider>();
services.AddSingleton<INotificationGateway, NotificationGateway>();
services.AddSingleton<ISettingsStore, SettingsStore>();
services.AddSingleton<IPresetRepository, PresetRepository>();
```
`Program.Main` вызывает `host.Start()`, резолвит представление/презентер и передаёт форму в `Application.Run`. По завершении выполняется `host.StopAsync`.

## 8. Тестируемость
- Application слой не зависит от WinForms, поэтому покрыт unit-тестами (xUnit):
  - `ConversionCommandBuilderTests` проверяют построение параметров.
  - `QueueServiceTests` — событие прогресса и завершения на фейковом оркестраторе.
  - `MainPresenterTests` — корректную постановку в очередь при валидных данных.
- Инфраструктурные адаптеры можно мокировать через интерфейсы, а `System.IO.Abstractions` легко добавить при необходимости.

## 9. Расширение
- Добавление нового UI (например, WPF/CLI) потребует реализации `IMainView` и регистрации в DI.
- Новые пресеты/источники настроек реализуются через альтернативный `IPresetRepository`/`ISettingsStore`.
- Для других медиабиблиотек достаточно реализовать `IFFmpegExecutor` и зарегистрировать в DI.
