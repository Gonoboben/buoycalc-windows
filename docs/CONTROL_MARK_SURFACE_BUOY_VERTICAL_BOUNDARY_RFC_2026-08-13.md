# Control mark: surface-buoy vertical boundary reaction

Date: 2026-08-13  
Physics RFC: #413  
Depends on: #407  
Base main: `5f1982820933f31a4d7ee5e15d6b627689e82b36`

## Purpose

The signed-orientation work has reached a boundary that must be solved before any force-derived X/Z geometry can replace the historical geometric closure.

The current force ownership is now regression-verified, but a normal free surface buoy still lacks one solved boundary state:

```text
the buoy-side vertical cable-tension component
```

Equivalently, the solver does not yet know the buoy's **actual equilibrium displacement/buoyancy**.

This document defines the validation boundary only. It does not change production physics.

## Primary source

Primary project source:

> H. O. Berteaux / Г. О. Берто, *Buoy Engineering / Океанографические буи*, 1979, Part II, Chapter 2, §2.1.

### Surface-buoy equilibrium

Berteaux gives:

```text
B_b = W_b + T_vert - R_vert                  (2.36)
D_b = T_hor                                  (2.37)
```

For the present Chapter-2 static-current specialization, BuoyCalc has no modeled vertical hydrodynamic/lift force on the buoy, so the validation assumption is:

```text
R_vert = 0
```

and therefore:

```text
T_vert = B_actual - W_b
T_hor  = D_b
```

### Surface displacement is an equilibrium state

Berteaux states that when the vertical cable-tension component for a surface buoy is unknown, another physical constraint such as line length or horizontal excursion is prescribed and the solution is obtained by successive approximations.

The same text separately states that the buoyancy of a subsurface/submerged buoy is constant.

Therefore:

```text
surface buoy:      B_actual is a solved/constrained displacement
submerged buoy:    B may be prescribed/constant
```

The two cases must not be silently collapsed into one.

## Current BuoyCalc semantics

The current calculation core computes:

```text
BuoyancyKg = rho * VolumeM3
```

for every buoy input.

The current input does not contain:

```text
surface/submerged mode
actual immersed volume
waterline
freeboard
```

Therefore the existing value is retained as the **full-volume buoyancy capacity** of the current screening model.

It is not yet accepted as actual surface equilibrium displacement.

## Unknown for a surface boundary solve

Define:

```text
Q0 = downward vertical cable-tension component acting on the buoy
```

with:

```text
Q0 >= 0
```

Under the static-current specialization:

```text
B_actual = W_b + Q0
```

The maximum available buoyancy force is:

```text
B_max = rho * V_full * g
```

so the maximum cable vertical component supportable while the buoy remains within full-volume capacity is:

```text
Q_capacity = max(0, B_max - W_b)
```

The admissible shooting interval is therefore:

```text
0 <= Q0 <= Q_capacity
```

No validation or production solver may extrapolate beyond this interval and still call the result a normal surface equilibrium.

## Shape-plane convention

The existing production `XOffsetM` is a non-negative horizontal offset magnitude. Do not reinterpret it as a signed East/North coordinate.

For the validation shooting model define:

```text
s = 0 at buoy and increases toward anchor
+Z = downward
+X_shape = from buoy toward anchor
```

For a steady current this local `+X_shape` direction is opposite the environmental drag direction.

This remains planar 2D X/Z. No 3D is introduced.

## Frozen steady-load field

Phase A intentionally does not alter drag based on the new geometry.

It reuses the existing steady-current loads exactly as calculated by the core today.

Wave is excluded from this static Chapter-2 field.

### Top boundary

Horizontal cable-tension magnitude toward the anchor:

```text
H(0+) = D_b
```

where `D_b` is steady buoy current drag only.

Vertical:

```text
V(0+) = Q0
```

### Distributed segment crossing

For each line segment crossed from top to bottom:

```text
H += Segment.CurrentForceN
V -= Segment.WeightWaterKg * g
```

The water-weight term remains signed.

A heavy section reduces the remaining downward cable component; a buoyant section increases it.

### Discrete connector/payload crossing

At the element's existing `s` position:

```text
H += Point.CurrentForceN
V -= Point.WeightWaterKg * g
```

Buoy and anchor are boundary objects and are not added as internal point loads.

## Local validation tangent

At a non-degenerate cut:

```text
T = hypot(H, V)
tx = H / T
tz = V / T
```

For an inextensible segment of length `ds`:

```text
dx = ds * tx
dz = ds * tz
```

This tangent belongs only to the validation shooting model until reference validation is complete.

It is distinct from Phase-A `SignedOrientation`, which normalizes a distributed-load ledger without the solved top boundary.

## Closure condition

Known project inputs already include:

