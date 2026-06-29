# BuoyCalc Windows v0.39.1

Windows-ветка проекта BuoyCalc на C# + Avalonia.

## Статус

v0.39.1 соответствует плану проекта: после каркаса итерационного solver в v0.39 добавлены явные критерии сходимости solver.

Главный принцип остаётся прежним: старый `MooringShapeSolver` не заменяется, 2D/PDF-схемы пока не переключаются на новую форму.

## План версий

```text
v0.39   — каркас итерационного solver
v0.39.1 — критерии сходимости solver
v0.39.2 — отчёт по итерациям
v0.40   — включение дискретных нагрузок в основной solver
v0.41   — режимы постановки: surface/submerged/short/excess line
v0.42   — тестовые сценарии и автопроверки
v0.43   — улучшение базы элементов
v0.44   — UX редактора последовательности
v0.45   — финальная структура PDF-отчёта
v0.46   — подготовка release build
```

## Что уже есть в v0.39

Итерационный диагностический цикл:

```text
форма → силы по форме → натяжения → дискретные нагрузки → новая форма → сравнение со старой → критерий сходимости
```

Основные типы:

```text
MooringIterativeSolverIteration
MooringIterativeSolverResult
MooringIterativeSolverStore
```

## Что добавлено в v0.39.1

1. Добавлена явная модель критериев:

```text
MooringIterativeSolverCriteria
```

2. Добавлена явная причина остановки solver:

```text
MooringIterativeSolverStopReason
```

3. Критерии сходимости теперь проверяются отдельно:

```text
|ΔXсноса| ≤ OffsetToleranceM
max Δузла ≤ NodeDeltaToleranceM
|невязка Z| ≤ GeometryResidualToleranceM
```

4. Добавлена защитная остановка от грубой расходимости:

```text
|ΔX| > DivergenceOffsetChangeM
или
max Δузла > DivergenceNodeDeltaM
```

5. Итерация теперь хранит диагностические флаги:

```text
OffsetWithinTolerance
NodeDeltaWithinTolerance
GeometryResidualWithinTolerance
DivergenceDetected
StopReason
StopReasonText
```

6. Итог solver теперь хранит:

```text
Criteria
StopReason
StopReasonText
FinalGeometryResidualM
Diverged
```

7. В `AppInfo` отображаемая версия обновлена:

```text
v0.39.1 - solver convergence criteria
```

## Дополнительный подготовительный сервис

В коде также есть подготовительный сервис:

```text
Services/MooringIterativeShapeComparison.cs
```

Он был добавлен как вспомогательная диагностика для будущей проверки кандидатной формы, но по плану проекта не считается основным содержанием v0.39.1. Основное содержание v0.39.1 — именно критерии сходимости solver.

## Что не изменилось

1. Основная форма X/Z всё ещё берётся из `MooringShapeSolver`.
2. Новый solver остаётся диагностическим слоем.
3. 2D-схема не переключена на кандидатную форму.
4. PDF-схемы не переключены на кандидатную форму.
5. Отчёт по итерациям остаётся задачей v0.39.2.

## Как проверить

1. Обновить проект локально:

```text
Git → Pull
Build → Clean Solution
Build → Rebuild Solution
```

2. Убедиться, что проект собирается.
3. Выполнить обычный расчёт и проверить, что отчёт по-прежнему формируется.
4. Проверить CI status:

```text
BuoyCalc Windows Build: success
```

## Следующий шаг

Следующий шаг по плану — v0.39.2: отчёт по итерациям. В нём нужно вывести в отчёт не только текущую таблицу итераций, но и новые поля v0.39.1:

```text
критерии допуска
флаги выполнения критериев
причину остановки
признак защитной остановки
финальную невязку Z
```
