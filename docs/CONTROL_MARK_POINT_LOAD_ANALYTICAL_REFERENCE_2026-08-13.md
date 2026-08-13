# Control mark: point-load analytical reference for surface-boundary shooting

Date: 2026-08-13  
Physics RFC: #413  
Depends on: #407  
Base main: `48dd68ab392e6be8e66902a8593a56f81f0091fc`

## Purpose

The constant distributed-load shooting integrator is now protected by an exact continuous analytical regression.

The next validation problem is the internal discrete load used by the project sequence model:

```text
upper line -> connector -> payload -> lower line
```

This control mark defines the exact **piecewise analytical** reference for that case.

No production solver or geometry change is authorized.

## Source and model distinction

Berteaux provides the physical statics framework and treats equipment/load effects as part of the force balance of the mooring system.

The exact piecewise integration below is again a mathematical reference for BuoyCalc's simplified frozen-load validation field. It is not presented as a verbatim Berteaux formula and does not replace the source's angle-dependent drag treatment.

## Continuous line interval

On any line interval with constant distributed load:

```text
H(s) = H_start + q_x s
V(s) = V_start - w s
```

and:

```text
dR/ds = [H,V] / hypot(H,V)
```

The exact displacement of that interval is already defined and regression-verified by:

`ConstantLoadAnalyticalReferenceRegression`.

## Point-load jump

An idealized connector/payload point load has zero arc length.

Therefore crossing it changes force components but does not directly change X/Z:

```text
H_after = H_before + F_point_x
V_after = V_before - W_point_water * g

DeltaX_point = 0
DeltaZ_point = 0
```

The sign of `W_point_water` remains signed.

For several point elements at the same line coordinate, the jumps add algebraically and geometry remains continuous.

## Piecewise exact geometry

For an upper line of length `L1`, a point-load group, and lower line `L2`:

1. integrate the exact continuous upper interval from `(H0,Q0)` over `L1`;
2. update the terminal force state by the point-load jump;
3. integrate the exact continuous lower interval from the jumped force state over `L2`;
4. add the two interval displacements.

Thus:

```text
R_total(Q0) = R_upper(Q0) + R_lower(Q0 after force jump)
```

The shooting equation remains:

```text
Z_total(Q0) = target depth
```

with `Q0` bounded by surface-buoy capacity.

## Deterministic connector + payload case

The existing synthetic validation case is:

```text
Depth = 50 m
Upper heavy line = 30 m
Lower heavy line = 25 m
Total line length = 55 m
H0 = steady buoy drag = 82 N
q_x = 3.075 N/m
w = 0.980665 N/m
```

The connector is:

```text
dry mass = 5 kg
volume = 0.0007 m3
projected area = 0.01 m2
Cd = 1
```

The payload is:

```text
dry mass = 40 kg
volume = 0.005 m3
projected area = 0.05 m2
Cd = 1
```

At water density `1025 kg/m3` and steady current `0.5 m/s`, the combined point-load jump is:

```text
F_point_x = 7.6875 N
W_point_water = 39.1575 kg
W_point_water * g = 384.003897375 N
```

The two point elements are physically separate sequence rows but occupy the same line coordinate in this deterministic assembly; their force jumps may therefore be grouped for the analytical reference while the ownership regression continues to verify that there are exactly two point-load sources.

## Exact reference solution

Piecewise exact integration plus a tight bounded root solve gives approximately:

```text
Q0_exact = 741.26584043891 N
X_exact  = 19.48029920259 m
Z_exact  = 50 m
```

This differs from the earlier temporary shooting measurement:

```text
Q0_loose_midpoint = 741.34958030701 N
X_loose_midpoint  = 19.47719196964 m
Z_loose_midpoint  = 50.00188552400 m
```

because the temporary study stopped when the depth residual entered its intentionally loose `0.01 m` numerical band.

The difference must not be interpreted as a physical model change.

## Tight midpoint convergence targets

When the point jump is applied exactly at the interval boundary and the line intervals use midpoint integration, a tight depth root converges to the piecewise exact solution.

Representative pre-check values are approximately:

```text
nominal ds   Q0_midpoint (N)    X_midpoint (m)
0.4          741.2654436720      19.4803251374
0.2          741.2657421451      19.4803056070
0.1          741.2658158655      19.4803008037
0.05         741.2658342955      19.4802996029
```

For the production target segmentation `0.20 m`, the validation-only mathematical errors for this synthetic case are approximately:

```text
|Q0_mid - Q0_exact| ~= 9.83e-5 N
|X_mid  - X_exact|  ~= 6.40e-6 m
```

These values validate the numerical discretization only; they do not authorize a production solver switch or segmentation change.

## Required validation regression

The next validation-only package should prove:

1. the connector and payload water weights and steady drags independently reproduce the analytical point jump;
2. two separate point-load sources are crossed exactly once;
3. grouping co-located zero-length jumps gives the same terminal H/V as sequential application;
4. X/Z is continuous across the point coordinate;
5. piecewise exact shooting gives the reference values above;
6. midpoint 0.20 m integration converges to the exact piecewise result with errors below explicit numerical limits;
7. mesh refinement continues to reduce both Q0 and X errors;
8. moving the point load to a different `s` coordinate changes the solution, proving that position — not only total force — matters.

## Point position is part of the physics state

For a cable with distributed loads, applying the same finite force jump at different `s` positions changes the force field over the remaining line and therefore changes the final X/Z geometry and the required `Q0`.

A future production boundary solver must therefore consume point loads with their ordered line coordinates from the calculation/read model. It must not collapse all internal payloads into one global aggregate if their positions differ.

## No double counting

When a point-load row is used in the boundary field:

```text
connector/payload aggregate ElementRows must not be added again
```

The already merged boundary-ledger validation remains the ownership guard.

Buoy and anchor remain boundary/support objects and are not internal point loads.

## Next step after this regression

After the point-load analytical regression is green, the next safe source-validation step is to compare the boundary-conditioned constant-current heavy-line case with Berteaux's constant-current/cable-function solutions under explicitly overlapping assumptions.

That comparison must account for the fact that Berteaux retains orientation-dependent normal/tangential drag while the current frozen reference uses constant distributed `q_x`.

Do not claim equality unless those assumptions are reconciled.

## Production boundary remains unchanged

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

The next validation package should prove the exact effect of ordered internal point-load jumps before any surface-boundary solver enters production.
