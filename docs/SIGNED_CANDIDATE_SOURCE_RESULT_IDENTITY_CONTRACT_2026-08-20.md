# Signed-Candidate Source / Result Identity Contract

Date: 2026-08-20  
RFC: #497  
Package: D  
Status: contract only; no runtime implementation and no selected-shape authority switch

## Purpose

Define the smallest calculation-core representation required to carry a future signed candidate truthfully through acceptance and later shadow arbitration.

This package follows the Package C acceptance contract. It does **not** implement candidate calculation, selection, persistence, report/UI fields, or any production authority change.

The central rule is:

```text
candidate state is a calculation-core fact
selected source is a calculation-core arbitration fact
read models only project those facts
```

No read-model, report, PDF, 2D or UI layer may infer candidate status or source identity from coordinates, strings, stop reasons or visual behavior.

## 1. Existing boundaries that must remain distinct

Current production contains three different concepts that must not be collapsed:

1. `MooringShapeResult` — geometry/result payload;
2. `MooringPrimaryShapeSelectionResult` — current selector/gate result using the existing fallback/iterative path;
3. `SelectedShapeReadModel` — application/read-model projection consumed by user-facing paths.

The future signed candidate must enter **before** `SelectedShapeReadModel` is built.

Package D therefore forbids implementing signed state by merely adding experimental fields to:

```text
SelectedShapeReadModel
CalculationSnapshot presentation fields
TechnicalReportData only
PDF/report DTOs
JSON persistence
UI ViewModels
```

Those layers may later project an already-owned core result, but they must not become the owner of candidate acceptance or source identity.

## 2. Minimal typed source identity

A future calculation-core arbitration result needs typed source identity rather than a free-form display string.

Conceptual contract:

```csharp
public enum MooringShapeSourceIdentity
{
    FallbackShapeSolver,
    IterativeDiscreteSolver,
    SignedBoundaryFeedback
}
```

Exact implementation names may differ, but the semantic distinctions may not.

### `FallbackShapeSolver`

Current/fallback geometry produced by `MooringShapeSolver` and selected because no higher-priority production candidate is truthfully eligible.

### `IterativeDiscreteSolver`

Current iterative/discrete candidate path when it passes the existing primary gate and is actually selected.

### `SignedBoundaryFeedback`

Future accepted signed equilibrium candidate produced from the signed boundary/tension/feedback path governed by RFC #497.

A display `Source` string may still be generated later for reports, but it must be derived from this typed identity rather than becoming the authority itself.

## 3. Minimal signed candidate status

Package C semantic states remain authoritative. A future core type must carry them explicitly:

```csharp
public enum MooringSignedCandidateStatus
{
    Accepted,
    RejectedPhysical,
    RejectedNumerical,
    BudgetExhausted,
    Indeterminate,
    Unavailable
}
```

The enum represents candidate outcome, not selected-source outcome.

Therefore:

```text
SignedCandidateStatus = Accepted
```

means the signed candidate is eligible for later arbitration; it does **not** mean the signed candidate is already the selected production source.

## 4. Minimal signed candidate result

The smallest future calculation-core result should conceptually carry:

```csharp
public sealed record MooringSignedCandidateResult(
    MooringShapeSourceIdentity SourceIdentity,
    MooringSignedCandidateStatus Status,
    MooringShapeResult? Shape,
    MooringSurfaceBoundaryInfoResult? Boundary,
    bool ExactFixedPointReached,
    int FeedbackIterations,
    bool ContainsDiscreteLoads,
    int PointLoadCrossings,
    string DiagnosticCode,
    string DiagnosticText);
```

Exact field/type names may differ. The semantic content is the contract.

No duplicate scalar fields should be added when an existing core object already owns the value. In particular:

- signed X/Z nodes and endpoint geometry belong in `Shape`;
- unique Q0 and boundary classification belong in `Boundary`;
- boundary root validity remains owned by `MooringSurfaceBoundaryInfoAnalyzer`;
- fixed-point acceptance facts belong to the signed candidate result;
- point-load presence/crossing facts belong to the signed candidate result until Package E defines selected-source discrete-load semantics.

## 5. Why `Shape` is nullable

A non-accepted candidate can still carry partial diagnostic state, but no contract requires every failure state to synthesize geometry.

Rules:

