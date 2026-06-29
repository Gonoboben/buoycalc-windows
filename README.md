# BuoyCalc Windows v0.40

Windows-ветка проекта BuoyCalc на C# + Avalonia.

## Статус

v0.40 выполняет этап плана: дискретные нагрузки подключены к выбору основной формы solver через безопасный gate. Старый `MooringShapeSolver` не удалён и остаётся fallback.

## План версий

```text
v0.39   — каркас итерационного solver
v0.39.1 — критерии сходимости solver
v0.39.2 — отчёт по итерациям
v0.39.3 — gate перед подключением кандидатной формы как основной
v0.40   — включение дискретных нагрузок в основной solver
v0.41   — режимы постановки: surface/submerged/short/excess line
v0.42   — тестовые сценарии и автопроверки
v0.43   — улучшение базы элементов
v0.44   — UX редактора последовательности
v0.45   — финальная структура PDF-отчёта
v0.46   — подготовка release build
```

## Что было подготовлено до v0.40

### v0.39

Добавлен диагностический цикл:

```text
форма -> силы по форме -> натяжения -> дискретные нагрузки -> новая форма -> сравнение со старой -> критерий сходимости
```

### v0.39.1

Добавлены критерии и причины остановки solver:

```text
MooringIterativeSolverCriteria
MooringIterativeSolverStopReason
```

### v0.39.2

В отчёт по итерациям добавлены признаки:

```text
reason=<StopReason>
dX=OK/NO
maxNode=OK/NO
Z=OK/NO
divergence=YES/NO
```

### v0.39.3

Добавлен gate перед подключением кандидатной формы:

```text
MooringPrimaryShapeGate
MooringPrimaryShapeGateDecision
MooringPrimaryShapeGateResult
```

## Что добавлено в v0.40

1. Добавлен выбор основной формы через новый selector:

```text
MooringPrimaryShapeSelector
MooringPrimaryShapeSelectionResult
MooringPrimaryShapeSelectionStore
```

2. После расчёта `MooringIterativeSolver` теперь выполняется выбор основной формы:

```text
fallback = MooringShapeSolver
candidate = MooringIterativeSolver.FinalShape
selection = MooringPrimaryShapeSelector.Select(fallback, candidate)
MooringShapeStore.Set(selection.Shape)
```

3. Если gate возвращает:

```text
CandidateReadyForPrimary
```

то основной формой в `MooringShapeStore` становится кандидатная форма итерационного solver, которая уже включает дискретные нагрузки.

4. Если gate возвращает:

```text
KeepCurrentMainShape
CandidateRejected
```

то основной формой остаётся fallback от старого `MooringShapeSolver`.

5. `MooringIterativeSolverStore.Set(...)` теперь не только сохраняет результат итерационного solver, но и запускает `MooringPrimaryShapeSelector`.

6. `MooringPrimaryShapeSelectionStore` хранит решение selector для будущего вывода в отчёт и UI.

7. В `AppInfo` отображаемая версия обновлена:

```text
v0.40 - discrete loads primary solver gate
```

## Что не изменилось

1. Старый `MooringShapeSolver` не удалён.
2. Если кандидатная форма не прошла gate, приложение автоматически остаётся на старой форме.
3. 2D/PDF получают форму из `MooringShapeStore`, как и раньше.
4. Отдельная косметика PDF-отчёта не менялась.
5. Режимы постановки surface/submerged/short/excess line остаются задачей v0.41.

## Как проверить

1. Обновить проект локально:

```text
Git -> Pull
Build -> Clean Solution
Build -> Rebuild Solution
```

2. Выполнить обычный расчёт.
3. Проверить, что расчёт не падает, а 2D/PDF строятся как раньше.
4. В сценарии, где итерационный solver не сходится, должна использоваться fallback-форма `MooringShapeSolver`.
5. В сценарии, где gate даёт `CandidateReadyForPrimary`, `MooringShapeStore` должен получить форму с дискретными нагрузками.
6. Проверить CI status:

```text
BuoyCalc Windows Build: success
```

## Следующий безопасный шаг

Перед переходом к v0.41 можно сделать промежуточную v0.40.1: вывести решение `MooringPrimaryShapeSelectionStore` в отчёт, чтобы пользователь видел, какая форма выбрана основной и почему.
