# 2026-07-01 — UserResultTextBuilder wrapper удалён

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `retire-user-result-builder`.

Что изменено:

```text
Удалён Services/UserResultTextBuilder.cs.
Перед удалением выполнен повторный поиск по UserResultTextBuilder.
Поиск не нашёл оставшихся ссылок.
Пользовательский итог теперь собирается через Services/UserReportBuilder.cs.
```

Что сознательно не трогали:

```text
UserReportBuilder.cs
ReportBuildBoundary.cs
TechnicalReportBuilder.cs
ReportBuilder.cs
PdfReportBuilder.cs
MainWindowViewModel.cs
BuoyCalculator
MooringShapeSolver
логику расчёта
текст пользовательского итога
текст полного технического отчёта
```

Почему это важно:

```text
Старый промежуточный builder удалён после переноса реализации и аудита ссылок. В пользовательском report-слое осталась одна основная точка сборки: UserReportBuilder.
```

Следующий допустимый шаг:

```text
refactor: audit ReportBuilder responsibilities before TechnicalReportBuilder extraction
```

Статус CI:

```text
ожидает проверки PR
```

Примечание:

```text
Основной docs/CONTROL_MARK_UPDATES.md большой. Чтобы не потерять предыдущие записи при полном перезаписывании файла через contents API, этот шаг зафиксирован отдельной контрольной заметкой. При наличии безопасного patch-режима запись нужно перенести в основной журнал.
```
