# Control mark: surface-boundary shooting evidence

Date: 2026-08-13  
Physics RFC: #413  
Depends on: #407  
Measurement branch/PR: #416 (experimental, not merged)  
Current base main: `7412e4be961144b5975016087fcaad21d867ecf6`

## Purpose

This control mark records the first validation-only shooting study for the unresolved vertical boundary reaction of a free surface buoy.

The study followed the boundary defined in:

`docs/CONTROL_MARK_SURFACE_BUOY_VERTICAL_BOUNDARY_RFC_2026-08-13.md`

and used the ordered force-ledger ownership now protected by the validation work merged through #415.

The measurement code itself is not part of this control mark and is not approved for production.

## Primary source boundary

Primary project source:

> H. O. Berteaux / Г. О. Берто, *Buoy Engineering / Океанографические буи*, 1979, Part II, Chapter 2, §2.1.

For a surface buoy, Berteaux gives the static balance in Eqs. (2.36)–(2.37):

```text
B_b = W_b + T_vert - R_vert
D_b = T_hor
```

The surrounding discussion states that when the vertical cable-tension component is unknown, another physical constraint such as cable length or horizontal excursion must be supplied and the problem is solved by successive approximations.

The current BuoyCalc static path has no vertical hydrodynamic/lift term on the buoy, so the validation specialization was:

```text
T_vert = B_actual - W_b
T_hor  = D_b
```

The full-volume value `rho * V_full * g` was treated only as an upper buoyancy-capacity bound, not as the automatically realized surface reaction.

## Validation model

The shooting unknown was:

```text
Q0 = downward vertical cable-tension component at the buoy
```

bounded by:

```text
0 <= Q0 <= Q_capacity
Q_capacity = max(0, (rho * V_full - W_b) * g)
```

The local shape-plane convention was:

```text
s = 0 at buoy, increasing toward anchor
+Z = downward
+X_shape = from buoy toward anchor
```

No East/North or 3D geometry was introduced.

### Frozen steady-load field

The study deliberately reused the existing steady loads without geometry feedback.

Top boundary:

```text
H(0) = steady buoy current drag
V(0) = Q0
```

Distributed segment crossing:

```text
H += Segment.CurrentForceN
V -= Segment.WeightWaterKg * g
```

Connector/payload point-load crossing:

```text
H += Point.CurrentForceN
V -= Point.WeightWaterKg * g
```

Buoy and anchor remained boundary objects and were not added as internal point loads.

`WaveForceN` was excluded from this Chapter-2 static-current field.

### Validation geometry

For each line segment, the study used the midpoint force state:

```text
T_mid = hypot(H_mid, V_mid)
tx = H_mid / T_mid
tz = V_mid / T_mid

dx = ds * tx
dz = ds * tz
```

For slack cases, bounded bisection searched only inside `[0, Q_capacity]` for:

```text
Z_anchor(Q0) = target depth
```

The temporary study used `0.01 m` only as a numerical stopping target. This value is not an engineering or physical-existence criterion.

## Measurement-head CI

The successful measurement head was:

```text
c366183327165508eaf1c4241bfbce286864d1df
```

Its checks were:

```text
.NET Build                     success
Selected Shape Consumer Scan   success
Report Store Consumer Scan     success
```

Production project build:

```text
0 Warning(s)
0 Error(s)
```

The committed five-scenario engineering golden verification remained unchanged and passed.

## Scenario A — zero-current vertical heavy line

Inputs:

```text
Depth = 50 m
Line length = 50 m
steady horizontal force = 0
Q_capacity = 9071.151249999999 N
```

Result:

```text
Classification = VerticalGeometryBoundaryNonUnique
Q0 = 0        -> Z = -50.00000000000016 m
Q0 = capacity -> Z = +50.00000000000016 m
minimum Q for all sampled directions to remain downward
  = 49.03325000100025 N
```

The minimum value is approximately the total positive water weight of the line.

### Interpretation

For zero horizontal load and `L == depth`, geometry does not uniquely identify the top reaction once the cable is everywhere vertically downward.

Therefore:

```text
vertical geometry closure != unique Q0
```

A future solver must classify this limit rather than manufacture a unique surface reaction from geometry alone.

## Scenario B — uniform-current slack heavy line

Inputs:

