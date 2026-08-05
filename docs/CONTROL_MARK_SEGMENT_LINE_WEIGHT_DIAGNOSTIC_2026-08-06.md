# Контрольная отметка: диагностика сохранения веса линии при сегментации

Дата: 2026-08-06
Issue: #217
Scope: documentation only

## Причина

После Issue #214 каждый `SegmentCalculationRow` содержит рассчитанный локальный вес в воде:

```text
WeightWaterKg = SegmentLengthM × RopePreset.WeightWaterKgM
```

Сумма этих значений должна восстанавливать вес всех распределённых участков линии из `ElementRows`:

```text
Σ SegmentRows.WeightWaterKg
≈
Σ ElementRows.WeightWaterKg, где Kind = Линия
```

Сейчас эта согласованность не контролируется автоматически.

## Назначение проверки

Новая строка проверяет целостность цепочки:

```text
линейные пресеты
→ строки элементов
→ расчётные сегменты
→ tension-анализаторы
```

Это программная проверка read models, а не новый инженерный критерий выбора линии.

## Вычисление

```text
lineElementWeightWaterKg =
    Σ result.ElementRows.WeightWaterKg для Kind = Линия

segmentWeightWaterKg =
    Σ result.SegmentRows.WeightWaterKg

absoluteResidualKg =
    abs(segmentWeightWaterKg - lineElementWeightWaterKg)

relativeResidual =
    absoluteResidualKg / max(1 кг, abs(lineElementWeightWaterKg))
```

Критерий:

```text
relativeResidual ≤ 1e-6
```

Порог соответствует другим программным проверкам согласованности в `EngineeringDiagnostics`.

## Строка диагностики

Название:

```text
Согласованность веса линии и расчётных сегментов
```

Значение:

```text
Δm={absoluteResidualKg} кг ({relativeResidual})
```

Допуск:

```text
relative ≤ 1e-6
```

Severity:

```text
OK    — relativeResidual ≤ 1e-6
ERROR — relativeResidual > 1e-6
```

Примечание:

```text
Вес участков линии={lineElementWeightWaterKg} кг; Σ веса сегментов={segmentWeightWaterKg} кг. Проверяется сохранение распределённого веса линии при сегментации.
```

## Влияние

Строка может влиять только на существующий `EngineeringDiagnosticsResult.OverallSeverity`.

Не меняются:

```text
- CalculationResult.Verdict и MainRisk;
- WeightWaterKg сегментов и элементов;
- TotalWeightWaterKg и NetBuoyancyKg;
- натяжения, углы и формы;
- solver, gate и selected-shape routing;
- 2D и PDF.
```

## Разрешённый production-diff

Только:

```text
Services/EngineeringDiagnostics.cs
```

Разрешено:

```text
- вычислить две суммы и их невязку;
- добавить одну EngineeringDiagnosticRow;
- использовать существующий severity pipeline.
```

## Запрещённые изменения

```text
- не менять SegmentCalculationRow;
- не менять расчёт веса или сегментацию;
- не менять tension-анализаторы;
- не менять расчётный verdict;
- не менять JSON, DTO, XAML, команды или версию;
- не добавлять 3D.
```

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
