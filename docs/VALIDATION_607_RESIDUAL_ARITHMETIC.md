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
Sub-0.0001 m segments still disable recovery. Proof heads left this second
intervention disabled; the reviewed activation enables it only after the second
exact cycle. The grouped-product alternative stays diagnostic-only and disabled.

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

## Additional controls and wider matrix

Exact-head 6f232bac Windows .NET 41445375561, Validation601 41445375301,
Selected Shape 41445375330 and Report Store 41445375241 succeeded. The downloaded
630-row proof artifact is 13143005555, SHA256
638e8a4e484c47c339222f37fdba3326b1126fe5ccddb0cf6ae10325e190cb01.
All 81 current Accepted trees stayed exact. Core and residual final-state
transcripts were directly compared; same-state force rounding differences are
reported independently from convergence.

H2R4 is a diagnostic negative/alternative control: after a third exact cycle,
group the final product as `k * (normalSpeed * normalSpeed)`. It remains disabled
in production. The initial eight-fixture pilot did not fix 20/24/0.6; it retained
the H2R3 fixed points for 20/21/0.6 and 380/418/0.45. Recovery transition sequences
are emitted and directly compared across both replays; no history crosses maps.

Coverage is expanded to 174 fixtures with 36 shallow/deep residual neighborhoods,
9 additional 380 m neighborhoods, 18 Cd/diameter/density combinations, 4 explicit
wave-proxy combinations and 2 deep vertical-current cases. These are declared
numerical test inputs, not environmental/field claims. Final counts for this
wider matrix must come from its actual emitted evidence, not the earlier 105-row
fixture totals. All previously Accepted production outputs must stay exact.

## Expanded proof and production disposition

Proof head e9a044f16093cd166934a7a1f932b21b6c28938f passed exact-head .NET
41447160107, Validation601 41447160127, Selected Shape 41447160228 and Report
Store 41447160276, plus classic Windows Build. Downloaded artifact 13142134971
SHA256 f8994c645b28c0a2cf0ba484643b778ac948a823f4647f2f4f7df3cdb0ff1e16
was independently inspected: 1,218 mode rows, 292,796 equal previous/next raw
fixed-point fields, all original-map digests/parity and unchanged current
Accepted outputs. All Windows/Linux selected-authority rows agreed.

174 labelled fixtures contain intentional overlapping inputs; do not treat them
as 174 independent physical validations. H2R: 143 Accepted / 10 BudgetExhausted;
H2R3: 147 Accepted / 6 BudgetExhausted. Both preserve 12 RejectedPhysical,
6 RejectedNumerical and 3 Indeterminate. The four improved fixture entries are
two repeated input sets. An independently compiled original c3c06df core was
also run on all 174 fixtures: all 143 original Accepted candidate/F1-F4 trees
matched exactly. The maximum same-state force-expression delta was
1.1102230246251565e-16 N. This is rounding evidence, not a convergence epsilon.
H2R4 added no successful result, so it is not activated in production.

The final public path enables H2R3; diagnostic H2R remains an explicit original
recovery reference. Actual production must equal H2R3 across the full matrix,
and every old H2R Accepted candidate/F1-F4 must stay exact. Actual retained
InputHash values are emitted to identify repeated fixture inputs.

Validation601 keeps 20/21/0.6 as a new real Accepted control, adds 20/24/0.6 as
the remaining budget control and retains all five physical rejections: 10 cases,
3 Accepted, 1 BudgetExhausted, 1 Indeterminate. The PDF identity regression also
retains the recovered Accepted fixture and separately tests the actual unresolved
BudgetExhausted fixture, instead of dropping non-acceptance/provenance coverage.
No physical test, equation, acceptance threshold, selection policy or frozen
baseline is weakened. Final activation-head CI/artifact review is still required.
