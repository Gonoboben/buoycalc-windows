# Control mark: constant-load analytical reference for surface-boundary shooting

Date: 2026-08-13  
Physics RFC: #413  
Depends on: #407  
Base main: `44621410e73b8caff04e5adfda48d63ad96556de`

## Purpose

The first surface-boundary shooting experiment showed that a bounded buoy-side vertical reaction `Q0` can close slack frozen-load geometries while respecting buoyancy capacity.

Before any production boundary solver exists, the discrete midpoint integration must be checked against an independent continuous reference.

This control mark defines an **exact analytical solution of the simplified constant distributed-load validation model**.

It is important to distinguish sources:

- Berteaux is the primary source for the physical boundary/equilibrium structure and signed cable statics;
- the closed-form integral below is a mathematical derivation of BuoyCalc's deliberately simplified **frozen-load validation model**;
- the closed-form expression is not attributed to Berteaux and does not replace Berteaux's angle-dependent cable-drag equations.

No production physics is changed by this document.

## Physical/source boundary from Berteaux

Chapter 2 §2.1 establishes that submerged line weight is signed (`P = W - B`), that cable statics are vector equilibrium, and that a surface buoy with unknown vertical tension/displacement is solved subject to another physical constraint by successive approximations.

The present analytical reference keeps only the already-defined Phase-A simplifications:

```text
- planar X/Z shape plane;
- inextensible line;
- constant frozen horizontal distributed load per arc length;
- constant signed vertical distributed water weight per arc length;
- no geometry feedback into drag;
- no wave term;
- no internal point load for the first analytical case.
```

Therefore this is a numerical/mathematical reference for the validation integrator, not yet a complete physical mooring solution.

## Continuous constant-load field

Let:

```text
s in [0, L]       arc coordinate from buoy toward anchor
H0                buoy-side horizontal cable component toward anchor
Q0                buoy-side downward vertical cable component
q_x > 0           frozen horizontal distributed drag per unit arc length
w                 signed downward water-weight force per unit arc length
```

For a heavy line:

```text
w > 0
```

For a buoyant line:

```text
w < 0
```

The top-to-bottom frozen tension-component field is:

```text
H(s) = H0 + q_x s
V(s) = Q0 - w s
```

Define vector notation:

```text
r(s) = [ H(s), V(s) ]
q    = [ q_x, -w ]
```

so:

```text
r(s) = r0 + q s
r0   = [H0, Q0]
```

The local validation tangent is:

```text
t(s) = r(s) / |r(s)|
```

when `|r(s)| > 0`.

Geometry follows:

```text
dR/ds = t(s)
R = [x,z]
```

and the endpoint displacement is:

```text
DeltaR = integral_0^L r(s)/|r(s)| ds
```

## Exact vector antiderivative

Let:

```text
Q = |q| = sqrt(q_x^2 + w^2)
```

For `Q > 0`, define an orthonormal basis aligned with `q`:

```text
e = q / Q
n = [-q_z, q_x] / Q = [w, q_x] / Q
```

Decompose the tension vector as:

```text
u(s) = dot(r(s), e)
c    = dot(r(s), n)
```

Because `r(s) = r0 + q s`:

```text
u(s) = u0 + Q s
c = constant
```

and:

```text
r(s) = e u(s) + n c
|r(s)| = sqrt(u(s)^2 + c^2)
```

For `c != 0`, direct integration gives:

```text
DeltaR =
    e * (R1 - R0) / Q
  + n * (c / Q) * [ asinh(u1/|c|) - asinh(u0/|c|) ]
```

where:

```text
u0 = u(0)
u1 = u(L)
R0 = sqrt(u0^2 + c^2) = |r(0)|
R1 = sqrt(u1^2 + c^2) = |r(L)|
```

The X and Z components of `DeltaR` are the exact continuous frozen-load endpoint:

```text
X_exact(Q0)
Z_exact(Q0)
```

No segment discretization appears in this expression.

## Collinear/degenerate limit

If:

```text
c = 0
```

then `r(s)` is collinear with `q`.

If `u(s)` never crosses zero, the tangent is constant up to sign and the exact integral reduces to a straight segment:

```text
DeltaR = e * sign(u) * L
```

If `u(s)` crosses zero inside `[0,L]`, then:

```text
|r(s*)| = 0
```

at the crossing and cable orientation/tension is indeterminate there.

A validation implementation must classify that case explicitly rather than manufacture a tangent.

## Zero distributed-load limit

If:

```text
Q = 0
```

then both distributed load components are zero and `r(s)=r0` is constant.

If `|r0| > 0`:

```text
DeltaR = L * r0 / |r0|
```

If `|r0| = 0`, orientation is indeterminate and geometry alone cannot define the cable direction.

## Exact shooting equation

For the surface-boundary validation problem, `Q0` remains bounded by buoyancy capacity:

```text
0 <= Q0 <= Q_capacity
```

