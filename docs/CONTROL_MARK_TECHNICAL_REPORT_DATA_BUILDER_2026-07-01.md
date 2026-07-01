# 2026-07-01 — добавлен TechnicalReportDataBuilder

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-15`.

Что изменено:

```text
Добавлен Services/TechnicalReportDataBuilder.cs.
Builder создаёт TechnicalReportData из тех же technical result objects, которые сейчас создаются в начале ReportBuilder.Build(...).
Порядок analyzer-вызовов сохранён относительно текущего ReportBuilder.Build(...).
ReportBuilder.cs пока не переключался на TechnicalReportDataBuilder.
MooringShapeStore.Set(...) и MooringIterativeSolverStore.Set(...) не переносились и не менялись.
```

Что сознательно не трогали:

```text
ReportBuilder.cs
TechnicalReportBuilder.cs
ReportBuildBoundary.cs
TechnicalReportData.cs
UserReportBuilder.cs
PdfReportBuilder.cs
MainWindowViewModel.cs
MooringShapeStore
MooringIterativeSolverStore
BuoyCalculator
MooringShapeSolver
MooringIterativeSolver
логику расчёта
текст пользовательского итога
текст полного технического отчёта
```

Почему это важно:

```text
Появился отдельный builder технических данных отчёта. Следующий шаг сможет подключить ReportBuilder к TechnicalReportDataBuilder с сохранением текущего Markdown-вывода и текущего порядка публикации store.
```

Следующий допустимый шаг:

```text
refactor: connect ReportBuilder to TechnicalReportDataBuilder without changing output
```

Статус CI:

```text
ожидает проверки PR
```

Примечание:

```text
Основной docs/CONTROL_MARK_UPDATES.md большой. Чтобы не потерять предыдущие записи при полном перезаписывании файла через contents API, этот шаг зафиксирован отдельной контрольной заметкой. При наличии безопасного patch-режима запись нужно перенести в основной журнал.
```
