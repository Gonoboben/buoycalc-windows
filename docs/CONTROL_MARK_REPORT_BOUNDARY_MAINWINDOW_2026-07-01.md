# 2026-07-01 — MainWindow подключён к ReportBuildBoundary

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `report-boundary-mainwindow`.

Что изменено:

```text
ViewModels/MainWindowViewModel.cs больше не присваивает ResultText и ReportText напрямую через UserResultTextBuilder и ReportBuilder.
В Calculate() добавлен вызов ReportBuildBoundary.Build(ProjectName, environment, buoy, anchor, result).
ResultText теперь берётся из reports.UserResultText.
ReportText теперь берётся из reports.TechnicalReportText.
```

Что сознательно не трогали:

```text
ReportBuildBoundary
ReportBuilder
UserResultTextBuilder
PdfReportBuilder
BuoyCalculator
MooringShapeSolver
логику расчёта
текст пользовательского итога
текст полного технического отчёта
```

Почему это важно:

```text
MainWindow теперь использует явную границу отчётов. Это подготавливает дальнейшее разделение UserReportBuilder и TechnicalReportBuilder без изменения текущего содержимого отчётов.
```

Следующий допустимый шаг:

```text
refactor: introduce TechnicalReportBuilder wrapper
```

Статус CI:

```text
ожидает проверки PR
```

Примечание:

```text
Основной docs/CONTROL_MARK_UPDATES.md большой. Чтобы не потерять предыдущие записи при полном перезаписывании файла через contents API, этот шаг зафиксирован отдельной контрольной заметкой. При наличии безопасного patch-режима запись нужно перенести в основной журнал.
```
