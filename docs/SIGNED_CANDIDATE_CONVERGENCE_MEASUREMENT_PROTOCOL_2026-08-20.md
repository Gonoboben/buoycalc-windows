# Signed-Candidate Convergence Measurement Protocol

Date: 2026-08-20  
RFC: #497  
Package: A1  
Status: validation measurement protocol only; no production acceptance criterion

## Purpose

Pre-register the convergence-trajectory measurement plan for the two canonical fixtures that currently produce a boundary-conditioned signed validation candidate:

```text
uniform-current-slack-line
discrete-payload
```

Package F of RFC #487 established that both candidates remain validation-only because the historical feedback audit terminates at the fixed 64-iteration budget with `Stop=BudgetReached`. This document fixes the next measurement budgets and observables **before** extended results are inspected, so no convergence threshold can be selected after seeing a desired answer.

This package does not change the solver, feedback equations, selected X/Z, selected source, downstream force/verdict authority, golden baseline, DTOs, UI, report, PDF, 2D, elasticity, or 3D behavior.

## 1. Existing validation path is authoritative for this measurement

The extended experiment must reuse the existing validation feedback implementation in `BoundaryConditionedFeedbackCouplingRegression` / the same core builders and analyzers it invokes.

Do not create an alternative force/geometry solver merely to obtain a second-looking trajectory.

The production application remains untouched. The experiment may expose additional validation-only accessors/helpers if needed, but they must not become application/runtime authority.

## 2. Predeclared budgets

The extended trajectory budgets are fixed as:

```text
64
128
256
512
1024
```

The sequence intentionally continues the established power-of-two budget family used by existing feedback validation (`4, 8, 16, 32, 64`).

Every requested budget is a measurement horizon, not a stopping criterion. The extended validation must run to each fixed budget unless the existing feedback path reaches a deterministic terminal state such as unavailable boundary/trace, invalid/non-finite state, or another already-defined hard failure.

No new numerical convergence tolerance is introduced in Package A1/A2.

## 3. Fixtures

Only the two currently measurable historical candidates are in scope:

### `uniform-current-slack-line`

Package F 64-step evidence:

```text
InitialClass = SolvedByBoundedBisection
Iterations   = 64
Stop         = BudgetReached
Q0N          = 379.810165863037
Endpoint X   = 22.073605655669077 m
Endpoint Z   = 50.0007670051935 m
```

### `discrete-payload`

Package F 64-step evidence:

```text
InitialClass = SolvedByBoundedBisection
Iterations   = 64
Stop         = BudgetReached
Q0N          = 720.8641923522947
Endpoint X   = 19.583341922076137 m
Endpoint Z   = 49.99914589480685 m
PointLoads   = 2
```

The exact vertical force-family case and the two physically infeasible taut/no-root historical fixtures are not extended feedback candidates and are excluded from this trajectory experiment.

## 4. Required observables at every budget

For each fixture and each predeclared budget, record at minimum:

```text
Budget
Iterations actually executed
StopReason
boundary Classification
Q0N
EndpointXM
EndpointZM
DepthResidualM
LineForceN
last DeltaLineForceN
last MaxSegmentForceDeltaN
last DeltaXM
last DeltaZM
last DeltaQ0N
last MaxNodeDeltaM
NegativeDzSegmentCount
PointLoadCrossings
MaxPointJumpResidualN
```

All values must come from the existing validation/core calculation state. No report/UI reconstruction is allowed.

## 5. Trajectory samples within the longest run

For the 1024-step horizon, retain deterministic trajectory samples sufficient to distinguish broad behavior without inventing a convergence threshold.

At minimum emit state at these iteration indices:

```text
1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024
```

For each sampled iteration record the same core quantities where available, especially:

```text
Q0N
Endpoint X/Z
Depth residual
LineForceN
DeltaLineForceN
MaxSegmentForceDeltaN
Delta X/Z
Delta Q0
MaxNodeDeltaM
classification
point-load closure / negative-dz diagnostics
```

The implementation may emit more raw iterations, but Package A2 must not discard the predeclared samples above.

## 6. No post-hoc convergence label

Package A2 is an evidence package. It must **not** declare `Accepted`, `Converged`, or a new production-ready state based solely on small-looking numbers.

The raw evidence should support a later Package B/C decision about whether the trajectory is contracting, plateauing, oscillating, drifting, divergent, or otherwise. If a numerical tolerance is eventually proposed, it belongs to a later docs/contract package and must be justified independently from these observed outputs.

`BudgetReached` remains exactly what it says: the requested measurement horizon was exhausted.

## 7. Deterministic hard checks allowed in A2

A2 may fail validation on objective state-integrity violations that do not require a new convergence tolerance, including:

```text
non-finite Q0 / coordinates / force state
negative or impossible segment-force values under existing contracts
node-count mismatch
trace/segment mapping mismatch
point-load crossing/closure contract violation
unexpected negative-dz geometry where the existing signed geometry contract prohibits it
candidate becoming unavailable for a reason not emitted as an explicit terminal state
```

Existing tolerances already owned by established trace/force contracts may continue to be used for those contracts. They must not be repurposed as a new feedback-convergence acceptance threshold.

## 8. Comparison rules

A2 must report both absolute state and successive-state deltas. It may additionally report dimensionless ratios such as:

```text
|delta at 2N| / |delta at N|
```

when both values are finite and the denominator is non-zero.

Such ratios are descriptive evidence only in A2. No ratio threshold is authorized as a production stopping rule.

## 9. Golden and production authority remain frozen

During A1/A2:

```text
GoldenBaselineModified       = False
SelectedAuthoritySwitch      = False
SelectedSourceSchemaChange   = False
DownstreamAuthoritySwitch    = False
ProductionConvergenceRule    = NotDefined
NewConvergenceTolerance      = None
```

The historical iterative/fallback selection path remains production authority.

## 10. A1 exit condition

A1 is complete when this protocol is merged with required CI green and no runtime change.

The next package is A2: implement the predeclared extended trajectory measurement in validation only, publish the exact results, and keep production/golden behavior unchanged.