```text
Depth = 50 m
Line length = 55 m
steady total current force = 251.12500000000048 N
steady buoy drag = 82 N
wave force excluded from static solve = 89.92306232103637 N
Q_capacity = 9071.151249999999 N
```

Capacity-bound endpoint values:

```text
Z(Q0=0) = -7.904564851099594 m
residual = -57.90456485109959 m

Z(Q_capacity) = 54.989856235130624 m
residual = +4.989856235130624 m
```

The target was bracketed and the sampled relation was monotone.

Solved result:

```text
Classification = SolvedByBoundedBisection
Q0 = 405.2784860229491 N
Q0 / Q_capacity = 0.04467773437499999
B_actual / B_max = 0.13787990663109753
root iterations = 12
endpoint X = 21.919036791100037 m
endpoint Z = 49.9963893072144 m
vertical residual = -0.0036106927855996673 m
H range = 82 ... 251.12500000000142 N
V range = 351.341911022956 ... 405.2784860229491 N
V sign change = false
point-load crossings = 0
```

### Interpretation

A bounded surface-displacement root exists far below full-volume buoyancy capacity.

The solved displaced-water force corresponds to only about 13.8% of full-volume capacity in this synthetic case.

This is direct validation evidence that `rho * V_full * g` is naturally a capacity bound rather than the default surface reaction.

## Scenario C — buoyant line at taut `L == D` limit

Inputs:

```text
Depth = 30 m
Line length = 30 m
steady total current force = 62.72999999999993 N
steady buoy drag = 29.520000000000003 N
Q_capacity = 9071.151249999999 N
```

Result:

```text
Classification = TautLimitNonZeroHorizontalLoad_NoFiniteRootExpected
Z(Q0=0) = 4.31436427714968 m
Z(Q_capacity) = 29.999596233561476 m
capacity residual = -0.00040376643852368943 m
```

### Interpretation

For an inextensible cable with vertical span equal to arc length, any finite non-zero horizontal component makes the exact vertical span strictly smaller than the arc length.

The capacity residual is only about 0.4 mm — much smaller than the historical 1 cm geometry tolerance — yet the exact finite-tension solution still does not exist.

Therefore:

```text
small numerical depth residual != proof of physical existence
```

A taut-limit classification must be independent of the numerical root tolerance.

## Scenario D — connector + payload on slack line

Inputs:

```text
Depth = 50 m
Line length = 55 m
steady total current force = 258.8125000000005 N
steady buoy drag = 82 N
wave force excluded = 32.3723024355731 N
Q_capacity = 9071.151249999999 N
internal point loads = connector + payload
```

Solved result:

```text
Classification = SolvedByBoundedBisection
Q0 = 741.3495803070066 N
Q0 / Q_capacity = 0.08172607421874999
B_actual / B_max = 0.1713137742949695
root iterations = 14
endpoint X = 19.477191969640348 m
endpoint Z = 50.00188552399706 m
vertical residual = +0.0018855239970605453 m
H range = 82 ... 258.8125000000014 N
V range = 303.40910793200504 ... 741.3495803070066 N
V sign change = false
point-load crossings = 2
```

### Interpretation

The connector/payload point loads materially increase the required top vertical reaction compared with the similar heavy-line-only case:

```text
about 405 N -> about 741 N
```

Both internal point loads were crossed exactly once.

This agrees with the load ownership/order protected by the boundary-ledger validation.

## Scenario E — depth-varying current profile, slack line

Inputs:

```text
Depth = 50 m
Line length = 55 m
steady total current force = 203.76967199999984 N
steady buoy drag = 118.07999999999998 N
wave = 0
Q_capacity = 9071.151249999999 N
```

Solved result:

```text
Classification = SolvedByBoundedBisection
Q0 = 425.21021484374995 N
Q0 / Q_capacity = 0.046875
B_actual / B_max = 0.13986280487804878
root iterations = 6
endpoint X = 22.634793012070006 m
endpoint Z = 50.00413619427474 m
vertical residual = +0.004136194274742877 m
H range = 118.07999999999998 ... 203.7696719999999 N
V range = 371.2736398437569 ... 425.21021484374995 N
V sign change = false
```

### Interpretation

A bounded root also exists for the frozen depth-varying load field.

