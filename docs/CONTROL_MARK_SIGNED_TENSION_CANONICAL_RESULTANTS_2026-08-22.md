# Control mark — canonical Accepted signed tension resultants

Date: 2026-08-22  
Issue: #516  
Package: E1-B2 — canonical Accepted surface/end/max-mid evidence  
Base main: `0fdf265e905445a8ba679a33cf6743fcf86a9189`

## Scope

E1-B2 records three distinct, physically located signed tension magnitudes for the two canonical `Accepted` `SignedBoundaryFeedback` scenarios:

```text
SignedBoundarySurfaceResultantN
SignedBoundaryAnchorEndResultantN
SignedTraceMaxMidResultantN
```

These are evidence names only. None is renamed or wired to `CalculationResult.TensionKn`.

## Source-backed quantities

For an actually selected Accepted signed candidate, `MooringSelectedSignedBoundaryState` directly exposes the solved boundary state already carried by that candidate:

```text
surface H = BuoySteadyDragN
surface V = Q0N
anchor-end H = EndHN
anchor-end V = EndVN
```

Therefore:

```text
SignedBoundarySurfaceResultantN = sqrt(BuoySteadyDragN² + Q0N²)
SignedBoundaryAnchorEndResultantN = sqrt(EndHN² + EndVN²)
```

No trace reconstruction is required for either endpoint magnitude.

## Stored local midpoint resultants

`MooringSignedCandidateEvaluator.TryBuildAcceptedShape(...)` builds the Accepted shape from the final exact-fixed-point boundary tension trace. For every production segment it stores:

```text
MooringShapePoint.SegmentTensionKn = traceRow.MidTensionN / 1000
```

The first shape node is the boundary origin; each subsequent segment node stores the final trace midpoint resultant for the segment that ends at that node.

E1-B2 therefore defines, for validation evidence only:

```text
SignedTraceMaxMidResultantN = max(acceptedShape segment midpoint tensions)
```

and reports the segment plus physical midpoint coordinate:

```text
s_mid = node.AlongLineM - node.SegmentLengthM / 2
```

## Canonical coverage

The package requires exactly the two already-established Accepted scenarios:

```text
uniform-current-slack-line : no internal point loads
discrete-payload           : one or more internal point loads
```

For both scenarios it verifies:

- selected source is `SignedBoundaryFeedback`;
- selected core uses the exact Accepted candidate shape;
- one positive finite stored midpoint tension exists per production segment;
- surface/end/max-mid resultants are finite positive values;
- point-load/discrete identity agrees between sequence, candidate and selected signed state;
- signed boundary method remains `wave excluded`;
- legacy `CalculationResult.TensionKn` remains unchanged and is emitted only for side-by-side evidence.

## Physical interpretation

The three quantities answer different questions:

```text
surface resultant   : line force at the buoy/surface boundary
anchor-end resultant: line force at the anchor-end boundary
max-mid resultant   : largest stored local segment-midpoint line force
```

They are not generally interchangeable. The maximum-local value can occur away from either endpoint, especially when signed distributed weights and internal point loads are present.

## Wave limitation

All three signed resultants belong to the current signed boundary/feedback model, whose method explicitly excludes wave loading. Legacy `CalculationResult.TensionKn` is based on the original aggregate horizontal force including `WaveForceN` plus the legacy vertical net-buoyancy term.

Therefore E1-B2 still does **not** justify direct replacement of the legacy design-demand scalar.

## Non-change statement

Unchanged:

- solver equations;
- signed candidate acceptance/exact fixed-point rule;
- selected geometry/source authority;
- production `TensionKn` and reserves;
- weak-link/WLL policy;
- checks, verdict and main risk;
- exact 0.20 m segmentation;
- production feedback budget 64;
- signed submerged-weight semantics;
- wave equations;
- anchor/seabed model;
- PDF/2D/UI physics;
- persistence/schema;
- golden baseline;
- 3D.

## Next step — E1-C

E1-C must now make the design-demand disposition explicitly. The available evidence does not permit choosing a scalar merely because one of surface/end/max-mid values is numerically convenient. E1-C must address wave exclusion and whether weak-link policy requires one global demand or location-specific demand before authorizing or refusing any later production migration.
