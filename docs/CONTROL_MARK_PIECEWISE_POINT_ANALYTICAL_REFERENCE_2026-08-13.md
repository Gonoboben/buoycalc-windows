# Control mark: piecewise / point-load analytical reference for surface-boundary shooting

Date: 2026-08-13  
Physics RFC: #413  
Depends on: #407  
Base main: `48dd68ab392e6be8e66902a8593a56f81f0091fc`

## Purpose

PR #420 validated the exact continuous integral and midpoint mesh convergence for one smooth constant distributed-load interval.

The next required validation step is to prove that the same surface-boundary shooting construction remains internally consistent when the line contains:

- multiple distributed intervals;
- connector / payload point loads;
- multiple point-load source rows at the same line coordinate `s`.

This control mark defines the **piecewise exact reference** before implementation.

No production physics is changed by this document.

## Source boundary

Primary physical source remains H. O. Berteaux / Г. О. Берто, *Океанографические буи*, 1979, Chapter 2 §2.1.

The source establishes signed cable statics, resultant-tension vector geometry and the need to solve the surface-buoy vertical boundary by successive approximation when that component is unknown.

The exact constant-load antiderivative used here is the independently derived BuoyCalc validation reference already defined in:

`docs/CONTROL_MARK_CONSTANT_LOAD_ANALYTICAL_REFERENCE_2026-08-13.md`

The **point-load jump and same-s grouping rules are project discretization / ownership conventions** already protected by the boundary-load and Candidate-B validation. They are not presented as a separate Berteaux formula.

## Coordinate and sign convention

Validation plane:

```text
s = 0 at buoy, increasing toward anchor
+X_shape = from buoy toward anchor
+Z = downward
```

For one continuous interval `j`:

```text
H_j(u) = H_j,0 + qx_j * u
V_j(u) = V_j,0 - w_j  * u
0 <= u <= L_j
```

where:

```text
qx_j = frozen steady horizontal distributed load [N/m]
w_j  = signed downward submerged weight load [N/m]
```

Thus:

```text
w_j > 0  heavy interval
w_j < 0  buoyant interval
```

and local tangent is:

```text
T = hypot(H,V)
tx = H/T
tz = V/T
```

whenever `T` is non-degenerate.

No `Abs(V)` is allowed in the reference field.

## Exact interval propagation

For every interval, use the exact continuous antiderivative already validated by #420.

Given input state:

```text
r0 = [H0, V0]
q  = [qx, -w]
```

and interval length `L`, the exact integral returns:

```text
DeltaR = integral_0^L (r0 + q*u) / |r0 + q*u| du
```

plus the terminal force state:

```text
H1 = H0 + qx*L
V1 = V0 - w*L
```

The terminal state is the input state for the next event at the same line coordinate.

## Point-load jump

At a mechanical point node located between two distributed intervals, geometry has zero arc length across the point. The point load therefore changes the force state but contributes no direct `dx` / `dz` increment.

For the project top-to-bottom convention:

```text
H_after = H_before + F_point
V_after = V_before - W_point_water * g
```

where `W_point_water` retains its sign.

A heavy point load has:

```text
W_point_water > 0
```

and reduces the downward cable component `V` below the point.

A buoyant point load has:

```text
W_point_water < 0
```

and increases `V` below the point.

## Same-s grouping

All connector / payload source rows at the same line coordinate form one mechanical point event:

```text
F_group = sum(F_point,k)
W_group = sum(W_point_water,k)
```

and the force jump is applied **once**:

```text
H_after = H_before + F_group
V_after = V_before - W_group*g
```

Grouping must be algebraically equivalent to applying the same source rows sequentially at zero arc-length separation.

No point source may be counted again through aggregate `ElementRows`.

## Event ordering

For a point at the common boundary between upper interval `A` and lower interval `B`:

```text
1. integrate interval A exactly to its lower endpoint;
2. record the pre-point node state;
3. apply the grouped point jump exactly once;
4. record the post-point node state;
5. integrate interval B from that post-point state.
```

This ordering is the permanent reference for later midpoint validation.

## Surface-boundary shooting variable

As in #413 / #419:

```text
Q0 = downward buoy-side vertical cable-tension component
0 <= Q0 <= Q_capacity
```

For an admissible trial `Q0`, piecewise propagation gives:

```text
X_anchor(Q0)
Z_anchor(Q0)
```

The exact reference root is:

```text
Z_anchor(Q0*) = target depth D
```

searched only inside the buoyancy-capacity interval.

## Deterministic point-load reference case

Use the same synthetic assembly already present in the boundary / Candidate-B regressions:

```text
Depth D = 50 m
Upper line = 30 m heavy line
Connector + payload at s = 30 m
Lower line = 25 m same heavy line
Total line length L = 55 m
steady current = 0.5 m/s
wave excluded from static reference
```