This still does not validate production geometry because the drag field remains frozen from the current preliminary calculation and is not yet re-evaluated from the new X/Z orientation.

## Scenario F — line shorter than depth

Inputs/result:

```text
Depth = 50 m
Line length = 45 m
Classification = LineShorterThanDepth
Z(Q_capacity) = 44.99982775245438 m
residual = -5.0001722475456205 m
```

### Interpretation

The bounded study correctly refuses to invent a geometric root.

`L < D` is a hard inextensible-geometry no-solution case.

## Scenario G — heavy line with `L == D` and nonzero current

Inputs:

```text
Depth = 50 m
Line length = 50 m
steady total current force = 235.75000000000026 N
steady buoy drag = 82 N
Q_capacity = 9071.151249999999 N
```

Result:

```text
Classification = TautLimitNonZeroHorizontalLoad_NoFiniteRootExpected
Z(Q_capacity) = 49.99167696507376 m
capacity residual = -0.008323034926242201 m
```

### Interpretation

The finite-capacity residual is again smaller than 1 cm while exact depth is still not reached.

This independently repeats the conclusion that the old `0.01 m` geometry tolerance is numerical only and cannot define physical existence.

## Scenario H — deliberately insufficient buoyancy capacity

The synthetic buoy volume was reduced while retaining 100 kg dry mass and the slack heavy line.

```text
Depth = 50 m
Line length = 55 m
steady total current force = 251.12500000000048 N
steady buoy drag = 82 N
Q_capacity = 34.568441250000056 N
Classification = InsufficientBuoyancyCapacity
Z(Q_capacity) = 4.386812682542805 m
residual = -45.6131873174572 m
```

### Interpretation

The study remains bounded by the available full-volume buoyancy and refuses to extrapolate to the several-hundred-newton reaction required by the normal-capacity slack case.

This supports a future physical classification such as:

```text
surface equilibrium unavailable: insufficient buoyancy reserve
```

without changing the current production verdict yet.

## Main findings

### 1. `Q0` is a meaningful closure variable for slack frozen-load cases

The three slack scenarios with sufficient buoyancy produced deterministic bounded roots:

```text
uniform heavy      Q0 ~= 405 N
discrete payload   Q0 ~= 741 N
profile slack      Q0 ~= 425 N
```

### 2. Point loads fit the same boundary field

Connector/payload loads changed the required top reaction and entered exactly once.

### 3. Full-volume buoyancy acts naturally as an upper bound

The low-capacity case had no admissible root and was not extrapolated beyond capacity.

### 4. Exact limiting cases require explicit classification

```text
L < D                    -> hard no geometric solution
L == D, horizontal > 0   -> no finite exact inextensible solution
L == D, horizontal = 0   -> geometry may not uniquely identify Q0
```

### 5. Root tolerance and physical existence are different concepts

Two taut cases reached residuals below `0.01 m` while exact finite equilibrium remained absent.

The future solver must preserve this distinction.

## What remains unvalidated

The shooting experiment does **not** yet prove that midpoint frozen-load integration is the correct production numerical method.

Before production use the project still requires:

1. independent continuous/analytical validation for constant distributed loads;
2. comparison with Berteaux constant-current statics where assumptions overlap;
3. mesh-sensitivity evidence around the existing 0.20 m target segmentation;
4. formal exact taut-limit tests;
5. controlled analytical validation of point-load jumps;
6. later geometry-dependent drag coupling;
7. explicit review of historical selected-X/Z changes before any production switch.

## Next safe step

The next work is independent analytical validation of the constant-load frozen shooting model.

For a continuous cable with constant distributed horizontal load `q_x` and signed vertical distributed load `q_z`:

```text
H(s) = H0 + q_x s
V(s) = Q0 - q_z s
```

and:

```text
dx/ds = H / sqrt(H^2 + V^2)
dz/ds = V / sqrt(H^2 + V^2)
```

The midpoint discrete integration must be compared against an independent continuous/closed-form solution before any production boundary-solver type is introduced.

## Production boundary remains unchanged

This evidence does not authorize changes to:

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

The surface-boundary shooting variable is physically promising for slack cases and correctly respects buoyancy capacity and internal point-load ownership.

It remains validation-only.

The next gate is analytical/reference validation, not production integration.
