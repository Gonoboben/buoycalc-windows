# BuoyCalc — отменённые и контрольные шаги

## v0.46.5 — отменён

Статус: отменено по решению пользователя.

Причина:

```text
Шаг был сделан преждевременно. Пользователь указал, что сначала требуется не точечная правка 2D, а оптимизация структуры проекта и согласованный архитектурный план.
```

Что откатили:

```text
Services/AppInfo.cs возвращён к v0.46.4.
Views/Mooring2DCanvas.cs возвращён к состоянию v0.46.4.
```

Что не нужно делать дальше без плана:

```text
не менять 2D
не добавлять 3D
не менять solver
не добавлять новые функции
не делать точечные UI-фиксы
```

Следующий правильный шаг:

```text
Подготовить архитектурный аудит и план стабилизации проекта.
Сначала документы и согласование, потом код.
```

## 2026-06-29 — уточнён порядок refactor-плана

Статус: зафиксировано по подтверждению пользователя.

Что изменено:

```text
docs/REFACTOR_PLAN.md обновлён.
3D перенесён после solver.
Добавлено рабочее правило: перед каждым шагом сверяться с планом, после шага обновлять журнал изменений.
```

Почему:

```text
3D — это визуализация, а не инженерное ядро. Он не должен идти раньше solver как приоритет.
2D может идти раньше solver только как архитектурная очистка отображения выбранной формы, без изменения физики.
```

Что сознательно не трогали:

```text
код приложения
solver
2D canvas
PDF builder
AppInfo
окна Avalonia
```

Следующий допустимый шаг:

```text
refactor: use AppInfo in WindowVersionHelper
```

## 2026-06-29 — WindowVersionHelper переведён на AppInfo

Пункт плана:

```text
1. Единый источник версии
```

Статус: выполнено.

Что изменено:

```text
Views/WindowVersionHelper.cs больше не содержит собственного CurrentVersion / CurrentVersionNote.
WindowVersionHelper читает AppInfo.Version для заголовков окон.
WindowVersionHelper читает AppInfo.DisplayVersion для замены старых version badges в runtime.
```

Что сознательно не трогали:

```text
Services/AppInfo.cs
solver
MooringShapeSolver
Mooring2DCanvas
PDF builder
XAML-разметку окон
расчётную модель
```

Почему это важно:

```text
В проекте убран второй источник актуальной версии v0.38.4 внутри WindowVersionHelper.
Актуальная версия остаётся в Services/AppInfo.cs.
```

Следующий допустимый шаг:

```text
audit: add XAML version scan script
```

Статус CI:

```text
ожидает проверки после коммита
```

## 2026-06-29 — добавлен ручной audit XAML-версий

Пункт плана:

```text
2. Audit-скрипт против захардкоженных v0.* в XAML
```

Статус: выполнено как ручной audit-step.

Что изменено:

```text
Добавлен scripts/audit-xaml-versions.ps1.
Скрипт ищет строки v0. в файлах Views/**/*.axaml.
По умолчанию скрипт только сообщает найденные строки и не ломает сборку.
Для строгого режима добавлен флаг -FailOnFinding.
```

Что сознательно не трогали:

```text
CI workflow
XAML-разметку окон
WindowVersionHelper
AppInfo
solver
MooringShapeSolver
Mooring2DCanvas
PDF builder
расчётную модель
```

Почему это важно:

```text
Теперь перед правкой пользовательского интерфейса можно быстро увидеть старые v0.* версии в XAML.
Скрипт пока не подключён к CI, потому что baseline старых XAML-версий ещё не очищен и не согласован.
```

Следующий допустимый шаг:

```text
refactor: introduce UserStatusPolicy
```

Статус CI:

```text
ожидает проверки после коммита
```

## 2026-06-29 — введён UserStatusPolicy

Пункт плана:

```text
3. UserStatusPolicy
```

Статус: начат и подключён к первому display-слою.

Что изменено:

```text
Добавлен Services/UserStatusPolicy.cs.
Политика переводит технические префиксы OK / INFO / WARNING / FAILED / ERROR в пользовательские формулировки.
Services/VerdictDisplayAdvisor.cs подключён к UserStatusPolicy для отображаемого вердикта и главного риска.
```

Что сознательно не трогали:

```text
BuoyCalculator
CalculationResult
технические Checks
ReportBuilder
PDF builder
MooringShapeSolver
Mooring2DCanvas
XAML-разметку окон
расчётную модель
```

Почему это важно:

```text
Технические статусы остаются внутри расчётного ядра и диагностики.
Пользовательские формулировки начинают собираться на границе отображения.
```

Следующий допустимый шаг:

```text
refactor: wire UserStatusPolicy into short UI summary
```

Статус CI:

```text
ожидает проверки после коммита
```

## 2026-06-29 — UserStatusPolicy подключён к таблице элементов

Пункт плана:

```text
3. UserStatusPolicy
```

Статус: продолжено для пользовательского UI/PDF-вывода.

Что изменено:

```text
Models/ElementCalculationDisplayRow.cs теперь переводит row.Status через UserStatusPolicy.ToUserStatus(...).
Технические статусы элементов из расчётного ядра не изменяются.
Пользовательская таблица элементов получает человекочитаемый статус.
```

Что сознательно не трогали:

```text
BuoyCalculator
CalculationResult
технические Checks
ReportBuilder
MooringShapeSolver
Mooring2DCanvas
XAML-разметку окон
логику расчёта
```

Почему это важно:

```text
Колонка Status в пользовательской таблице и PDF больше не должна показывать сырые OK / INFO / WARNING без перевода.
```

Следующий допустимый шаг:

```text
refactor: wire UserStatusPolicy into MainWindow short ResultText
```

Статус CI:

```text
ожидает проверки после коммита
```

## 2026-06-29 — добавлен UserResultTextBuilder

Пункт плана:

```text
3. UserStatusPolicy
```

Статус: подготовлен отдельный builder для краткого пользовательского итога.

Что изменено:

```text
Добавлен Services/UserResultTextBuilder.cs.
Builder собирает краткий итог через VerdictDisplayAdvisor и UserStatusPolicy.
Текстовые подписи краткого итога приведены к пользовательским формулировкам: чистая плавучесть, нагрузка слабого звена, главный риск без технических префиксов.
```

Что сознательно не трогали:

```text
MainWindowViewModel.cs
BuoyCalculator
CalculationResult
технические Checks
ReportBuilder
MooringShapeSolver
Mooring2DCanvas
XAML-разметку окон
логику расчёта
```

Почему это важно:

```text
Краткий итог вынесен в отдельный display-builder. Следующий коммит к MainWindowViewModel должен заменить ручную сборку ResultText на UserResultTextBuilder.Build(...).
```

Следующий допустимый шаг:

```text
refactor: connect MainWindow ResultText to UserResultTextBuilder
```

Статус CI:

```text
ожидает проверки после коммита
```

## 2026-06-29 — MainWindow ResultText подключён к UserResultTextBuilder

Пункт плана:

```text
3. UserStatusPolicy
```

Статус: выполнено для краткого пользовательского итога главного окна.

Что изменено:

```text
ViewModels/MainWindowViewModel.cs больше не собирает ResultText вручную после расчёта.
ResultText теперь берётся из UserResultTextBuilder.Build(environment, result).
Diff по MainWindowViewModel: 1 строка добавлена, 1 строка удалена.
```

Что сознательно не трогали:

```text
BuoyCalculator
CalculationResult
технические Checks
ReportBuilder
PDF builder
MooringShapeSolver
Mooring2DCanvas
XAML-разметку окон
логику расчёта
```

Почему это важно:

```text
Краткий пользовательский итог главного окна теперь использует общий display-слой.
```

Следующий допустимый шаг:

```text
refactor: wire UserStatusPolicy into visualization status summary
```

Статус CI:

```text
ожидает проверки после коммита
```

## 2026-06-30 — VisualizationStatusText проходит через UserStatusPolicy

Пункт плана:

```text
3. UserStatusPolicy
```

Статус: выполнено для краткого статуса визуализации.

Что изменено:

```text
ViewModels/MainWindowViewModel.cs: setter VisualizationStatusText теперь применяет UserStatusPolicy.ToUserStatus(...).
Diff по MainWindowViewModel: 1 строка добавлена, 1 строка удалена.
```

Что сознательно не трогали:

```text
BuoyCalculator
CalculationResult
ReportBuilder
PDF builder
MooringShapeSolver
Mooring2DCanvas
XAML-разметку окон
логику расчёта
```

Почему это важно:

```text
Краткий статус визуализации в главном окне теперь проходит через общий пользовательский display-слой.
```

Следующий допустимый шаг:

```text
refactor: introduce SelectedShapeStore read model
```

Статус CI:

```text
ожидает проверки после коммита
```

## 2026-06-30 — добавлен SelectedShapeStore read model

Пункт плана:

```text
4. SelectedShapeStore
```

Статус: добавлен read-model без подключения 2D/PDF.

Что изменено:

```text
Добавлен Services/SelectedShapeStore.cs.
Добавлен SelectedShapeReadModel.
SelectedShapeStore читает MooringPrimaryShapeSelectionStore.Current, если выбор формы уже есть.
Если gate selection отсутствует, SelectedShapeStore возвращает fallback из MooringShapeStore.Current.
```

Что сознательно не трогали:

```text
MooringShapeSolver
MooringIterativeSolver
MooringPrimaryShapeGate
MooringShapeStore
MooringPrimaryShapeSelectionStore
Mooring2DCanvas
PdfReportBuilder
ReportBuilder
логику расчёта
```

Почему это важно:

```text
Появился единый read-фасад для выбранной формы. Следующие UI/PDF-шаги смогут читать выбранную форму через SelectedShapeStore, а не напрямую из нескольких технических store.
```

Следующий допустимый шаг:

```text
refactor: read selected shape from SelectedShapeStore in 2D canvas
```

Статус CI:

```text
ожидает проверки после коммита
```
