# Control mark: Berteaux planar vector boundary

Date: 2026-08-13  
Physics RFC: #430  
Depends on: #407, #413, #428, #429  
Scope: docs/source validation only

## Source

Primary source: Г. О. Берто, «Океанографические буи», 1979, user-provided searchable scan.

Visually checked printed pages:

- p.35: `D = R sin^2(phi)` and `F = pi*gamma*R cos^2(phi)`;
- p.45: Poud constant-tangential-resistance specialization;
- p.55: Wilson variable-tangential-resistance specialization;
- p.92: vector normal/tangential resistance law.

On p.92 Berteaux defines unit cable tangent `u` and velocity decomposition:

```text
U_N = U - (U dot u) u
U_T = (U dot u) u
```

For cable-element velocity `V`, the source gives vector resistance proportional to:

```text
normal:     |U_N - V_N| (U_N - V_N)
tangential: |U_T - V_T| (U_T - V_T)
```

with normal factor `1/2 rho C_n d ds` and tangential factor `1/2 rho C_t pi d ds`.

For the present static validation, `V = 0`, so:

```text
f_n = 1/2 rho C_n d ds |U_N| U_N
f_t = 1/2 rho C_t pi d ds |U_T| U_T
f_hydro = f_n + f_t
```

This defines both magnitude and direction.

## Current BuoyCalc overlap

#429 regression proves that `MooringShapeForceAnalyzer` reproduces the Berteaux **normal-force magnitude** under the restricted interpretation `DragCoefficient = C_n`.

It already constructs a candidate tangent, removes the tangent projection of the current vector and computes `normalSpeed`.

## Current gaps

Production still does not implement the full vector law:

1. `ShapeForceN` is a scalar;
2. `MooringShapeTensionAnalyzer` accumulates the scalar wholly as horizontal force;
3. no separate tangential term exists;
4. `RopePreset` has only one generic `DragCoefficient`;
5. shape force uses non-negative `LocalSpeedMS` as horizontal current, so signed planar current direction is unavailable;
6. `VerticalCurrentMS` exists, but its project sign convention is not explicit enough for production X/Z force vectors.

Do not guess any of these semantics in production code.

## Phase A allowed package

Validation only: `BerteauxPlanarResistanceVectorRegression`.

Use explicit synthetic vectors `U=(Ux,Uz)` and unit tangent `t=(tx,tz)`, independent of application input sign conventions.

Required checks:

```text
parallel cable/current:
  U=(1,0), t=(1,0)
  f_n=0, f_t parallel current

normal cable/current:
  U=(1,0), t=(0,1)
  f_t=0, f_n parallel current

45 degrees:
  U=(1,0), t=(1/sqrt(2),1/sqrt(2))
  U_t=(0.5,0.5)
  U_n=(0.5,-0.5)

orthogonality:
  f_n dot t = 0

collinearity:
  f_t parallel t

current reversal:
  U -> -U gives f_n -> -f_n and f_t -> -f_t

zero current:
  f_n=f_t=0
```

The vector magnitudes must also reduce to the already validated p.35 scalar identities.

No production files and no golden-baseline change are allowed in Phase A.

## Production blockers after Phase A

Before production wiring, separate decisions are required for:

- signed East/North current projection into the chosen planar `+X_shape` axis;
- `VerticalCurrentMS` sign mapping into project `+Z` downward;
- whether legacy `DragCoefficient` may be declared `C_n`;
- source/data policy for `C_t` or `gamma`;
- Reynolds/coefficient policy;
- chain applicability (do not silently apply circular-cable coefficient semantics to chain);
- connector/payload resistance vectors;
- coupling with surface-boundary solve `Q0`;
- convergence and under-relaxation evidence;
- explicit selected-X/Z and golden impact review.

## Non-goals

Do not change:

```text
BuoyCalculator base CurrentForceN
MooringShapeForceAnalyzer
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

## Decision

The next safe step is a validation-only proof of:

```text
U -> (U_N, U_T) -> (f_n, f_t) -> f_hydro
```

Production remains blocked on current-sign and coefficient-data semantics.
