# BuoyCalc Windows v0.41

Windows-ветка проекта BuoyCalc на C# + Avalonia.

## Статус

v0.41 выполняет этап плана: добавлена классификация режимов постановки `surface/submerged/short/excess line`. Классификация сделана безопасно: она не меняет solver, силы, натяжения или форму, а только добавляет явный режим в отчёт и подготавливает будущие правила расчёта.

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

2. Если gate даёт `CandidateReadyForPrimary`, в `MooringShapeStore` попадает форма с дискретными нагрузками.

3. Если gate даёт `KeepCurrentMainShape` или `CandidateRejected`, в `MooringShapeStore` остаётся fallback-форма старого solver.

## Что добавлено в v0.41

1. Добавлен сервис:

```text
Services/MooringDeploymentModeClassifier.cs
```

2. Добавлены типы:

```text
MooringDeploymentMode
MooringDeploymentModeResult
```

3. Поддерживаемые режимы:

```text
surface
submerged
short
excess line
overloaded
unknown
```

4. Классификатор проверяет:

```text
глубину
длину линии
отношение L/Depth
избыток линии
недостаток линии
глубину буя
чистую плавучесть
состояние буя из формы
```

5. В полный отчёт добавлена таблица:

```text
## Режим постановки v0.41
```

6. Таблица показывает:

```text
Режим
Название
Глубина
Длина линии
L/Depth
Избыток линии
Недостаток линии
Глубина буя
Чистая плавучесть
Статус
```

7. Таблица добавлена через `MooringIterativeSolverResult.MethodNote`, как и таблица выбора основной формы. Это безопасный путь: `ReportBuilder` уже выводит это поле, поэтому структура генератора отчёта не перестраивалась.

8. В `AppInfo` отображаемая версия обновлена:

```text
v0.41 - deployment modes
```

## Что не изменилось

1. Классификатор режима не меняет физику расчёта.
2. Старый `MooringShapeSolver` не удалён.
3. Если кандидатная форма не прошла gate, приложение остаётся на старой форме.
4. 2D/PDF продолжают брать основную форму из `MooringShapeStore`.
5. Тестовые сценарии и автопроверки остаются задачей v0.42.

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
Режим постановки v0.41
```

4. Проверить, что режим отображается как один из:

```text
surface
submerged
short
excess line
overloaded
unknown
```

5. Проверить CI status:

```text
BuoyCalc Windows Build: success
```

## Следующий безопасный шаг

Если v0.41 проходит CI, следующий этап по плану — v0.42: тестовые сценарии и автопроверки.
