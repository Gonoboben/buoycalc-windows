# Control mark: Berteaux constitutive drag boundary

Date: 2026-08-13  
Physics RFCs: #413, #407  
Depends on: #426, #427  
Scope: documentation / primary-source validation only

## Purpose

The signed vector-equilibrium mapping is now source-backed and regression-verified.

The next question is narrower and different:

```text
Does the current BuoyCalc line-drag law reproduce the orientation-dependent
normal and tangential hydrodynamic resistance used by Berteaux?
```

This control mark records the answer before any production solver change.

No production physics is changed here.

## Primary source

Primary project source:

```text
Г. О. Берто, Океанографические буи,
Л.: Судостроение, 1979,
русское издание H. O. Berteaux, Buoy Engineering, 1976.
```

The formulas below were checked against the user-provided searchable scan, not reconstructed from a secondary source.

Relevant printed pages are 34–35, 45 and 55 in Chapter 2, §2.1.

## 1. Berteaux reference drag for an elementary cable element

For a cable element of diameter `d`, arc length `ds`, steady-current speed `V`, and angle `phi` between the cable axis and the current direction, Berteaux first defines the normal-incidence reference resistance:

```text
R ds = 1/2 * rho * C_n * d * V^2 * ds                (2.2)
```

The normal velocity component is:

```text
V_n = V sin(phi)
```

and the normal resistance per unit length becomes:

```text
D = R sin^2(phi)                                      (2.3)
```

Thus:

```text
phi = 0 deg   -> D = 0
phi = 90 deg  -> D = R
```

The tangential velocity component is:

```text
V_t = V cos(phi)
```

Berteaux defines a separate tangential coefficient:

```text
C_t = gamma * C_n
```

and obtains:

```text
F = pi * gamma * R * cos^2(phi)                       (2.5)
```

Therefore the source does not describe cable drag by one orientation-independent scalar force.

It explicitly separates:

```text
normal resistance     D(phi)
tangential resistance F(phi)
```

with different coefficient semantics and different directions relative to the cable.

## 2. Limiting cases fixed by the source

### Cable parallel to current

```text
phi = 0 deg
D = 0
F = pi * gamma * R
```

Normal drag vanishes. A tangential skin/friction contribution may remain.

### Cable normal to current

```text
phi = 90 deg
D = R
F = 0
```

This is the one limiting orientation where a conventional transverse drag law using area `d*ds` and coefficient `C_n` directly overlaps the Berteaux normal-resistance magnitude.

### Intermediate angle

For example:

```text
phi = 45 deg
D = 0.5 R
F = 0.5 pi gamma R
```

Both components are generally present.

These are mathematical/source limiting identities, not engineering acceptance thresholds.

## 3. Poud specialization

On printed p.45 Berteaux describes Poud's cable-function solution.

Poud assumes the tangential resistance `F` is constant and writes:

```text
dT = (P sin(phi) - F) ds                              (2.23)
T dphi = (R sin^2(phi) + P cos(phi)) ds               (2.24)
```

Berteaux later states explicitly that the Poud tables are applicable when the following assumptions are acceptable:

```text
- tangential hydrodynamic resistance is constant;
- current is unchanged;
- cable is absolutely rigid.
```

Therefore `constant F` is a named approximation, not the general constitutive law.

## 4. Wilson specialization

On printed p.55 Berteaux introduces Wilson's variable-tangential-resistance treatment.

The source gives:

```text
dT = (P sin(phi) - pi*gamma*R*cos^2(phi)) ds
T dphi = (R sin^2(phi) + P cos(phi)) ds
```

and states that Wilson's cable-function tables were obtained by numerical integration.

Berteaux specifically lists as an advantage of Wilson's tables that they reflect changes in the tangential resistance as cable inclination changes.

Therefore the Wilson model is orientation-dependent in both the normal and tangential hydrodynamic terms.

## 5. What the current base BuoyCalc segment model does

`Models/EngineeringModels.cs` currently builds each line segment using:

```text
localSpeed = horizontal current-speed magnitude
projectedArea = ds * diameter
CurrentForceN = 1/2 * rho * Cd * projectedArea * localSpeed^2
```

In current profile mode:

```text
localSpeed = sqrt(East^2 + North^2)
```

The segment tangent is not an input to this base `CurrentForceN` calculation.

Therefore the base segment force is orientation-independent.

This is a historical screening/load field. It is not equivalent to Berteaux `D(phi) + F(phi)`.

## 6. What MooringShapeForceAnalyzer already does correctly

`MooringShapeForceAnalyzer` is more advanced than the base segment field.

It reconstructs the local X/Z tangent from the candidate shape and forms a local current vector in that plane:

```text
u = (HorizontalSpeedMagnitude, VerticalCurrent)
t = (tx, tz)
```

It then calculates the velocity component normal to the cable:

```text
dot = u . t
V_n = sqrt(|u|^2 - dot^2)
```

and computes:

```text
ShapeForceN = 1/2 * rho * Cd * (d*ds) * V_n^2
```

Under the restricted assumptions:

```text
Cd == C_n
projected area == d*ds
steady local relative current
```

this is consistent with the **magnitude** of Berteaux normal resistance `D ds`.

This partial overlap should be preserved.

## 7. What is still missing from the current shape-force path

The current shape-force path does not yet reproduce the full Berteaux constitutive model.

### 7.1 Tangential resistance is absent

There is no separate term corresponding to:

```text
F = pi * gamma * R * cos^2(phi)
```

and the rope input currently exposes one scalar `DragCoefficient`, not a source-backed pair such as:

```text
C_n
C_t or gamma
```

A future implementation must not silently reuse one `Cd` as both coefficients.

