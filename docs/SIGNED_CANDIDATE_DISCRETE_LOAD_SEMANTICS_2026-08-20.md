# Signed-Candidate Discrete-Load Semantics

Date: 2026-08-20  
RFC: #497  
Package: E  
Status: contract + validation only; no production selected-shape switch

## Purpose

Define what discrete-load participation means for a future signed candidate and, later, for `SelectedUsesDiscreteLoads` when that signed candidate is actually selected.

Package E closes the ambiguity left intentionally by Packages C and D:

```text
assembly contains a connector/payload
```

is **not** by itself sufficient to claim:

```text
SelectedUsesDiscreteLoads = true
```

The selected flag must describe the actual calculation path that produced the selected geometry.

## 1. Existing core ownership

The signed boundary path already has one explicit point-load ownership chain:

```text
AssemblyItemInput
  -> CalculationResult.ElementRows
  -> MooringSequencePositioner
  -> internal discrete sequence points at coordinate s
  -> MooringSurfaceBoundaryIntegrationKernel
  -> MooringSurfaceBoundaryInfoResult / SolutionState
  -> MooringSurfaceBoundaryTensionTraceResult
  -> signed geometry / feedback update
```

Internal discrete points are sequence rows where:

```text
IsDiscrete = true
and the row is not the top buoy boundary
and the row is not the bottom anchor boundary
```

They include connector/payload elements. Buoy and anchor remain boundary-condition owners and do not make `ContainsDiscreteLoads` true by themselves.

## 2. Point-load force jump

For each internal point at line coordinate `s`, the shared signed integration kernel applies the local force jump:

```text
DeltaH = + point.CurrentForceN
DeltaV = - point.WeightWaterKg * g
```

with the project sign convention `+Z` downward.

The point is applied before the first segment whose start coordinate has reached/passed that point position within the existing line-position tolerance.

Multiple point loads at the same `s` remain distinct points and are all consumed. They must not be silently merged into one anonymous aggregate if that would lose element identity/crossing accounting.

## 3. Candidate-level `ContainsDiscreteLoads`

For the future `MooringSignedCandidateResult` from Package D:

```text
ContainsDiscreteLoads = true
```

means all of the following are true:

1. the candidate input sequence contains at least one internal discrete point;
2. the signed boundary/trace path used those internal points as local H/V force jumps;
3. the candidate trace reports the expected crossing count;
4. the point-jump closure check passes;
5. the resulting candidate geometry/feedback state is the state being described.

It does **not** mean merely that the project assembly contains any zero-length or non-line object.

For a candidate with no internal discrete points:

```text
ContainsDiscreteLoads = false
PointLoadCrossings = 0
```

## 4. Crossing identity

For any signed candidate state that carries a usable trace:

```text
PointLoadCrossings
```

must equal the number of internal discrete sequence points actually consumed by that trace.

The existing feedback validation already requires:

```text
trace.PointLoadCrossings == boundary.SolutionState.PointLoadCrossings
trace.PointLoadCrossings == internalPointCount
```

and requires the per-row crossing count to be monotone.

Thus crossings are a core trace identity fact, not a report/UI counter.

## 5. Point-jump closure

The existing `BoundaryConditionedFeedbackCouplingRegression` independently reconstructs the expected H/V jump from each internal sequence point and compares it with the actual jump visible in the signed trace.

Its established validation-only closure rule is:

```text
residual = sqrt(
    (actualDeltaH - expectedDeltaH)^2 +
    (actualDeltaV - expectedDeltaV)^2)

MaxPointJumpResidualN <= 1e-6 N
```

This `1e-6 N` value is an existing force-identity regression tolerance. Package E does not introduce or change it.

It is **not**:

- a feedback convergence tolerance;
- a boundary depth tolerance;
- a production candidate acceptance tolerance.

The canonical Package E measurements emit exact `0.0 N` point-jump residual for both target fixtures at the 64-step feedback horizon.

## 6. `SelectedUsesDiscreteLoads` semantic rule

For a future selected-core result, the flag describes the actually selected source only.

### Future signed source

If the actually selected source is `SignedBoundaryFeedback`, then:

```text
SelectedUsesDiscreteLoads = selectedSignedCandidate.ContainsDiscreteLoads
```

but only after:

- the signed candidate is `Accepted` under Package C;
- the candidate point-load ownership/crossing/closure facts satisfy this Package E contract;
- the signed candidate is actually chosen by production arbitration.

### Signed candidate exists but is not selected

If a signed candidate exists, even if accepted, but the current selected source remains fallback/iterative:

