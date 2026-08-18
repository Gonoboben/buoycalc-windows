# Control mark: vertical limiting force-state boundary — 2026-08-18

Issue: #487 — Physics RFC: resolve signed-geometry production blockers before authority switch.

This document defines the validation boundary for the exact `vertical-zero-current` historical fixture after Package A (#488). It does **not** change production behavior.

## Source anchor

Primary project source: H. O. Berteaux / Г. О. Берто, *Океанографические буи*, 1979.

The source treats mooring-line tension as a force state that changes along the line with the submerged weight and buoyancy of line components. In the Russian edition:

- equation (2.48) relates tension at the buoy attachment to the lower-line tension plus submerged system weight;
- equation (5.2) explicitly forms line tension from submerged weights and component buoyancy.

These source relations support preserving the sign of submerged line weight in vertical equilibrium. They do not supply an arbitrary unique surface reaction for a geometry that does not constrain it.

## Existing project sign convention

```text
+Z = downward
s  = line coordinate from buoy/top toward anchor/bottom
Q0 = downward vertical cable-tension component at the buoy side
```

The shared surface-boundary integration kernel already propagates

```text
V(s) = Q0 - Wcum(s)
```

where `Wcum(s)` is the cumulative **signed** submerged weight force of crossed distributed and discrete line components.

For a zero-horizontal-force state:

```text
H(s) = 0
T(s) = |V(s)|
TangentZ = sign(V) when T > 0
```

No absolute-value conversion of `V` may be used to manufacture a downward tangent.

## Exact historical fixture

The committed `vertical-zero-current` fixture is:

```text
water density = 1025 kg/m^3
target depth  = 50 m
line length   = 50 m
steady current = 0 m/s
wave = 0
line signed WeightWater = +0.1 kg/m
point loads = 0
buoy volume = 1.0 m^3
buoy dry mass = 100 kg
```

Package A proved the geometry is uniquely the straight vertical limiting geometry because `L = depth` and horizontal loading is zero.

## Exact vertical force bounds for this fixture

The line submerged-weight force is

```text
W_line = 50 m * 0.1 kg/m * 9.80665 m/s^2
       = 49.03325 N
```

The current surface-buoy model contains a full-volume buoyancy capacity but no draft/waterplane constitutive relation:

```text
B_max      = rho * Volume * g
Q_capacity = B_max - W_buoy
           = (1025 kg - 100 kg) * 9.80665 m/s^2
           = 9071.15125 N
```

Therefore the exact fixture has a very large capacity interval above the line-weight requirement.

## General vertical limiting condition

Define cumulative signed submerged weight from top to line coordinate `s`:

```text
Wcum(s)
```

and

```text
Q_required = max(0, max_s Wcum(s))
```

Then for `H = 0`, `L = depth`:

- `Q0 < Q_required`: some cable section must reach negative `V`; a monotone straight-down tensile state is not admissible;
- `Q0 = Q_required`: limiting non-compressive state; at least one cross-section reaches `V = 0` where `Wcum` attains its maximum;
- `Q0 > Q_required`: strictly positive vertical tension at every cross-section represented by the cumulative model;
- any admissible `Q0` must also satisfy `Q0 <= Q_capacity`.

A zero-tension **interior** cross-section is direction-indeterminate and cannot be silently assigned a tangent. If the maximum cumulative weight occurs only at the bottom endpoint, `Q0 = Q_required` is a limiting hanging-line state with zero anchor-end cable tension; the finite line segments above it remain straight downward. This endpoint limit must be distinguished from strict positive-tension operation.

## Exact fixture force-state result

For `vertical-zero-current`, cumulative weight increases monotonically to the anchor and there are no point loads. Hence:

```text
Q_required = 49.03325 N
Q_capacity = 9071.15125 N
```

The straight vertical geometry is compatible with a **family** of surface reactions:

```text
limiting non-compressive: Q0 = 49.03325 N
strictly tensile:         49.03325 N < Q0 <= 9071.15125 N
```

Every strictly tensile Q0 in that interval gives the same ideal inextensible geometry:

```text
X = 0
Z = 50 m
```

Consequently, target-depth closure cannot select a unique `Q0` for this fixture.

## Why Q0 is not unique in the current product model

`BuoyInput` currently provides:

```text
VolumeM3
WeightKg
ProjectedAreaM2
DragCoefficient
```

The calculation derives maximum/full-volume buoyancy from `rho * VolumeM3`, but there is no buoy waterplane, draft, immersion curve, hydrostatic stiffness or other relation that maps a particular vertical reaction to a unique actual submerged volume.

Surface vertical equilibrium can state

```text
B_actual = W_buoy + Q0
```

subject to `B_actual <= B_max`, but the current input model does not select a unique `B_actual` inside that range for the zero-horizontal taut limiting case.

Therefore a unique Q0 must **not** be invented from geometry, from the historical selected shape, or from presentation code.

## Required semantic separation

Package B must distinguish at least these concepts:

```text
GeometryUnique
ForceStateUnique
ForceStateFamily
CapacityInsufficient
InteriorZeroTensionIndeterminate
EndpointZeroTensionLimit
```

For the exact historical fixture the expected validation conclusion is:

```text
GeometryUnique = true
ForceStateUnique = false
ForceStateFamily = [Q_required, Q_capacity]
StrictlyTensileFamily = (Q_required, Q_capacity]
CapacityInsufficient = false
```

The current production enum value `VerticalGeometryBoundaryNonUnique` is therefore semantically too coarse: what is non-unique here is the available force/reaction state, not the inextensible geometry.

This document does not rename or change that production classification.

## Controlled cases required in validation

The next validation package must cover:

1. heavy vertical line, `Q_capacity > Q_required`: unique geometry, non-unique force family;
2. heavy vertical line, `Q_capacity < Q_required`: capacity insufficient;
3. exact equality `Q_capacity = Q_required`: limiting endpoint-zero-tension state when the cumulative maximum is only at the anchor;
4. internal discrete positive weight that makes `Wcum` attain a maximum at an interior point: zero-tension interior state must be classified indeterminate at equality;
5. signed buoyant section: cumulative signed load may decrease; `Q_required` must use the **maximum cumulative signed** load, not total absolute weight;
6. zero-resultant segment/cross-section must never receive a fabricated tangent.

Validation tolerances may only handle floating-point comparison; they must not redefine the physical inequalities.

## Production gate

No production analyzer special case is authorized until the above force-state semantics are validated.

A later production package may then consider splitting the current taut-zero-horizontal branch into explicit geometry/force-state classifications. Any such change must remain in the calculation core and must not switch selected X/Z, downstream tension/anchor/verdict, PDF or 2D authority in the same PR.