```text
target depth D
line length L
steady segment loads
point loads
buoy steady drag
full-volume buoyancy capacity
```

For a trial `Q0`, integrate the frozen-load geometry and calculate:

```text
Z_anchor(Q0)
```

The physical surface-boundary closure condition is:

```text
Z_anchor(Q0) - D = 0
```

Use a bounded root search only inside:

```text
[0, Q_capacity]
```

The corresponding horizontal excursion is an output rather than a fitted input:

```text
X_anchor(Q0*)
```

## Why this is different from historical AngleScale

The current fallback shape changes all force-derived angle magnitudes by a global geometric scale until depth closes.

The proposed validation model instead keeps the existing frozen external loads fixed and varies a physically identifiable **boundary reaction** `Q0` within the buoy's available capacity.

Therefore the root variable has an explicit free-body meaning.

No production replacement is authorized yet.

## Classification of limiting cases

### L < D

An inextensible line cannot connect the surface to the target depth.

Expected classification:

```text
NoGeometricSolution_LineShorterThanDepth
```

Do not create an artificial root.

### L == D, zero horizontal load

A vertical line can satisfy the geometry.

Depth closure may not uniquely determine `Q0`; many positive tension states can have the same straight vertical geometry.

Expected classification must distinguish:

```text
geometry solved
boundary reaction not uniquely identified by geometry alone
```

### L == D, non-zero horizontal load

At finite tension, any non-zero horizontal tangent reduces vertical span below arc length.

This is a limiting/no-finite-root case for an inextensible line at exact depth equality.

Do not hide it by driving `Q0` to an arbitrary large value.

### Capacity exhausted

If no depth-closing root exists in `[0, Q_capacity]`, report a bounded no-solution classification.

Do not use `Q0 > Q_capacity`.

### Negative WeightWaterKg

Buoyant sections remain valid and signed.

No absolute-value replacement is allowed.

### Internal V sign change

A sign change in `V(s)` must be recorded rather than clamped. It may indicate a horizontal tangent/local extremum and requires physical interpretation in the final geometry.

## Validation-only Phase A

The first allowed code package is a validation-only shooting study.

It must not change production files.

Required output per scenario:

```text
Q_capacity
root classification
Q0 if identified
Q0/Q_capacity
B_actual/B_max
endpoint X/Z
vertical residual
root iterations
minimum and maximum H
minimum and maximum V
whether V changed sign
point-load crossing count
```

Required scenarios:

1. zero-current vertical heavy line;
2. uniform-current slack heavy line;
3. buoyant line;
4. connector + payload;
5. depth-varying current profile;
6. line shorter than depth;
7. line length equal to depth with non-zero current;
8. deliberately insufficient buoyancy capacity.

The committed five-scenario golden baseline remains unchanged.

## Numerical root policy

No engineering acceptance threshold is introduced here.

A validation-only numerical depth tolerance may reuse a clearly labeled numerical target such as the existing geometric `0.01 m`, but it must not be presented as a physics acceptance criterion.

The root algorithm should be bounded and deterministic. Bisection is preferred for Phase A if monotonicity is observed/bracketed.

Do not assume monotonicity silently. The validation result should record endpoint residuals at both capacity bounds and whether a sign-changing bracket exists.

## Phase B reference work

Before production use:

- compare constant-current heavy cases with Berteaux solutions under overlapping assumptions;
- compare variable-current stepped cases with the structure of (2.34)–(2.35);
- test mesh sensitivity around the existing 0.20 m target;
- verify point loads separately;
- verify capacity/no-root classifications;
- document the difference between actual surface displacement and full-volume capacity.

## Later coupled solver

Only after the surface boundary is independently validated may the project return to iterative coupling:

```text
geometry
-> orientation-dependent line drag
-> boundary Q0 solve
-> updated geometry
-> convergence check
```

This is later work. It must not be mixed into the Phase-A shooting study.

## Wave boundary

`WaveForceN` remains outside this Chapter-2 steady-current static solve.

The application may continue to calculate/report wave loading separately. A dynamic/design-envelope model requires separate validation.

## No production changes authorized

Do not change:

```text
MooringShapeSolver
MooringDiscreteLoadShapeBuilder
MooringIterativeSolver
MooringPrimaryShapeGate
CalculationResult.Verdict
selected X/Z
production MaxIterations
anchor or weak-link calculations
0.20 m target segmentation
unlimited segment count
signed WeightWaterKg
PDF/2D physics
JSON/DTO
golden baseline
3D
```

## Decision

The next problem is no longer an arbitrary angle correction.

It is a bounded surface-boundary reaction problem:

```text
find Q0 within buoyancy capacity
such that the force-derived frozen-load line reaches the prescribed depth
```

The next implementation step is validation-only. Production X/Z remains unchanged until analytical/reference and mesh evidence are available.
