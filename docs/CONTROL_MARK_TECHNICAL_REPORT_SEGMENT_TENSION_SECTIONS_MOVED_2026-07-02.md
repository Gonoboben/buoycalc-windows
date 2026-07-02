# 2026-07-02 — segment and tension sections moved

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-29`.

Что изменено:

```text
В Services/TechnicalReportMarkdownMovedSections.cs перенесены секции:
- AppendSegmentRows
- AppendTensionRows
- SampleRows
```

Как подключено:

```text
TechnicalReportMarkdownMovedSections.TryAppend(...) теперь обрабатывает:
- AppendVectorBalanceRows
- AppendElementRows
- AppendSequencePositionRows
- AppendModelCoverageRows
- AppendSegmentRows
- AppendTensionRows
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
Форма X/Z.
Shape projection.
Shape-based force/tension секции.
Дискретные нагрузки.
Итерационный solver.
Проверки.
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
refactor: move shape rows markdown sections without changing output
```

Статус CI:

```text
ожидает проверки PR
```
