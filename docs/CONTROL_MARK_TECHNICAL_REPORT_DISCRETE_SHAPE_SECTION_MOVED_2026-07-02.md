# 2026-07-02 — discrete shape section moved

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-32-discrete`.

Что изменено:

```text
Добавлен Services/TechnicalReportMarkdownDiscreteShapeSections.cs.
В него перенесена секция:
- AppendDiscreteLoadShapeRows
```

Как подключено:

```text
TechnicalReportMarkdownSectionBridge.Append(...) сначала проверяет:
- TechnicalReportMarkdownMovedSections.TryAppend(...)
- TechnicalReportMarkdownDiscreteShapeSections.TryAppend(...)

Если секция не перенесена, bridge продолжает использовать legacy helper из ReportBuilder.
```

Что важно:

```text
Markdown-output не меняется.
Тексты строк перенесены без изменения.
Форматирование чисел сохранено.
Порядок разделов сохранён.
```

Что осталось через legacy bridge:

```text
AppendDiscreteLoadTensionRows.
AppendAlternativeDiscreteNodeRows.
AppendIterativeSolverRows.
AppendChecks.
```

Что сознательно не трогали:

```text
ReportBuilder.cs не менялся.
TechnicalReportMarkdownBuilder.cs не менялся.
TechnicalReportBuilder.cs не менялся.
TechnicalReportDataBuilder.cs не менялся.
TechnicalReportData.cs не менялся.
TechnicalReportStorePublisher.cs не менялся.
PDF-отчёт не менялся.
Расчётная физика и solver не менялись.
```

Следующий допустимый шаг:

```text
refactor: move discrete load tension markdown section without changing output
```

Статус CI:

```text
ожидает проверки PR
```
