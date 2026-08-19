# Signed-Candidate Production Acceptance Contract

Date: 2026-08-20  
RFC: #497  
Package: C  
Status: contract only; no runtime implementation and no selected-shape authority switch

## Purpose

Define deterministic production candidate states and termination ownership **before** any signed candidate is allowed to participate in production selected-shape arbitration.

This contract is intentionally conservative. Package A2 showed that the two currently measurable canonical candidates reach an exact emitted floating-point fixed point by iteration 16 and remain unchanged through 1024. Package B showed that an exact feedback fixed point can still retain a small depth residual because feedback stability, boundary-root closure and continuous-reference error are different numerical properties.

No new numerical tolerance is introduced here.

## 1. Governing principle

A signed candidate is not accepted merely because:

```text
BoundaryInfo.Available = true
BoundaryInfo.Solved    = true
Classification         = SolvedByBoundedBisection
CandidateAvailable     = true
BudgetReached          = true
```

Production acceptance requires a valid boundary state **and** a deterministic feedback fixed point **and** all candidate-owned hard invariants.

The first production acceptance contract deliberately uses an **exact fixed-point rule** rather than inventing a new epsilon from the observed A2 numbers. If a candidate does not reach that conservative rule within the fixed budget, current production authority remains in force.

## 2. Candidate states

The calculation core must be able to represent at least these semantic states. Exact enum/type names may differ in implementation, but the meanings may not.

### `Accepted`

A unique signed candidate exists, its boundary state is solved by the existing boundary analyzer, the coupled feedback state reaches an exact deterministic fixed point within the production budget, and all signed hard invariants pass.

Only this state may later be eligible for signed selected-shape arbitration.

### `RejectedPhysical`

The current physical/model assumptions admit no signed production equilibrium candidate for the requested state.

Examples include already-established physical classifications such as exact taut `L=depth` with non-zero horizontal load under the current inextensible/no-stretch model, or another explicitly classified physical/capacity impossibility.

This state is not a numerical convergence failure.

### `RejectedNumerical`

A candidate calculation encounters an invalid numerical/core state that violates an existing hard invariant, for example:

```text
non-finite candidate-owned values
invalid trace/segment mapping
negative-dz geometry where the signed contract forbids it
invalid segment force state
point-load closure/ownership violation
candidate trace unavailable after a solved state
```

This state must preserve an explicit core diagnostic reason.

### `BudgetExhausted`

The candidate remains physically/numerically valid but has not reached the exact fixed-point acceptance rule when the fixed production feedback budget is exhausted.

`BudgetExhausted` is **not** convergence and must not set future signed `SelectedConverged=true`.

### `Indeterminate`

The model can describe some geometry/state information but cannot truthfully produce one unique accepted signed force/equilibrium state.

The canonical exact vertical `L=depth, H=0` case with unique straight-vertical geometry but a non-unique admissible Q0/vertical-force family belongs to this semantic family unless a later physics/product contract supplies a uniquely owned force state.

### `Unavailable`

Required candidate inputs/state are absent or the signed candidate path cannot be formed. This is distinct from physical rejection and from numerical failure.

## 3. Fixed production feedback budget

The first production contract fixes:

```text
MaximumFeedbackIterations = 64
```

This is not a convergence tolerance.

Rationale:

- 64 is the already-established historical feedback budget used before RFC #497;
- Package A1 pre-registered it as the first required extended horizon;
- Package A2 showed both current measurable canonical candidates reach an exact fixed point by iteration 16 and remain unchanged through 64, 128, 256, 512 and 1024;
- using 64 therefore stays inside the already-validated envelope while providing a conservative margin beyond the observed fixed-point iteration;
- candidates that need more than 64 are not silently accepted: they become `BudgetExhausted` and current production authority remains available.

Changing this budget later requires separate evidence/contract review, not an ad hoc runtime tweak.

## 4. Boundary validity gate

Every feedback iteration must use the existing `MooringSurfaceBoundaryInfoAnalyzer` result as the owner of frozen-load boundary feasibility/root closure.

An iteration is eligible to continue toward `Accepted` only when the boundary result truthfully supplies a unique solved state required by the candidate path, including:

