# BuoyCalc Windows — архитектурный аудит

Дата: 2026-06-29  
Базовое состояние: логическая рабочая версия `v0.46.4` после отмены преждевременного шага `v0.46.5`.

## 0. Цель документа

Этот документ фиксирует текущее устройство проекта перед архитектурной стабилизацией.

На этом этапе не требуется:

- менять расчётное ядро;
- менять `MooringShapeSolver`;
- чинить отдельные окна точечно;
- добавлять 3D;
- добавлять новые пользовательские функции.

Главная цель — увидеть, где сейчас смешаны ответственности, где есть несколько источников одних и тех же данных, и какие места нужно стабилизировать перед следующими функциями.

## 1. Окна приложения

| Окно | Файлы | Назначение | Текущие архитектурные замечания |
|---|---|---|---|
| Главное окно | `Views/MainWindow.axaml`, `Views/MainWindow.axaml.cs` | Основной ввод проекта, условий, буя, якоря, последовательности элементов, запуск расчёта, открытие отчётов, 2D и PDF | В XAML остались старые версии. Code-behind не только открывает окна, но и подменяет пользовательские тексты, экспортирует PDF и содержит вложенный файловый dialog-service. |
| Профиль течения | `Views/CurrentProfileWindow.axaml`, `Views/CurrentProfileWindow.axaml.cs` | Редактирование профиля течения по глубине | Пользователь видит технические обозначения `U/V/W`, `U East`, `V North`, `W Vert.`, `ρ`, а также старую версию `v0.19`. |
| Библиотека элементов | `Views/ElementLibraryWindow.axaml`, `Views/ElementLibraryWindow.axaml.cs` | Редактирование буёв, линий/буйрепов, соединителей, приборов и якорей | Окно содержит сразу пять редакторов. XAML очень плотный, часть пользовательских полей содержит технические сокращения `MBL`, `Cd`. |
| Проверка схемы постановки | `Views/SequencePreviewWindow.axaml`, `Views/SequencePreviewWindow.axaml.cs` | Предпросмотр последовательности перед расчётом | Использует текстовую последовательность `SequenceDiagramLines`, а не отдельную модель визуализации. |
| 2D-схема | `Views/Mooring2DWindow.axaml`, `Views/Mooring2DWindow.axaml.cs`, `Views/Mooring2DCanvas.cs` | Отображение формы постановки | В XAML осталась старая версия `v0.24.4`. Canvas смешивает основную форму, альтернативную форму, парсинг полного отчёта и fallback-отрисовку. Пользователь видит технические термины `MooringShapeSolver`, `X/Z`, `альтернативная форма`, `WARNING`. |
| Полный отчёт | `Views/ReportTextWindow.axaml`, `Views/ReportTextWindow.axaml.cs` | Просмотр технического Markdown-отчёта | Это техническое окно, поэтому solver-термины допустимы. Но сейчас тот же текст используется как источник данных для других частей приложения. |

## 2. ViewModel и MVVM-инфраструктура

| Файл | Назначение | Текущие архитектурные замечания |
|---|---|---|
| `ViewModels/ViewModelBase.cs` | Базовый `INotifyPropertyChanged` | Нормальная инфраструктура MVVM. |
| `ViewModels/RelayCommand.cs` | Командная инфраструктура | Нормальная инфраструктура, но не отдельная ViewModel. |
| `ViewModels/MainWindowViewModel.cs` | Главная ViewModel проекта | Слишком много ответственностей: состояние UI, проектные поля, библиотечные пресеты, работа с проектным JSON, запуск расчёта, генерация `ResultText`, генерация `ReportText`, подготовка данных для визуализации и статусов. |
| `ViewModels/AssemblyItemViewModel.cs` | ViewModel элемента последовательности постановки | Смешивает UI-состояние, выбор типа `Line/Connector/Payload`, работу с библиотечными storage, применение пресетов, конвертацию в `AssemblyItemInput`, пользовательские подсказки и технические термины. |
| `ViewModels/CurrentProfilePointViewModel.cs` | ViewModel точки профиля течения | Содержит технические обозначения `z`, `U`, `V`, `W`, `ρ` в пользовательском Summary. |
| `ViewModels/ElementLibraryViewModel.cs` | ViewModel библиотеки элементов | Один класс обслуживает пять разных редакторов: буи, линии, соединители, приборы, якоря. Содержит много однотипных операций `New/Save/Delete/Refresh/LoadSelected`. |

## 3. Расчётные сервисы и расчётные слои

