# Control mark — signed tension boundary ownership

Date: 2026-08-22  
Issue: #516  
Package: E1-A — location/orientation/load-ownership map  
Base main: `e53491bc0a87cc120a0e2030a422e9fe4c03ac49`

## Scope

This package establishes the physical ownership and direction of the already-existing signed surface-boundary force state before any scalar tension-demand definition is proposed.

It is validation/documentation only. It does **not** define or switch production `CalculationResult.TensionKn`, `TensionReserve`, element reserves, checks, verdict or main risk.

## Proven sequence direction

`MooringSequencePositioner` starts with `s = 0` and walks `CalculationResult.ElementRows` in sequence order. The buoy is explicitly classified as the `верхний граничный узел`, the anchor as the `нижний граничный узел`, and line intervals advance `s` only for distributed line elements.

`BuildSegmentRows` likewise starts line segments at `StartLengthM = 0`; estimated depth increases with line coordinate and the final segment ends at total line length.

Therefore the production signed boundary integration direction is:

```text
s = 0                         s = L
surface / buoy  ----------->  seabed / anchor
```

The E1-A canonical regression requires the first discrete sequence row to be the buoy at `s=0`, the last discrete row to be the anchor at `s=L`, and segment/depth ordering to advance from surface toward seabed.

## Boundary state ownership

`MooringSurfaceBoundaryInfoAnalyzer` prepares internal point loads by excluding the first and last discrete boundary rows. Thus buoy and anchor are boundary conditions, not internal point loads.

For whatever `CalculationResult` is supplied to that analyzer it derives:

```text
BuoySteadyDragN = CurrentForceN
                 - sum(line segment current forces)
                 - sum(internal discrete point current forces)
```

and bounds the vertical start component by buoy capacity:

```text
QCapacityN = max(0, full-volume buoyancy force - buoy weight force)
0 <= Q0N <= QCapacityN
```

The shared production integration kernel is then called with:

```text
initial H = BuoySteadyDragN
initial V = Q0N
```

Consequently the E1-A names are:

```text
SurfaceLineHN = BuoySteadyDragN
SurfaceLineVN = Q0N
AnchorEndLineHN = EndHN
AnchorEndLineVN = EndVN
```

These are names for validation semantics only. None is an alias for `CalculationResult.TensionKn`.

## Base boundary versus Accepted feedback boundary

There are two different current-force stages and they must not be conflated.

The snapshot technical-report boundary is built from the original `CalculationResult`. For that **base** stage E1-A verifies:

```text
BaseBoundary.EndHN = CalculationResult.CurrentForceN
```

through the explicit buoy + line-segment + internal-point steady-current balance.

The Accepted `SignedBoundaryFeedback` candidate is different. `MooringSignedCandidateEvaluator` repeatedly runs `MooringShapeForceAnalyzer` and then creates a new intermediate result with:

```text
SegmentRows     = updatedSegments
CurrentForceN   = updatedTotalCurrentForceN
HorizontalForceN = updatedTotalCurrentForceN + baseResult.WaveForceN
```

before solving the next boundary. The final Accepted candidate therefore owns a **feedback-updated** current-force state. That intermediate `CalculationResult` is not published as the application's legacy `run.Result`.

Therefore this equation is explicitly invalid for the final selected signed state:

```text
SelectedSigned.EndHN = original run.Result.CurrentForceN   // DO NOT ASSUME
```

E1-A instead requires exact identity between `MooringSelectedSignedBoundaryState` and the Accepted candidate's own solved `Boundary/SolutionState`.

## Distributed and point-load accumulation

For each line segment the kernel applies:

```text
H += segment.CurrentForceN
V -= segment.WeightWaterKg * g
```

For each internal discrete point crossed at its `s` position:

```text
H += point.CurrentForceN
V -= point.WeightWaterKg * g
```

Thus each solved boundary state satisfies, against the segment-current-force stage that produced it:

```text
AnchorEndLineHN
  = SurfaceLineHN
  + sum(stage line current forces)
  + sum(internal point current forces)

AnchorEndLineVN
  = Q0N
  - g * [sum(signed line submerged weights)
         + sum(signed internal-point submerged weights)]
```

Feedback changes the line current-force rows, but does not change the signed submerged weights. E1-A therefore independently verifies the final Accepted `EndVN` from the original segment/internal-point submerged weights and the final candidate `Q0N`.

The negative-weight convention is intentional: if `WeightWaterKg < 0`, the update `V -= W_water*g` increases V. E1-A verifies this using the canonical `buoyant-line` fixture without selecting that rejected candidate as authority.

## Point-load ownership

The analyzer excludes:

- buoy/top boundary row;
- anchor/bottom boundary row.

Every other discrete sequence row is ordered by `PositionAlongLineM` and then by element number and is applied when the integration crosses that coordinate.

For the two Accepted canonical scenarios E1-A requires:

```text
uniform-current-slack-line : 0 internal point loads
discrete-payload           : >0 internal point loads
```

and the selected signed `PointLoadCrossings` must equal the internal discrete-row count.

## Wave ownership — critical blocker for direct scalar substitution

`MooringSurfaceBoundaryInfoAnalyzer` explicitly describes its method as:

```text
steady current; wave excluded
```

This applies both to the base boundary and every feedback-updated candidate boundary.

Legacy global scalar force, however, is explicitly composed as:

```text
CalculationResult.HorizontalForceN
  = CalculationResult.CurrentForceN
  + CalculationResult.WaveForceN
```

Both Accepted canonical fixtures deliberately have non-zero `WaveForceN`.

The E1-A proof is semantic rather than a fragile numerical inequality: signed boundary H/V belongs to a steady-current boundary path, while legacy `HorizontalForceN` is a wave-inclusive aggregate. A feedback-updated `EndHN` must therefore not be aliased to legacy `HorizontalForceN` or original `CurrentForceN` merely because both are horizontal-force values.

This prevents a later implementation from silently replacing the wave-inclusive legacy design-demand scalar with a steady-current-only signed resultant.

## Tension quantity names reserved for later E1-B evidence

E1-A does not yet choose a design tension. Later validation may calculate explicitly named magnitudes such as:

```text
SignedBoundarySurfaceResultantN = hypot(SurfaceLineHN, SurfaceLineVN)
SignedBoundaryAnchorEndResultantN = hypot(AnchorEndLineHN, AnchorEndLineVN)
SignedTraceMaxMidResultantN = max(local midpoint resultants)
```

Those names are intentionally **not** `TensionKn`. Their physical location and load ownership remain explicit.

## E1-A conclusion

The production signed boundary state is now unambiguously mapped as a surface-to-anchor, steady-current, signed-submerged-weight integration path with internal discrete point loads applied by `s` crossing.

The important stage distinction is:

```text
base boundary H-state            = original CalculationResult steady-current stage
Accepted selected boundary H-state = feedback-updated steady-current stage
legacy TensionKn demand           = original wave-inclusive horizontal aggregate + legacy net-buoyancy vertical term
```

Therefore E1-A does **not** justify a production tension-authority switch. It supplies the ownership map required for E1-B independent/reference resultants.

## Non-change statement

Unchanged:

- solver equations;
- signed candidate acceptance;
- selected geometry/source authority;
- exact 0.20 m segmentation;
- production feedback budget 64;
- signed `WeightWaterKgM` semantics;
- `CalculationResult` fields/formulas;
- weak-link/WLL policy;
- anchor/seabed model;
- PDF/2D/UI physics;
- persistence/schema;
- golden baseline;
- 3D.
