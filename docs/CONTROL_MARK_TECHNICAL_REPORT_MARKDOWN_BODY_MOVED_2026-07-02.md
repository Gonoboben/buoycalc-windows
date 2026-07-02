# 2026-07-02 — Markdown assembly moved into TechnicalReportMarkdownBuilder

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-23`.

Что изменено:

```text
Services/TechnicalReportMarkdownBuilder.cs больше не делегирует напрямую в ReportBuilder.Build(...).
Верхнеуровневая сборка полного Markdown-отчёта перенесена в TechnicalReportMarkdownBuilder.Build(...).
```

Что сохранено для стабильности output:

```text
Существующие section append helpers пока переиспользуются из ReportBuilder.
Порядок подготовки TechnicalReportData сохранён.
Порядок публикации TechnicalReportStorePublisher.Publish(data) сохранён.
Порядок разделов отчёта сохранён.
Текст заголовков и строк сохранён.
Форматирование чисел сохранено.
```

Что сознательно не трогали:

```text
ReportBuilder.cs не менялся.
TechnicalReportBuilder.cs не менялся после предыдущего шага.
TechnicalReportDataBuilder.cs не менялся.
TechnicalReportData.cs не менялся.
TechnicalReportStorePublisher.cs не менялся.
PDF-отчёт не менялся.
Расчётная физика и solver не менялись.
```

Почему helpers пока остались в ReportBuilder:

```text
Это снижает риск изменения output при переносе большого Markdown-рендера.
На этом шаге переносится основная assembly-точка, а сами таблицы остаются прежними.
Следующими малыми шагами helpers можно переносить из ReportBuilder в TechnicalReportMarkdownBuilder по группам.
```

Следующий допустимый шаг:

```text
refactor: make ReportBuilder a compatibility facade after Markdown boundary owns the assembly
```

Статус CI:

```text
ожидает проверки PR
```
