# Control mark: surface-boundary shooting evidence

Date: 2026-08-13  
Physics RFC: #413  
Depends on: #407  
Measurement PR: #416

## Purpose

This record captures the first validation-only surface-buoy boundary shooting experiment defined by:

`docs/CONTROL_MARK_SURFACE_BUOY_VERTICAL_BOUNDARY_RFC_2026-08-13.md`

The temporary evidence logger used to collect the values below is removed from the final PR. Production calculation code was not changed.

## Validation model

The unknown was the buoy-side downward vertical cable-tension component:

```text
Q0 >= 0
```

bounded by the current full-volume buoyancy capacity:

```text
Q_capacity = max(0, (rho * V_full - W_b) * g)
```

For a trial `Q0`, the frozen steady-load field was integrated from buoy toward anchor.

Top boundary:

```text
H(0) = D_b
V(0) = Q0
```

where `D_b` is steady buoy current drag reconstructed from the already regression-verified load ownership.

Distributed segments:

```text
H += Segment.CurrentForceN
V -= Segment.WeightWaterKg * g
```

Connector/payload point loads were crossed at their existing `s` positions exactly once:

```text
H += Point.CurrentForceN
V -= Point.WeightWaterKg * g
```

Wave was excluded from this Chapter-2 static-current field.

The validation geometry used midpoint segment force state:

```text
T_mid = hypot(H_mid, V_mid)
tx = H_mid / T_mid
tz = V_mid / T_mid

dx = ds * tx
dz = ds * tz
```

For slack cases, a bounded bisection search solved:

```text
Z_anchor(Q0) = target depth
```

within:

```text
0 <= Q0 <= Q_capacity
```

The `0.01 m` depth target used by the evidence runner was explicitly numerical only and is not accepted as a physical-existence criterion.

## CI state of the measurement head

Measurement head:

```text
c366183327165508eaf1c4241bfbce286864d1df
```

Exact-head results:

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

The existing five-scenario engineering golden verifier passed unchanged.

## Scenario A: zero-current vertical heavy line

Inputs:

```text
Depth = 50 m
Line length = 50 m
steady horizontal force = 0
```

Result:

```text
Classification = VerticalGeometryBoundaryNonUnique
Q_capacity = 9071.151249999999 N
minimum Q needed to keep all sampled cable directions downward
  = 49.03325000100025 N
```

At the lower trial boundary:

```text
Q0 = 0
Z = -50.00000000000016 m
```

At full capacity:

```text
Z = +50.00000000000016 m
```

The minimum downward value is approximately the total positive line water weight.

### Interpretation

With zero horizontal load and `L == depth`, geometry does not uniquely identify the top reaction once the entire line remains vertically downward.

This is a correct limiting-case warning:

```text
vertical geometry closure != unique boundary reaction
```

A future solver must classify this case rather than invent a unique `Q0` from geometry alone.

## Scenario B: uniform-current slack heavy line

Inputs:

```text
Depth = 50 m
Line length = 55 m
steady total current force = 251.12500000000048 N
steady buoy drag = 82 N
wave force, excluded from static solve = 89.92306232103637 N
Q_capacity = 9071.151249999999 N
```

Bound values:

```text
Z(Q0=0) = -7.904564851099594 m
residual = -57.90456485109959 m

Z(Q_capacity) = 54.989856235130624 m
residual = +4.989856235130624 m
```

The target was bracketed and the sampled response was monotone.

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

A physically bounded surface-displacement root exists far below full-volume buoyancy capacity.

The current full-volume `rho*V` value is therefore demonstrably unnecessary as the default actual surface buoyancy for this case.

Equivalent displaced-water mass at the solved state is roughly:

```text
B_actual/g ~= 141.3 kg
```

for a full-volume capacity of about `1025 kg` displaced water.

This is consistent with treating full-volume buoyancy as a capacity bound rather than the normal surface reaction.

## Scenario C: buoyant line at the taut limit

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
residual at full capacity = -0.00040376643852368943 m
```

### Interpretation

For an inextensible line with:

```text
L == vertical depth
```

any finite non-zero horizontal component implies:

```text
vertical span < arc length
```

so exact closure requires the zero-angle/infinite-tension limit rather than a finite root.

The key numerical observation is that the full-capacity residual is only about:

```text
0.000404 m
```

which is much smaller than the historical `0.01 m` geometry tolerance.

Therefore:

```text
|depth residual| <= 0.01 m
```

must **not** be interpreted as proof that a physically finite taut equilibrium exists.

The project needs an exact limiting-case classification independent of the numerical root tolerance.

## Scenario D: connector + payload on a slack line

Inputs:

```text
Depth = 50 m
Line length = 55 m
steady total current force = 258.8125000000005 N
steady buoy drag = 82 N
wave force, excluded = 32.3723024355731 N
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

