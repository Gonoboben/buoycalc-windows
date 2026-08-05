# Контрольная отметка: идентичность веса каждого сегмента линии

Дата: 2026-08-06
Issue: #214
Scope: documentation only

## Причина

Три анализатора натяжений повторно восстанавливают вес сегмента через пользовательское название участка линии:

```csharp
result.ElementRows
    .Where(x => x.Kind == "Линия" && x.LengthM > 0)
    .GroupBy(x => x.Title)
```

Затем для каждого `SegmentCalculationRow` выполняется lookup по:

```text
segment.SourceElement
```

Затронуты:

```text
SegmentTensionAnalyzer
MooringShapeTensionAnalyzer
MooringDiscreteLoadTensionAnalyzer
```

## Дефект повторяющихся названий

Названия участков редактируются пользователем и не являются идентификаторами. Несколько участков могут называться одинаково, например `Участок линии`.

Пусть одноимённые участки используют разные канаты:

```text
участок A: wA кг/м
участок B: wB кг/м
```

Текущая группировка создаёт одно средневзвешенное значение:

```text
wGroup = (WA + WB) / (LA + LB)
```

и назначает его всем сегментам обоих участков.

Общий вес группы может сохраниться, но распределение веса по глубине и координате `s` становится неверным.

## Инженерные последствия

Неверное локальное распределение веса изменяет:

```text
- накопленную вертикальную силу снизу вверх;
- базовые сегментные натяжения и углы;
- натяжения по форме X/Z;
- натяжения с дискретными нагрузками;
- альтернативную кандидатную форму;
- итерационный feedback-цикл и решение gate.
```

Это реальный расчётный дефект, даже если суммарный вес всей линии остаётся тем же.

## Выбранная граница

Вес сегмента рассчитывается в том же месте, где уже рассчитываются его:

```text
- длина;
- глубина;
- U/V/W;
- плотность;
- проецируемая площадь;
- Cd;
- сила течения.
```

В `SegmentCalculationRow` добавляется поле в конце positional record:

```text
WeightWaterKg
```

В `BuoyCalculator.BuildSegmentRows(...)`:

```text
segmentWeightWaterKg = SegmentLengthM × RopePreset.WeightWaterKgM
```

Поле добавляется в конец record, чтобы не менять порядок существующих свойств и аргументов.

## Потребители

Три анализатора используют только:

```csharp
segment.WeightWaterKg
```

Удаляются:

```text
- GroupBy(x => x.Title);
- словари weightPerMeterByElement;
- lookup по segment.SourceElement;
- повторное умножение усреднённого кг/м на длину сегмента.
```

## Сохраняемые расчёты

Не меняются:

```text
- RopePreset.WeightWaterKgM;
- WeightInWaterKg для якоря, соединителей и приборов;
- суммарный lineWeightWater в BuoyCalculator;
- TotalWeightWaterKg и NetBuoyancyKg;
- разбивка на сегменты до 1 м;
- глубина и профиль течения;
- плотность, площадь, Cd и DragForce;
- CurrentForceN и HorizontalForceN;
- якорь, волна и слабое звено.
```

Для проектов, где одинаковые названия относятся к канатам с одинаковым весом на метр, численные результаты сохраняются.

Для одноимённых участков с различным весом на метр исправляется локальное распределение веса и все зависимые натяжения/формы.

## Публичная модель

`SegmentCalculationRow` является расчётным read model, а не входным DTO проекта. Новое поле:

```text
- не требует изменения пользовательского ввода;
- не требует миграции сохранённых проектов;
- не меняет JSON/DTO входных данных;
- не выводится новым столбцом отчёта в этом issue.
```

## Разрешённый production-diff

Только четыре файла:

```text
Models/EngineeringModels.cs
Services/SegmentTensionAnalyzer.cs
Services/MooringShapeTensionAnalyzer.cs
Services/MooringDiscreteLoadTensionAnalyzer.cs
```

Разрешено:

```text
- добавить WeightWaterKg в конец SegmentCalculationRow;
- вычислить его при создании сегмента;
- заменить три title-based weight lookup на segment.WeightWaterKg.
```

## Запрещённые изменения

```text
- не требовать уникальных названий;
- не добавлять ID в пользовательскую модель;
- не менять сегментацию или формулы;
- не менять отчётную таблицу сегментов;
- не менять solver gate или selected-shape routing;
- не менять 2D, PDF, XAML, команды или версию;
- не добавлять 3D.
```

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
