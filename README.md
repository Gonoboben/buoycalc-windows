# BuoyCalc Windows v0.40.1

Windows-ветка проекта BuoyCalc на C# + Avalonia.

## Статус

v0.40.1 — безопасное уточнение после v0.40. Дискретные нагрузки уже подключены к выбору основной формы через gate, а теперь решение выбора выводится в полный отчёт через `MooringIterativeSolverResult.MethodNote`.

Старый `MooringShapeSolver` не удалён и остаётся fallback.

## План версий

```text
v0.39   — каркас итерационного solver
v0.39.1 — критерии сходимости solver
v0.39.2 — отчёт по итерациям
v0.39.3 — gate перед подключением кандидатной формы как основной
v0.40   — включение дискретных нагрузок в основной solver
v0.40.1 — отчёт о выборе основной формы
v0.41   — режимы постановки: surface/submerged/short/excess line
v0.42   — тестовые сценарии и автопроверки
v0.43   — улучшение базы элементов
v0.44   — UX редактора последовательности
v0.45   — финальная структура PDF-отчёта
v0.46   — подготовка release build
```

## Что добавлено в v0.40

1. Основная форма выбирается через:

```text
MooringPrimaryShapeSelector
MooringPrimaryShapeSelectionResult
MooringPrimaryShapeSelectionStore
```

2. Логика выбора:

```text
fallback = MooringShapeSolver
candidate = MooringIterativeSolver.FinalShape
selection = MooringPrimaryShapeSelector.Select(fallback, candidate)
```

3. Если gate даёт `CandidateReadyForPrimary`, в `MooringShapeStore` попадает форма с дискретными нагрузками.

4. Если gate даёт `KeepCurrentMainShape` или `CandidateRejected`, в `MooringShapeStore` остаётся fallback-форма старого solver.

## Что добавлено в v0.40.1

1. В полный отчёт добавлен отчётный маркер выбора основной формы через `iterativeSolver.MethodNote`.

2. В отчёте теперь можно найти строку вида:

```text
v0.40.1 report: primaryShapeDecision=...
```

3. Эта строка показывает:

```text
primaryShapeDecision
primaryShapeSource
usesDiscreteLoads
fallbackX
candidateX
dX
stopReason
```

4. Это сделано без большой перестройки `ReportBuilder`: он уже выводит `iterativeSolver.MethodNote` в разделах отчёта, поэтому риск регрессии минимальный.

5. В `AppInfo` отображаемая версия обновлена:

```text
v0.40.1 - primary shape selection report
```

## Что не изменилось

1. Старый `MooringShapeSolver` не удалён.
2. Если кандидатная форма не прошла gate, приложение остаётся на старой форме.
3. 2D/PDF продолжают брать основную форму из `MooringShapeStore`.
4. Режимы постановки surface/submerged/short/excess line остаются задачей v0.41.

## Как проверить

1. Обновить проект локально:

```text
Git -> Pull
Build -> Clean Solution
Build -> Rebuild Solution
```

2. Выполнить обычный расчёт.
3. В полном отчёте найти:

```text
primaryShapeDecision
primaryShapeSource
usesDiscreteLoads
```

4. Проверить CI status:

```text
BuoyCalc Windows Build: success
```

## Следующий безопасный шаг

Перед v0.41 можно сделать v0.40.2, если потребуется более красивая отдельная таблица выбора основной формы. Если текущего вывода достаточно, можно переходить к v0.41.