```text
Solved = true
SolutionState != null
Q0N has a unique finite value
classification is a solved boundary classification compatible with a unique candidate state
```

The candidate layer must not duplicate or reinterpret the analyzer's internal depth-root tolerance.

Current analyzer ownership remains:

```text
DepthToleranceM = 0.01 m
MaxRootIterations = 80
```

Package C does **not** create a second 0.01 m test and does not redefine 0.01 m as the feedback-convergence tolerance. It relies on `BoundaryInfo.Solved` for the boundary-root contract.

A vertical unique-geometry/non-unique-force-family state is not promoted merely because its geometric depth is exact; it lacks the unique force/Q0 state required for a signed equilibrium candidate.

## 5. Exact feedback fixed-point rule

No new epsilon is introduced for feedback-state stabilization.

A signed candidate reaches the Package C production fixed point only when one complete deterministic feedback update produces no change in the candidate-owned state required for the next update.

Implementation must compare the actual candidate-owned state, not only a display/report summary. At minimum equivalence must imply all of the following existing A2 deltas are exactly zero:

```text
DeltaQ0N                 == 0
DeltaXM                  == 0
DeltaZM                  == 0
DeltaLineForceN          == 0
MaxSegmentForceDeltaN    == 0
MaxNodeDeltaM            == 0
```

and the following structural/invariant facts remain unchanged and valid:

```text
boundary classification / unique solved-state availability
segment count and segment identity
node count and node ordering
point-load crossing ownership
candidate discrete-load state
finite-state validity
negative-dz hard invariant
```

If the implementation carries additional candidate-owned values that can change the next deterministic iteration, those values are part of fixed-point equality even if they are not listed above.

### Why exact equality is used initially

The goal is not to claim that exact equality is the universally optimal numerical convergence policy. The goal is to introduce **no new fitted tolerance** before broader evidence exists.

For the currently measured canonical candidates:

```text
all recorded successive-state deltas == 0 by iteration 16
and remain == 0 through iteration 1024
```

Thus the conservative exact rule accepts the two evidence-backed candidates without weakening the numerical contract, while any future case that only approaches but never exactly repeats falls back safely as `BudgetExhausted` rather than being accepted by an unvalidated epsilon.

A later RFC may replace or supplement exact equality with an independently justified numerical convergence tolerance. That must be a separate reviewed change.

## 6. Candidate hard invariants

`Accepted` additionally requires all applicable existing core/validation hard invariants to remain satisfied at the fixed point.

At minimum:

```text
all required values finite
no invalid/non-positive segment lengths
signed trace available
trace/segment/node mappings consistent
no prohibited negative-dz segment
point-load crossings deterministic and correctly owned
point-load jump closure satisfies its existing core contract
boundary result remains a unique solved state
candidate state does not change after the fixed-point update
```

Package C does not invent a new point-load tolerance. Existing trace/force closure ownership remains authoritative.

## 7. Discrete-load boundary

For a candidate containing internal connector/payload point loads, `Accepted` does not yet by itself authorize future `SelectedUsesDiscreteLoads` semantics.

The candidate acceptance state may record that discrete loads are present and that the signed trace crosses/closes them consistently. The future selected-source meaning of `SelectedUsesDiscreteLoads` remains RFC #497 Package E and must be explicit before `discrete-payload` is actually selected in production.

Thus:

```text
candidate accepted with valid point-load closure
```

and

```text
selected result truthfully advertises discrete-load semantics
```

are separate gates.

## 8. Termination precedence

A future core implementation must terminate/report deterministically. Recommended semantic precedence:

1. required input/path unavailable -> `Unavailable`;
2. explicit physical/model impossibility -> `RejectedPhysical`;
3. invalid/non-finite/hard-invariant failure -> `RejectedNumerical`;
4. valid exact fixed point within budget -> `Accepted`;
5. valid state reaches iteration 64 without exact fixed point -> `BudgetExhausted`;
6. unique accepted force/equilibrium state cannot be truthfully defined despite partial geometry/state information -> `Indeterminate` where the governing boundary/model classification requires it.

