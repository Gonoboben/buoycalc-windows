# Signed-Candidate Independent Reference Evidence

Date: 2026-08-20  
RFC: #497  
Package: B  
Status: independent validation evidence only; no production acceptance criterion

## Purpose

Interpret the Package A2 feedback fixed points against an already-existing independent analytical reference and against the numerical contract already owned by the production surface-boundary analyzer.

This package does not introduce a second solver, change the production solver, change segmentation, choose a new tolerance, switch selected X/Z/source, modify downstream authority, or change the golden baseline.

## 1. Evidence sources

Package A2 established that the two currently measurable canonical candidates reach an exact emitted feedback-state fixed point by iteration 16 and remain unchanged through iteration 1024:

```text
uniform-current-slack-line
  Q0N          = 379.810165863037
  Endpoint X   = 22.073605655669077 m
  Endpoint Z   = 50.0007670051935 m
  DepthResidual= +0.000767005193502257 m

discrete-payload
  Q0N          = 720.8641923522947
  Endpoint X   = 19.583341922076137 m
  Endpoint Z   = 49.99914589480685 m
  DepthResidual= -0.0008541051931487686 m
```

Every recorded successive-state delta is exactly zero from iteration 16 onward, but Package A2 deliberately did not call that production acceptance.

The independent comparison already exists in:

```text
validation/BuoyCalc.EngineeringRegression/BoundaryFeedbackIndependentReferenceRegression.cs
```

Its reference fixture is intentionally simple and independent of the feedback integration result:

- neutral 100 m line;
- 80 m target depth;
- uniform 0.5 m/s current;
- 20 mm diameter;
- drag coefficient 1.2;
- zero line water weight;
- zero buoy drag;
- no wave force;
- no internal discrete point loads.

The reference solves the continuous analytical problem using separate 160-iteration roots for Q0 and terminal horizontal force. The candidate path uses the production 0.20 m segmentation (500 segments) and the existing feedback calculation.

## 2. Existing boundary-root numerical contract

`MooringSurfaceBoundaryInfoAnalyzer` currently owns:

```text
DepthToleranceM = 0.01 m
MaxRootIterations = 80
```

For a bracketed non-taut solution, the analyzer classifies the boundary as `SolvedByBoundedBisection` once:

```text
abs(EndpointZM - targetDepthM) <= 0.01 m
```

This is an existing boundary-root tolerance, not a feedback-convergence tolerance and not a new Package B acceptance rule.

The distinction is important:

- **feedback-state convergence** asks whether successive coupled geometry/force states continue to change;
- **boundary-root residual** asks how closely one frozen-load boundary solve closes target depth;
- **continuous-reference error** asks how the production-segmented numerical candidate differs from an independent analytical continuous solution.

Those quantities must not be silently collapsed into one tolerance.

## 3. Independent analytical result

Exact captured output from the already-existing independent regression:

```text
Analytical continuous reference
  kNPerM              = 3.075
  Q0N                 = 160.1062112251539
  X                    = 54.633504127415236 m
  Z                    = 79.99999999999999 m
  EndHN                = 201.35025679395034 N
  LineForceN           = 201.35025679395034 N
  LengthResidualM      = -2.842170943040401e-14 m
  DepthResidualM       = -1.4210854715202004e-14 m
  QRootIterations      = 160
  HRootIterations      = 160
```

The analytical reference therefore closes length and target depth at essentially floating-point roundoff for this idealized fixture.

## 4. Production-segmented feedback candidate on the same reference fixture

```text
FeedbackBudget         = 64
Iterations             = 64
Stop                   = BudgetReached
Segments               = 500
Q0N                    = 160.1182215270996
X                      = 54.631743732148905 m
Z                      = 80.00134323139692 m
EndHN                  = 201.35647834808202 N
LineForceN             = 201.35647834808202 N
DepthResidualM         = +0.0013432313969161669 m
LastDeltaX             = 0
LastDeltaZ             = 0
LastDeltaQ0N           = 0
LastMaxNodeDeltaM      = 0
LastDeltaLineForceN    = 0
NegativeDz             = 0
PointLoads             = 0
```

Like the two canonical Package A2 candidates, the independent-reference candidate is at an emitted feedback fixed point while retaining a small non-zero depth residual.

## 5. Analytical-versus-candidate differences

| Field | Analytical continuous reference | Production-segmented feedback candidate | Candidate - reference |
|---|---:|---:|---:|
| Q0 N | 160.1062112251539 | 160.1182215270996 | +0.012010301945679203 |
| X m | 54.633504127415236 | 54.631743732148905 | -0.001760395266330761 |
| Z m | 79.99999999999999 | 80.00134323139692 | +0.0013432313969303777 |
| End H N | 201.35025679395034 | 201.35647834808202 | +0.00622155413168457 |
| Line force N | 201.35025679395034 | 201.35647834808202 | +0.00622155413168457 |

