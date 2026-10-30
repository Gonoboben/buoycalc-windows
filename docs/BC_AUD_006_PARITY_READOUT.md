# BC-AUD-006-PARITY-READOUT — mirror-only observation contract

Issue: #602. Baseline: ad803049dafa38d1af69fac11b5fc30c8fe53697. Branch: `physics/602-parity-readout`. Validation-only Draft PR.

This package implements the first, **mirror-only** part of RFC-002. It does **not** claim internal production parity, does not instrument production, and cannot change production acceptance.

## Experiments

- Three existing Series A cases A06/A11/A15; two identical diagnostic replays of 64 iterations.
- H1: raw-bit `FixedBuoyDragN`, `LineForceN`/`SegmentSumN`, `PointSumN`, `DiscreteCurrentForceN`, `ResultCurrentForceN`, `RecoveredBuoyDragN`, `BoundaryBuoyDragN`, `TraceInitialHN`; capture before and after each feedback step. Recover using original `(result.total - segment.sum) - point.sum` order. Independently reconstruct `(fixed + line) + discrete`. The comparisons are diagnostic, **not** modifications to the calculation.
- H2: for each existing target segment 105/150/166/243, capture prior node X/Z, derived ΔX/ΔZ from accumulated geometry, segment length, tx/tz, current velocity, `dot`, `normalSpeed`, actual output `ShapeForceN` and next segment force.
- Field equality uses raw IEEE-754 hex, never tolerances.
- Verify both complete mirror runs bitwise; publish 192 per-step SHA-256 digests, raw fields, and orbit stage differences at 63/64.
- The CLI accepts `--parity-output`; CI requires artifact `bc-aud-006-parity-${{ github.sha }}` (four text files).
- Keep existing BC-AUD-006 capture, baseline tests, BC-AUD-008/009 evidence and scans.

## Evidence boundaries

- Mirror stage instrumentation records actual data from `Program.Fixed.cs` after its calls to the existing public physics utilities.
- The per-step observational calculations are supplemental; results **never** feed back into solver.
- There is no per-stage production observer. Therefore this is NOT full production↔mirror parity.
- A15 force-recovery hypothesis and A06/A11 geometry/projection sensitivity are tested descriptively; neither is declared causally proven.
- No canonical A/B selection, no epsilon, no fabricated F1–F4, no release-state change.

## Engineering invariants

No changes to `Services/`, `Models/`, `ApplicationModel/`, production source, signed force formulas, signed WeightWaterKgM, 0.20 m segmentation, 64 budget, selected authority, exact fixed-point `Accepted`, DTO, frozen baseline, release gate. Issue #602 stays OPEN.

**HOLD:** No Ready for Review or merge without separate authorization. Follow-up RFC required before any production instrumentation or solver change.