```text
SelectedUsesDiscreteLoads
```

continues to describe that current selected source. The unselected signed candidate must not overwrite the selected flag.

This selection separation is exercised in Package F shadow arbitration.

### No internal points

If an accepted signed candidate is selected and it contains no internal connector/payload point loads:

```text
SelectedUsesDiscreteLoads = false
```

even though the signed method still includes distributed line loads and boundary buoy/anchor conditions.

## 7. Canonical false case — `uniform-current-slack-line`

The canonical assembly contains only one distributed line between buoy and anchor.

Expected signed candidate facts:

```text
InternalPoints = 0
SequenceDiscreteElementCount = 0
BoundaryPointLoadCrossings = 0
TracePointLoadCrossings = 0
FeedbackPointLoadCrossings = 0
MaxPointJumpResidualN = 0
CandidateContainsDiscreteLoads = false
```

Therefore, if this signed candidate were later accepted and selected:

```text
SelectedUsesDiscreteLoads = false
```

## 8. Canonical true case — `discrete-payload`

The historical fixture assembly is:

```text
Upper line, 30 m
-> Shackle (connector)
-> Payload
-> Lower line, 25 m
```

The connector and payload are two distinct internal sequence points at the same coordinate:

```text
s = 30 m
```

Expected signed candidate facts:

```text
InternalPoints = 2
SequenceDiscreteElementCount = 2
Point 1 = Shackle at s=30 m
Point 2 = Payload at s=30 m
BoundaryPointLoadCrossings = 2
TracePointLoadCrossings = 2
FeedbackPointLoadCrossings = 2
MaxPointJumpResidualN = 0
CandidateContainsDiscreteLoads = true
```

The shared boundary kernel applies both local H/V jumps before the lower-line segment. The validation explicitly protects the fact that two same-position point loads remain two consumed crossings.

Therefore this fixture now has evidence for future signed-source discrete-load identity.

This still does **not** select the signed candidate in production.

## 9. What the flag does not mean

`SelectedUsesDiscreteLoads = true` must never be inferred from any of these alone:

```text
assembly contains Connector
assembly contains Payload
sequence.DiscreteElementCount > 0
candidate Shape exists
signed candidate Status = Accepted
technical report contains discrete-load rows
legacy discrete tension calculation exists
```

The flag means that the **actually selected geometry source** incorporated internal discrete point-load jumps according to its governing calculation path.

## 10. Relationship to existing iterative source

The current `MooringPrimaryShapeSelectionResult.UsesDiscreteLoads` remains unchanged by Package E.

Package E only defines the future signed-source meaning so that a later typed selected-core result can report equivalent semantics truthfully across different source identities.

No current selector string/flag is changed in this package.

## 11. Validation regression

Package E adds:

```text
SignedCandidateDiscreteLoadSemanticsRegression
```

It reuses:

- the canonical historical fixture definitions;
- `ApplicationCalculationRunner`;
- production sequence/boundary/trace objects;
- the existing `BoundaryConditionedFeedbackCouplingRegression.RunBudget` feedback path and its existing point-jump closure validation.

It does not copy or replace the signed feedback solver/path.

The regression verifies:

1. the uniform fixture has zero internal points/crossings and candidate discrete-load fact `false`;
2. the discrete fixture contains one connector + one payload;
3. both become distinct internal sequence points at `s=30 m`;
4. boundary and trace crossing counts equal two;
5. the existing 64-step feedback path still consumes two crossings;
6. emitted maximum point-jump residual remains exactly zero for the canonical fixtures;
7. no runtime `SelectedUsesDiscreteLoads` value is introduced by this package.

## 12. Frozen production state

```text
SignedCandidateRuntimeTypeImplemented = False
SelectedUsesDiscreteLoadsSignedRuntime = False
SelectedAuthoritySwitch               = False
SelectedXorZChanged                    = False
GoldenBaselineModified                = False
ProjectJsonChanged                     = False
ReadModelSchemaChanged                 = False
DownstreamAuthoritySwitch             = False
NewConvergenceTolerance                = None
```

## 13. Exit criterion

Package E is complete when:

- the contract is merged;
- `SignedCandidateDiscreteLoadSemanticsRegression` is green in the full engineering regression suite;
- all required repository checks are green on the exact final head;
- no production selected-shape/source/flag change is present.

Next: Package F validation-only shadow arbitration across accepted and non-accepted signed candidate states. Only after Package F may RFC #497 evaluate whether a separate production authority implementation PR is justified.
