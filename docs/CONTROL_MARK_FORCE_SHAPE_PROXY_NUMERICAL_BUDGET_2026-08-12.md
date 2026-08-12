# Контрольная отметка: numerical budget Candidate A force↔shape proxy

Дата: 2026-08-12  
Issues: #374, #378  
PR: #377  
Scope: validation evidence; no solver change

## Причина

Первый exact-head validation run Candidate A показал, что заранее предложенные константы:

```text
max R_rel <= 1e-8
max Δθ <= 1e-6 deg
```

не выполняются в каноническом случае:

```text
Depth = 50 m
LineLength = 50 m
Current = 0
```

Нельзя ослаблять пороги только ради зелёного CI. Поэтому причина была воспроизведена до уровня базовой строки силы, геометрии сегмента и исходного кода `MooringShapeSolver`.

## Exact-head evidence

Production C# на draft PR #377 собрался:

```text
0 warnings
0 errors
```

Архитектурные smoke-checks прошли.

Новая validation остановила build на:

```text
max R_rel = 8.024520884654815e-8
```

Повторный evidence-run показал худший segment 242:

```text
SegmentCurrentForceN = 0 N
BaseCumulativeH      = 0 N
BaseCumulativeV      = 1.765197000000018 N
BaseAngleDeg         = 0 deg

ShapeNodeAngleDeg = 4.597711793052989e-6 deg

dx = 1.6049041769309896e-8 m
dz = 0.20000000000000284 m

Candidate-A R_rel = 8.024520884654815e-8
Candidate-A Δθ    = 4.597711793053067e-6 deg
```

Следовательно, force state действительно вертикален, а маленький X уже присутствует в fallback geometry.

## Root cause

`MooringShapeSolver.Build(...)` сначала использует:

```text
lineLengthM = result.LineLengthM
```

Для канонического случая:

```text
result.LineLengthM = 50
Depth = 50
```

`SolveAngleScale(...)` поэтому проходит ветку:

```text
lineLengthM <= targetAnchorDepthM
AngleScale = 1
```

Но `BuildNodes(...)` повторно определяет локальную длину:

```text
lineLengthM = orderedSegments.Sum(x => x.SegmentLengthM)
```

Для 250 сегментов получается:

```text
ΣSegmentLengthM = 50.00000000000016
```

После этого `ScaleAngle(...)` видит:

```text
lineLengthM > Depth
```

и вводит геометрический угол:

```text
θ_budget = acos(Depth / ΣSegmentLengthM)
         = acos(50 / 50.00000000000016)
         = 4.597711793052989e-6 deg
```

хотя force-derived angle равен нулю.

Эта pre-existing solver inconsistency вынесена отдельно в Physics RFC #378.

## Почему Candidate A не исправляется

Candidate A должен показывать фактическую разницу между:

```text
existing force direction
и
existing X/Z tangent
```

Поэтому нельзя внутри analyzer:

```text
- округлять dx до нуля;
- подменять segment-summed geometry input;
- нормализовать residual под ожидаемую вертикаль;
- скрывать существующую solver geometry inconsistency.
```

#377 остаётся diagnostic-only и не меняет `MooringShapeSolver`.

## Representation-derived software budget

Для vertical zero-current limiting test численный budget определяется из того же текущего представления геометрии:

```text
segmentSum = Σ SegmentLengthM

if segmentSum > Depth:
    θ_budget = acos(clamp(Depth / segmentSum, 0, 1))
else:
    θ_budget = 0
```

Для двух единичных направлений chord difference:

```text
R_direction = 2 sin(θ_budget / 2)
```

Candidate A использует:

```text
R_rel = R / max(T, 1 N)
```

Поэтому:

```text
R_rel <= 2 sin(θ_budget / 2)
```

является безопасной верхней границей: при `T >= 1 N` она совпадает с directional chord, а при `T < 1 N` знаменатель `max(T,1)` только уменьшает `R_rel`.

Для текущего канонического случая:

```text
2 sin(θ_budget / 2)
= 8.024520884654677e-8
```

что совпадает с измеренным:

```text
8.024520884654815e-8
```

с разницей только на уровне дополнительных floating-point операций.

## Validation rule

Первый implementation PR использует:

```text
max Δθ <= θ_budget + tiny comparison margin
max R_rel <= chordBudget + tiny comparison margin
```

Comparison margin допускает только округление дополнительных `sqrt/sin/atan2` операций и не является engineering tolerance.

Synthetic cases, не проходящие через production segmented geometry, продолжают использовать строгий algebraic tolerance:

```text
1e-10
```

## Что это НЕ означает

Representation-derived budget не является допустимой физической невязкой реальной постановки.

Для реальных задач по-прежнему:

```text
severity = INFO
engineering tolerance = TBD
solver convergence unchanged
primary-shape gate unchanged
CalculationResult.Verdict unchanged
selected X/Z unchanged
```

## Future behavior after #378

Если #378 унифицирует источник line length, то для идеального случая:

```text
LineLength == Depth
Current == 0
force angle == 0
```

ожидается:

```text
θ_budget -> 0
R_rel budget -> 0
```

То есть текущая формула validation автоматически станет строже без изменения Candidate A и без ручной подгонки константы.

## Инварианты #377

```text
- solver formulas unchanged;
- ShapeSolver line-length behavior unchanged;
- AngleScale unchanged;
- iterative solver convergence unchanged;
- MooringPrimaryShapeGate unchanged;
- CalculationResult.Verdict unchanged;
- signed WeightWaterKg preserved;
- target segmentation 0.20 m preserved;
- segment count unlimited;
- selected X/Z unchanged;
- 2D/PDF unchanged;
- existing golden baseline unchanged;
- no 3D.
```

## Safety gate

PR #377 разрешён к merge только после successful exact final head:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```

Physics RFC #378 решается отдельным будущим work package.
