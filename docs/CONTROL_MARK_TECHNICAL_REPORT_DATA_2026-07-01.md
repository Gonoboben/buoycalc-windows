# 2026-07-01 — добавлена модель TechnicalReportData

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-14`.

Что изменено:

```text
Добавлен Services/TechnicalReportData.cs.
Модель собирает набор технических результатов, которые сейчас создаются в начале ReportBuilder.Build(...).
ReportBuilder.cs пока не изменялся.
TechnicalReportBuilder.cs пока не изменялся.
Поведение полного технического отчёта не менялось.
```

Что сознательно не трогали:

```text
ReportBuilder.cs
TechnicalReportBuilder.cs
ReportBuildBoundary.cs
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
Появилась явная модель данных технического отчёта. Следующий шаг сможет добавить TechnicalReportDataBuilder.Build(...) и перенести в него создание technical result objects без изменения Markdown-вывода ReportBuilder.
```

Следующий допустимый шаг:

```text
refactor: introduce TechnicalReportDataBuilder without changing ReportBuilder output
```

Статус CI:

```text
ожидает проверки PR
```

Примечание:

```text
Основной docs/CONTROL_MARK_UPDATES.md большой. Чтобы не потерять предыдущие записи при полном перезаписывании файла через contents API, этот шаг зафиксирован отдельной контрольной заметкой. При наличии безопасного patch-режима запись нужно перенести в основной журнал.
```
