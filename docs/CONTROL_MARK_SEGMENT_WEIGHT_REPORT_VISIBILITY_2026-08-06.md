# Контрольная отметка: отображение веса каждого расчётного сегмента

Дата: 2026-08-06
Issue: #226
Scope: documentation only

## Причина

После Issue #214 расчётный read model каждого сегмента содержит авторитетное значение:

```text
SegmentCalculationRow.WeightWaterKg
```

Оно вычисляется в `BuoyCalculator.BuildSegmentRows(...)`:

```text
WeightWaterKg = SegmentLengthM × RopePreset.WeightWaterKgM
```

Три tension-анализатора используют это поле напрямую:

```text
SegmentTensionAnalyzer
MooringShapeTensionAnalyzer
MooringDiscreteLoadTensionAnalyzer
```

Issue #217 дополнительно проверяет:

```text
Σ SegmentRows.WeightWaterKg
≈
Σ ElementRows.WeightWaterKg для Kind = Линия
```

Но исходная таблица `Расчётные сегменты линии` не показывает локальный вес сегмента.

## Текущая таблица

`TechnicalReportMarkdownMovedSections.AppendSegmentRows(...)` выводит:

```text
№
Элемент
Пресет
s0
s1
L
z
U
V
W
|Uгор|
ρ
A
Cd
Сила
```

Поле `WeightWaterKg` отсутствует.

В результате пользователь видит локальный вес только в производной таблице натяжений, но не в первичной сегментной ведомости, где также показаны локальные течение, плотность, площадь, Cd и сила.

## Цель

Показать уже рассчитанное значение без нового вычисления и без изменения расчётного ядра.

## Production-правка

Изменяется только:

```text
Services/TechnicalReportMarkdownMovedSections.cs
```

В `AppendSegmentRows(...)`:

1. После столбца `L, м` добавить:

```text
Вес в воде, кг
```

2. В строке каждого сегмента вывести:

```text
row.WeightWaterKg
```

3. Перед существующей строкой суммарной силы добавить:

```text
Суммарный вес линии по расчётным сегментам: {Σ WeightWaterKg} кг
```

## Порядок столбцов

```text
№ | Элемент | Пресет | s0 | s1 | L | Вес в воде | z | U | V | W | |Uгор| | ρ | A | Cd | Сила
```

Вес размещается рядом с длиной сегмента, поскольку он определяется длиной и выбранным линейным пресетом.

## Sampling

Сохраняется существующая выборка:

```text
первые 40 сегментов
+
последние 40 сегментов
```

Контрольная сумма рассчитывается по всем `result.SegmentRows`, а не только по показанной выборке.

## Инварианты

Не меняются:

```text
- SegmentCalculationRow и его значения;
- формула WeightWaterKg;
- сегментация и нумерация;
- профиль течения и глубина;
- плотность, площадь, Cd и CurrentForceN;
- натяжения, углы и формы;
- инженерная диагностика;
- CalculationResult.Verdict и MainRisk;
- solver, gate и selected-shape routing;
- stores, 2D и PDF diagram sources;
- JSON/DTO входных данных;
- XAML, команды и версия;
- 3D не добавляется.
```

## Разрешённый diff

Только три изменения внутри `AppendSegmentRows(...)`:

```text
- заголовок таблицы;
- разделительная строка и строка данных;
- одна итоговая строка веса.
```

Порядок разделов технического отчёта не меняется.

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
