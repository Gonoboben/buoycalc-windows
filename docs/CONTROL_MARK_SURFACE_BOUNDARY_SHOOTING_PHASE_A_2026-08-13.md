# Control mark: surface-buoy boundary shooting — Phase A

Date: 2026-08-13  
Issues: #413, #407  
PR: #417

## Purpose

This control mark records the first bounded, validation-only shooting study for the surface-buoy vertical boundary reaction.

It does **not** change the production solver or selected X/Z. It tests whether the source-backed surface-buoy boundary unknown can close a frozen-load planar geometry without exceeding the buoyancy capacity already present in the current input model.

## Primary-source basis

Primary project source: H. O. Berteaux / Г. О. Берто, *Океанографические буи*, 1979, Chapter 2 §2.1.

### Stepped current / node-vector construction

Printed p. 57 states that, for stepped-current approximation, gravitational and drag loads are evaluated for every step, the resultant tension vector is determined at the nodes, and cable geometry is approximated by a stepped line following those tension vectors from node to node (Fig. 2.11, Eq. 2.35 context).

This Phase-A integrator therefore constructs each segment direction from the tension state at the segment **start node**, then crosses that segment's distributed load to obtain the next node state.

### Surface buoy

Printed pp. 58–59 give the free surface-buoy balance in Eqs. (2.36)–(2.37) and explain that when the vertical cable-tension component is unknown, another physical system constraint must be specified and the solution is obtained by successive approximations.

For this validation, the prescribed constraint is the existing project depth / inextensible line-length geometry. Full-volume buoyancy remains a **capacity bound**, not an already solved surface displacement.

## Frozen-load model

Wave load is excluded from this Chapter-2 static study.

The unknown is

```text
Q0 = downward buoy-side vertical cable-tension component
```

bounded by

```text
0 <= Q0 <= Q_capacity
Q_capacity = max(0, B_max - W_b)
```

with

```text
B_actual = W_b + Q0
B_actual <= B_max
```

under the current Phase-A assumption of no independently modelled vertical hydrodynamic lift on the buoy.

The top steady state is

```text
H(0+) = steady buoy drag
V(0+) = Q0
```

and top-to-bottom load crossing is

```text
H += steady horizontal load
V -= signed WeightWaterKg * g
```

for distributed segments and same-s grouped connector/payload point loads.

The tangent is kept vectorial:

```text
T  = hypot(H,V)
tx = H/T
tz = V/T
```

No `Abs(V)` and no first-quadrant scalar-angle reconstruction is used in this validation geometry.

## Numerical root policy

The root search is bounded to `[0, Q_capacity]`.

- no extrapolation beyond buoyancy capacity;
- no manufactured direction for degenerate zero tension;
- bisection only when the two capacity-bound depth residuals provide a sign-changing bracket;
- `0.01 m` is only the existing numerical depth target used by this validation, not an engineering tolerance;
- analytical limiting cases override a misleading tolerance-only classification.

## First measurement evidence

The first evidence run used the exact draft head before deterministic classification assertions were added. Production C# built with `0 Warning(s), 0 Error(s)` and the unchanged five-scenario engineering golden verification passed.

| Case | Classification | Q0, N | Q0 / Q_capacity | B_actual / B_max | X, m | Z, m | depth residual, m |
|---|---|---:|---:|---:|---:|---:|---:|
| A zero-current taut heavy | `VerticalGeometryBoundaryNonUnique` | n/a | n/a | n/a | n/a | n/a | n/a |
| B uniform-current slack heavy | `Solved` | 404.7248269 | 0.0446167 | 0.1378248 | 21.9067294 | 50.0006117 | +0.0006117 |
| C buoyant slack line | `Solved` | 95.7830302 | 0.0105591 | 0.1070899 | 13.5878940 | 29.9903207 | -0.0096793 |
| D connector + payload | `Solved` | 740.7959212 | 0.0816650 | 0.1712587 | 19.4685822 | 50.0030597 | +0.0030597 |
| E depth-varying current | `Solved` | 424.6565557 | 0.0468140 | 0.1398077 | 22.6393915 | 50.0007664 | +0.0007664 |
| F line shorter than depth | `NoGeometricSolutionLineShorterThanDepth` | n/a | n/a | n/a | n/a | n/a | n/a |
| G taut line + non-zero current | `NoFiniteRootTautWithHorizontalLoad` | n/a | n/a | n/a | n/a | n/a | n/a |
| H insufficient buoyancy capacity | `NoRootWithinBuoyancyCapacity` | n/a | n/a | n/a | n/a | n/a | n/a |

