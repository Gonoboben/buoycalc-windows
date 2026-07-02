# 2026-07-02 — basic Markdown sections moved

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-25`.

Что изменено:

```text
В Services/TechnicalReportMarkdownBuilder.cs перенесены первые простые section helpers:
- AppendEnvironment
- AppendBuoy
- AppendAnchor
- DisplayBuoyState
```

Что важно:

```text
Markdown-output не меняется.
Тексты строк перенесены без изменения.
Форматирование чисел сохранено.
Порядок разделов сохранён.
```

Что осталось через временный bridge:

```text
Сводные итоги.
Диагностика.
Векторный баланс.
Таблицы элементов.
Позиционная модель.
Расчётные сегменты.
Натяжения.
Форма X/Z.
Shape-based секции.
Дискретные нагрузки.
Итерационный solver.
Проверки.
```

Что сознательно не трогали:

```text
ReportBuilder.cs не менялся.
TechnicalReportBuilder.cs не менялся.
TechnicalReportDataBuilder.cs не менялся.
TechnicalReportData.cs не менялся.
TechnicalReportStorePublisher.cs не менялся.
PDF-отчёт не менялся.
Расчётная физика и solver не менялись.
```

Следующий допустимый шаг:

```text
refactor: move totals and diagnostics markdown sections into TechnicalReportMarkdownBuilder without changing output
```

Статус CI:

```text
ожидает проверки PR
```
