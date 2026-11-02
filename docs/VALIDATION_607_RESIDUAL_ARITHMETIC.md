# Validation #607 residual feedback arithmetic research

Prerequisite: #602 resolved by #635/#636; production H2R uses the original map,
then direct trace tangents after a directly proven exact period-2 cycle.
No acceptance predicate, model equation, signed weight or 64-step budget changes.

Three of 105 expanded fixtures still exhaust 64: D/L/U = 20/21/0.6,
20/24/0.6 and 380/418/0.45. They have no legacy selected F1-F4 reference.

## H2R3 hypothesis (validation only)

After H2R itself enters a raw-bit-proven period-2 orbit, evaluate the same force
law as `k * normalSquared` instead of `k * sqrt(normalSquared) * sqrt(normalSquared)`.
This removes one floating-point square-root round trip; for nonnegative real
normalSquared these are the same equation. The force coefficient, projected
area, current components, weights, point loads, root search and all actual
production acceptance / snapshot / F1-F4 checks remain shared and unchanged.
Cycle history is cleared when changing arithmetic maps; cycles are never inferred
across different maps. All transitions consume the SAME original 64 steps.
Sub-0.0001 m segments still disable recovery. Public production defaults leave
this second intervention **disabled** throughout the proof package.

## Local observations

105 fixtures × 6 twice-replayed modes = 630 rows. H2R3: 83 Accepted,
1 BudgetExhausted, 12 RejectedPhysical, 6 RejectedNumerical, 3 Indeterminate.
All 81 current production Accepted full candidate and F1-F4 trees stayed raw-bit
identical, also compared against the earlier independent compiled core evidence.
20/21/0.6 reaches actual Accepted at 21 iterations; 380/418/0.45 at 24.
20/24/0.6 remains BudgetExhausted. A03/A05 and A06/A11/A15 are unchanged.

The same invariant argument applies: a run that reaches the existing fixed point
under H2R cannot first enter a strict repeated 2-state H2R orbit. Recovery therefore
only affects runs that could not be accepted by the current deterministic map.
No orbit representative is selected or accepted. A new true fixed point still
must pass every existing gate, including raw-state observation, exact accepted
shape/boundary endpoint, retained final trace and the actual F1-F4 projectors.

## Evidence and limits

The regular diagnostic command emits all raw original-map states, the 630-row
authority summary, measured geometry/force residuals and core/residual full
candidates plus consecutive final-state CSVs. `MaxNormalArithmeticDeltaN`
measures the same-state force-expression rounding difference; it is never a
feedback convergence threshold. Windows exact-head CI and actual artifact
review remain mandatory before any production activation. No frozen baseline
may be changed to hide a difference. This is numerical research, not field or
anchor/soil suitability evidence.
