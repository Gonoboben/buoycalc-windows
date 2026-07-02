# 2026-07-02 — TechnicalReportMarkdownSectionBridge legacy fallback removed

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-38-remove-legacy-fallback`.

Что изменено:

```text
В Services/TechnicalReportMarkdownSectionBridge.cs удалён reflection fallback в legacy ReportBuilder.
```

До изменения:

```text
Если ни один новый renderer-класс не принимал имя секции, bridge искал private static helper в ReportBuilder через reflection и вызывал его.
```

После изменения:

```text
Если ни один новый renderer-класс не принимает имя секции, bridge выбрасывает явную ошибку:
Technical report Markdown section renderer not found: <methodName>
```

Почему это безопасно для нормального output:

```text
Все известные вызовы TechnicalReportMarkdownSectionBridge.Append(...) из TechnicalReportMarkdownBuilder.Build(...) уже покрыты новыми renderer-классами.
Нормальный порядок разделов технического отчёта не меняется.
Тексты markdown-секций не меняются.
Форматирование чисел не меняется.
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
refactor: audit ReportBuilder after markdown extraction without changing output
```

Статус CI:

```text
ожидает проверки PR
```
