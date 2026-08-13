# Control mark: boundary-inclusive planar tension source mapping

Date: 2026-08-13  
Issue: #407  
Base main: `011e89b04a2d412c78448db8316088a0a048c814`

## Purpose

This control mark defines the source/code ownership boundary required before any future signed planar tension field may drive X/Z geometry.

It follows the Phase-B measurement recorded in:

`docs/CONTROL_MARK_SIGNED_GEOMETRY_BOUNDARY_MEASUREMENTS_2026-08-13.md`

That measurement proved both:

1. historical absolute-angle handling loses the vertical quadrant;
2. normalized distributed-load H/V alone does not satisfy the anchored endpoint problem.

The missing state is a **boundary-conditioned tension field**.

This document is source mapping only. It does not authorize a production solver change.

---

## Primary source

Primary project source:

> H. O. Berteaux / Г. О. Берто, *Buoy Engineering / Океанографические буи*, 1979, Part II, Chapter 2, §2.1.

### Signed cable load

Printed p. 34:

```text
P = W - B                                      (2.1)
```

`P` may act downward or upward. It is signed.

### Element equilibrium

Printed pp. 37–38:

```text
T dφ = (D + P cosφ) ds                        (2.6)

dT = (P sinφ - F) ds                          (2.7)
```

The sign of submerged weight/buoyancy remains part of equilibrium.

### Variable-current step approximation

Printed p. 57 states that for each current step:

- gravitational force is evaluated;
- cable drag is evaluated;
- the resultant tension vector is determined at the nodes;
- geometry is then approximated by a stepped line following the tension vectors.

Eqs. (2.34)–(2.35) include **buoy-side boundary terms** together with accumulated cable loads before deriving tension magnitude and angle.

This is the critical source distinction from the current BuoyCalc line-only cumulative state.

### Surface-buoy equilibrium

Printed p. 58 gives the surface-buoy force balance:

```text
B_b = W + T_vert - R_vert                    (2.36)

D_b = T_hor                                  (2.37)
```

where the surrounding text identifies:

- `B_b` as buoyancy / weight of displaced water;
- `W` as buoy weight;
- `T_vert` as vertical cable-tension component;
- `R_vert` as vertical hydrodynamic component acting on the buoy;
- `D_b` as buoy drag;
- `T_hor` as horizontal cable-tension component.

### Surface versus submerged buoy

Printed p. 59 is explicit that when the vertical tension component of a **surface buoy** is unknown, other physical characteristics such as allowed horizontal excursion or cable length must be specified and the problem is solved by successive approximations.

The same source separately states that the buoyancy of a **subsurface/submerged buoy is constant**.

Therefore:

```text
surface-buoy actual B_b
```

and

```text
full-volume buoyancy capacity rho*V*g
```

must not silently be treated as identical unless full immersion/prescribed displacement is an explicit model assumption.

---

## Current BuoyCalc force ownership

### BuoyCalculator steady-current terms

`Models/EngineeringModels.cs` currently computes:

```text
buoyancyKg = buoy.VolumeM3 * waterDensityKgM3
```

and separately:

```text
buoyCurrentForce = DragForce(... buoy ...)
lineCurrentForce
connectorCurrentForce
payloadCurrentForce
```

then:

```text
CurrentForceN =
    buoyCurrentForce
  + lineCurrentForce
  + connectorCurrentForce
  + payloadCurrentForce
```

This `CurrentForceN` is the steady-current horizontal aggregate.

### Wave term is separate

The same calculator computes a buoy wave-drag proxy separately:

```text
WaveForceN
```

and only then forms:

```text
HorizontalForceN = CurrentForceN + WaveForceN
```

Berteaux Chapter-2 static-current boundary mapping must therefore use the steady-current family, not silently include `WaveForceN`.

Wave loading remains a separate physical load family.

### Current buoyancy semantics

The current calculator uses the full input volume directly:

```text
BuoyancyKg = rho * VolumeM3
```

This is an available/full-volume displacement quantity in the existing screening model.

The current input has no explicit:

```text
surface / submerged mode
waterline
actual immersed volume
freeboard
```

Therefore `CalculationResult.BuoyancyKg` is **not yet proven to be actual equilibrium `B_b` for a surface buoy**.

For a submerged/prescribed-displacement case, `rho*V` can be treated as known under the explicit full-submergence assumption.

For a surface/free-displacement case, actual `B_b` remains an equilibrium unknown or externally constrained value.

### ElementRows

The buoy `ElementCalculationRow` currently publishes:

```text
WeightWaterKg = buoy.WeightKg - buoyancyKg
CurrentForceN = buoyCurrentForceN
```

This is useful provenance, but `Kind = "Буй"` is localized text.

A future physics service must not deepen dependency on localized display strings merely to discover the top boundary.

### SegmentRows

