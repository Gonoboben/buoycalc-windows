# 2026-07-02 — TechnicalReportMarkdownSectionBridge fallback review

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-37-bridge-fallback-review`.

Что проверено:

```text
TechnicalReportMarkdownBuilder.Build(...) вызывает TechnicalReportMarkdownSectionBridge.Append(...) только для известных markdown-секций технического отчёта.
```

Проверенные bridge-вызовы:

```text
- AppendVectorBalanceRows
- AppendElementRows
- AppendSequencePositionRows
- AppendModelCoverageRows
- AppendSegmentRows
- AppendTensionRows
- AppendShapeRows
- AppendShapeProjectionRows
- AppendShapeForceRows
- AppendShapeTensionRows
- AppendDiscreteLoadTensionRows
- AppendDiscreteLoadShapeRows
- AppendAlternativeDiscreteNodeRows
- AppendIterativeSolverRows
- AppendChecks
```

Куда они маршрутизируются после предыдущих шагов:

```text
- TechnicalReportMarkdownMovedSections
- TechnicalReportMarkdownDiscreteShapeSections
- TechnicalReportMarkdownDiscreteTensionSections
- TechnicalReportMarkdownDiscreteNodeSections
- TechnicalReportMarkdownIterativeSolverSections
- TechnicalReportMarkdownCheckSections
```

Вывод:

```text
Известных markdown-секций технического отчёта, которые должны проходить через reflection fallback в legacy ReportBuilder, не осталось.
```

Почему fallback пока не удалён:

```text
Reflection fallback сохранён как защитный механизм на случай невыявленных helper-вызовов.
Его удаление лучше делать отдельным шагом после дополнительной проверки по всему проекту и CI.
```

Что сознательно не трогали:

```text
Production-код не менялся.
ReportBuilder.cs не менялся.
TechnicalReportMarkdownBuilder.cs не менялся.
TechnicalReportBuilder.cs не менялся.
TechnicalReportDataBuilder.cs не менялся.
TechnicalReportData.cs не менялся.
TechnicalReportStorePublisher.cs не менялся.
PDF-отчёт не менялся.
Расчётная физика и solver не менялись.
Markdown-output не менялся.
```

Следующий допустимый шаг:

```text
refactor: remove or narrow legacy ReportBuilder fallback after full call-site verification
```

Статус CI:

```text
ожидает проверки PR
```
