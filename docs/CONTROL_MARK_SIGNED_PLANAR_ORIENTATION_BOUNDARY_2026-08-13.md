# Control mark: signed planar orientation boundary

Date: 2026-08-13  
Issue: #407  
Base main: `5e2583852af31e96cb33499cf0763acf5dae3878`

## Purpose

This control mark records the source-backed boundary for preserving planar force direction between the signed force ledger and any future X/Z geometry construction.

It is documentation only. It does not authorize a solver change.

## Primary source

Primary project source:

> H. O. Berteaux / Г. О. Берто, *Buoy Engineering / Океанографические буи*, Russian edition, Судостроение, 1979.

Relevant material is in Part II, Chapter 2, §2.1.

### Signed submerged line load

Printed p. 34 defines the gravitational load per unit length as:

```text
P = W - B                                      (2.1)
```

The surrounding text states explicitly that the gravitational/buoyancy force can act upward or downward.

Therefore `P` is a signed quantity. A buoyant line section is not equivalent to a heavy line section with `|P|`.

### Differential static equilibrium

Printed pp. 37–38, Fig. 2.4 and Eqs. (2.6)–(2.7), give the elementary flexible-line equilibrium:

```text
T dφ = (D + P cosφ) ds                        (2.6)

dT = (P sinφ - F) ds                          (2.7)
```

`φ` is the angle between the cable axis and current direction.

The sign of `P` remains part of the equilibrium equations. No absolute-value substitution is made.

### Variable-current stepped construction

Printed p. 57 describes the step approximation for variable currents:

- gravity and drag are evaluated for each step;
- the resultant tension vector is determined at each node;
- cable geometry is then approximated by a stepped line following those tension vectors.

Eq. (2.35) derives the node angle from a signed vertical resultant divided by the horizontal resultant.

This is a directional vector construction. It is not a first-quadrant magnitude-only angle construction.

## BuoyCalc coordinate convention

For the planar model:

```text
+X = horizontal offset direction
+Z = depth, positive downward
s  = line coordinate from buoy/top toward anchor/bottom
```

Signed water weight is already retained in the calculation core:

```text
WeightWaterKg > 0  -> downward contribution
WeightWaterKg < 0  -> upward contribution
```

The current force ledger therefore has enough information to distinguish vertical quadrants before angle conversion.

## Current sign-loss path

### SegmentTensionAnalyzer

`Services/SegmentTensionAnalyzer.cs` accumulates signed components:

```text
CumulativeHorizontalForceN
CumulativeVerticalForceN
```

but converts them to angle using:

```csharp
Math.Atan2(
    Math.Abs(cumulativeHorizontalForceN),
    Math.Abs(cumulativeVerticalForceN))
```

This is the first confirmed loss of directional quadrant in the base tension-to-shape path.

### MooringShapeTensionAnalyzer

`Services/MooringShapeTensionAnalyzer.cs` repeats the same conversion:

```text
signed H/V -> Abs(H), Abs(V) -> 0..90 degree angle
```

### MooringDiscreteLoadTensionAnalyzer

`Services/MooringDiscreteLoadTensionAnalyzer.cs` repeats the same conversion for discrete-load cumulative state.

### MooringShapeSolver

`Services/MooringShapeSolver.cs` then applies another magnitude-only transformation:

```csharp
Math.Abs(tensionAngleDeg)
Math.Clamp(..., 0, 89)
```

and advances geometry as:

```text
dx = L sin(angle)
dz = L cos(angle)
```

Thus every constructed segment advances toward positive X and positive Z-depth.

### MooringDiscreteLoadShapeBuilder

`Services/MooringDiscreteLoadShapeBuilder.cs` repeats the same absolute-angle and `0..89` clamp behavior.

### MooringShapeProjection

`Services/MooringShapeProjection.cs` reports a magnitude-only angle through `Abs(dx)` / `Abs(dz)`.

That is acceptable as a display metric, but it is not suitable as an authoritative directional physics state.

## Confirmed engineering consequence

The project currently has two different semantics at once:

```text
force ledger: signed
geometry orientation: unsigned first quadrant
```

Therefore the statement

```text
negative WeightWaterKg is preserved
```

is true for force accumulation but is not sufficient to prove that the resulting geometry preserves the corresponding direction.

The current shape path cannot represent a segment whose top-to-bottom tangent has `dz < 0`.

## Relation to experiment #405

Experimental PR #405 made shape-feedback H/V authoritative in the next discrete candidate state.

The experiment correctly caused feedback to reach geometry, but the unchanged historical verifier then showed 60 changed fields. In particular:

```text
buoyant-line:
Converged -> DivergenceGuard
selected FinalShape -> fallback

discrete-payload:
Converged -> MaxIterationsReached
```