`SegmentCalculationRow` contains the distributed line family:

```text
SegmentLengthM
CurrentForceN
WeightWaterKg
```

These are the authoritative existing distributed line loads for the current preliminary model.

When segment rows are used, aggregate line `ElementRows.CurrentForceN` / `WeightWaterKg` must not be added again.

### SequencePositioner point loads

`MooringSequencePositioner` positions elements along `s` and publishes aggregate point-load ownership:

```text
DiscreteWeightWaterKg
DiscreteCurrentForceN
```

Its discrete aggregate explicitly excludes buoy and anchor and therefore represents connector/payload point loads only.

Those loads must enter a boundary-conditioned ledger exactly once at their `s` positions.

### Anchor is a boundary/support, not a cable distributed load

The anchor element has its own weight/holding model.

For cable statics:

```text
anchor != distributed cable load
anchor != internal point load to be accumulated through the line
```

The line delivers a terminal tension/resultant to the lower boundary. Anchor and seabed equilibrium then react that load in a separate support problem.

---

## Why the current SegmentTensionAnalyzer cannot be the boundary-conditioned field

`SegmentTensionAnalyzer` starts from:

```text
H = 0
V = 0
```

at the lower end and accumulates only segment distributed loads bottom-to-top.

It does not include:

```text
buoy steady drag boundary term
surface/submerged buoy vertical boundary state
connector/payload point loads
anchor boundary reaction
```

Its signed H/V are therefore a useful **distributed-load cumulative ledger**, but not a solved cable-tension vector for the anchored system.

The Phase-B experiment demonstrated the consequence numerically.

---

## Free-body convention for the next validation ledger

The next validation step should avoid angle conventions entirely.

Use a top-subsystem free body in the existing planar design plane.

### Coordinates

For source mapping only:

```text
+X = direction of steady horizontal environmental load in the chosen X/Z plane
+Z = downward
s  = 0 at buoy/top and increases toward anchor/bottom
```

The current solver treats horizontal load as a scalar magnitude, so this is a collinear planar convention. It does not add 3D or restore East/North azimuth variation.

### Upper-subsystem external force

At a cut at coordinate `s`, define the total external force acting on everything **above the cut**:

```text
Fext(s) = (Fx_ext(s), Fz_ext(s))
```

with:

```text
Fx_ext > 0  for current drag in +X
Fz_ext > 0  for downward signed water weight
Fz_ext < 0  for net upward buoyancy
```

At the lower boundary of that upper subsystem, the remaining cable pulls the subsystem along the local top-to-bottom cable direction.

Static equilibrium requires:

```text
T_cut_vector(s) + Fext(s) = 0
```

therefore:

```text
T_cut_vector(s) = -Fext(s)
```

This is the vector to validate before any conversion to geometry.

### Top boundary: horizontal component

For steady-current Chapter-2 statics:

```text
Fx_ext(0+) = D_b
```

where `D_b` is the steady buoy drag.

In the current calculation family this corresponds to `buoyCurrentForceN`, not total `HorizontalForceN` with wave added.

### Top boundary: vertical component

For a prescribed-displacement/submerged case with no modeled vertical hydrodynamic lift:

```text
Fz_ext(0+) = (W_b - B_b) g
```

and the cut-tension vector becomes:

```text
Tz_cut(0+) = (B_b - W_b) g
```

For the current surface-buoy model, however, actual equilibrium `B_b` is not explicitly known. Therefore this vertical boundary term must remain:

```text
UNRESOLVED_FOR_SURFACE_MODE
```

until actual displacement or an equivalent boundary unknown is represented and solved.

---

## Load accumulation from top to bottom

After the top boundary is defined, the external-force ledger crosses loads exactly once.

### Distributed segment contribution

For each segment increment:

```text
Fx_ext += Segment.CurrentForceN
Fz_ext += Segment.WeightWaterKg * g
```

`WeightWaterKg` remains signed.

### Discrete point-load contribution

When crossing a connector/payload point at its `s`:

```text
Fx_ext += PointLoad.CurrentForceN
Fz_ext += PointLoad.WeightWaterKg * g
```

Buoy and anchor are not included in this point-load family.

### Terminal conservation identities

Under the **current full-volume screening semantics** and steady-current-only load family, a complete ownership ledger can be checked algebraically at the bottom.

Horizontal:

```text
Fx_ext(bottom-) = CalculationResult.CurrentForceN
```

Vertical, if the current full-volume buoyancy capacity is temporarily treated as prescribed displacement for validation:

```text
Fz_ext(bottom-)
    = -CalculationResult.NetBuoyancyKg * g
```

therefore the required terminal cable-tension vector acting on the upper subsystem would be:

```text
T_cut_x(bottom-) = -CalculationResult.CurrentForceN
T_cut_z(bottom-) = +CalculationResult.NetBuoyancyKg * g
```

