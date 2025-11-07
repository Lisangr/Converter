# Техническая документация: Converter

## Обзор архитектуры
Приложение — WinForms GUI над FFmpeg с использованием библиотеки `Xabe.FFmpeg` для построения и запуска конверсий. Вся логика сосредоточена в `Form1` (частичном классе) и его UI-части `Form1.UI.cs`.

Основные блоки:
- Инициализация UI динамически в `BuildUi()` и подметодах: `BuildLeftPanel`, `BuildRightPanel`, `BuildBottomPanel`, `BuildVideoTab`, `BuildAudioTab`, `BuildOutputTab`, `BuildAdvancedTab`.
- Очередь файлов в `ListView` с колонками: Имя, Путь, Формат, Разрешение, Длительность, Размер, Статус.
- Обеспечение FFmpeg: `EnsureFfmpegAsync()` скачивает и настраивает бинарники, если путь не задан.
- Обработка: `btnStart_Click` → `ProcessAllFilesAsync` → по каждому файлу `ConvertFileAsync`.
- Пресеты: `btnSavePreset_Click`/`btnLoadPreset_Click` — сохранение/загрузка настроек в простой kv-файл `.preset`.
- Логирование: `AppendLog` с таймстампами, потоко-безопасно через `BeginInvoke`.

## Стек и зависимости
- .NET 8, Windows Forms
- NuGet:
  - `Xabe.FFmpeg` 6.0.2 — оболочка над FFmpeg
  - `Xabe.FFmpeg.Downloader` 5.1.0 — загрузка FFmpeg
  - `System.Text.Json` 9.0.0, `Newtonsoft.Json` 9.0.1 (зарезервировано под возможные функции)

## Жизненный цикл приложения
- `Program.Main` → `ApplicationConfiguration.Initialize()` → `new Form1()`
- `Form1.OnLoad` → `BuildUi()` → `SetDefaults()` → `EnsureFfmpegAsync()` (fire-and-forget)

## UI и взаимодействия
- Перетаскивание файлов на форму и в список (`DragEnter/DragDrop`).
- Кнопки: Добавить файлы, Удалить выбранные, Очистить всё.
- Вкладки настроек:
  - Видео: формат, видеокодек, качество (CRF), масштаб (пресет или процент), разрешение-пресет.
  - Аудио: включение/выключение, кодек, битрейт.
  - Вывод: папка, подпапка `Converted`, шаблон имени `{original}`, `{format}`, `{codec}`, `{resolution}`.
  - Дополнительно: путь к FFmpeg, потоки `-threads`, аппаратное ускорение.
- Нижняя панель: прогресс общий/текущий, кнопки Старт/Стоп, сохранение/загрузка пресетов, журнал.

## Конвейер обработки
1. `ProcessAllFilesAsync` собирает параметры из UI:
   - `format` (низкий регистр)
   - `vcodec` и `acodec` (через `ExtractCodecName` из отображаемой строки)
   - `abitrate` (например, `192k`), `crf` (из текста качества через Regex)
2. Для каждого файла:
   - Генерация пути вывода `GenerateOutputPath` по шаблону и опциям подпапки.
   - Формирование фильтра `scale`:
     - По пресету высоты `PresetToHeight` → `scale=-2:<H>`.
     - По проценту: `scale=trunc(iw*pct/100/2)*2:trunc(ih*pct/100/2)*2`.
3. Построение конверсии через Xabe:
   - Общие параметры: `-loglevel verbose`, `-i <input>`, `-vf <scale?>`, `-c:v <vcodec>`, `-pix_fmt yuv420p`.
   - Для H.264/H.265: `-crf <N> -preset medium`.
   - Потоки: `-threads <N>` если > 0.
   - Аудио: `-c:a <acodec> -b:a <abitrate>` или `-an`.
   - Для MP4: `-movflags +faststart`.
   - Выходной файл в выбранной папке.
4. Прогресс и лог:
   - `conv.OnProgress` обновляет прогресс-бары и метки.
   - `conv.OnDataReceived` пишет вывод FFmpeg в лог.

## Обеспечение FFmpeg
- Если `txtFfmpegPath` задан и директория существует — используется она (`FFmpeg.SetExecutablesPath`).
- Иначе используется `%LocalAppData%/Converter/ffmpeg`.
- При отсутствии бинарников — загрузка `FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, baseDir)`.

## Потоки и UI
- Конвертация запускается через `await conv.Start(token)`.
- Обновления UI выполняются через `this.BeginInvoke(...)`/`txtLog.BeginInvoke(...)`.
- Отмена — через `CancellationTokenSource.Cancel()` и перехват `OperationCanceledException`.

## Обработка ошибок
- Локальные try/catch вокруг анализа, конвертации, сохранения/загрузки пресетов.
- Визуальный статус элемента списка (цвет фона, текст статуса).
- Критические ошибки отражаются в логе и диалоговых окнах.

## Расширение проекта
- Добавить выбор пресетов кодировщика (x264/x265) и профилей (`-preset slow/fast`, `-profile`)
- Поддержка копирования потоков (`-c:v copy`, `-c:a copy`)
- Очередь/пул параллельных заданий с ограничением по числу потоков на процесс
- Сохранение и загрузка настроек приложения (например, JSON в `%AppData%`)
- Локализация ресурсов UI

## Структура файлов
- `Converter.csproj` — проект .NET 8 WinForms и NuGet-зависимости
- `Program.cs` — точка входа
- `Form1.cs` — часть класса формы с конструктором
- `Form1.UI.cs` — основная логика UI и конвертации (большая часть кода)
- `Form1*.resx` — ресурсы формы

## Замечания по сборке
- Проект SDK-стиля, сборка стандартная: `dotnet build -c Release`.
- Артефакты появляются в `bin/` и `obj/`, исключаются из VCS.