The internal connector/payload loads materially increase the required top vertical reaction compared with the otherwise similar heavy-line case:

```text
405.28 N -> 741.35 N
```

The validation path crossed both internal point loads exactly once.

This supports the ownership/order established by #412 and #413.

## Scenario E: depth-varying current profile, slack line

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

The bounded surface reaction remains well behaved for the frozen depth-varying load profile and again requires only a small fraction of the available full-volume buoyancy capacity.

The result is not yet evidence for changing production shape because line drag is still the existing preliminary/frozen load family, not geometry-coupled drag.

## Scenario F: line shorter than depth

Inputs:

```text
Depth = 50 m
Line length = 45 m
```

Result:

```text
Classification = LineShorterThanDepth
Z(Q_capacity) = 44.99982775245438 m
residual = -5.0001722475456205 m
```

### Interpretation

The bounded validation solver correctly refuses to invent a geometric root.

This is a hard inextensible-geometry no-solution case.

## Scenario G: heavy line with L == D and nonzero current

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
residual at full capacity = -0.008323034926242201 m
```

### Interpretation

The finite-capacity endpoint again lies within the historical `0.01 m` geometry tolerance while remaining strictly below exact depth.

This repeats the taut-limit warning with a heavy line:

```text
numerically small residual != finite exact inextensible solution
```

Production physics must not use the old numerical tolerance to erase this distinction.

## Scenario H: deliberately insufficient buoyancy capacity

The buoy volume was reduced while its 100 kg dry weight and the slack heavy line were retained.

Inputs/result:

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

The solver remains bounded by available buoyancy and does not extrapolate to the several-hundred-newton `Q0` required by the normal-capacity slack case.

This provides the first direct validation evidence for a future engineering state such as:

```text
surface equilibrium unavailable: insufficient buoyancy reserve
```

without changing the current production verdict yet.

## Main findings

### 1. `Q0` is a meaningful surface-boundary closure variable for slack cases

The three slack cases with sufficient buoyancy capacity produced deterministic monotone brackets and bounded roots:

```text
uniform heavy        Q0 ~= 405 N
discrete payload     Q0 ~= 741 N
profile slack        Q0 ~= 425 N
```

No full-volume boundary reaction was required.

### 2. Point loads enter the expected boundary field cleanly

Connector/payload point loads increased the top reaction and were crossed once.

### 3. Full-volume buoyancy behaves naturally as a capacity bound

The deliberately low-capacity case correctly had no root in the admissible interval.

### 4. Exact limiting cases need their own classification

`L < D` is a hard no-solution case.

`L == D` with nonzero horizontal load is an exact no-finite-root/infinite-tension limit for an inextensible line even when the finite-capacity residual is numerically small.

### 5. The historical 0.01 m residual threshold is not a physics criterion

Two taut cases reached residuals smaller than 1 cm at high finite `Q`, but exact geometry remained unclosed.

Therefore the future physical solver must separate:

```text
root numerical tolerance
```

from:

```text
physical existence classification
```

## What remains unvalidated

This experiment does not yet prove that the midpoint frozen-load discretization is the correct production numerical method.

Before production use the project still needs:

1. independent continuous/analytical validation for constant distributed loads;
2. comparison with Berteaux constant-current solutions where assumptions overlap;
3. mesh sensitivity around the existing 0.20 m target segment length;
4. a formal exact taut-limit classification;
5. validation of point-load jump handling against an analytical controlled case;
6. later geometry-dependent drag coupling;
7. explicit historical selected-X/Z impact review.

## Next safe step

The next work should be independent analytical/reference validation of the **constant-load frozen shooting model**.

Do not yet create a production surface-boundary solver.

A useful analytical check is the continuous cable with constant horizontal distributed load `q_x` and constant signed vertical distributed load `q_z`, for which:

```text
H(s) = H0 + q_x s
V(s) = Q0 - q_z s
```

and:

```text
dx/ds = H / sqrt(H^2 + V^2)
dz/ds = V / sqrt(H^2 + V^2)
```

The discrete midpoint integration can be compared against high-accuracy/closed-form or independent numerical integration of this continuous field without changing production code.

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

Phase-A shooting is promising and physically interpretable for slack surface cases, but it remains validation-only.

The next gate is analytical/reference validation, not production integration.