| Сервис / файл | Роль | Замечания по стабилизации |
|---|---|---|
| `Models/EngineeringModels.cs` / `BuoyCalculator` | Базовое расчётное ядро: плавучесть, вес в воде, силы течения/волны, слабое звено, якорь, запасы, строки элементов и сегменты | На этапе стабилизации не менять. В файле одновременно лежат модели и расчётный класс. Позже можно разделить модели и calculator, но не сейчас. |
| `Services/SegmentTensionAnalyzer.cs` | Сегментные натяжения и углы | Технический расчётный слой для отчёта/формы. |
| `Services/MooringShapeSolver.cs` | Основная X/Z-форма постановки | Не менять на этом этапе. В этом же файле находится `MooringShapeStore`, что смешивает solver и хранение текущей формы. |
| `Services/MooringNodeAnalyzer.cs` | Старый/дополнительный анализ узлов формы по сегментам | Похоже на вспомогательный слой формы. Нужно проверить, используется ли он сейчас активно или остался как исторический слой. |
| `Services/MooringShapeProjection.cs` | Проекция формы, суммы `dX/dZ`, проверка замыкания геометрии | Технический слой отчёта. |
| `Services/MooringShapeForceAnalyzer.cs` | Shape-based силы линии | Технический слой сравнения старой силы и силы по форме. |
| `Services/MooringShapeTensionAnalyzer.cs` | Shape-based натяжения | Технический слой сравнения натяжений. |
| `Services/MooringSequencePositioner.cs` | Позиционная модель последовательности по координате `s` | Важный слой между пользовательской последовательностью и расчётными формами. |
| `Services/MooringDiscreteLoadTensionAnalyzer.cs` | Натяжения с учётом дискретных нагрузок | Технический слой для приборов/соединителей. |
| `Services/MooringDiscreteLoadShapeBuilder.cs` | Форма с дискретными нагрузками | Сейчас воспринимается как альтернативная/более пользовательская форма для PDF. Нужно стабилизировать как один из кандидатов формы. |
| `Services/MooringAlternativeDiscreteNodeProjector.cs` | Проекция дискретных элементов на альтернативную X/Z-форму | Используется для отображения приборов/соединителей на форме. |
| `Services/MooringAlternativeShapeStore.cs` | Хранилище альтернативной формы для отображения | Store, но находится среди сервисов. Нужна единая политика выбора формы. |
| `Services/MooringIterativeSolver.cs` | Диагностический итерационный solver-слой / кандидатная форма | Не менять сейчас. Содержит расчёт, отчётные фрагменты и `MooringIterativeSolverStore`. |
| `Services/MooringIterativeShapeComparison.cs` | Сравнение основной и кандидатной формы | Техническая диагностика, не пользовательский UI. |
| `Services/MooringPrimaryShapeGate.cs` | Gate выбора, можно ли кандидатную форму сделать основной | Содержит gate, selector и store. Термины fallback/candidate/primary уже оформлены, но источник выбранной формы ещё не стабилизирован для UI/PDF/2D. |
| `Services/MooringDeploymentModeClassifier.cs` | Классификация режима постановки | Техническая диагностика для отчёта/автопроверок. |
| `Services/MooringAutocheckSuite.cs` | Автопроверки сценариев | Техническая диагностика, содержит статус `Fail`. |
| `Services/MooringVectorBalance.cs` | Векторная ведомость сил | Техническая таблица полного отчёта. |
| `Services/EngineeringDiagnostics.cs` | Инженерная диагностика и severity | Нужна политика отображения severity отдельно для пользователя и для технического отчёта. |
| `Services/VerdictDisplayAdvisor.cs` | Корректировка отображаемого вердикта без изменения ядра | Полезный слой user-facing интерпретации, но ещё не оформлен как единая политика пользовательских статусов. |

## 4. Storage / Store / состояние между слоями

### 4.1. In-memory Store текущего расчёта

| Store | Где находится | Что хранит | Проблема |
|---|---|---|---|
| `MooringShapeStore` | В конце `Services/MooringShapeSolver.cs` | Текущую основную форму `MooringShapeResult` | Store встроен в файл solver. После `MooringIterativeSolverStore.Set` может быть перезаписан выбранной формой. |
| `MooringAlternativeShapeStore` | `Services/MooringAlternativeShapeStore.cs` | Альтернативную форму с дискретными нагрузками и дискретные узлы | PDF берёт схему отсюда, 2D сравнивает её с основной формой. |
| `MooringIterativeSolverStore` | В конце `Services/MooringIterativeSolver.cs` | Результат итерационного solver | При установке результата запускает выбор primary shape и меняет `MooringShapeStore`. |
| `MooringPrimaryShapeSelectionStore` | В конце `Services/MooringPrimaryShapeGate.cs` | Результат выбора основной формы | Нужен, но сейчас не оформлен как единый источник для user UI/PDF/2D. |

