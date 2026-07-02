# 2026-07-02 — report checks section moved

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-36-report-checks`.

Что изменено:

```text
Добавлен Services/TechnicalReportMarkdownCheckSections.cs.
В него перенесена секция:
- AppendChecks
```

Как подключено:

```text
TechnicalReportMarkdownSectionBridge.Append(...) теперь проверяет:
- TechnicalReportMarkdownMovedSections.TryAppend(...)
- TechnicalReportMarkdownDiscreteShapeSections.TryAppend(...)
- TechnicalReportMarkdownDiscreteTensionSections.TryAppend(...)
- TechnicalReportMarkdownDiscreteNodeSections.TryAppend(...)
- TechnicalReportMarkdownIterativeSolverSections.TryAppend(...)
- TechnicalReportMarkdownCheckSections.TryAppend(...)

После этого legacy fallback в ReportBuilder остаётся только как защитный механизм для ещё не выявленных helper-вызовов.
```

Что важно:

```text
Markdown-output не меняется.
Тексты строк перенесены без изменения.
Форматирование сохранено.
Порядок разделов сохранён.
```

Что осталось через legacy bridge:

```text
Нет известных markdown-секций технического отчёта.
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
refactor: review remaining ReportBuilder legacy fallback without changing output
```

Статус CI:

```text
ожидает проверки PR
```
