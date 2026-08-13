# Control mark: uniform-current normal vector diagnostic

Date: 2026-08-13  
Physics RFC: #430  
Depends on: #407, #413, #428, #429, #431, #432  
Scope: production read-model boundary only; no solver change

## Decision

The first production use of the Berteaux planar vector law will be an **additive INFO diagnostic only** for the existing scalar/uniform-current mode:

```text
environment.UseCurrentProfile == false
```

It will not change selected X/Z, solver feedback, gate, verdict, anchor or weak-link calculations.

## Why uniform-current only

In scalar current mode the existing input is a speed magnitude and the project already defines the local shape-plane orientation:

```text
+X_shape = buoy -> anchor
+X_shape is opposite the environmental current/drag direction
+Z = downward
```

Therefore the static validation current vector can be represented unambiguously as:

```text
U = (-abs(CurrentSpeedMS), 0)
```

No East/North plane-selection policy and no vertical-current sign mapping are required in this restricted mode.

For:

```text
environment.UseCurrentProfile == true
```

the diagnostic must return `Unavailable/Indeterminate` with an explicit reason until a separate planar projection policy is approved.

Do not silently replace signed East/North profile vectors with `HorizontalSpeedMS` in the new vector result.

## Source specialization

Berteaux printed p.55 lists Wilson table cases including:

```text
gamma = 0
```

With:

```text
C_t = gamma C_n
```

this is the source-supported normal-only specialization:

```text
f_t = 0
```

The first diagnostic therefore does not need to invent tangential coefficients for existing rope presets.

## Normal vector

For each segment with signed top-to-bottom unit tangent `t` from existing X/Z projection:

```text
U_t = (U dot t) t
U_n = U - U_t

f_n = 1/2 rho C_n d ds |U_n| U_n
```

For this first diagnostic only, retain the **existing historical interpretation** already used by `MooringShapeForceAnalyzer`:

```text
C_n candidate = SegmentCalculationRow.DragCoefficient
```

This does not rename `RopePreset.DragCoefficient` or claim that the catalog has completed coefficient validation. It preserves current normal-force magnitude semantics while adding vector direction.

## Required software identity

For every available uniform-current segment:

```text
|f_n| == existing MooringShapeForceRow.ShapeForceN
```

within a strict software tolerance, because both calculations use the same `rho`, `Cd`, `d*ds` and normal-speed magnitude.

Any mismatch is an implementation error, not a new engineering tolerance.

## Result semantics

Suggested immutable row fields:

```text
SegmentNumber
SourceElement
CurrentXMS
CurrentZMS
TangentX
TangentZ
NormalVelocityXMS
NormalVelocityZMS
NormalSpeedMS
NormalForceXN
NormalForceZN
NormalForceMagnitudeN
ExistingShapeForceN
MagnitudeDifferenceN
Status
```

Suggested result-level fields:

```text
Available
Rows
SumNormalForceXN
SumNormalForceZN
SumNormalForceMagnitudeN
MethodNote
```

Profile mode should remain structurally present but unavailable rather than publishing fake zeros.

## Required validation

Before merge of production diagnostic code:

1. uniform current + vertical cable -> force along environmental current direction;
2. uniform current + cable parallel to current -> zero normal force;
3. 45-degree cable -> both X and Z normal-force components appear with correct signs;
4. zero current -> zero vector;
5. every row magnitude matches existing `MooringShapeForceAnalyzer.ShapeForceN`;
6. profile mode -> unavailable, not zero;
7. existing five-scenario golden baseline unchanged.

## Report/UI boundary

The first production package only adds the immutable read model to `TechnicalReportData`.

Do not render it in Markdown/PDF/2D in the same PR.

A later passive report PR may display it after the read-model package is green.

## Still blocked

This package does not solve:

```text
- East/North profile -> planar axis projection;
- VerticalCurrentMS sign convention;
- tangential C_t/gamma data;
- chain coefficient applicability;
- connector/payload vector resistance;
- coupled geometry/current iteration;
- production convergence policy.
```

## Non-goals

Do not change:

```text
BuoyCalculator base CurrentForceN
MooringShapeForceAnalyzer historical output
MooringShapeTensionAnalyzer
MooringShapeSolver
MooringDiscreteLoadShapeBuilder
MooringIterativeSolver
MooringPrimaryShapeGate
CalculationResult.Verdict
selected X/Z
anchor / weak-link physics
0.20 m target segmentation
unlimited segment count
signed WeightWaterKg
PDF / 2D physics
JSON / DTO
golden baseline
3D
```

## Merge discipline

Merge only after exact final head has:

- `.NET Build` success;
- `Selected Shape Consumer Scan` success;
- `Report Store Consumer Scan` success.
