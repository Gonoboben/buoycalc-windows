# BC-AUD-006: exact-cycle-triggered numerical recovery

Issue #602. Prerequisite: reviewed validation-only PR #635 and its exact-head
105-fixture authority proof. This document precedes production activation.

## Engineering question and unchanged model

The frozen feedback map sometimes enters a strict IEEE-754 period-2 orbit and
cannot reach its existing exact fixed point. Can the same signed midpoint model
reach an actual fixed point by avoiding the coordinate subtraction/division
round-trip only after the original map has proved periodic?

The unchanged force law is
`F = 0.5 * rho * Cd * A * |U_normal|^2`,
`|U_normal|^2 = max(0, |U|^2 - (U dot t)^2)`.
The trace gives `t = (Hmid/Tmid, Vmid/Tmid)`. Geometry integrates that same
tangent over segment arc length; the original force evaluation reconstructs it
from accumulated coordinate differences. In exact arithmetic these tangents
agree above the historical 0.0001 m denominator floor. In floating arithmetic
the coordinate round-trip can lose bits. No equation, projected area, force
sign, signed submerged weight, point-load ownership or wave proxy is replaced.

Units: lengths m, mass kg, force N, density kg/m3, speed m/s; +Z is downward,
and `WeightWaterKgM` retains its sign. Existing surface/steady-current signed
eligibility and all unsupported/indeterminate dispositions remain authoritative.

## Algorithm and acceptance contract

1. Start with the original feedback map and original inputs.
2. Compare complete changing state vectors directly as raw bits. Immutable
   inputs/segment properties are copied unchanged from the same base result.
3. Only `state[n] == state[n-2]` and `state[n] != state[n-1]` proves a strict
   period-2 orbit. There is no hashing-only decision or numerical tolerance.
4. For subsequent steps, use direct trace tangents in the same force equation.
   If any production segment is shorter than 0.0001 m, disable this recovery.
5. Keep the same single total budget of 64 iterations. Never restart the budget.
6. Accept only through the existing actual exact-fixed-point, trace, projection,
   point-load closure, Accepted-shape endpoint and downstream authority gates.

No orbit member is accepted or canonicalized. The period detector does not grant
engineering authority. The recovery may still fail, in which case the existing
non-Accepted outcome remains. No new tolerance is introduced. Existing geometry
and force identity scales remain 1e-9 m and 1e-6 N; neither measures convergence.

## Preservation argument and evidence

For one deterministic map, a strict repeated 2-state orbit excludes a future
fixed point under that map. Therefore a previously Accepted run cannot trigger
recovery: its arithmetic, iteration count, full candidate and F1-F4 remain exact.
The proof compares the complete old Accepted candidate and authority trees,
not merely rounded values. Public candidate output must match the validated
H2R adapter bit for bit across all 105 fixtures.

A03 remains RejectedPhysical; A05 remains exactly unchanged. Target A06/A11/A15
reach actual accepted fixed points at 16/17/17 steps. Expanded H2R evidence:
81 Accepted, 3 BudgetExhausted, 12 RejectedPhysical, 6 RejectedNumerical and
3 Indeterminate. Blanket H2 was rejected because it loses 9 old Accepted cases.
Initial BudgetExhausted cases have no old selected F1-F4; none are invented.

Validation: original exact-cycle trajectories and observed shared-core parity
remain as explicit **legacy-map** controls; new production is checked against
the proven H2R path. Full Series A, frozen regression, Validation 601, consumer
scans and exact-head Windows Build must pass before integration. The old A06
BudgetExhausted control is retained as an Accepted recovery control, with a new
still-BudgetExhausted fixture added instead of dropping non-acceptance coverage.

## Limits and rollout

This is a numerical repair within the existing model. It does not establish
field suitability, a seabed/soil capacity model, dynamic-wave validity or
production-grade physical validation. The 3 residual budget failures remain
visible. No tag or public v1.0 release is authorized. A failure in a mandatory
gate blocks merge. No frozen baseline regeneration is permitted.

## First production-path measurements

Local actual application path: all 105 production candidates/F1-F4 match the
validated H2R adapter; all 67 old Accepted candidates/F1-F4 stay exact. Series A
now has 9 Accepted, 9 RejectedPhysical, 2 Indeterminate, no BudgetExhausted.
Validation 601 retains the 5 physical failures and now has 2 genuine Accepted
controls plus 1 unresolved budget control (9 cases total).

Raw Series A still emits 32 findings (23 Geometry / 9 Presentation). They are
retained unchanged for review. Its historical classifier inspects fallback and
iterative diagnostic shapes and expects F4 on physical rejections, although that
is not the current terminal physical-disposition contract. These findings must
be dispositioned against actual selected authority; the campaign's exit code 0
only confirms execution/evidence generation and does NOT mean 32 findings passed.
An actual before/after run using the same Series A harness and old/new core
found **no new raw finding**. Two old A11 selected-chord findings disappeared
because it now selects the actual accepted signed shape. Every old Accepted
Series A endpoint and F1-F4 scalar stayed exact. A new selected-authority gate
requires all 20 expected dispositions, genuine signed source, finite X/Z/F1,
selected chord/arc bounds, exact fixed point within 64 and correct absence of
F1-F4 on non-Accepted outcomes; unexplained raw finding codes fail this gate.