```text
Accepted
  -> Shape must exist and contain at least two ordered finite nodes

RejectedPhysical / RejectedNumerical / BudgetExhausted / Indeterminate
  -> Shape may exist for diagnostics but is not selection-eligible

Unavailable
  -> Shape normally absent
```

Consumers must never use `Shape != null` as a synonym for `Accepted`.

The only eligibility test is the typed status plus later arbitration rules.

## 6. Boundary/Q0 identity

For `Accepted`, the future result must preserve the exact boundary state that owns the accepted geometry update.

Required truth:

```text
Boundary != null
Boundary.Solved = true
Boundary.SolutionState != null
Boundary.Q0N is finite and uniquely defined
boundary classification is compatible with a unique signed candidate state
```

The candidate layer must not copy Q0 into an unrelated DTO and discard its governing boundary classification.

For states where a unique force/Q0 state cannot be defined, `Status` must not be `Accepted`.

The canonical exact vertical zero-horizontal case therefore remains representable as `Indeterminate` even though its straight-vertical geometry is unique.

## 7. Fixed-point facts

Package C defines exact deterministic fixed-point acceptance with a production feedback budget of 64.

The signed result must carry enough truth to distinguish:

```text
Accepted
  ExactFixedPointReached = true
  FeedbackIterations <= 64

BudgetExhausted
  ExactFixedPointReached = false
  FeedbackIterations = 64
```

`FeedbackIterations` is the count of completed deterministic feedback updates, not a UI/report counter reconstructed later.

The result does not need to persist every intermediate trajectory in production. Extended trajectory history remains validation/diagnostic evidence unless a later debugging contract explicitly requires it.

## 8. Diagnostics identity

Status and diagnostic reason must remain separate.

The status gives the machine-semantic outcome. The diagnostic gives the specific core reason.

Conceptual examples:

```text
Status = RejectedPhysical
DiagnosticCode = "TautNonZeroHorizontalLoadNoFiniteRoot"

Status = RejectedNumerical
DiagnosticCode = "CandidateTraceUnavailable"

Status = BudgetExhausted
DiagnosticCode = "ExactFixedPointNotReachedWithinBudget"

Status = Indeterminate
DiagnosticCode = "VerticalGeometryUniqueForceStateNonUnique"
```

`DiagnosticCode` should be stable/machine-oriented where practical. `DiagnosticText` may be localized or human-oriented later.

No renderer may infer a different status from `DiagnosticText`.

## 9. Candidate source identity is not selected source identity

Every `MooringSignedCandidateResult` created by this RFC path has:

```text
SourceIdentity = SignedBoundaryFeedback
```

But until a later arbitration package selects it, the **selected production source** may remain:

```text
FallbackShapeSolver
or
IterativeDiscreteSolver
```

Thus two truths can coexist without contradiction:

```text
Signed candidate:
  Status = Accepted
  SourceIdentity = SignedBoundaryFeedback

Current selected production result:
  SourceIdentity = IterativeDiscreteSolver
```

Package F shadow arbitration must test exactly this separation before any production switch.

## 10. Future selected-core result

When production arbitration is eventually implemented, the selected result needs truthful typed source metadata at calculation/application boundary before read-model projection.

Conceptually the smallest selected-core representation is:

```csharp
public sealed record MooringSelectedShapeResult(
    MooringShapeResult Shape,
    MooringShapeSourceIdentity SourceIdentity,
    bool SelectedConverged,
    bool SelectedUsesDiscreteLoads,
    string MethodNote);
```

This is a future integration contract, not a Package D runtime change.

Rules:

- `Shape` must come from exactly the source named by `SourceIdentity`;
- `SelectedConverged` describes the actually selected source only;
- `SelectedUsesDiscreteLoads` describes the actually selected source only and remains blocked on Package E for the signed path;
- signed candidate status may be retained alongside the selection for diagnostics, but a rejected candidate must not contaminate selected-source convergence facts.

## 11. Relationship to the current selector/read model

Current code still uses:

```text
MooringPrimaryShapeSelectionResult.Source       // string
MooringPrimaryShapeSelectionResult.UsesDiscreteLoads
SelectedShapeReadModel.Source                   // string projection
SelectedShapeReadModel.UsesDiscreteLoads
```

Package D does not change those types.

A later integration may introduce typed source identity inside calculation/application arbitration and then derive the existing read-model string from it while preserving user-visible behavior.

