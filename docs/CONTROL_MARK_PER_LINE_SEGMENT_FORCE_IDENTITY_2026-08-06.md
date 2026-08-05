# Контрольная отметка: идентичность сегментной силы каждого участка линии

Дата: 2026-08-06
Issue: #205
Scope: documentation only

## Причина

`BuoyCalculator.BuildAssemblyRows(...)` связывает строку участка линии с сегментами по пользовательскому полю `Title`:

```csharp
segmentRows.Where(x => x.SourceElement == item.Title).Sum(x => x.CurrentForceN)
```

Это ненадёжная граница идентичности.

`AssemblyItemViewModel.Title` редактируется пользователем, а новый участок линии по умолчанию получает название:

```text
Участок линии
```

Поэтому несколько активных линий с одинаковым названием являются допустимым и обычным состоянием проекта.

## Дефект повторяющихся названий

Пусть есть два участка линии с одинаковым `Title`:

```text
Line A: сегментная сила FA
Line B: сегментная сила FB
```

Текущий lookup по названию присваивает каждой строке:

```text
Line A row = FA + FB
Line B row = FA + FB
```

Сумма строк линии становится:

```text
2 × (FA + FB)
```

вместо:

```text
FA + FB
```

При этом основной расчёт `CalculationResult.CurrentForceN` остаётся корректным, потому что суммирует `SegmentRows` один раз.

Следовательно, искажаются только производные представления:

```text
- ElementRows.CurrentForceN для каждого участка;
- сумма Fx в MooringVectorBalance;
- RequiredReactionFxN;
- контрольный AnchorHorizontalReserve.
```

## Дефект нулевой локальной силы

После lookup текущий код использует fallback при условии:

```csharp
currentForceN <= 0
```

Но ноль может быть корректным сегментным результатом, например когда участок расположен в слое профиля с нулевой горизонтальной скоростью, а в другой части профиля скорость ненулевая.

В таком случае строка участка ошибочно пересчитывается по `EffectiveCurrentSpeedMS`, то есть по максимуму профиля.

Правильная граница:

```text
fallback применяется только при отсутствии сегментного результата;
нулевой существующий сегментный результат сохраняется как ноль.
```

## Выбранное исправление

Публичные модели не меняются.

Внутри `BuoyCalculator` вводится приватный результат построения сегментов:

```text
SegmentRows
LineCurrentForces
```

`LineCurrentForces` хранит сумму сегментных сил каждого `lineItems[i]` в том же порядке.

`BuildAssemblyRows(...)` получает этот список и ведёт отдельный индекс активных линий с пресетом:

```text
первая строка линии  → LineCurrentForces[0]
вторая строка линии → LineCurrentForces[1]
...
```

Название элемента остаётся только отображаемой подписью и больше не используется как ключ связи расчётных данных.

## Сохраняемые расчёты

Не меняются:

```text
- разбивка на сегменты до 1 м;
- накопленная координата вдоль линии;
- оценка глубины сегмента;
- интерполяция U/V/W и плотности;
- площадь сегмента;
- Cd;
- формула DragForce;
- сумма SegmentRows;
- CalculationResult.CurrentForceN;
- HorizontalForceN;
- основной AnchorReserve;
- натяжения и форма;
- solver и selected-shape routing.
```

## Ожидаемая согласованность

После исправления для любой допустимой последовательности:

```text
Σ ElementRows.CurrentForceN + WaveForceN = HorizontalForceN
```

с точностью вычислений `double`.

Следовательно:

```text
|RequiredReactionFxN| = HorizontalForceN
AnchorHorizontalReserve = AnchorReserve
```

при обычной положительной нагрузке.

## Разрешённый production-diff

Только:

```text
Models/EngineeringModels.cs
```

Разрешены следующие структурные изменения внутри `BuoyCalculator`:

```text
- приватный record результата построения сегментов;
- возврат строк и per-line force list из BuildSegmentRows;
- передача per-line force list в BuildAssemblyRows;
- выбор силы строки по порядку линии;
- fallback только при отсутствии соответствующего результата.
```

## Запрещённые изменения

```text
- не добавлять идентификаторы в SegmentCalculationRow;
- не менять публичные record/DTO;
- не менять пользовательские названия;
- не требовать уникальных Title;
- не менять формулы и коэффициенты;
- не менять solver, stores, selected shape, 2D или PDF;
- не менять JSON, XAML, команды и версию;
- не добавлять 3D.
```

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
