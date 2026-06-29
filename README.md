# BuoyCalc Windows v0.44

Windows-ветка проекта BuoyCalc на C# + Avalonia.

## Статус

v0.44 выполняет этап плана: UX редактора последовательности. Сделано безопасно: расчётная физика, solver, 2D и PDF не изменены. Улучшены карточки элементов последовательности в ViewModel: summary теперь явно показывает роль элемента в модели.

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

## Что уже есть в v0.43

1. В `MooringAutocheckSuite` добавлен сценарий:

```text
element-database
```

2. Он проверяет качество строк базы элементов, но не меняет сами элементы.

## Что добавлено в v0.44

1. Обновлён файл:

```text
ViewModels/AssemblyItemViewModel.cs
```

2. Карточка элемента теперь формирует более понятный `Summary`:

```text
active | distributed line | <preset> | L=<...> m
active | point connector | <preset> | count=1
active | discrete payload | <preset> | weight=<...> kg | A=<...> m2 | Cd=<...>
disabled | ...
```

3. Добавлен `EditorHint` для будущего вывода в UI:

```text
Line      -> distributed line; length affects total line and X/Z shape
Connector -> point connector; count fixed to 1; position from sequence order
Payload   -> discrete payload; values from preset
```

4. При включении/отключении элемента `Summary` обновляется сразу.

5. Для payload теперь обновление объёма также обновляет `Summary`.

6. В `AppInfo` отображаемая версия обновлена:

```text
v0.44 - sequence editor UX
```

## Что не изменилось

1. Физика расчёта не изменилась.
2. Последовательность по-прежнему сохраняется в тот же JSON.
3. 2D/PDF продолжают брать основную форму из `MooringShapeStore`.
4. Никакие координаты не добавлены вручную в UI.
5. Финальная структура PDF-отчёта остаётся задачей v0.45.

## Как проверить

1. Обновить проект локально:

```text
Git -> Pull
Build -> Clean Solution
Build -> Rebuild Solution
```

2. Открыть редактор последовательности.
3. Добавить линию, соединитель и прибор.
4. Проверить, что под названием карточки видно role-summary:

```text
distributed line
point connector
discrete payload
```

5. Отключить элемент и проверить, что summary меняется на `disabled`.
6. Проверить CI status:

```text
BuoyCalc Windows Build: success
```

## Следующий безопасный шаг

Если v0.44 проходит CI, следующий этап по плану — v0.45: финальная структура PDF-отчёта.
