# BC-AUD-006 — exact signed-feedback two-cycle research
Issue: #602. Status: research-only, no solver decision and no release authorization.
Source baseline: main 1ea0ddb76f07a41a811390632b64afbf257283eb.

## Questions and evidence
- A06 D20/L22/U0.2, A11 D50/L55/U0.5, A15 D100/L110/U0.3 with payload.
- Production ApplicationCalculationRunner must continue to return BudgetExhausted without selected F1-F4.
- Validation mirror observes states 0..64 and runs twice using identical physical inputs.
- Compare ordered fields by BitConverter.DoubleToInt64Bits, not floating equality, epsilon, or truncated hash.
- Require strict period-two recurrence from first repetition to the end; emit raw differing field bits.
- Control true period-two, nonperiodic drift, and exact one-state sequence with synthetic probe.
- Control real production A03 RejectedPhysical and A05 Accepted with F1-F4.
- Upload four files under artifacts/bc-aud-006: feedback-budget-diagnostics.csv, feedback-budget-diagnostics.md, exact-cycle-bit-differences.csv, exact-cycle-research.md.

## Limits
The validation mirror reuses public stages but is not an independent solver or physical reference.
No canonical orbit member is accepted and no F1-F4 equivalence is claimed.
Later RFC must review operation-order source of oscillation, all authority impacts and alternatives.
No solver/physics/formulas, 0.20 m segmentation, signed WeightWaterKgM, 64 budget, exact fixed-point-only Accepted, golden baseline, schemas, UI, PDF, DTO or release-state change.

## Review
Require all five existing CI/status checks and inspect the actual uploaded research artifacts.
PR stays Draft. Merge and changes of physics/convergence policy require distinct authorization.