### 4.2. Файловые storage

| Storage | Назначение |
|---|---|
| `ProjectJsonStorage.cs` | Сохранение/загрузка проекта в JSON. |
| `BuoyLibraryStorage.cs` | Библиотека буёв. |
| `RopeLibraryStorage.cs` | Библиотека линий/буйрепов. |
| `ConnectorLibraryStorage.cs` | Библиотека соединителей. |
| `PayloadLibraryStorage.cs` | Библиотека приборов/нагрузок. |
| `AnchorLibraryStorage.cs` | Библиотека якорей. |
| `IProjectFileDialogService.cs` | Абстракция выбора файлов, но текущая Avalonia-реализация вложена в `MainWindow.axaml.cs`. |

## 5. Отчёты

| Отчёт / файл | Что делает | Проблема |
|---|---|---|
| `Services/ReportBuilder.cs` | Строит полный Markdown-отчёт и одновременно запускает большую часть расчётно-диагностического pipeline | Слишком много ответственности. Кроме текста, он создаёт shape, projection, force/tension diagnostics, sequence positions, discrete load shape, iterative solver, diagnostics, vector balance и пишет в Store. |
| `Views/ReportTextWindow.axaml` | Показывает `ReportText` как технический отчёт | Само окно корректно техническое, но `ReportText` используется не только для просмотра. |
| `Services/PdfReportBuilder.cs` | Строит пользовательский PDF: итог, схема, цепочка, таблица элементов | Берёт данные из разных источников: `MooringAlternativeShapeStore`, `resultText`, `sequenceLines`, `elementRows`, `reportText`, visualization fields. Частично парсит `reportText` для метрик. |
| `Services/PdfReportStructureGuide.cs` | Подготовка структуры текста для PDF | Нужно определить, останется ли он после разделения user/technical report builders. |
| `Services/PdfReportTextCleanup.cs` | Очистка/фильтрация текста PDF | Это признак того, что пользовательский отчёт сейчас получается фильтрацией технического. Лучше заменить отдельным `UserReportBuilder`. |
| `MainWindowViewModel.ResultText` | Краткий итог в главном окне и PDF | Формируется строкой вручную внутри `Calculate`. Нужна отдельная модель user summary. |
| `MainWindowViewModel.ReportText` | Полный технический отчёт | Сейчас является и отображаемым текстом, и косвенным источником данных. |

## 6. Визуализации

| Визуализация | Источник данных | Проблема |
|---|---|---|
| Главная сводка визуализации | Поля `VisualizationDepthM`, `VisualizationLineLengthM`, `VisualizationOffsetM`, `VisualizationStatusText` в `MainWindowViewModel` | Это не отдельная read model, а набор строк/чисел во ViewModel. Статусы `OK/WARNING` видны пользователю. |
| Текстовая цепочка | `SequenceDiagramLines` | Используется для preview и fallback-узлов 2D. Это строковая визуализация, а не модель цепочки. |
| `Mooring2DCanvas` | `MooringShapeStore`, `MooringAlternativeShapeStore`, `ReportText`, fallback visualization values | Смешивает несколько источников формы и несколько режимов отрисовки. Парсит Markdown-отчёт. Показывает техническое сравнение пользователю. |
| PDF-схема | `MooringAlternativeShapeStore.Current` | Пользовательский PDF уже ближе к нужному подходу: показывает форму с дискретными элементами. Но источник данных отличается от 2D и полного отчёта. |
| Таблицы полного отчёта | `ReportBuilder` | Это технические визуализации расчётного pipeline. Их нужно оставить в technical diagnostics, но не использовать как источник пользовательских данных. |

## 7. Где сейчас хранится версия

Текущий правильный источник версии:

```text
Services/AppInfo.cs
AppInfo.Version = v0.46.4
AppInfo.VersionNote = пользовательские статусы PDF
AppInfo.DisplayVersion = v0.46.4 - пользовательские статусы PDF
```

Проблема: это уже не единственный источник версии.

## 8. Где захардкожены старые версии

