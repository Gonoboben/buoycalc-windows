# Control mark: vertical force-state family production classification — 2026-08-18

Issue: #487 — Physics RFC: resolve signed-geometry production blockers before authority switch.

This document authorizes one narrow calculation-core INFO semantics change after Package B validation (#489/#490). It does **not** authorize a selected-X/Z switch or a solved Q0 value.

## Proven facts entering this package

For the historical `vertical-zero-current` fixture:

```text
L = depth = 50 m
steady horizontal load = 0
Q_required = 49.03325 N
Q_capacity = 9071.15125 N
```

Validation has proven:

- inextensible geometry is uniquely straight vertical (`X=0`, `Z=depth`);
- `Q0 = Q_required` is an endpoint-zero-tension limiting state;
- every strictly tensile `Q0` in `(Q_required, Q_capacity]` has the same straight-vertical geometry;
- at least two distinct admissible Q0 values therefore produce identical geometry but different force state;
- geometry/depth closure cannot select a unique Q0.

The current production classification `VerticalGeometryBoundaryNonUnique` is consequently inaccurate for this strict-family historical case: geometry is not non-unique; the vertical reaction/tension state is.

## New INFO classification

Add one enum member:

```text
VerticalGeometryUniqueForceStateFamily
```

Its required semantics are:

```text
Available = true
Solved = false
Classification = VerticalGeometryUniqueForceStateFamily
Q0N = null
SolutionState = null
RootBracketed = false
Iterations = 0
```

Existing boundary diagnostics may continue to publish:

```text
TargetDepthM
LineLengthM
BuoySteadyDragN
QCapacityN
LowerBoundaryState
CapacityBoundaryState
MinimumQForDownwardVerticalGeometryN
```

No representative Q0 may be inserted merely for display.

## Eligibility for the new classification

This first production package intentionally covers only the **strict family available** case already represented safely by current analyzer data.

For the taut zero-horizontal branch, the new classification may be returned only when:

1. `L = depth` under the existing production length comparison;
2. steady horizontal current force is within the existing force-zero branch;
3. buoy capacity is above the analyzer's strict downward-vertical minimum Q boundary;
4. the capacity-boundary integration state is determinate;
5. capacity-boundary Z closes the target depth under the existing INFO depth tolerance.

This package does not change those existing production numerical tolerances.

## Equality and edge cases deliberately deferred

Do **not** use the new family classification for unresolved equality/edge cases merely because validation has described their physics.

In particular, this package does not yet reclassify production cases where:

```text
Q_capacity ~= Q_required
```

because production currently does not publish a typed indication of whether the cumulative-load maximum occurs only at the anchor endpoint or at an interior cross-section.

Those cases require a separate package before they can distinguish:

```text
EndpointZeroTensionLimit
InteriorZeroTensionIndeterminate
```

Likewise, `VerticalGeometryCapacityInsufficient` remains unchanged.

## What remains unchanged

The package must not change:

- integration kernel arithmetic;
- Q0 bisection;
- drag or signed submerged-weight equations;
- production segmentation (`0.20 m` target, no segment-count cap);
- `WeightWaterKgM` sign semantics;
- `SelectedMooringShapeProvider`;
- selected X/Z or selected source;
- iterative feedback/gate/verdict;
- tension/anchor/weak-link calculations;
- 2D/PDF geometry;
- JSON/DTO;
- golden baseline;
- 3D.

## Required regression updates

The production-classification PR must prove:

1. the exact historical `vertical-zero-current` fixture now reports `VerticalGeometryUniqueForceStateFamily`;
2. `Available=true`, `Solved=false`, `Q0N=null`, `SolutionState=null`;
3. capacity and minimum-Q diagnostics remain available and numerically unchanged apart from no intended arithmetic change;
4. the historical selected shape remains byte/numerically unchanged;
5. taut non-zero-horizontal fixtures remain `TautNonZeroHorizontalLoadNoFiniteRoot`;
6. ordinary solved slack cases remain unchanged;
7. golden baseline is not modified.

## Presentation semantics

Existing INFO report rendering may display the new classification name through the already prepared `SurfaceBoundaryInfo` read model. A renderer must not invent a representative Q0 or claim `Solved=true`.

A separate presentation wording improvement may follow if needed, but is not required to establish the calculation-core classification.

## Merge gate

Merge only after the exact final PR head has:

- `.NET Build` = SUCCESS;
- `Selected Shape Consumer Scan` = SUCCESS;
- `Report Store Consumer Scan` = SUCCESS.

This control mark authorizes only the narrow INFO-classification change above.