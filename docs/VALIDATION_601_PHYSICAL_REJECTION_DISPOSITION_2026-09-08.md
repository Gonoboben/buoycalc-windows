# Validation package #601 — signed physical rejection disposition

Date: 2026-09-08

Issue: #601 — `v1 blocker: promote signed physical rejection to explicit hard failure`

## Purpose

Series A proved that the calculation core can already know that a mooring state is physically impossible while the legacy/iterative selected-shape presentation still exposes an X/Z shape and F4 remains unavailable. This package freezes the intended authority contract **before any production change**.

This package does not alter solver equations, feedback iteration, F1/F2/F3, the frozen engineering baseline, or user/PDF code.

## Authority rule under validation

A `MooringSignedCandidateStatus.RejectedPhysical` result is sufficient typed evidence of a physical hard failure because the signed candidate has already terminated on a physical boundary classification. The production fix shall therefore introduce/project one physical-disposition authority with these semantics:

1. `RejectedPhysical` => physical disposition `HardFailure`.
2. The verdict projected from that disposition is `Не подходит`.
3. The exact `SignedCandidate.DiagnosticCode` and diagnostic text are retained as provenance.
4. Legacy/fallback/iterative X/Z may remain available as diagnostics, but **must not be presented as selected engineering authority, `OK`, or a valid converged design geometry** for that result.
5. F1, F2 and F3 are **not fabricated** when there is no Accepted signed equilibrium. Their unavailability is downstream of the physical rejection.
6. The hard-failure disposition may provide the F4-equivalent outcome directly; it does not require fake F1/F2/F3 values.
7. `Accepted` continues through the existing F1 -> F2 -> F3 -> F4 chain without an override.
8. `RejectedNumerical`, `BudgetExhausted`, `Indeterminate` and `Unavailable` do **not** become physical hard failures under this rule.

## Independent geometry truth used by this package

For an inextensible line with depth `D`, line length `L`, and non-zero horizontal environmental load `H`:

- `L < D` is physically impossible.
- `L = D` and `H != 0` is physically impossible: the only possible geometry at `L = D` is the vertical line (`X = 0`).
- `L = D`, `H = 0` is geometrically admissible and must not be hard-failed merely because the current force-state model is indeterminate.
- `L > D` is geometrically eligible for a slack equilibrium search; geometry alone does not prove that the signed feedback candidate will reach an Accepted exact fixed point.

No convergence epsilon is introduced by these statements.

## Validation matrix

| ID | D (m) | L (m) | Current | Expected signed state | Expected physical disposition |
|---|---:|---:|---:|---|---|
| V601-01 | 20 | 19 | 0.0 m/s | RejectedPhysical / LineShorterThanDepth | HardFailure |
| V601-02 | 20 | 19 | 0.3 m/s | RejectedPhysical / LineShorterThanDepth | HardFailure |
| V601-03 | 20 | 20 | 0.0 m/s | Indeterminate / VerticalGeometryUniqueForceStateFamily | None |
| V601-04 | 20 | 20 | 0.2 m/s | RejectedPhysical / TautNonZeroHorizontalLoadNoFiniteRoot | HardFailure |
| V601-05 | 50 | 50 | 0.5 m/s | RejectedPhysical / TautNonZeroHorizontalLoadNoFiniteRoot | HardFailure |
| V601-06 | 100 | 100 | 0.8 m/s | RejectedPhysical / TautNonZeroHorizontalLoadNoFiniteRoot | HardFailure |
| V601-07 | 50 | 50.5 | 0.5 m/s | Accepted | None; normal F1-F4 path |
| V601-08 | 20 | 22 | 0.2 m/s | BudgetExhausted exact two-cycle control | None; not a physical rejection |

All profiles are explicit two-point constant profiles; the scalar legacy current slot is zero and has no authority.

## Pass criteria for this validation package

The validation executable must prove on the current production pipeline that:

- every analytically impossible fixture listed above is classified `RejectedPhysical` with the expected exact diagnostic code;
- the validation-only target disposition maps only `RejectedPhysical` to `HardFailure / Не подходит` and preserves the candidate diagnostic provenance;
- physical rejection does not invent F1/F2/F3 authority;
- the `L=D, H=0` control is not converted into a hard failure;
- the Accepted slack control keeps the normal selected signed source and complete F1/F2/F3/F4 chain;
- the BudgetExhausted slack control remains non-physical and is not converted into a hard failure;
- current legacy selected-shape exposure for RejectedPhysical is recorded as the defect to be removed, not treated as validation truth.

## Production-change boundary after this package

A later production PR may add a typed physical-disposition state and projection into selected geometry/user/PDF/F4 boundaries. It must not change the signed boundary equations or candidate acceptance rules to solve #601. The exact two-cycle problem remains isolated in #602.
