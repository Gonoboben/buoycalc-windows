# 2026-07-01 — ReportBuilder подключён к TechnicalReportDataBuilder

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: проверяется в PR-ветке `step-18`.

Что изменено вручную на `main`:

```text
Services/ReportBuilder.cs в начале Build(...) теперь получает technical result objects через TechnicalReportDataBuilder.Build(environment, result).
Локальные переменные tensionRows, shape, shapeProjection, shapeForces, shapeTensions, sequencePositions, discreteLoadTensions, discreteLoadShape, alternativeDiscreteNodes, iterativeSolver, diagnostics и vectorBalance берутся из TechnicalReportData.
Отступ строки var sb = new StringBuilder(); исправлен.
```

Что сохранено:

```text
MooringShapeStore.Set(shape) остаётся после получения shape.
MooringIterativeSolverStore.Set(iterativeSolver) остаётся после получения iterativeSolver.
Порядок публикации store сохранён.
Append... секции ReportBuilder не менялись.
Markdown-вывод полного технического отчёта не должен измениться.
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

Проверка:

```text
Так как изменение ReportBuilder было загружено напрямую в main, этот PR добавляет только контрольную заметку поверх текущего main, чтобы .NET Build проверил уже изменённое состояние проекта через обычный PR-triggered workflow.
```

Следующий допустимый шаг:

```text
после успешного CI — merge PR и затем refactor: move store publishing behind explicit method
```

Статус CI:

```text
ожидает проверки PR
```
