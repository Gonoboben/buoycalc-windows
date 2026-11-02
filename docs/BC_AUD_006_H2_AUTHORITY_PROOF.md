# BC-AUD-006 H2 authority proof (validation only)

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
