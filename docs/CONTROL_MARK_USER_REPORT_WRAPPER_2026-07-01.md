# 2026-07-01 — добавлен UserReportBuilder wrapper

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `user-report-wrapper`.

Что изменено:

```text
Добавлен Services/UserReportBuilder.cs.
UserReportBuilder.Build(...) пока является wrapper вокруг существующего UserResultTextBuilder.Build(...).
Services/ReportBuildBoundary.cs теперь получает UserResultText через UserReportBuilder.Build(...), а не напрямую через UserResultTextBuilder.Build(...).
```

Что сознательно не трогали:

```text
UserResultTextBuilder
TechnicalReportBuilder
ReportBuilder
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
Появилась явная точка расширения для пользовательского отчёта. Следующий шаг сможет постепенно переносить пользовательский вывод из UserResultTextBuilder в UserReportBuilder без изменения технического отчёта.
```

Следующий допустимый шаг:

```text
refactor: rename or retire UserResultTextBuilder after boundary is stable
```

Статус CI:

```text
ожидает проверки PR
```

Примечание:

```text
Основной docs/CONTROL_MARK_UPDATES.md большой. Чтобы не потерять предыдущие записи при полном перезаписывании файла через contents API, этот шаг зафиксирован отдельной контрольной заметкой. При наличии безопасного patch-режима запись нужно перенести в основной журнал.
```