| Место | Старые версии / текст | Что не так |
|---|---|---|
| `Views/MainWindow.axaml` | `Title="BuoyCalc Windows v0.21.3"`, badge `v0.21.3 cleanup` | XAML хранит старую версию. Сейчас это маскируется code-behind override. |
| `Views/Mooring2DWindow.axaml` | `Title="2D-схема постановки v0.24.4"`, badge `v0.24.4` | Старый XAML-текст. |
| `Views/CurrentProfileWindow.axaml` | `Title="Профиль течения по глубине v0.19"`, badge `v0.19`, текст про `v0.19` | Пользователь видит историческую версию и техническую дорожную карту. |
| `Views/WindowVersionHelper.cs` | `CurrentVersion = "v0.38.4"`, `CurrentVersionNote = "PDF solver diagram"` | Дублирует `AppInfo` и уже устарел относительно `v0.46.4`. |
| `WindowVersionHelper.LegacyVersionTexts` | список `v0.19`, `v0.21.2`, `v0.21.3`, `v0.24.4`, `v0.36`, `v0.37`, `v0.38.*` | Временный runtime-патч вместо нормального источника версии. |
| `MainWindowViewModel.UpdateCurrentProfileSummary` | `В v0.19 расчёт использует...` | Пользовательская строка содержит старую версию. |
| `ReportBuilder` | `v0.39`, `v0.40`, `v0.42` в методических заметках | Для полного технического отчёта исторические версии допустимы, но они не должны попадать в user UI/PDF без политики. |
| `MooringShapeSolver` | `v0.38.2` в `MethodNote` | Техническая методическая строка. |
| `MooringPrimaryShapeGate` | `v0.40` в gate/selector notes | Техническая методическая строка. |
| `MooringIterativeSolver` | `v0.42`, `v0.39` в method notes/autocheck tables | Техническая методическая строка. |
| `README.md` | Раздел “Следующий шаг v0.46.5” | После отмены `v0.46.5` README логически устарел; отмена зафиксирована в `docs/CONTROL_MARK_UPDATES.md`. |

## 9. Где дублируются формы: fallback / candidate / selected / alternative

Сейчас в проекте есть несколько понятий формы:

| Понятие | Текущий источник | Где видно |
|---|---|---|
| Fallback / основная старая форма | `MooringShapeSolver.Build`, затем `MooringShapeStore` | Полный отчёт, 2D, gate/selector. |
| Alternative / форма с дискретными нагрузками | `MooringDiscreteLoadShapeBuilder`, `MooringAlternativeShapeStore` | PDF-схема, 2D-сравнение, полный отчёт. |
| Candidate / финальная итерационная форма | `MooringIterativeSolver.FinalShape` | Полный технический отчёт, gate/selector. |
| Selected / выбранная основная форма | `MooringPrimaryShapeSelector`, `MooringPrimaryShapeSelectionStore` | Частично есть, но не стал единым user-facing источником для PDF/2D/отчёта. |

Проблема: пользовательские визуализации и отчёты не читают одну стабильную `SelectedShape`-модель.

Сейчас:

- полный отчёт показывает почти все формы и сравнения;
- PDF показывает альтернативную форму с дискретными элементами;
- 2D показывает инженерное сравнение основной и альтернативной формы;
- 2D при отсутствии Store может парсить `ReportText`;
- 2D при отсутствии расчётных узлов рисует fallback-линию из summary-полей.

## 10. Где дублируются статусы: OK / INFO / WARNING / Fail

| Место | Примеры статусов | Проблема |
|---|---|---|
| `BuoyCalculator` | `OK`, `FAILED`, `WARNING`, `ERROR`, `INFO` | Эти статусы формируют verdict и checks. Это технический слой ядра. |
| `MooringShapeSolver` | `OK`, `INFO`, `WARNING` | Технические статусы формы. |
| `ReportBuilder` | `OK`, `WARNING`, `INFO: отличается...` | Технический отчёт. |
| `MooringIterativeSolver` | `Continue`, `Converged`, `MaxIterationsReached`, `Fail` в autocheck tables | Технические статусы итерационного слоя. |
| `EngineeringDiagnostics` | severity-логика | Нужен user-facing mapper. |
| `MainWindowViewModel.VisualizationStatusText` | `OK: длина линии...`, `WARNING: ...` | Пользователь видит технические англоязычные статусы. |
| `Mooring2DCanvas` | `альтернативная форма: OK/WARNING` | Пользователь видит технический статус. |
| `PdfReportBuilder` | `форма: ОК`, `форма: требует проверки` | Уже сделан частичный user-facing перевод, но только в PDF. |

Вывод: нужен единый `UserStatusPolicy`, который не меняет solver-статусы, а только переводит технические статусы в пользовательские формулировки.

## 11. Где пользователь видит технические термины

