# 2026-07-01 — добавлен TechnicalReportStorePublisher

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-19`.

Что изменено:

```text
Добавлен Services/TechnicalReportStorePublisher.cs.
Publisher содержит явный метод Publish(TechnicalReportData data).
Метод публикует shape stores через:
MooringShapeStore.Set(data.Shape)
MooringIterativeSolverStore.Set(data.IterativeSolver)
```

Что пока не изменено:

```text
ReportBuilder.cs пока не переключался на TechnicalReportStorePublisher.Publish(data).
Порядок публикации store в рабочем коде не менялся.
Поведение расчёта и отчёта не менялось.
```

Что сознательно не трогали:

```text
ReportBuilder.cs
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

Почему это важно:

```text
Появилась явная точка публикации technical report stores. Следующий малый шаг сможет заменить две строки в ReportBuilder.Build(...) на TechnicalReportStorePublisher.Publish(data), не меняя порядок публикации и не меняя Markdown-вывод.
```

Следующий допустимый шаг:

```text
refactor: connect ReportBuilder store publishing to TechnicalReportStorePublisher
```

Статус CI:

```text
ожидает проверки PR
```

Примечание:

```text
ReportBuilder.cs большой и не должен перезаписываться целиком через contents API без полного безопасного patch-режима. Поэтому подключение ReportBuilder к publisher будет отдельным шагом или ручным точечным патчем.
```