Do not first add `SignedCandidateStatus` to `SelectedShapeReadModel` and then work backward into the core. The dependency direction must remain core -> application/read model -> presentation.

## 12. Discrete-load fields remain candidate facts, not final selected semantics

For a signed candidate, Package D may carry:

```text
ContainsDiscreteLoads
PointLoadCrossings
```

because those are facts about the candidate trace/result and are required to prevent loss of identity before Package E.

However Package D deliberately does **not** define:

```text
SelectedUsesDiscreteLoads = ContainsDiscreteLoads
```

That equivalence is not yet authorized.

Package E must define what selected discrete-load semantics mean, including connector/payload point-load crossings and closure ownership.

## 13. Hard invariants for an `Accepted` result

A future runtime implementation must reject construction or acceptance of an internally contradictory `Accepted` result.

At minimum `Accepted` implies:

```text
SourceIdentity == SignedBoundaryFeedback
Shape != null
Shape.Nodes.Count >= 2
all required node/shape values finite
Boundary != null
Boundary.Solved == true
Boundary.SolutionState != null
Boundary.Q0N is finite and unique
ExactFixedPointReached == true
1 <= FeedbackIterations <= 64
PointLoadCrossings >= 0
candidate hard invariants from Package C passed
```

If `ContainsDiscreteLoads == false`, `PointLoadCrossings` must be zero.

If `ContainsDiscreteLoads == true`, the point-load crossing count and closure must be validated by Package E semantics before future production selection.

## 14. Hard invariants for non-accepted results

Non-accepted results must never masquerade as selection-ready geometry.

```text
Status != Accepted
  -> candidate is not eligible for signed primary authority
```

Additional expectations:

- `BudgetExhausted` cannot have `ExactFixedPointReached=true`;
- `RejectedPhysical` must identify a physical/model classification, not a generic timeout;
- `RejectedNumerical` must identify the violated numerical/core invariant;
- `Indeterminate` must not invent a unique Q0 or force state;
- `Unavailable` must not synthesize coordinates merely to satisfy a schema.

## 15. No JSON/persistence expansion in Package D

This contract describes in-memory calculation truth.

Package D does not authorize adding signed-candidate fields to project JSON or persistence formats.

Reason:

- candidate state is recomputable from engineering inputs;
- persisting experimental calculated state would create migration/versioning obligations before production authority is established;
- RFC #497 explicitly forbids adding DTO/JSON fields merely to carry an experiment.

Any future persistence of calculated candidate state requires a separate material format decision.

## 16. No downstream force authority transfer

`MooringSignedCandidateResult` may carry boundary/Q0 and geometry identity needed to describe the candidate itself.

It does **not** automatically replace current owners of:

```text
scalar TensionKn
anchor reserve
legacy EstimatedOffsetM
weak-link/verdict
other downstream force summaries
```

Those ownership boundaries remain as established by RFC #487 Package E.

A future signed geometry selection may therefore temporarily require explicit mixed-authority diagnostics unless downstream signed force authority is separately validated.

Package D must not hide that distinction inside one generic `Result` label.

## 17. Shadow-arbitration inputs for Package F

After Package E is complete, Package F validation-only arbitration should be able to operate using only:

```text
current selected result
signed candidate result
```

Expected shadow rules:

```text
signed.Status == Accepted
  -> signed candidate may be considered by arbitration

signed.Status != Accepted
  -> current selected authority remains unchanged
```

The shadow test must verify source identity, `SelectedConverged`, discrete-load truth and fallback preservation without calling report/UI logic.

## 18. Frozen production state

```text
SignedCandidateRuntimeTypeImplemented = False
TypedSelectedSourceImplemented         = False
SelectedAuthoritySwitch               = False
SelectedXorZChanged                    = False
GoldenBaselineModified                = False
ProjectJsonChanged                     = False
ReadModelSchemaChanged                 = False
DownstreamAuthoritySwitch             = False
```

## 19. Exit criterion

Package D is complete when this contract is merged with the required checks green.

Next before any production selected-X/Z switch:

1. Package E — define and validate signed discrete-load semantics;
2. Package F — validation-only shadow arbitration using the accepted/rejected identity contract;
3. only after all RFC #497 gates pass, consider a separate production authority implementation PR.