The `B_actual / B_max` values above are reconstructed from the same Phase-A relation:

```text
B_actual = W_b + Q0
```

and are well below full-volume capacity in the four solved slack cases.

## Important limiting-case evidence

### A — zero-current taut vertical line

Depth closure alone does not uniquely identify `Q0` for a perfectly vertical zero-horizontal geometry. This is intentionally classified as a boundary non-uniqueness, not a solved unique reaction.

### C — buoyant line

The solved candidate preserves signed negative distributed `WeightWaterKg`. Under the top-to-bottom convention,

```text
V_next = V_current - WeightWaterKg*g
```

so a negative line water weight increases the downward cable-tension component. The deterministic regression now protects this sign behavior.

### D — same-s connector + payload

The two source elements form one mechanical point crossing in the shooting ledger. The final regression explicitly requires one grouped point crossing.

### G — taut line with horizontal load

This case is particularly important.

At full buoyancy capacity, the finite trial produced a depth residual of approximately

```text
-0.008293 m
```

which lies inside the validation's `0.01 m` numerical band.

Nevertheless `L == Depth` with non-zero horizontal load cannot have an exact finite-tension non-vertical inextensible geometry whose vertical span equals its arc length. Therefore the case remains:

```text
NoFiniteRootTautWithHorizontalLoad
```

The deterministic regression explicitly protects this analytical override so a numerical tolerance cannot silently convert it into `Solved`.

### H — insufficient capacity

The deliberately low-capacity buoy had

```text
Q_capacity = 24.516625 N
```

and both bounded trials stayed on the same side of the depth target. No extrapolation beyond capacity was permitted, so the result is explicitly:

```text
NoRootWithinBuoyancyCapacity
```

## Interpretation

Phase A demonstrates that a boundary-conditioned, signed-vector, frozen-load construction behaves coherently across:

- heavy slack line under uniform current;
- buoyant slack line;
- same-s connector/payload point loads;
- depth-varying current;
- line-too-short geometry;
- taut/non-zero-current limiting geometry;
- insufficient buoyancy capacity.

This is **not yet an independent validation of the production solver** and is not authority to replace `MooringShapeSolver`.

The Phase-A values are evidence from the current 0.20 m production segmentation and current frozen steady-load calculation. Exact numerical `Q0` values are deliberately **not** made historical golden outputs; deterministic regression freezes only the physical classifications, capacity bounds, sign invariants and point-load ownership needed for the next validation stage.

## Remaining blockers before production use

At minimum:

1. independent/reference comparison for a simple constant-current heavy-line case using Berteaux-compatible statics;
2. mesh sensitivity around the current 0.20 m segmentation in validation only;
3. explicit comparison of the stepped approximation with an analytical/simple limiting case;
4. review of actual surface-buoy displacement semantics and any future vertical buoy hydrodynamic force model;
5. only after those checks, a separate proposal for production boundary-conditioned X/Z.

## Numerical / product impact

None in production.

This Phase-A package does not change:

- `MooringShapeSolver`;
- `MooringDiscreteLoadShapeBuilder`;
- `MooringIterativeSolver`;
- selected X/Z;
- `MooringPrimaryShapeGate`;
- `CalculationResult.Verdict`;
- anchor or weak-link calculations;
- 0.20 m target segmentation or unlimited segment count;
- signed `WeightWaterKg` semantics;
- report/PDF/2D physics;
- JSON/DTO;
- committed five-scenario golden baseline;
- 3D.

Issue #413 remains open after this Phase-A validation package. Issue #407 also remains open.