Frozen distributed field for both line intervals:

```text
H0 at buoy = 82 N
qx = 3.075 N/m
w  = 0.980665 N/m
```

The same-s point group is:

```text
connector submerged weight = 4.2825 kg
payload submerged weight   = 34.875 kg
--------------------------------------
group submerged weight     = 39.1575 kg

group vertical force magnitude = 384.003897375 N

group steady horizontal drag = 7.6875 N
```

Full available buoyancy reserve remains:

```text
Q_capacity = 9071.15125 N
```

## Exact piecewise reference target

Applying:

```text
exact 30 m interval
-> grouped point jump
-> exact 25 m interval
```

and solving the depth equation tightly gives approximately:

```text
Q0_exact_point ~= 741.26584043891 N
X_exact_point  ~= 19.48029920259 m
Z_exact_point  = 50 m
```

The terminal frozen force state is approximately:

```text
H_bottom = 258.8125 N
V_bottom = 303.3253680639 N
```

The implementation must recompute these values from the equations rather than merely hard-code them.

## Midpoint mesh convergence target

For validation-only midpoint integration, each line item is independently segmented by the existing target rule:

```text
N_j = ceil(L_j / target_ds)
ds_j = L_j / N_j
```

The point jump is applied exactly at the shared line-item boundary `s=30 m` and is never smeared across a segment.

Independent pre-check values are approximately:

```text
nominal ds   N_upper  N_lower   Q0_midpoint (N)   X_midpoint (m)   |Q-Qexact| (N)    |X-Xexact| (m)
0.8          38       32        741.2643267709     19.4803974128     1.514e-3           9.821e-5
0.4          75       63        741.2654507714     19.4803245104     3.897e-4           2.531e-5
0.2          150      125       741.2657421451     19.4803056070     9.829e-5           6.404e-6
0.1          300      250       741.2658158655     19.4803008037     2.457e-5           1.601e-6
0.05         600      500       741.2658342955     19.4802996029     6.143e-6           4.003e-7
```

The error trend is again approximately second order for this smooth piecewise case when the point location is represented exactly as a mesh boundary.

These are validation targets only; production target segmentation remains 0.20 m.

## Required algebraic identities

The focused regression must prove all of the following.

### 1. Zero-point identity

If the grouped point load is set to zero and both intervals use identical distributed loads, exact piecewise propagation across `30 m + 25 m` must match a single exact `55 m` interval to tight floating-point tolerance.

### 2. Grouping identity

Two same-s source loads applied sequentially at zero arc length must give the same post-node `(H,V)` and endpoint `(X,Z)` as one grouped equivalent load.

### 3. Point jump identity

At the point node:

```text
DeltaH = +F_group
DeltaV = -W_group*g
```

with no position jump:

```text
DeltaX_point = 0
DeltaZ_point = 0
```

### 4. Signed buoyant-point identity

A synthetic negative `W_group` must increase `V` below the point; no absolute value is permitted.

### 5. Capacity bound

The exact point-load root must remain inside:

```text
0 < Q0* < Q_capacity
```

and no reference search may extrapolate beyond capacity.

### 6. Mesh convergence

The midpoint piecewise roots and X coordinates must converge toward the exact piecewise solution as validation target step decreases.

The 0.20 m error must be recorded explicitly.

## Optional piecewise-load discontinuity check

After the point-load case is green, the same exact interval propagator can also validate a distributed-load discontinuity:

```text
interval A: qx_A, w_A
interval B: qx_B, w_B
```

with no point load.

The force state must remain continuous at the interface while its derivative changes.

This is useful preparation for later depth-profile / multi-line-section reference work, but it must remain validation-only.

## Relation to Berteaux

This reference preserves the Berteaux-derived physical ideas of:

- signed submerged load;
- vector cable tension;
- geometry related to the local tension direction;
- a surface-boundary unknown solved under an additional geometric/system constraint.

However, the exact interval field still freezes distributed horizontal load `qx` instead of recalculating Berteaux's angle-dependent normal/tangential hydrodynamic resistance.

Therefore the piecewise exact solution validates **BuoyCalc's Phase-B frozen-load discretization and point ownership**, not the complete Berteaux cable equations.

## Next allowed implementation

Validation only, e.g.:

`PiecewisePointLoadAnalyticalReferenceRegression`

It may implement:

- exact interval propagation using the #420 formula;
- exact grouped point jumps;
- bounded exact Q0 root;
- midpoint mesh convergence;
- zero-point / same-s grouping / buoyant-point identities;
- optional two-interval distributed-load discontinuity.

It must not be called by production services.

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

The next gate is exact piecewise / point-load validation.

Only after that regression is green should #413 proceed to the first Berteaux-overlap comparison or a production read-model proposal. A production surface-boundary solver is still not authorized.
