# Signed-Candidate Shadow Arbitration Evidence

Date: 2026-08-20  
RFC: #497  
Package: F  
Status: validation-only shadow arbitration; production selected-shape authority remains unchanged

## Purpose

Close the final RFC #497 gate by exercising the Packages C–E contracts together without adding a production selector or changing selected X/Z.

Package F asks one narrow question:

```text
If the signed candidate contract were presented to an arbitrator today,
would Accepted and non-Accepted states produce truthful selected-source metadata
without changing current production state?
```

The answer is measured in validation only.

## 1. No runtime selector is introduced

The shadow selector exists only inside:

```text
SignedCandidateShadowArbitrationRegression
```

It does not modify:

- `MooringPrimaryShapeSelector`;
- `MooringPrimaryShapeSelectionResult`;
- `SelectedMooringShapeProvider`;
- `SelectedShapeReadModel`;
- `CalculationSnapshot`;
- report/PDF/2D/UI consumers;
- project JSON;
- engineering golden baseline.

After every shadow decision, the regression captures the real `run.Snapshot.SelectedShape` again and requires its source, convergence, discrete-load flag, X, Z and node count to remain exactly unchanged.

## 2. Inputs reused from earlier packages

Package F deliberately does not implement a second signed solver.

It reuses:

1. canonical historical scenario definitions from `HistoricalGoldenImpactRegression`;
2. its existing 64-step candidate geometry path;
3. `BoundaryConditionedFeedbackCouplingRegression.RunBudget` for exact fixed-point facts;
4. production boundary classifications;
5. Package E point-load/crossing semantics;
6. the current production `SelectedShapeReadModel` as the authority that must remain untouched.

## 3. Shadow candidate states

The Package C state meanings are applied to the five canonical historical fixtures.

### Accepted

A shadow candidate is `Accepted` only when the already-established Package C conditions are observed at the fixed 64-step production budget:

```text
boundary solved with unique usable state
trace available
LastDeltaX = 0
LastDeltaZ = 0
LastDeltaQ0 = 0
LastMaxNodeDelta = 0
LastDeltaLineForce = 0
LastMaxSegmentForceDelta = 0
no negative-dz segments
point-load closure satisfies the existing validation identity contract
```

No new epsilon is introduced.

### Indeterminate

`vertical-zero-current` remains `Indeterminate` because the straight vertical geometry is available as a geometric family limit, but the core classification is:

```text
VerticalGeometryUniqueForceStateFamily
```

The geometry does not define one unique signed Q0/force state, so the signed candidate cannot become production authority under Package C.

### RejectedPhysical

`buoyant-line` and `depth-varying-current-profile` remain `RejectedPhysical` because both canonical fixtures are exactly taut (`line length == depth`) while carrying non-zero horizontal load. Production boundary classification is:

```text
TautNonZeroHorizontalLoadNoFiniteRoot
```

The existing feasibility regression independently identifies these fixtures as physically infeasible under the current exact inextensible model. Package F does not relax that model.

## 4. Five-scenario truth table

Expected/validated shadow behavior:

| Scenario | Signed status | Shadow selected source | Shadow `SelectedConverged` | Shadow `SelectedUsesDiscreteLoads` | Production runtime |
|---|---|---|---:|---:|---|
| `uniform-current-slack-line` | `Accepted` | `SignedBoundaryFeedback` | `true` | `false` | unchanged |
| `discrete-payload` | `Accepted` | `SignedBoundaryFeedback` | `true` | `true` | unchanged |
| `vertical-zero-current` | `Indeterminate` | current production source | current selected shape convergence | current selected flag | unchanged |
| `buoyant-line` | `RejectedPhysical` | current production source | current selected shape convergence | current selected flag | unchanged |
| `depth-varying-current-profile` | `RejectedPhysical` | current production source | current selected shape convergence | current selected flag | unchanged |

This table is a shadow decision table only. The production source displayed by the application does not change in Package F.

## 5. Accepted-source geometry identity

For the two Accepted fixtures, Package F requires the signed geometry measured by `HistoricalGoldenImpactRegression.RunCandidate` at 64 steps to match the endpoint X/Z emitted by the Package C fixed-point measurement path at the same budget exactly.

It also requires the candidate node count to remain:

```text
segment count + 1
```

This prevents a shadow source label from being detached from the geometry it claims to select.

## 6. `SelectedConverged` shadow semantics

When the shadow-selected source is `SignedBoundaryFeedback`:

```text
SelectedConverged = true
```