This control mark does **not** claim that unsigned angle handling is the sole root cause.

It records only that signed orientation is a missing prerequisite and must be validated before another coupling attempt.

## Free-body mapping for a cut

Let the resultant external load of the subsystem below a cut, expressed in BuoyCalc +X/+Z coordinates, be:

```text
H_i
V_i
```

with magnitude:

```text
T_i = hypot(H_i, V_i)
```

Static equilibrium of the lower subsystem requires the cut tension acting on that subsystem to be opposite the external resultant.

If `t_i` denotes the cable tangent oriented from top to bottom, then the natural candidate collinear mapping is:

```text
t_x,i = H_i / T_i

t_z,i = V_i / T_i
```

because the cut tension force on the lower subsystem is:

```text
-T_i * t_i
```

This mapping is not yet authorized for production geometry. It must first be proven in deterministic free-body tests.

## Why a vector is preferred over another angle

A scalar angle introduces convention questions immediately:

- from horizontal or from vertical;
- clockwise or counter-clockwise;
- range `0..180`, `-180..180`, or another interval;
- whether an angle and its opposite represent the same cable axis or opposite traversal direction.

The force ledger already contains the unambiguous state:

```text
H
V
```

Therefore the preferred authoritative diagnostic representation is:

```text
HorizontalForceN
VerticalForceN
TensionN
TangentX
TangentZ
OrientationState
```

An angle can be derived later for display only.

## Phase A: read-only signed-orientation diagnostic

The next permitted production package is additive and diagnostic only.

It may:

1. read existing cumulative H/V;
2. normalize them into signed `TangentX/TangentZ` when tension is non-degenerate;
3. publish immutable diagnostic rows;
4. classify zero/near-zero tension as `Indeterminate` or `NotApplicable`;
5. add deterministic validation.

It must not:

```text
change SegmentTensionAnalyzer historical angle output
change MooringShapeSolver
change MooringDiscreteLoadShapeBuilder
change MooringIterativeSolver
change MooringPrimaryShapeGate
change CalculationResult.Verdict
change selected X/Z
change anchor or weak-link calculations
change PDF/2D physics
change JSON/DTO
change target 0.20 m segmentation
change unlimited segment count
change golden baseline
add 3D
```

## Required Phase-A synthetic cases

### Case A: heavy downward resultant

```text
H = +3
V = +4
T = 5
```

Expected:

```text
TangentX = +0.6
TangentZ = +0.8
```

### Case B: buoyant upward resultant

```text
H = +3
V = -4
T = 5
```

Expected:

```text
TangentX = +0.6
TangentZ = -0.8
```

The existing unsigned angle path maps Cases A and B to the same magnitude angle. The signed-vector diagnostic must distinguish them.

### Case C: pure downward

```text
H = 0
V > 0
```

Expected:

```text
TangentX = 0
TangentZ = +1
```

### Case D: pure upward

```text
H = 0
V < 0
```

Expected:

```text
TangentX = 0
TangentZ = -1
```

### Case E: degenerate

```text
H ~= 0
V ~= 0
```

Expected:

```text
OrientationState = Indeterminate/NotApplicable
```

No artificial `(0,+1)` direction is allowed.

## Phase B: validation-only signed geometry

Only after Phase A is merged green, a temporary validation package may construct X/Z directly from signed tangent vectors.

It must remain outside production selection and must not update the golden baseline.

The study must record whether and where:

```text
dz < 0
```

occurs in:

- heavy-line case;
- buoyant-line case;
- discrete-payload case;
- depth-varying-current case.

## Iteration-budget study

The next feedback experiment must separate orientation behavior from iteration-budget behavior.

Production `MaxIterations` remains unchanged.

Validation-only budgets may include:

```text
4
8
16
32
64
```

For each iteration record:

```text
representative cut H/V
signed tangent X/Z
candidate endpoint X/Z
max node delta
offset delta
geometry residual
Candidate-B residual
stop reason
```

No new production limit is chosen from this control mark.

## Acceptance before any production geometry change

A future production proposal must provide all of the following:

1. source-backed coordinate/sign convention;
2. deterministic quadrant tests;
3. no `Abs` between authoritative H/V and authoritative tangent;
4. analytical limiting cases;
5. convergence-study evidence;
6. independent/reference comparison;
7. explicit review of every historical golden change.

## Decision

The current first-quadrant angle is retained temporarily for historical behavior only.

It is not accepted as the authoritative physics representation for future coupled force/shape solving.

The next implementation step is a read-only signed-vector diagnostic. No solver feedback or selected-shape behavior may change until that diagnostic and its validation are green.