# Контрольная отметка: реальные невязки накопления сил линии

Дата: 2026-08-06
Issue: #223
Scope: documentation only

## Причина

`EngineeringDiagnostics.BuildForceResiduals(...)` сейчас сравнивает каждую величину саму с собой:

```text
LineSumFxN = topRow.CumulativeHorizontalForceN
TopTensionFxN = topRow.CumulativeHorizontalForceN

LineSumFzN = topRow.CumulativeVerticalForceN
TopTensionFzN = topRow.CumulativeVerticalForceN
```

Поэтому:

```text
ResidualFxN = 0
ResidualFzN = 0
```

для любых входных данных.

Строки `Контроль накопления ΣFx линии` и `Контроль накопления ΣFz линии` не являются фактическими проверками.

## Независимые стороны сравнения

Локальная сторона уже доступна в `SegmentTensionRow`:

```text
LineSumFxN = Σ SegmentCurrentForceN
LineSumFzN = Σ WeightWaterKg × g
```

Накопленная сторона:

```text
TopTensionFxN = topRow.CumulativeHorizontalForceN
TopTensionFzN = topRow.CumulativeVerticalForceN
```

Используется:

```text
g = 9.80665 м/с²
```

Это то же значение, которое применяет `SegmentTensionAnalyzer`.

## Невязки

```text
ResidualFxN = abs(LineSumFxN - TopTensionFxN)
ResidualFzN = abs(LineSumFzN - TopTensionFzN)

RelativeResidualFx = ResidualFxN / max(1 Н, abs(LineSumFxN))
RelativeResidualFz = ResidualFzN / max(1 Н, abs(LineSumFzN))
```

Критерий:

```text
relative ≤ 1e-6
```

`InternalLineBalanceOk` сохраняет существующую семантику:

```text
RelativeResidualFx ≤ 1e-6
&&
RelativeResidualFz ≤ 1e-6
```

## Строки диагностики

Для обеих строк:

```text
Допуск: relative ≤ 1e-6
Severity: OK / ERROR
```

Примечания сохраняют смысл внутренней проверки накопления и не заявляют полного равновесия постановки.

## Разрешённый production-diff

Только:

```text
Services/EngineeringDiagnostics.cs
```

Разрешено:

```text
- добавить константу g;
- заменить две segment-side суммы;
- изменить tolerance/severity двух существующих строк.
```

## Инварианты

```text
- SegmentTensionRow не меняется;
- SegmentTensionAnalyzer не меняется;
- локальные силы и веса не пересчитываются;
- накопленные компоненты натяжения не меняются;
- CalculationResult.Verdict не меняется;
- остальные diagnostic rows не меняются;
- solver, gate, selected shape, 2D и PDF не меняются;
- JSON, DTO, XAML, команды и версия не меняются;
- 3D не добавляется.
```

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
