# 2026-07-02 — Markdown section bridge extracted

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-24`.

Что изменено:

```text
Добавлен временный bridge:
Services/TechnicalReportMarkdownSectionBridge.cs
```

```text
Services/TechnicalReportMarkdownBuilder.cs теперь вызывает section helpers через:
TechnicalReportMarkdownSectionBridge.Append(...)
```

Что важно:

```text
Markdown-output не меняется.
Сами section helpers пока остаются в ReportBuilder.
Reflection-доступ к legacy helpers вынесен из TechnicalReportMarkdownBuilder в отдельный временный bridge.
```

Что сохранено:

```text
ReportBuilder.cs не менялся.
TechnicalReportBuilder.cs не менялся.
TechnicalReportDataBuilder.cs не менялся.
TechnicalReportData.cs не менялся.
TechnicalReportStorePublisher.cs не менялся.
Порядок разделов отчёта не менялся.
Текст строк отчёта не менялся.
Форматирование чисел не менялось.
PDF-отчёт не менялся.
Расчётная физика и solver не менялись.
```

Почему фасад ReportBuilder не сделан в этом же шаге:

```text
ReportBuilder всё ещё владеет private section helpers.
Перед превращением ReportBuilder.Build(...) в тонкий compatibility facade нужно безопасно стабилизировать временную связь с этими helpers или переносить helpers группами.
Этот шаг изолирует bridge и снижает риск изменения output.
```

Следующий допустимый шаг:

```text
refactor: move first ReportBuilder section helper group into TechnicalReportMarkdownBuilder without changing output
```

Статус CI:

```text
ожидает проверки PR
```
