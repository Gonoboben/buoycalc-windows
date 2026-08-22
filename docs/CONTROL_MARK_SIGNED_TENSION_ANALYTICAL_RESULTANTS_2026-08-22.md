# Control mark — signed tension analytical resultants

Date: 2026-08-22  
Issue: #516  
Package: E1-B1 — independent analytical surface/end resultant evidence  
Base main: `bc23df63f463368e556a08c06f7956fa569d35dd`

## Purpose

E1-B1 names and measures two physically located signed tension magnitudes without assigning production design-demand authority:

```text
SignedBoundarySurfaceResultantN
SignedBoundaryAnchorEndResultantN
```

The package is validation/documentation only. `CalculationResult.TensionKn` and every downstream reserve/check/verdict remain unchanged.

## Independent reference basis

The existing `BoundaryFeedbackIndependentReferenceRegression` already contains a validation-only analytical fixture that is independent of the production boundary discretization:

- constant horizontal current;
- neutral submerged line weight;
- zero buoy drag;
- zero wave force;
- zero internal point loads;
- analytical closed-form geometry/force reference;
- separate production-style feedback candidate run for comparison.

E1-B1 deliberately reuses those already validated outcomes instead of creating a second copy of the analytical equations.

For this neutral fixture:

```text
Surface H = 0
Surface V = Q0
Anchor-end H = EndH
Anchor-end V = Q0
```

so the two location-specific magnitudes are unambiguous:

```text
SignedBoundarySurfaceResultantN = |Q0|
SignedBoundaryAnchorEndResultantN = hypot(EndH, Q0)
```

## Evidence policy

E1-B1 emits reference and production-candidate values plus absolute and relative deltas.

It does **not** introduce a new acceptance tolerance. The existing independent-reference regression remains responsible for fixture validity and analytical/candidate evidence. E1-B1 adds only the physical resultant interpretation.

Therefore a small numerical delta is evidence, not permission to transfer production authority.

## Frozen load ownership

The analytical fixture is steady-current only and wave-free. This matches the signed boundary load ownership established by E1-A but does not represent the legacy wave-inclusive global design-demand contract.

Accordingly:

```text
SignedBoundary*ResultantN != automatic alias for CalculationResult.TensionKn
```

## Non-change statement

Unchanged:

- solver equations;
- signed candidate evaluator;
- selected geometry/source authority;
- production `CalculationResult.TensionKn`;
- tension/anchor/weak-link reserves;
- checks, verdict and main risk;
- exact 0.20 m segmentation;
- production feedback budget 64;
- signed submerged-weight semantics;
- wave model;
- anchor/seabed model;
- PDF/2D/UI physics;
- persistence/schema;
- golden baseline;
- 3D.

## Next step

E1-B2 must inspect the two canonical Accepted signed candidates and prove the identity/location of the stored local midpoint resultant (`MooringShapePoint.SegmentTensionKn`) including the max-local value. Only after B1+B2 evidence exists may a separate package discuss which physical resultant, if any, should become a future design-demand authority.
