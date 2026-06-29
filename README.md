# BuoyCalc Windows v0.40.2

Windows-ветка проекта BuoyCalc на C# + Avalonia.

## Статус

v0.40.2 — безопасное уточнение после v0.40.1. Дискретные нагрузки уже подключены к выбору основной формы через gate, а теперь в полном отчёте появляется отдельная Markdown-таблица выбора основной формы.

Старый `MooringShapeSolver` не удалён и остаётся fallback.

## План версий

```text
v0.39   — каркас итерационного solver
v0.39.1 — критерии сходимости solver
v0.39.2 — отчёт по итерациям
v0.39.3 — gate перед подключением кандидатной формы как основной
v0.40   — включение дискретных нагрузок в основной solver
v0.40.1 — отчёт о выборе основной формы
v0.40.2 — таблица выбора основной формы в отчёте
v0.41   — режимы постановки: surface/submerged/short/excess line
v0.42   — тестовые сценарии и автопроверки
v0.43   — улучшение базы элементов
v0.44   — UX редактора последовательности
v0.45   — финальная структура PDF-отчёта
v0.46   — подготовка release build
```

## Что уже есть в v0.40

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

В полный отчёт добавлен текстовый маркер выбора основной формы через `iterativeSolver.MethodNote`:

```text
primaryShapeDecision
primaryShapeSource
usesDiscreteLoads
fallbackX
candidateX
dX
stopReason
```

## Что добавлено в v0.40.2

1. Текстовый маркер заменён на отдельную Markdown-таблицу:

```text
## Выбор основной формы v0.40.2
```

2. Таблица показывает:

```text
Решение gate
Источник основной формы
Используются дискретные нагрузки
Fallback X-снос
Candidate X-снос
ΔX candidate - fallback
Candidate max Δузла
Candidate Z-невязка
Iterative solver converged
Divergence guard
StopReason
```

3. Таблица добавлена через `MooringIterativeSolverResult.MethodNote`. Это сделано специально безопасно: `ReportBuilder` уже выводит это поле, поэтому структура генератора отчёта не перестраивалась.

4. В `AppInfo` отображаемая версия обновлена:

```text
v0.40.2 - primary shape selection table
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
Выбор основной формы v0.40.2
```

4. Проверить строки таблицы: `Решение gate`, `Источник основной формы`, `Используются дискретные нагрузки`, `StopReason`.
5. Проверить CI status:

```text
BuoyCalc Windows Build: success
```

## Следующий безопасный шаг

Если v0.40.2 проходит CI и таблица читается нормально, можно переходить к v0.41: режимы постановки `surface/submerged/short/excess line`.
