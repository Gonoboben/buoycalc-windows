# 2026-07-01 — аудит ссылок на UserResultTextBuilder

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `user-result-builder-audit`.

Что проверено:

```text
Выполнен поиск по репозиторию по строкам UserResultTextBuilder и UserResultTextBuilder Build.
GitHub code search не вернул внешних ссылок. Нужно учитывать возможную задержку индекса после свежего merge.
Дополнительно вручную проверены ключевые файлы на ветке user-result-builder-audit.
```

Фактическое состояние:

```text
Services/ReportBuildBoundary.cs уже вызывает UserReportBuilder.Build(...) для пользовательского текста.
Services/UserResultTextBuilder.cs сейчас является совместимым wrapper и только делегирует вызов в UserReportBuilder.Build(...).
```

Что сознательно не трогали:

```text
UserResultTextBuilder.cs не удалялся.
UserReportBuilder.cs не менялся.
ReportBuildBoundary.cs не менялся.
TechnicalReportBuilder.cs не менялся.
ReportBuilder.cs не менялся.
PdfReportBuilder.cs не менялся.
MainWindowViewModel.cs не менялся.
BuoyCalculator и solver не менялись.
```

Почему это важно:

```text
Перед удалением старого UserResultTextBuilder подтверждено, что основная цепочка пользовательского отчёта уже идёт через UserReportBuilder. Следующий шаг может быть отдельным PR на удаление UserResultTextBuilder, если повторный поиск перед удалением также не найдёт внешних ссылок.
```

Следующий допустимый шаг:

```text
refactor: retire UserResultTextBuilder compatibility wrapper
```

Статус CI:

```text
ожидает проверки PR
```

Примечание:

```text
Основной docs/CONTROL_MARK_UPDATES.md большой. Чтобы не потерять предыдущие записи при полном перезаписывании файла через contents API, этот шаг зафиксирован отдельной контрольной заметкой. При наличии безопасного patch-режима запись нужно перенести в основной журнал.
```
