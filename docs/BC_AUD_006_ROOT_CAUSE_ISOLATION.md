# BC-AUD-006 ROOT-CAUSE-ISOLATION — validation-only closed-loop research

Issue #602; initial main 92e95d9bd0a5ef9a64e87fab277f348249e4a91b.

## Scenarios and variants
B0 A06/A11/A15: current full-bit baseline (already production-observer parity verified).
H1 A15: independent fixed original buoy drag into existing boundary solver.
H1N1 A15: diagnostic total-(line+point) reconstruction.
H1N2 A15: diagnostic (total-point)-line reconstruction.
H2 A06/A11/A15: trace TangentX/TangentZ supplied directly to existing force law.
H1H2 A15: combined H1 and H2.

All alternative calculations are complete 64-step feedback loops unless an actual
non-solved boundary/trace produces an explicit early stop; no invented steps.
Each is executed twice with strict field-by-field IEEE-754 replay comparison.
Experiment results DO NOT enter application calculation and CANNOT become Accepted.
No convergence epsilon, canonicalization or golden baseline updates are permitted.
The baseline B0 is independently verified against production stage bit fields
by the pre-existing mandatory ParityReadoutReport.

The only Services changes are INTERNAL research adapters for the buoy-drag input
and direct-tangent input. Public Build signatures, original operation ordering
when no research adapter is used, integration kernel, formulas and checks remain.
Signed WeightWaterKgM, 0.20m segmentation and budget 64 remain fixed.

## Published evidence
The existing bc-aud-006-parity exact-commit artifact contains
root-cause-isolation.zip with full-states.csv (every observed state),
state-sha256.csv, outcomes.csv, first-differences.csv,
first-stage-differences.csv and method.md. Digest encoding is UTF-8
Field=RawBits + LF in source row order. H1/H2 orbit fixed points are diagnostic
observations only, not physical validity or F1-F4 equivalent accepted output.

The A03 rejection and A05 Accepted/F1-F4 invariants remain mandatory controls.
No merge, no Ready, no production solver policy changes. Issue #602 stays open.