### 7.2 Force direction is not preserved

`MooringShapeForceAnalyzer` publishes `ShapeForceN` as a scalar magnitude.

`MooringShapeTensionAnalyzer` then accumulates that entire scalar as:

```text
cumulativeShapeHorizontalForceN += ShapeSegmentForceN
```

while submerged weight is accumulated vertically.

Thus the normal hydrodynamic force is not resolved into its actual planar X/Z vector direction normal to the cable.

This is a constitutive/vector gap even when the magnitude `V_n^2` is correct.

### 7.3 Vertical-current effect is only partially represented

`MooringShapeForceAnalyzer` includes `VerticalCurrentMS` when calculating `V_n`.

But the resulting scalar normal force is subsequently placed entirely into the horizontal ledger.

Therefore a vertical-current contribution can alter the force magnitude without the corresponding force vector being resolved consistently in X/Z.

### 7.4 Horizontal direction is a magnitude

The local shape analyzer uses:

```text
currentX = LocalSpeedMS
```

where the base segment `LocalSpeedMS` is the non-negative horizontal speed magnitude.

This is compatible with a deliberately chosen single 2D current plane only while direction reversal/azimuth variation is outside the model.

A later planar coupled solver needs an explicit signed projection of the environmental current onto its chosen X/Z plane. It must not introduce 3D implicitly.

## 8. Exact overlap and exact non-overlap

### Source-backed overlap already achieved

```text
[x] signed submerged load P = W - B;
[x] elementary vector equilibrium;
[x] top-to-bottom project signs dH/ds = +qx, dV/ds = -w;
[x] point-load vector jump;
[x] normal-speed magnitude relative to local cable tangent is already computed in MooringShapeForceAnalyzer;
[x] normal drag magnitude has the same quadratic V_n structure as Berteaux when Cd is interpreted as C_n.
```

### Not yet source-validated/implemented as Berteaux hydrodynamics

```text
[ ] separate tangential resistance F;
[ ] separate normal/tangential coefficient semantics;
[ ] planar vector direction of normal drag;
[ ] planar vector direction of tangential drag;
[ ] signed local-current projection when current direction varies/reverses;
[ ] coupled re-evaluation of drag after geometry changes;
[ ] final convergence policy for the coupled solver.
```

## 9. Required validation-only next package

Before any production drag change, add a pure validation regression for the constitutive boundary.

Suggested name:

```text
BerteauxConstitutiveDragBoundaryRegression
```

It should not call or modify the production solver.

Required deterministic identities:

### Normal term

Normalize by `R` and verify:

```text
phi = 0 deg   -> D/R = 0
phi = 45 deg  -> D/R = 0.5
phi = 90 deg  -> D/R = 1
```

### Tangential term

Normalize by `pi*gamma*R` and verify:

```text
phi = 0 deg   -> F/(pi gamma R) = 1
phi = 45 deg  -> F/(pi gamma R) = 0.5
phi = 90 deg  -> F/(pi gamma R) = 0
```

### Current frozen-base contrast

For fixed `rho, V, d, ds, Cd`, the current base segment law is independent of `phi`.

The validation should demonstrate explicitly that:

```text
BaseCurrentForce(phi=0) == BaseCurrentForce(phi=90)
```

while source normal resistance satisfies:

```text
D(phi=0) != D(phi=90)
```

This is evidence of model scope, not a failing production test.

### Existing shape normal-magnitude overlap

For a synthetic steady 2D current and tangent, verify that the existing normal-speed construction gives:

```text
V_n = V sin(phi)
```

and therefore the existing shape-force magnitude equals the source `D ds` under the explicit identification `Cd=C_n`.

Do not add a tangential production term in this validation package.

## 10. Production change remains blocked

This control mark does not authorize changes to:

```text
BuoyCalculator / base CurrentForceN
MooringShapeForceAnalyzer production equations
MooringShapeTensionAnalyzer production equations
MooringDiscreteLoadTensionAnalyzer
MooringShapeSolver
MooringDiscreteLoadShapeBuilder
MooringIterativeSolver
MooringPrimaryShapeGate
CalculationResult.Verdict
selected X/Z
anchor / weak-link calculations
0.20 m target segmentation
unlimited segment count
signed WeightWaterKg
PDF / 2D physics
JSON / DTO
committed golden baseline
3D
```

## 11. What a later production RFC must decide

Before orientation-aware cable drag becomes production physics, a later RFC must define at minimum:

```text
1. authoritative planar current vector;
2. authoritative signed cable tangent;
3. normal relative-current vector and coefficient C_n;
4. tangential relative-current vector and coefficient C_t or gamma;
5. force-vector reconstruction in X/Z;
6. rope/library data requirements for coefficients;
7. Reynolds-number/coefficient policy;
8. interaction with vertical current;
9. point-load drag treatment;
10. geometry -> drag -> boundary-Q0 -> geometry coupling;
11. under-relaxation and convergence evidence;
12. historical/golden impact review.
```

This remains planar 2D X/Z work. No 3D is introduced.

## Decision

The current BuoyCalc model has a **partial Berteaux overlap**:

```text
MooringShapeForceAnalyzer correctly derives an orientation-dependent
normal-speed magnitude and therefore can reproduce the magnitude form
of Berteaux normal drag D under restricted coefficient assumptions.
```

But the current model is not yet a complete Berteaux cable-hydrodynamic model because:

```text
- tangential resistance is absent;
- hydrodynamic force direction is collapsed to a horizontal scalar;
- coefficient semantics are not separated;
- the coupled geometry/drag/boundary solve is not yet validated.
```

The next allowed step is validation-only constitutive limiting-case regression.
