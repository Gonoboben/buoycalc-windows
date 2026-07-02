# 2026-07-02 — TechnicalReportBuilder подключён к Markdown boundary

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-22`.

Что изменено:

```text
Services/TechnicalReportBuilder.cs теперь вызывает:
TechnicalReportMarkdownBuilder.Build(...)
```

Было:

```text
return ReportBuilder.Build(projectName, environment, buoy, anchor, result);
```

Стало:

```text
return TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, result);
```

Что важно:

```text
Markdown-вывод не меняется.
TechnicalReportMarkdownBuilder на этом шаге всё ещё совместимо делегирует в существующий ReportBuilder.Build(...).
ReportBuilder.cs не менялся.
Порядок публикации stores не менялся.
```

Что сохранено:

```text
Текст полного технического отчёта не менялся.
Порядок разделов не менялся.
Форматирование чисел не менялось.
TechnicalReportDataBuilder.cs не менялся.
TechnicalReportData.cs не менялся.
TechnicalReportStorePublisher.cs не менялся.
PDF-отчёт не менялся.
Расчётная физика и solver не менялись.
```

Почему это отдельный шаг:

```text
Это малый безопасный шаг перед переносом большого тела Markdown-рендера из ReportBuilder в TechnicalReportMarkdownBuilder.
Сначала boundary включается в основную цепочку, затем следующим PR можно переносить тело рендера без изменения output.
```

Следующий допустимый шаг:

```text
refactor: move ReportBuilder Markdown body into TechnicalReportMarkdownBuilder without changing output
```

Статус CI:

```text
ожидает проверки PR
```
