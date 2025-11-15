# Converter (Modular FFmpeg WinForms Client)

Полностью переработанная версия приложения построена на .NET 8 и разделена на четыре изолированных слоя:

```
src/
 ├─ Converter.Domain          # Чистые модели и record-типы
 ├─ Converter.Application     # Презентер, очередь, построитель команд, интерфейсы
 ├─ Converter.Infrastructure  # Адаптеры FFmpeg, миниатюры, уведомления, настройки
 └─ Converter.WinForms        # Тонкая реализация IMainView на Windows Forms
```

## Текущие возможности
- Очередь конверсии, управляемая `QueueService`, с последовательной обработкой, отменой и событиями прогресса.
- Оркестратор `ConversionOrchestrator`, который:
  - выполняет `ProbeAsync` через `IFFmpegExecutor` (обёртка над Xabe.FFmpeg);
  - строит команды через `ConversionCommandBuilder` (не зависит от UI);
  - публикует прогресс в `ConversionProgress` и возвращает детальный `ConversionResult`.
- Миниатюры через `ThumbnailProvider` (`IThumbnailProvider`, `IAsyncDisposable`, кэш в `MemoryCache`).
- Toast-уведомления через `NotificationGateway` (UWP Toast API) и файловое хранилище пресетов/настроек (`SettingsStore`, `PresetRepository`).
- MVP: `MainForm` реализует `IMainView`, а вся логика теперь в `MainPresenter`.

## Сборка и запуск
```bash
dotnet restore Converter.sln
dotnet build Converter.sln
dotnet run --project src/Converter.WinForms/Converter.WinForms.csproj
```

## Тесты
В папке `tests/` можно добавлять проекты с unit-тестами для Application-слоя. Примеры тестов см. в `tests/Converter.Application.Tests` (см. ниже).

## Основные интерфейсы
- `IMainView` — контракт слоя представления.
- `IConversionOrchestrator` — координация probing/command/execute.
- `IFFmpegExecutor` — тонкая обёртка над конкретной реализацией FFmpeg.
- `IQueueService` — безопасная очередь с событиями.
- `INotificationGateway`, `IThumbnailProvider`, `ISettingsStore`, `IPresetRepository` — инфраструктурные адаптеры.

## DI-композиция
`Program.cs` создаёт `Host`, регистрирует все сервисы и запускает `MainForm`. Освобождение ресурсов (`IAsyncDisposable`) происходит автоматически через DI.

## Дополнительные материалы
- `TECHNICAL_DOCUMENTATION.md` — расширенные схемы и логика оркестрации.