Implementation may structure control flow differently, but it must not convert physical impossibility, force-state non-uniqueness or hard numerical failure into `BudgetExhausted` merely because an iteration loop exists.

## 9. `SelectedConverged` semantics

Package D of RFC #487 requires `SelectedConverged` to describe the actually selected source.

For a future signed source:

```text
SignedCandidateStatus == Accepted
    => signed-source convergence fact may be true

SignedCandidateStatus != Accepted
    => signed source must not claim convergence
```

However Package C does not change the production selector. While the existing iterative/fallback source remains selected, its existing selected-source convergence semantics remain authoritative.

Do not publish a signed candidate's `Accepted` fact into `SelectedConverged` unless that signed candidate is actually the selected source.

## 10. Diagnostics ownership

Candidate status and reason are calculation-core facts.

Expected diagnostic intent:

```text
Accepted          -> candidate is eligible for later arbitration; no failure diagnostic implied
BudgetExhausted   -> deterministic non-acceptance; INFO/WARN severity to be defined by integration contract
RejectedPhysical  -> truthful physical/model diagnostic
RejectedNumerical -> numerical/core failure diagnostic
Indeterminate     -> truthful non-unique/indeterminate-state diagnostic
Unavailable       -> missing/unavailable candidate diagnostic
```

Exact severity mapping remains a later integration detail, but report/UI/PDF layers must not infer status from coordinates or stop reasons.

## 11. Shadow-selection expectation

Before any production authority switch, validation-only shadow arbitration must prove:

```text
Accepted signed candidate
  -> is representable with truthful signed source/status metadata

BudgetExhausted / Rejected* / Indeterminate / Unavailable
  -> deterministically leaves existing production selected-shape authority in force

No read-model/report/UI layer
  -> performs candidate acceptance or fallback selection
```

Actual arbitration implementation remains a later package after source identity and discrete-load contracts are closed.

## 12. Canonical evidence under this contract

Without changing runtime, existing A2 evidence predicts:

### `uniform-current-slack-line`

- unique boundary solve available;
- exact feedback fixed point by iteration 16;
- all recorded successive-state deltas zero thereafter;
- finite state, `NegativeDz=0`, no internal point loads;
- boundary residual is inside the already-owned boundary solver contract.

Therefore this fixture is an evidence-backed **acceptance candidate** under Package C semantics, subject to future implementation and source/downstream gates.

### `discrete-payload`

- unique boundary solve available;
- exact feedback fixed point by iteration 16;
- all recorded successive-state deltas zero thereafter;
- finite state, `NegativeDz=0`;
- exactly two point-load crossings with zero emitted point-jump residual through the measured trajectory;
- boundary residual is inside the already-owned boundary solver contract.

Therefore this fixture is an evidence-backed **acceptance candidate** under Package C semantics, but production selection still additionally requires explicit Package E discrete-load semantics.

### Other three historical fixtures

No status changes are authorized:

- exact vertical zero-horizontal: unique geometry / non-unique force-state family -> not a unique accepted signed equilibrium candidate;
- `buoyant-line`: physically infeasible under current exact inextensible taut model with non-zero horizontal load;
- `depth-varying-current-profile`: same current-model physical infeasibility for the taut case.

## 13. Frozen production state

```text
RuntimeCandidateAcceptanceImplemented = False
SelectedAuthoritySwitch               = False
SelectedSourceSchemaChange            = False
GoldenBaselineModified                = False
DownstreamAuthoritySwitch             = False
NewFeedbackConvergenceTolerance       = None
ProductionFeedbackBudgetContract      = 64
BoundaryDepthToleranceOwnership       = existing analyzer (0.01 m)
```

## 14. Exit criterion

Package C contract is complete when this document is merged with required CI green.

Next packages before any selected-X/Z authority switch:

1. Package D — define the smallest core signed candidate/result/source representation that carries status and fixed-point facts truthfully;
2. Package E — define/validate discrete-load semantics for the signed selected source;
3. Package F — validation-only shadow arbitration for accepted versus non-accepted candidate states;
4. only then consider a separate production authority implementation PR.