| Место | Термины |
|---|---|
| Главное окно / последовательность | `Cd`, `preset`, потенциально `Line/Connector/Payload`, техническая логика типов элементов. |
| `AssemblyItemViewModel.EditorHint` / `Summary` | `дискретная нагрузка`, `распределённый участок`, `форма X/Z`, `Cd`. |
| Профиль течения | `CurrentSpeed`, `U/V/W`, `U East`, `V North`, `W Vert.`, `ρ`, `v0.19`. |
| Библиотека элементов | `MBL`, `Cd`. |
| 2D | `MooringShapeSolver`, `основная X/Z`, `альтернативная X/Z`, `WARNING`, `масштаб X=Z`. |
| PDF | В основном уже user-facing, но таблица элементов всё ещё получает `row.Status` из расчётного слоя. |
| Полный отчёт | Solver, diagnostics, gate, fallback, candidate, selected, X/Z — допустимо, потому что окно заявлено как техническое. |

## 12. Где PDF, 2D и полный отчёт берут разные данные

| Выход | Текущий источник данных | Риск |
|---|---|---|
| Полный отчёт | `ReportBuilder.Build` создаёт расчётно-диагностический pipeline и пишет `ReportText` | Отчёт одновременно текст и источник побочных эффектов через Store. |
| PDF | `PdfReportBuilder.Build` получает `resultText`, `sequenceLines`, `elementRows`, `reportText`, visualization fields и читает `MooringAlternativeShapeStore.Current` | PDF не использует единый `SelectedShape`. При отсутствии Store частично читает метрики из Markdown-текста. |
| 2D | `Mooring2DCanvas` читает `MooringShapeStore.Current`, `MooringAlternativeShapeStore.Current`, затем при необходимости парсит `ReportText`, затем рисует fallback | 2D может показать не тот же источник, что PDF. Парсинг отчёта делает визуализацию зависимой от текста. |
| Главное окно | `ResultText`, `Visualization...` fields, `ElementRows`, `SequenceDiagramLines` | Пользовательская сводка строится отдельно от PDF и полного отчёта. |

Главный архитектурный вывод: нужен не “ещё один фикс 2D”, а единая read model результата расчёта, из которой читают user UI, PDF, 2D и technical report.

## 13. Файлы, которые стали слишком большими или смешивают ответственности

| Файл | Почему проблемный | Что делать позже |
|---|---|---|
| `ViewModels/MainWindowViewModel.cs` | UI-состояние, проект, библиотеки, расчёт, отчёты, визуализация, статусы | Разделить orchestration/result state/report state/library state. Не в первом кодовом шаге. |
| `Services/ReportBuilder.cs` | Генерирует текст и одновременно строит весь diagnostic pipeline + Store side effects | Разделить расчётный result pipeline и text builders. |
| `Services/PdfReportBuilder.cs` | Генерация PDF, рисование схемы, чтение Store, парсинг report text, status normalization | Перевести на `UserReportModel` / `SelectedShapeStore`. |
| `Views/Mooring2DCanvas.cs` | Отрисовка, выбор источника формы, сравнение форм, парсинг Markdown, fallback geometry | После стабилизации формы сделать canvas чистым renderer-ом. |
| `Views/MainWindow.axaml.cs` | Открытие окон, text overrides, PDF export, file dialog service | Разнести UI orchestration, export service, dialog service. |
| `ViewModels/ElementLibraryViewModel.cs` | Пять редакторов библиотеки в одном классе | Позже разделить по типам элементов или сделать общий generic pattern. |
| `Views/ElementLibraryWindow.axaml` | Очень плотный XAML, трудно сопровождать и точечно менять | Разделить вкладки/шаблоны позже. |
| `Models/EngineeringModels.cs` | Модели и `BuoyCalculator` в одном файле | Разделять только после тестового проекта. |
| `Services/MooringIterativeSolver.cs` | Solver, method notes, report tables, Store | Не менять физику; позже отделить store/report snippets. |
| `Services/MooringPrimaryShapeGate.cs` | Gate, selector, store в одном файле | После `SelectedShapeStore` можно разделить. |

## 14. Итог аудита

Текущее состояние проекта работоспособное, но архитектурно нестабильное вокруг четырёх тем:

1. Версия приложения хранится в нескольких местах.
2. Форма постановки имеет несколько конкурирующих представлений: fallback, alternative, candidate, selected.
3. Технические статусы напрямую попадают в пользовательский UI.
4. PDF, 2D, главное окно и полный отчёт читают данные из разных источников.

Поэтому следующий этап должен быть не функциональным, а стабилизационным:

```text
сначала единые источники версии, статуса, выбранной формы и отчётных моделей;
потом тесты;
только потом правка 2D;
только потом 3D;
только потом усиление физики solver.
```
