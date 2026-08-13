# Control mark: Berteaux vector-equilibrium overlap for the Phase-A frozen-load field

Date: 2026-08-13  
Physics RFCs: #413, #407  
Base main: `4d731032a21e18a647ec3e35e5c7aeee53509e93`

## Purpose

The surface-boundary validation has now passed two independent numerical gates:

1. an exact continuous reference for one smooth constant frozen distributed-load field (#419/#420);
2. an exact piecewise reference with grouped connector/payload point loads (#422/#423).

Before any production X/Z proposal, this document answers the next source question:

> Is the signed vector ODE used by the Phase-A frozen-load reference compatible with the elementary static equilibrium written by Berteaux, or is it merely a convenient unrelated construction?

The answer is:

```text
The vector equilibrium and sign structure overlap exactly.
The hydrodynamic constitutive law does not yet overlap.
```

This distinction is the boundary of the present validation.

No production code is changed by this document.

## Primary-source record

The project already contains a source-validation control mark created directly from the user-supplied OCR-searchable Russian Berteaux edition:

`docs/CONTROL_MARK_BERTEAUX_SIGNED_NODE_SOURCE_2026-08-13.md`

That primary-source record establishes the following.

### Signed submerged load

Berteaux, printed p. 34:

```text
P = W - B                                      (2.1)
```

so `P` remains signed.

### Elementary cable equilibrium

Berteaux, printed p. 38:

```text
T dφ = (D + P cos φ) ds                        (2.6)

dT   = (P sin φ - F) ds                        (2.7)
```

These are the normal/tangential component equations for the elementary cable free body.

### Vector end tensions

The stronger sign anchor for this control mark is the spatial/vector formulation on printed pp. 76–77.

For a unit tangent `u`, Berteaux explicitly assigns the end tension forces on an elementary cable piece as:

```text
upper/first end:   -T u
lower/second end:  (T + dT)(u + du)
```

The same source keeps submerged weight signed in the spatial formulation.

This explicit end-force orientation is sufficient to derive the project-space vector balance without guessing arrows from a figure or reconstructing Poud/Wilson signs from memory.

## Generic vector balance of an elementary segment

Define the internal tension vector following the cable coordinate `s` as:

```text
C(s) = T(s) t(s)
```

where `t` is the unit tangent in the direction of increasing `s`.

For an elementary piece from `s` to `s+ds`, the two end forces are:

```text
-C(s)
+C(s+ds)
```

Let `f_ext(s)` be the external force per unit arc length expressed in one common global coordinate system.

Static equilibrium gives:

```text
-C(s) + C(s+ds) + f_ext(s) ds = 0
```

or, in the differential limit:

```text
dC/ds = -f_ext                              (A)
```

Equation (A) is not introduced as a new physical law. It is the direct vector form of the Berteaux elementary free-body statement already source-validated above.

The scalar equations (2.6)–(2.7) are component forms of the same elementary equilibrium after resolving the external load into local tangent/normal directions and choosing Berteaux's angle convention.

For the present overlap check, no additional scalar-angle transformation is required.

## BuoyCalc shape-plane convention

The surface-boundary RFC already fixes the local validation plane as:

```text
s = 0 at buoy, increasing toward anchor
+Z = downward
+X_shape = from buoy toward anchor
```

For the steady-current cases used in the current validation:

```text
+X_shape is opposite the environmental drag direction.
```

This fact is essential for the horizontal sign.

The existing production `XOffsetM` remains an offset magnitude. This local validation axis must not be reinterpreted as a geographic signed East/North coordinate.

## Frozen distributed load in project coordinates

For one Phase-A interval define:

```text
q_x >= 0   steady horizontal drag magnitude per unit line length
w          signed submerged weight force per unit line length
```

with:

```text
w > 0   heavy in water
w = 0   neutral
w < 0   buoyant in water
```

Because drag acts opposite `+X_shape`, while signed weight acts along `+Z` when positive, the external load vector is:

```text
f_ext = (-q_x, +w)
```

Substituting this into the source-backed vector balance (A):

```text
dC/ds = -f_ext
       = (+q_x, -w)
```

Write:

```text
C = (H, V)
```

Then:

```text
dH/ds = +q_x                              (B1)

dV/ds = -w                                (B2)
```

These are exactly the force-state equations used by the Phase-A surface-boundary validation and by the exact constant-load / piecewise references.

Therefore the signs in the current validation field are not arbitrary numerical conventions.

They follow directly from:

1. the Berteaux end-tension free body;
2. the project choice that `+X_shape` points opposite environmental drag;
3. the project choice `+Z` downward;
4. signed submerged weight.

## Geometry relation

The same vector state is collinear with the cable tangent:

```text
C = T t
T = |C| = hypot(H,V)
```

For non-degenerate `T`:

```text
t = C / |C|

tx = H / hypot(H,V)
tz = V / hypot(H,V)
```

and the inextensible geometric relation is:

```text
dr/ds = t
```

or:

```text
dx/ds = H / hypot(H,V)

dz/ds = V / hypot(H,V)
```

This is the vector form used by the exact continuous reference in #420.

No absolute value is permitted on `V`.

A sign change in `V` therefore changes the sign of `dz/ds` and represents a real local vertical-direction change in the validation geometry.

## Concentrated point-load overlap

The Berteaux OCR source record used for Candidate B validates the elementary vector free-body structure. BuoyCalc additionally has an explicit project ownership convention for zero-length connector/payload point loads.

Integrate the same vector equilibrium across a zero-arc-length point with concentrated external force `F_point_ext`:

```text
C_after - C_before + F_point_ext = 0
```

In the project plane, a grouped point load has external force:

```text
F_point_ext = (-F_point_x, +W_point_water*g)
```

because:

- steady drag is opposite `+X_shape`;
- signed positive submerged weight is downward.

Therefore:

```text
H_after - H_before = +F_point_x

V_after - V_before = -W_point_water*g
```

or:

```text
H_after = H_before + F_point_x
V_after = V_before - W_point_water*g
```

This is exactly the jump rule already regression-verified in #423.

The zero-length point contributes no direct geometric displacement:

```text
DeltaX_point = 0
DeltaZ_point = 0
```

The same-s grouping rule remains a BuoyCalc discretization/ownership convention: co-located connector/payload source rows are summed before one mechanical jump is applied.

## Heavy and buoyant sign check

### Heavy distributed line

For:

```text
w > 0
```

(B2) gives:

```text
dV/ds < 0
```

so the downward cable-tension component decreases as the heavy distributed load is crossed from top to bottom.

### Buoyant distributed line

For:

```text
w < 0
```

(B2) gives:

```text
dV/ds > 0
```

so the downward cable-tension component increases as a buoyant distributed section is crossed.

This is the signed behavior already protected by the surface-boundary and point-load validation.

No `Abs(WeightWaterKg)` or `Abs(V)` is compatible with this overlap.

## What exactly overlaps with Berteaux

The following elements now have a direct source-backed correspondence:

```text
[x] signed submerged load;
[x] elementary static free-body structure;
[x] opposite end-tension signs;
[x] vector tension state C = T t;
[x] top-to-bottom differential force balance dC/ds = -f_ext;
[x] preservation of vertical sign;
[x] geometry tangent collinear with tension vector;
[x] concentrated point-load jump as the integrated vector balance.
```

This closes the **equilibrium/sign/vector** part of the first Berteaux-overlap gate.

## What does NOT yet overlap

The current exact reference deliberately prescribes a frozen global horizontal distributed load:

```text
q_x = constant, or piecewise constant in validation cases
```

Berteaux instead resolves hydrodynamic resistance into local cable-normal and cable-tangential components:

```text
D ds
F ds
```

whose values depend on cable orientation and the adopted hydrodynamic resistance law.

The project source record includes, for example, the normal relation:

```text
D = R sin^2(phi)                              (2.3)
```

with tangential resistance handled separately.

Therefore the following have **not** been validated by #420/#423 or by this document:

```text
[ ] Berteaux angle-dependent D/F constitutive drag law;
[ ] coupled update of hydrodynamic load when geometry changes;
[ ] Poud or Wilson cable-function solution equivalence;
[ ] dynamic / wave equations;
[ ] seabed / anchor support reaction;
[ ] engineering convergence threshold for a coupled solver.
```

The frozen global-load exact solution is thus a mathematically valid special external-load field inside the generic vector equilibrium, but it is **not** claimed to reproduce the complete Berteaux hydrodynamic cable model.

## Why this distinction matters

A numerical method can satisfy the correct vector force balance while still use an incomplete constitutive load model.

Accordingly, the project must keep two validation questions separate:

```text
Question 1: are force signs, boundary reactions, point jumps and geometry integration internally correct?
Status: increasingly well validated; this overlap confirms the source mapping.

Question 2: are q_x / D / F themselves the correct orientation-dependent hydrodynamic loads for the current cable geometry?
Status: not yet validated for the future coupled solver.
```

Production X/Z must not be switched merely because Question 1 is green.

## Next validation package

The next allowed package is validation-only and should test the vector-overlap identities directly, without introducing a production solver.

Suggested focused regression:

```text
BerteauxVectorOverlapRegression
```

It should verify:

### 1. Distributed element residual

For arbitrary non-degenerate `(H,V)`, signed `(q_x,w)` and small finite `ds`, construct:

```text
C0 = (H,V)
C1 = C0 + (q_x,-w) ds
f_ext ds = (-q_x,+w) ds
```

and prove:

```text
-C0 + C1 + f_ext ds = (0,0)
```

to synthetic floating-point tolerance.

### 2. Point jump residual

For heavy and buoyant point loads prove:

```text
-C_before + C_after + F_point_ext = (0,0)
```

with signed vertical force preserved.

### 3. Tangent collinearity

For all non-degenerate states:

```text
t = C/|C|
```

must be unit length and parallel to `C` without quadrant loss.

### 4. Coordinate-axis sign test

Explicitly verify that reversing from environmental drag direction to local `+X_shape` changes the external horizontal load sign but not its magnitude.

This protects the reasoning that makes (B1) positive.

### 5. Existing exact references remain unchanged

The new overlap regression must not change the established constant-load or piecewise exact target values and must not touch the product golden baseline.

## Later hydrodynamic overlap

Only after the generic vector overlap is regression-protected should #413/#407 proceed to a constitutive overlap using exact OCR-supported Berteaux assumptions.

That later step must retrieve and explicitly document the relevant Poud/Wilson `D/F` assumptions and angle definitions from the user-supplied searchable Berteaux source.

Do not reconstruct those laws from memory while OCR retrieval is unavailable.

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

The current Phase-A frozen-load field is now source-compatible at the level of **signed vector statics**:

```text
d(T t)/ds = -f_ext
```

and, under the already fixed local shape-plane orientation:

```text
dH/ds = +q_x
dV/ds = -w.
```

The remaining Berteaux gap is the **constitutive hydrodynamic law**, not the elementary vector equilibrium or force-sign mapping.
