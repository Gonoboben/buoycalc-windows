# 2026-07-01 — добавлен TechnicalReportBuilder wrapper

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `technical-report-wrapper`.

Что изменено:

```text
Добавлен Services/TechnicalReportBuilder.cs.
TechnicalReportBuilder.Build(...) пока является wrapper вокруг существующего ReportBuilder.Build(...).
Services/ReportBuildBoundary.cs теперь получает TechnicalReportText через TechnicalReportBuilder.Build(...), а не напрямую через ReportBuilder.Build(...).
```

Что сознательно не трогали:

```text
ReportBuilder
UserResultTextBuilder
PdfReportBuilder
MainWindowViewModel
BuoyCalculator
MooringShapeSolver
логику расчёта
текст пользовательского итога
текст полного технического отчёта
```

Почему это важно:

```text
Появилась явная точка расширения для полного технического отчёта. Следующий шаг сможет постепенно переносить технический отчёт из ReportBuilder в TechnicalReportBuilder без изменения пользовательского вывода.
```

Следующий допустимый шаг:

```text
refactor: introduce UserReportBuilder wrapper
```

Статус CI:

```text
ожидает проверки PR
```

Примечание:

```text
Основной docs/CONTROL_MARK_UPDATES.md большой. Чтобы не потерять предыдущие записи при полном перезаписывании файла через contents API, этот шаг зафиксирован отдельной контрольной заметкой. При наличии безопасного patch-режима запись нужно перенести в основной журнал.
```
