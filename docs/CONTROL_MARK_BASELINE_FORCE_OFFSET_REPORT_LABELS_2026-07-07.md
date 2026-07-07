# Control mark: baseline force and offset report labels

Date: 2026-07-07
Related issue: #165
Scope: documentation only

This record fixes the meaning of three totals labels before clarifying them in `Services/TechnicalReportMarkdownBuilder.cs`.

No production code, report order, numerical value, formula, calculation result, solver, gate, selected-shape flow, 2D, PDF, JSON, DTO, XAML, commands, application version, or 3D behavior changes are made by this document.

## Current labels

```text
Сила течения исходной модели
Горизонтальная сила исходной модели
Старая оценка сноса
```

The words `исходной` and `старая` do not explain which values are included or how these fields differ from later shape-based and X/Z results.

## CurrentForceN meaning

`BuoyCalculator` computes:

```text
CurrentForceN =
    buoyCurrentForce
  + lineCurrentForce
  + connectorCurrentForce
  + payloadCurrentForce
```

The line term uses the sum of depth-segment forces when segment rows exist. Connector, payload, and buoy terms use the effective current speed policy.

This value:

```text
- includes current drag for the whole configured system
- excludes WaveForceN
- is not ShapeLineForceN
- is not recalculated from the selected X/Z shape
```

## HorizontalForceN meaning

The exact formula is:

```text
HorizontalForceN = CurrentForceN + WaveForceN
```

This baseline total is used by the existing tension and required-anchor-holding calculations.

It is not a vector extracted from the selected shape.

## EstimatedOffsetM meaning

The exact formula is:

```text
verticalForceN = max(0, NetBuoyancyKg) × g
EstimatedOffsetM = HorizontalForceN / verticalForceN × DepthM
```

when `verticalForceN > 0`; otherwise the result is zero.

This is a force-ratio approximation. It is not:

```text
- MooringShapeResult.HorizontalOffsetM
- MooringIterativeSolverResult final offset
- SelectedShapeStore horizontal offset
- a 2D or PDF visualization-derived value
```

## Shape-based comparison boundary

`MooringShapeForceAnalyzer` calculates a separate orientation-aware force for line segments only:

```text
OriginalLineForceN
ShapeLineForceN
DifferenceN
RelativeDifference
```

It uses the normal velocity component relative to X/Z segment orientation. The report renders this later as a comparison. It does not replace `CalculationResult.CurrentForceN`.

## Exact replacement labels

```text
- Суммарная сила течения базовой модели (буй + линия + соединители + приборы): {CurrentForceN} Н
- Суммарная горизонтальная нагрузка базовой модели (течение + волна): {HorizontalForceN} Н
- Приближённый снос базовой модели (Fгор / Fверт × глубина): {EstimatedOffsetM} м
```

All existing numeric formats remain `0.####`.

## Allowed production diff

Only this file may change:

```text
Services/TechnicalReportMarkdownBuilder.cs
```

Only the three totals-label strings may change. Their interpolated fields, order, units, and numeric formatting must remain unchanged.

## Required invariants

```text
- CalculationResult unchanged
- BuoyCalculator unchanged
- CurrentForceN, WaveForceN, HorizontalForceN and EstimatedOffsetM values unchanged
- tension and anchor calculations unchanged
- MooringShapeForceAnalyzer unchanged
- fallback, iterative and selected shapes unchanged
- report section order unchanged
- no 2D or PDF changes
- no version bump
- no 3D
```
