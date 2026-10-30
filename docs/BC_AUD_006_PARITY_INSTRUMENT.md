# BC-AUD-006-PARITY-INSTRUMENT — production observation only

Source main: 4bd3f1b02ef2b066b30ef4ec08d146bf64d19634. Issue #602.

Objective: compare raw IEEE-754 fields from actual production feedback loop with the existing validation mirror for A06/A11/A15 over all 64 iterations.

## Boundary and method
- Existing Build(...) signature retained, calls unchanged production loop with null observer.
- Internal BuildWithDiagnosticObserver(...) invokes same production loop with an explicit callback. The only added friend access is BuoyCalc.ValidationFeedbackDiagnostics; no new public API.
- Read-only probe captures immutable strings after production projection / shape-force / boundary / tension-trace / geometry stages.
- Results of probe never feed back into the solver, accepted-shape constructor or selected engineering authority.
- Comparison checks 192 production/mirror steps field-by-field with no epsilon. Missing fields, order changes and differing bits FAIL.
- Production original calculation versus observed run must retain status, iteration count, diagnostic code, Q0, endpoints and exact-fixed-point flag.
- Direct A03 (RejectedPhysical) and A05 (Accepted) observer-versus-normal candidate comparisons use canonical reflection of every public property, IEEE-754 raw bits for all floating values, and ensure pre-existing selected F1-F4 authority remains unmodified. A03 must record zero observer steps, A05 at least one. parity-observer-controls.csv retains exact digests and results.
- Existing A03 / A05 regressions, 64-budget, signed WeightWaterKgM and 0.20 m segmentation remain unchanged.

## Artifact
- Existing mirror fields/step SHA256 and orbit readout retained.
- New parity-production-fields.csv, parity-production-step-sha256.csv and parity-production-mirror-comparison.csv.
- SHA256 serialization: UTF-8 Stage/SegmentNumber/Field=RawBits + LF per field in original CSV order; direct comparison remains authority, not hash.

## Limits
This is production↔mirror parity for INSTRUMENTED fields only, not exhaustive private state equality or independent physical validation.
Never interpret matched tiny differences between cyclic states as numerical convergence.
No solver formulas/criteria, F1-F4, DTO, frozen baselines, UI, PDF or release status changed.
Draft PR only; do not merge or close Issue #602 without distinct authorization.