The exact continuous depth closure for the constant-load case is:

```text
Z_exact(Q0) = target depth D
```

A deterministic bounded root search may still be used for `Q0`, but each function evaluation uses the exact continuous endpoint above rather than midpoint discretization.

This separates:

```text
root-search error
```

from:

```text
segment-integration error
```

## Reference uniform-current heavy-line case

The first shooting evidence used the deterministic slack case:

```text
Depth D = 50 m
Line length L = 55 m
H0 = buoy steady drag = 82 N
Total frozen line horizontal drag = 169.125 N
q_x = 169.125 / 55 = 3.075 N/m
Line WeightWater = 0.1 kg/m
w = 0.1 * 9.80665 = 0.980665 N/m
Q_capacity = 9071.15125 N
```

The earlier temporary 0.20 m midpoint shooting run stopped at its 0.01 m numerical depth criterion with:

```text
Q0_midpoint_loose = 405.2784860229491 N
X_midpoint_loose  = 21.919036791100037 m
Z_midpoint_loose  = 49.9963893072144 m
```

Those values are intentionally **not** the analytical reference because the root search stopped with several millimetres of depth residual.

Using the exact continuous integral and a tight bounded root solve gives the reference target for the next regression:

```text
Q0_exact ~= 405.43946352753 N
X_exact  ~= 21.91146859752 m
Z_exact  = 50 m
```

The next validation code must recompute these values independently in C# rather than hard-code only the numbers above.

## Midpoint mesh-convergence targets

When the midpoint method solves the same depth equation with a **tight root tolerance**, its result should converge to the exact continuous reference as segment length decreases.

Independent pre-check values for the deterministic constant-load case are approximately:

```text
nominal ds   Q0_midpoint (N)    X_midpoint (m)     |Q-Qexact| (N)      |X-Xexact| (m)
0.8          405.4364010032      21.9116605528       3.06e-3            1.92e-4
0.4          405.4386978997      21.9115165857       7.66e-4            4.80e-5
0.2          405.4392707262      21.9114806819       1.93e-4            1.21e-5
0.1          405.4394153272      21.9114716186       4.82e-5            3.02e-6
0.05         405.4394514774      21.9114693528       1.21e-5            7.55e-7
```

The approximately fourfold error reduction when halving `ds` is consistent with second-order midpoint integration for this smooth case.

These are validation targets only. They do not authorize changing production target segmentation from `0.20 m`.

## Required permanent validation

A focused validation-only regression should prove:

1. the C# exact integral matches direct high-accuracy numerical quadrature for representative non-degenerate inputs;
2. the exact depth root exists and lies inside `[0,Q_capacity]` for the deterministic slack heavy-line case;
3. the exact solution is near:

```text
Q0 = 405.4394635 N
X  = 21.9114686 m
Z  = 50 m
```

within a tight numerical tolerance justified for the analytical computation;
4. midpoint solutions at `ds = 0.4, 0.2, 0.1, 0.05 m` converge toward the exact result;
5. the 0.20 m midpoint error is recorded explicitly rather than hidden by the root tolerance;
6. halving mesh size produces the expected error trend for the smooth synthetic case;
7. collinear and zero-tension degeneracies are classified rather than divided by zero.

## Taut-limit proof independent of root tolerance

For an inextensible cable:

```text
|dz/ds| = |V| / sqrt(H^2 + V^2)
```

Whenever `H != 0` at finite tension:

```text
|dz/ds| < 1
```

Therefore, if a non-zero horizontal component exists over any interval of non-zero arc length:

```text
|Delta z| < L
```

Consequently a prescribed vertical span:

```text
D = L
```

cannot be reached by a finite-tension inextensible solution with non-zero horizontal loading over a finite interval.

This is an exact geometric statement and must take precedence over a numerical residual tolerance.

The next validation regression should encode this limit explicitly.

## Relation to Berteaux constant-current solutions

Berteaux's Chapter-2 constant-current equations retain angle-dependent normal/tangential hydrodynamic loads.

The present frozen model instead holds `q_x` and `w` constant by construction.

Therefore agreement with this exact integral validates **our numerical frozen-load integrator**, not the full Berteaux cable model.

A later source-overlap comparison must use a case where the assumptions are explicitly reconciled; it must not claim that the simplified constant `q_x` model is identical to Eqs. (2.23)–(2.28).

## Next allowed code package

Validation only:

```text
ConstantLoadAnalyticalReferenceRegression
```

or equivalent.

It may contain:

- the exact continuous integral;
- a tight bounded root solve;
- an independent adaptive numerical quadrature cross-check;
- midpoint mesh-convergence checks;
- exact taut-limit classification checks.

It must not be called by production calculation services.

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

The next gate is mathematical verification of the shooting integrator against the exact continuous constant-load solution.

Only after that regression is green should the project proceed to piecewise/point-load analytical validation and Berteaux-overlap reference checks.
