# BC-AUD-006 H2 authority proof (validation only)

Historical proof package: PR #635. Statements below about disabled production recovery describe that proof head; subsequent activation is governed by BC_AUD_006_EXACT_CYCLE_RECOVERY_RFC.md.

Parent research: PR #634, exact head 1791b9c1e67d3d649986881d24e2bc321a6ec11a.
Issue #602 remains open. Production numerics are unchanged.

## Question and method

Can direct tension-trace tangents (H2), original buoy drag (H1), or their
combination pass the **existing** production evaluator and selected F1-F4 chain?
The internal diagnostic adapter changes only the two explicitly selected inputs
to the shared loop. Trace validation, point-load closure, projection/length
checks, 64-step budget, exact fixed-point predicate and Accepted-shape construction
remain the same implementation. The public Build and observer entry points use
both flags false and must match the actual production candidate bit for bit.

The validation harness calls the actual arbitration, selected-boundary,
design-envelope, demand, anchor, local-demand/capacity and assessment projectors.
It does not manufacture Accepted results or copy their physical checks.
Each mode is repeated and the complete public candidate tree is compared using
raw IEEE-754 fields. A03 and A05 accompany A06/A11/A15.

No existing selected F1-F4 reference exists for the original BudgetExhausted
scenarios. Their new selected values are reported as candidates, never as parity
with invented legacy values. Existing Accepted controls have a real comparison.

Run:

```powershell
dotnet run --project validation/BuoyCalc.ValidationFeedbackDiagnostics --configuration Release -- --authority-proof --output artifacts/h2-authority-proof
```

The standard .NET Build also executes this package and emits its JSON in the
existing parity artifact. This is a computational validation, not field evidence.
Production change, acceptance-contract change and release are outside this PR.

## Initial exact-head observations

Head ec19c532fa29423ed88b49e54914c3bb19c7789d: .NET Build 41436065565,
Validation 601 41436065597, Selected Shape Scan 41436065815 and Report Store Scan
41436065665 succeeded. The .NET job checked out merge tree
164d83656a557083594dd55ef3a6bb4b952c69fe; tree identity must be verified separately.
Artifact 13134994832 SHA256
7eee5c0295cad8765c0ad2e5045b76714d042e662ac8b47b95000667ce4f4881 was downloaded.
H2 A06/A11/A15 passed actual Accepted + selected F1-F4 at 13/14/13 iterations.
A03 stayed RejectedPhysical; all A05 modes preserved bit-identical F1-F4.
All 20 Windows/Linux selected F1-F4 property trees matched. Some Atan2 display
angles differ at libm rounding scale; cross-platform full-candidate bit identity
is not claimed.

Independent PR #634 artifact verification recalculated 650 research hashes over
4,504,500 fields, 192 production and 192 mirror hashes and directly compared
14,976 production/mirror fields. Reproduce with
`python tools/verify-bc-aud-006-artifact.py <downloaded-parity-artifact.zip>`.

## Broader experiment: blanket H2 is not an admissible production migration

The first local expanded run used 105 fixtures (20 real Series A definitions,
5 actual historical definitions, 72 explicit depth/ratio/current combinations,
3 signed weights, 3 small line elements and 2 vertical-current variants), each
with four twice-replayed modes. B0 had 67 Accepted, 17 BudgetExhausted,
12 RejectedPhysical, 6 RejectedNumerical, 3 Indeterminate.
Blanket H2 had 72 Accepted but **lost 9 previously Accepted cases** and changed
selected F1-F4 property trees in 52 old Accepted cases. These failures/differences
must remain visible; no frozen baseline is changed.

A separate scope issue exists below the historical 0.0001 m tangent-denominator
floor: a direct unit tangent is no longer merely an arithmetic reformulation of
Delta/Max(0.0001,ds). Any recovery candidate must preserve this floor behavior or
explicitly exclude those elements. No physical-model change is authorized here.

Next hypothesis: enable H2 only after a directly compared raw-bit period-2 cycle
has proved that the original deterministic map cannot reach its fixed point.
Continue within the original total 64-step budget and still require the actual
production fixed-point and Accepted-shape gates. Do not accept an orbit member.
This is a new validation hypothesis, not an approved production fix.

## Exact period-2-triggered H2R experiment

H2R starts with the original map. It compares the complete changing feedback
state directly as ordered raw IEEE-754 fields (including total force, all
boundary states, segment forces, trace scalars and geometry nodes). Only after
`next == two_states_back` and `next != current` in raw bits does it enable direct
tangent arithmetic for subsequent iterations. The observed orbit state is never
accepted. The original exact-fixed-point and Accepted-shape gates remain the
only way to return Accepted. Total feedback steps remain at most 64. Any
segment shorter than 0.0001 m disables recovery, preserving the original floor.
Normal public Build and observer paths still disable H2R in this research PR.

The local 105-fixture H2R run produced 81 Accepted, 3 BudgetExhausted,
12 RejectedPhysical, 6 RejectedNumerical and 3 Indeterminate. Every one of the
67 old Accepted candidate trees and F1-F4 trees remained bit-identical. All 12
physical rejections remained rejected; all numerical/indeterminate controls
retained their status. A06/A11/A15 passed real acceptance at 16/17/17 iterations.
The other 3 budget failures remain non-accepted; no outcome is padded or forced.

The proof additionally observes actual consecutive states at the production
fixed-point gate and compares their complete raw state vectors. Candidate replay
alone is not substituted for this fixed-point verification.

The earlier 37144625 head failed a lexical architecture guard after the snapshot
adapter changed `var signedCandidate = Build(...)` into a nullable diagnostic
handoff. This was not a numerical test failure. The guards now explicitly require
the public path to pass null, keep the original evaluator as default, preserve
construction order, and reject every production caller of the internal handoff.
No physical check, baseline or regression expectation was removed.

For A03/A05/A06/A11/A15, emitted `*-fixed-point.csv` files contain both actual
consecutive raw numeric state vectors in the exact order of
`CaptureCycleStateBits`. Independent inspection must compare every row directly.
The 105-fixture summary retains candidate/authority SHA256 and their actual
production-reference SHA256, plus measured geometry and force residuals.
Synthetic detector controls cover absent history, fixed/drifting states,
one-ULP differences, schema lengths and distinct signed zero; these are numerical
unit controls and are not physical experiments.

The four required PR workflows explicitly check out `pull_request.head.sha`
(push/dispatch retain `github.sha`). The classic Windows Build status and
calculation artifacts also use that tested SHA. This removes the earlier
synthetic-merge-ref ambiguity; a prior green merge-tree run cannot authorize a
different head. Main-push release checks keep their existing source identity.
