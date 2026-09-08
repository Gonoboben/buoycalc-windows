# Production #601 — physical rejection authority boundary

Date: 2026-09-08

## Purpose

Implement the already validated #601 contract without changing signed solver physics, feedback acceptance semantics, production segmentation, or frozen engineering baselines.

## Authority rule

When `MooringSignedCandidateStatus.RejectedPhysical` is already produced by the signed calculation core:

- create a typed terminal physical disposition;
- verdict is `Не подходит`;
- preserve the exact signed `DiagnosticCode` and `DiagnosticText`;
- block legacy/fallback/iterative X/Z from becoming selected engineering geometry;
- do not create F1 design tension, F2 anchor reaction, F3 local structural capacity, or F4 selected assessment from a non-existent selected equilibrium.

For `Indeterminate`, `RejectedNumerical`, `BudgetExhausted`, and `Unavailable`, this rule does not create a physical hard failure and existing fallback behavior is preserved.

For `Accepted`, the existing `SignedBoundaryFeedback` X/Z and F1/F2/F3/F4 chain is unchanged.

## Presentation boundary

The physical disposition is projected once from `CalculationSnapshot` into user/report read models. PDF, 2D, UI, and Markdown renderers must not infer physical rejection from geometry or recalculate it.

`SelectedShape == null` is the presentation consequence for `RejectedPhysical`; therefore 2D/PDF diagram selectors naturally refuse to draw engineering X/Z without learning solver physics.

## Frozen invariants

- production segmentation: 0.20 m;
- signed feedback budget: 64;
- signed `WeightWaterKgM` semantics unchanged;
- Accepted candidate remains an exact deterministic fixed point with no convergence epsilon;
- `s=0` at buoy/surface and `s=L` at anchor/seabed;
- frozen engineering baseline must not be regenerated.

## Validation

`validation/BuoyCalc.PhysicalRejectionValidation` is upgraded from pre-production target evidence to a production regression. Its eight-scenario matrix requires exactly five `RejectedPhysical` terminal dispositions and five blocked selected geometries while protecting the `Indeterminate`, `Accepted`, and `BudgetExhausted` controls.