only because the candidate is `Accepted` under the exact fixed-point contract.

When the signed candidate is not Accepted:

```text
shadow SelectedConverged = current production selected Shape.Converged
```

The rejected/indeterminate signed candidate cannot overwrite convergence truth for the current selected source.

No production `SelectedConverged` field is added by this package.

## 7. `SelectedUsesDiscreteLoads` shadow semantics

Package E is applied directly:

### `uniform-current-slack-line`

```text
signed internal point loads = 0
shadow SelectedUsesDiscreteLoads = false
```

### `discrete-payload`

```text
signed internal point loads = 2
Shackle + Payload at s=30 m
shadow SelectedUsesDiscreteLoads = true
```

For every non-Accepted signed candidate, the shadow result copies the current production selected flag unchanged.

Thus a candidate's assembly/discrete-load facts cannot contaminate the actually selected source.

## 8. Current production source is preserved for non-Accepted candidates

For `Indeterminate` and `RejectedPhysical`, the shadow selection copies all of these from the current `SelectedShapeReadModel`:

```text
Source
Shape.Converged
UsesDiscreteLoads
Shape.HorizontalOffsetM
Shape.AnchorPoint.ZDepthM
Shape.Nodes.Count
```

The regression requires exact equality.

This is the fallback-preservation contract needed before any production authority switch can be considered.

## 9. Production state mutation guard

The regression captures production selection before shadow arbitration and after it.

Required exact invariants:

```text
Source_before == Source_after
Converged_before == Converged_after
UsesDiscreteLoads_before == UsesDiscreteLoads_after
X_before == X_after
Z_before == Z_after
NodeCount_before == NodeCount_after
```

This is intentionally redundant with the validation-only implementation boundary: it ensures Package F cannot silently become a runtime integration through a future refactor.

## 10. What Package F proves

If the regression is green on the exact final head, the RFC has evidence that:

1. the two measurable signed candidates satisfy the conservative exact fixed-point acceptance rule;
2. one vertical fixture remains indeterminate rather than receiving an invented Q0;
3. two taut/non-zero-horizontal fixtures remain physical rejections;
4. accepted signed source metadata can represent source/convergence/discrete-load truth consistently in shadow mode;
5. non-Accepted candidates preserve current selected authority exactly;
6. production selected state is not modified by the shadow test.

## 11. What Package F does not prove

Package F does **not** authorize or prove:

- production source switching;
- report/PDF/2D/UI changes;
- golden-baseline migration;
- downstream signed tension/anchor/verdict authority transfer;
- elasticity;
- extensible taut-line physics;
- a replacement for the current fallback/iterative solver path;
- persistence of candidate state.

It also does not make the two physically blocked taut/nonzero-horizontal historical fixtures solvable.

## 12. RFC #497 completion meaning

Packages A–F together establish a bounded acceptance/arbitration contract and validation evidence.

If Package F merges green, the next step is **not** an implicit switch inside the RFC branch.

The next permissible step is a separate production implementation issue/PR that must:

1. implement the typed core result/source contract from Package D;
2. implement the exact Package C acceptance rule with fixed budget 64;
3. preserve Package E selected discrete-load semantics;
4. reproduce the Package F truth table in runtime integration tests;
5. explicitly identify which downstream quantities remain under legacy authority;
6. make any selected X/Z/source output change reviewable as its own behavior change;
7. update golden baselines only in a separately reviewed migration step after the authority implementation is accepted.

## 13. Frozen production state

```text
ProductionSignedSelectorImplemented    = False
SelectedAuthoritySwitch               = False
SelectedXorZChanged                    = False
SelectedSourceChanged                 = False
SelectedShapeReadModelChanged         = False
GoldenBaselineModified                = False
ProjectJsonChanged                     = False
DownstreamAuthoritySwitch             = False
NewConvergenceTolerance                = None
Elasticity                             = False
3D                                     = False
```

## 14. Exit criterion

Package F is complete when:

- `SignedCandidateShadowArbitrationRegression` passes in the full engineering validation suite;
- the five-scenario truth table has counts `Accepted=2`, `RejectedPhysical=2`, `Indeterminate=1`;
- production selected state mutation is false for all five scenarios;
- all required repository checks are green on the exact final head;
- final PR diff contains validation/docs only.

After that, RFC #497 can be closed as a completed pre-production acceptance/arbitration RFC. Production authority remains unchanged until a new, explicitly scoped implementation issue/PR is reviewed and merged.
