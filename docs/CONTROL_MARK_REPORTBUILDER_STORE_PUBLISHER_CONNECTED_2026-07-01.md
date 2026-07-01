# 2026-07-01 — ReportBuilder подключён к TechnicalReportStorePublisher

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-20`.

Что изменено:

```text
В Services/ReportBuilder.cs прямые store writes заменены на явный publisher-вызов:
TechnicalReportStorePublisher.Publish(data)
```

Было:

```text
MooringShapeStore.Set(shape)
MooringIterativeSolverStore.Set(iterativeSolver)
```

Стало:

```text
TechnicalReportStorePublisher.Publish(data)
```

Что сохранено:

```text
Публикация stores остаётся в том же месте Build(...), сразу после подготовки technical data locals.
Порядок публикации внутри publisher сохранён:
1. MooringShapeStore.Set(data.Shape)
2. MooringIterativeSolverStore.Set(data.IterativeSolver)
Markdown-вывод ReportBuilder не менялся.
```

Что сознательно не трогали:

```text
TechnicalReportDataBuilder.cs
TechnicalReportData.cs
TechnicalReportBuilder.cs
ReportBuildBoundary.cs
UserReportBuilder.cs
PdfReportBuilder.cs
MainWindowViewModel.cs
BuoyCalculator
MooringShapeSolver
MooringIterativeSolver
логику расчёта
текст пользовательского итога
текст полного технического отчёта
```

Следующий допустимый шаг:

```text
refactor: prepare TechnicalReportMarkdownBuilder boundary without changing output
```

Статус CI:

```text
ожидает проверки PR
```

Примечание:

```text
При ручной точечной правке ReportBuilder использовать номера строк из GitHub editor. Для этой правки заменялись две строки store writes на одну строку publisher-вызова.
```