Relative differences emitted by the regression:

```text
Relative Q0 = +7.501459096292882e-05   (~75.0 ppm)
Relative X  = -3.2221899262130434e-05  (~32.2 ppm)
```

The independent candidate's depth error relative to the continuous reference is approximately 1.343 mm.

## 6. Comparison with Package A2 canonical residuals

| Fixture | Stable depth residual | Absolute residual | Fraction of existing 0.01 m boundary-root tolerance |
|---|---:|---:|---:|
| `uniform-current-slack-line` | +0.000767005193502257 m | 0.767005 mm | 0.0767 |
| `discrete-payload` | -0.0008541051931487686 m | 0.854105 mm | 0.0854 |
| independent neutral-line reference candidate | +0.0013432313969161669 m | 1.343231 mm | 0.1343 |

All three stable residuals are substantially inside the already-existing 10 mm boundary-root tolerance. The analytical continuous reference itself closes depth to floating-point roundoff.

## 7. What the independent comparison establishes

The combined evidence supports the following conclusions:

1. `BudgetReached` at 64 was not hiding continued drift for the two canonical fixtures; A2 showed exact repeated fixed points through 1024.
2. A non-zero millimetre-scale depth residual can coexist with an exact feedback-state fixed point even on the deliberately simple neutral-line analytical-reference fixture.
3. Therefore `successive feedback deltas == 0` and `depth residual == 0` are different numerical properties.
4. The existing surface-boundary root contract permits up to 10 mm absolute depth residual. The observed fixed-point residuals (0.767 mm, 0.854 mm, 1.343 mm) are all within that already-owned boundary classification tolerance.
5. The independent continuous reference shows that the numerical candidate is very close, but not identical, to the analytical continuous solution: about 75 ppm in Q0, 32 ppm in X, 1.76 mm absolute X, and 1.34 mm absolute Z for the reference fixture.
6. The current evidence does not support divergence or sustained oscillation in the measured feedback states.

## 8. What the comparison does not establish

Package B must not over-attribute the millimetre-scale difference.

The production candidate differs from the analytical reference in more than one numerical respect:

- the candidate uses exact production 0.20 m segmentation;
- the boundary solver uses midpoint/discrete integration;
- the boundary Q0 root accepts states inside the existing 0.01 m depth tolerance;
- the feedback path iterates force/geometry coupling around those discrete boundary solves.

Therefore this evidence does **not** prove that the residual is caused exclusively by the 0.01 m root tolerance, nor exclusively by 0.20 m segmentation. Separating those contributions would require an additional deliberately designed numerical-convergence study; changing production segmentation is outside RFC #497 and remains a non-goal.

Package B also does not prove that the existing 10 mm boundary-root tolerance should automatically become the future signed-production **candidate acceptance** tolerance. Reusing it for that purpose would be a separate semantic decision, because a candidate acceptance contract may need to include feedback-state stability, boundary feasibility, discrete-load closure, finite-state diagnostics, and possibly other invariants.

## 9. Discrete-load evidence boundary

The independent analytical reference deliberately has zero internal point loads, so it cannot independently validate the full `discrete-payload` point-load physics.

For the canonical `discrete-payload` trajectory, existing core/validation evidence does show:

```text
PointLoadCrossings = 2
MaxPointJumpResidualN = 0
NegativeDz = 0
```

at every A2 measured horizon, including the fixed point. That is a deterministic invariant of the existing signed trace/point-load closure contract, but it is not an analytical independent reference for the payload fixture. Explicit production `SelectedUsesDiscreteLoads` semantics therefore remain an RFC #497 Package E gate.

## 10. Package B conclusion

Observed trajectory class for both currently measurable canonical candidates:

```text
rapidly contracting feedback state
-> exact emitted floating-point fixed point by iteration 16
-> no drift/limit cycle/divergence through iteration 1024
-> stable millimetre-scale non-zero depth residual
```

Independent reference evidence shows the same qualitative distinction: the feedback candidate reaches an exact emitted fixed point while the continuous analytical solution is not numerically identical to the production-segmented candidate.

This is sufficient to move RFC #497 from trajectory discovery to **candidate acceptance contract design**, but it is not sufficient to choose a new acceptance threshold automatically.

## 11. Frozen state

```text
GoldenBaselineModified       = False
SelectedAuthoritySwitch      = False
SelectedSourceSchemaChange   = False
DownstreamAuthoritySwitch    = False
ProductionAcceptance         = NotDefined
NewFeedbackConvergenceTolerance = None
ProductionSegmentation       = unchanged at exact 0.20 m
```

The next RFC #497 package should define candidate states and acceptance/termination ownership in documentation before any runtime implementation. Any numerical threshold proposed there must state whether it is reusing an already-owned boundary-root tolerance or introducing a genuinely new candidate-level criterion, and must justify that distinction explicitly.