These are **conservation identities of the current load ownership**, not proof that the surface-buoy boundary assumption is physically solved.

---

## Boundary contribution can be reconstructed without localized strings for validation

A validation-only ownership check can recover the existing current-model buoy contribution by subtraction.

Horizontal:

```text
BuoySteadyDragN =
    result.CurrentForceN
    - sum(result.SegmentRows.CurrentForceN)
    - sequencePositions.DiscreteCurrentForceN
```

Vertical signed buoy contribution under current full-volume screening semantics:

```text
BuoySignedWeightWaterKg =
    -result.NetBuoyancyKg
    - sum(result.SegmentRows.WeightWaterKg)
    - sequencePositions.DiscreteWeightWaterKg
```

This avoids using `ElementRow.Kind == "Буй"` in a new physics diagnostic.

The subtraction is acceptable only as a **conservation/ownership reconstruction**. It must be documented as such and compared with the existing buoy element row during validation.

A future production boundary model should preferably receive typed buoy-boundary data directly from the calculation core rather than infer it from localized presentation rows.

---

## Surface versus submerged scope must be explicit

A future boundary-tension result must carry a boundary state classification such as conceptually:

```text
PrescribedDisplacement
SurfaceDisplacementUnresolved
```

The names are illustrative; no DTO/API change is authorized here.

### Prescribed-displacement / submerged

Allowed source assumption:

```text
B_b = rho * V * g
```

when full immersion is explicit.

This is suitable for deterministic validation of the boundary ledger.

### Surface / free displacement

Do not use:

```text
B_b = rho * full Volume * g
```

as actual equilibrium force merely because the capacity is available.

The actual displacement must be solved or constrained together with line geometry/tension, consistent with Berteaux's successive-approximation discussion.

---

## Existing wave force is outside this static boundary ledger

The current project computes a horizontal wave-drag proxy on the buoy.

For this Chapter-2 static source mapping:

```text
WaveForceN is excluded
```

from the boundary-conditioned steady-current tension ledger.

This does **not** remove wave loading from the application or engineering checks. It only prevents a time-varying load family from being silently folded into a static-current tension derivation.

A later wave/dynamic validation must have its own source and assumptions.

---

## Horizontal direction limitation remains planar

`SegmentCalculationRow` retains East/North current components, but its `CurrentForceN` is a non-negative scalar based on horizontal speed magnitude.

Therefore the present planar static ledger can only mean:

```text
all horizontal drag projected into one chosen design X direction
```

It does not solve changing current azimuth with depth.

This is an existing 2D limitation, not a reason to add 3D.

No 3D work is authorized.

---

## Semantic correction to Phase-A SignedOrientation

Phase-A fields named:

```text
TangentX
TangentZ
```

currently normalize the distributed-load cumulative H/V state.

After the boundary study, these values must be interpreted only as:

```text
normalized signed ledger-resultant diagnostics
```

They are **not yet authoritative cable tangents**.

Before any geometry consumer is allowed to use them, either:

- their API names should be clarified, or
- an explicitly boundary-conditioned tension/tangent type should be introduced separately.

Do not wire the existing Phase-A result directly into a shape builder.

---

## Next allowed implementation: validation-only boundary load ledger

The next code package may be validation-only and must not change production selection.

It should:

1. reconstruct the buoy steady-current horizontal contribution by ownership residual;
2. reconstruct the current full-volume signed buoy vertical contribution by ownership residual;
3. accumulate segment distributed loads top-to-bottom;
4. accumulate connector/payload point loads exactly once;
5. verify terminal horizontal and vertical conservation identities;
6. publish/print cut force vectors only;
7. not build X/Z geometry yet.

Required validation cases:

```text
A. zero current, heavy line
B. uniform current, heavy line
C. negative WeightWaterKg line
D. connector/payload point load
E. depth-varying current magnitude
F. zero/near-zero boundary resultant
```

For surface-buoy interpretation, results must be labeled as current-capacity-model ownership evidence, not solved actual displacement.

---

## Not yet allowed

Do not yet:

```text
change MooringShapeSolver
change MooringDiscreteLoadShapeBuilder
change MooringIterativeSolver
change production MaxIterations
change MooringPrimaryShapeGate
change CalculationResult.Verdict
change selected X/Z
replace historical angles
use SignedOrientation as production geometry
change anchor/weak-link behavior
change 0.20 m target segmentation
limit segment count
change golden baseline
change PDF/2D physics
add 3D
```

---

## Decision

The next physical boundary is now clear:

```text
loads alone -> insufficient
signed loads alone -> insufficient
boundary-conditioned tension vector -> required before geometry
```

For a surface buoy, actual vertical boundary displacement/buoyancy remains unresolved in the present input model.

Therefore the next step is a **validation-only boundary load ledger**, not a production solver change and not an iteration-count adjustment.
