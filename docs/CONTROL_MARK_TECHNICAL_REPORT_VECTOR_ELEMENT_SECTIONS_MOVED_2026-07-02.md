# 2026-07-02 — vector balance and element table sections moved

Пункт плана:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Статус: выполнено в PR-ветке `step-27`.

Что изменено:

```text
Добавлен Services/TechnicalReportMarkdownMovedSections.cs.
В него перенесены секции:
- AppendVectorBalanceRows
- AppendElementRows
```

Как подключено:

```text
TechnicalReportMarkdownSectionBridge.Append(...) сначала проверяет TechnicalReportMarkdownMovedSections.TryAppend(...).
Если секция уже перенесена, она рендерится новым классом.
Если секция ещё не перенесена, bridge продолжает использовать legacy helper из ReportBuilder.
```

Что важно:

```text
Markdown-output не меняется.
Тексты строк перенесены без изменения.
Форматирование чисел сохранено.
Порядок разделов сохранён.
```

Что осталось через legacy bridge:

```text
Позиционная модель.
Область учёта элементов.
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
TechnicalReportMarkdownBuilder.cs не менялся в этом шаге.
TechnicalReportBuilder.cs не менялся.
TechnicalReportDataBuilder.cs не менялся.
TechnicalReportData.cs не менялся.
TechnicalReportStorePublisher.cs не менялся.
PDF-отчёт не менялся.
Расчётная физика и solver не менялись.
```

Примечание:

```text
XML-summary в TechnicalReportMarkdownSectionBridge.cs был убран, потому что GitHub connector заблокировал update_file с комментарием.
Функционально bridge сохранил прежний fallback на ReportBuilder и добавил dispatch в TechnicalReportMarkdownMovedSections.
```

Следующий допустимый шаг:

```text
refactor: move sequence position and model coverage markdown sections without changing output
```

Статус CI:

```text
ожидает проверки PR
```
