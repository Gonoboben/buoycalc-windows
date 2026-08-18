# Control mark: boundary-conditioned raw-feedback convergence evidence

Date: 2026-08-18
Issue: #407
Prerequisites: merged #477, #478, #479, #480
Evidence source: `.NET Build` #923 `BOUNDARY_FEEDBACK_ROLLUP`
Scope: classify the numerical behavior of the validation-only raw (`alpha = 1`) boundary-conditioned fixed-point experiment. No production solver/shape change is authorized by this record.

## 1. Decision

The canonical solved scenarios A-E show clean numerical settling of the raw `alpha = 1` feedback map under the validation-only experiment.

Observed behavior:

- no oscillatory or divergent trajectory was observed in A-E;
- by budget 8, endpoint/node/force deltas are already approximately `1e-8 ... 1e-9` scale in geometry for the tested cases;
- by budget 16, endpoint and node deltas are zero or floating-point roundoff in all A-E scenarios;
- `DeltaQ0N` is zero in the reported budget summaries from budget 4 onward;
- `NegativeDz = 0` in every A-E budget result;
- point-load jump closure remains approximately `1e-13 N`;
- the controlled buoyant-line case does not enter the feedback iteration because its initial boundary classification is `TautNonZeroHorizontalLoadNoFiniteRoot`.

Therefore, the current canonical evidence does **not** justify adding under-relaxation as a remedy for instability. The next #407 acceptance item is independent/reference comparison, not a production solver switch.

This is empirical evidence for the tested canonical cases, not a proof of global convergence or contraction for every possible project input.

## 2. Exact measured results

The roll-up contained 32 compact records: five scenario headers plus five budgets for each A-E scenario, followed by the buoyant-line scenario header and terminal classification.

### Scenario A

Initial:

- `InitialX = 21.64602261169438 m`
- `InitialZ = 50.00032484663539 m`
- `InitialQ0 = 855.3165720825195 N`
- historical `SelectedX = 21.151843441661676 m`

Settled feedback state from budget 16 onward:

- `X = 21.85389416620768 m`
- `Z = 50.001510007783764 m`
- `Q0 = 792.1515467834473 N`
- `LineForce = 358.87467454351764 N`
- `DepthResidual = +0.0015100077837644221 m`
- `NegativeDz = 0`
- `PointLoads = 3`
- `MaxPointJumpResidual = 2.4868995751603507e-14 N`

Convergence observations:

- budget 4: `MaxNodeDelta = 1.9343668875790592e-4 m`, `DeltaLineForce = 8.333467402280803e-3 N`;
- budget 8: `MaxNodeDelta = 5.653651130264818e-9 m`, `DeltaLineForce = 7.520721965192934e-7 N`;
- budget 16+: endpoint/node/force deltas are zero in the recorded summaries.

Historical-selected comparison:

- `FeedbackX - SelectedX = +0.7020507245460053 m`
- relative to historical `SelectedX`: `+3.3191%`.

### Scenario B

Initial:

- `InitialX = 57.84849971237418 m`
- `InitialZ = 120.00216341627299 m`
- `InitialQ0 = 2518.5086103515623 N`
- historical `SelectedX = 58.26919754625708 m`

Settled feedback state:

- `X = 58.61086724640591 m`
- `Z = 119.99924706753029 m`
- `Q0 = 2265.106306640625 N`
- `LineForce = 1318.3302029191605 N`
- `DepthResidual = -0.0007529324697088668 m`
- `NegativeDz = 0`
- `PointLoads = 4`
- `MaxPointJumpResidual = 1.2710574864626038e-13 N`

Convergence observations:

- budget 4: `MaxNodeDelta = 3.05672806068752e-4 m`, `DeltaLineForce = -5.6339669511544344e-2 N`;
- budget 8: `MaxNodeDelta = 6.740046369423125e-8 m`, `DeltaLineForce = 1.0175224133490701e-5 N`;
- budget 16+: endpoint/node/force deltas are floating-point roundoff only.

Historical-selected comparison:

- `FeedbackX - SelectedX = +0.34166970014882736 m`
- relative to historical `SelectedX`: `+0.5864%`.

### Scenario C

Initial:

- `InitialX = 139.13182540637308 m`
- `InitialZ = 379.9917406829415 m`
- `InitialQ0 = 3196.8936797485353 N`
- historical `SelectedX = 143.90606326947662 m`

Settled feedback state:

- `X = 140.64610696028555 m`
- `Z = 379.9936003220721 m`
- `Q0 = 2973.391962020874 N`
- `LineForce = 1564.3566246497812 N`
- `DepthResidual = -0.006399677927902303 m`
- `NegativeDz = 0`
- `PointLoads = 4`
- `MaxPointJumpResidual = 1.139086659085919e-13 N`

Convergence observations:

- budget 4: `MaxNodeDelta = 3.894052980282644e-4 m`, `DeltaLineForce = 1.815720827835321e-3 N`;
- budget 8: `MaxNodeDelta = 1.835354146374623e-8 m`, `DeltaLineForce = 1.3209464668761939e-6 N`;
- budget 16+: endpoint/node/force deltas are zero or roundoff.

Historical-selected comparison:

- `FeedbackX - SelectedX = -3.259956309191068 m`
- relative to historical `SelectedX`: `-2.2653%`.

### Scenario D

Initial:

- `InitialX = 150.74304272227087 m`
- `InitialZ = 379.9943097703955 m`
- `InitialQ0 = 7423.533493530273 N`
- historical `SelectedX = 138.63369646261512 m`

Settled feedback state:

- `X = 151.09457645282004 m`
- `Z = 379.99796738366626 m`
- `Q0 = 7044.39968963623 N`
- `LineForce = 2094.928409440263 N`
- `DepthResidual = -0.0020326163337358594 m`
- `NegativeDz = 0`
- `PointLoads = 4`
- `MaxPointJumpResidual = 4.0990372275064023e-13 N`

Convergence observations:

- budget 4: `MaxNodeDelta = 4.524847678348086e-4 m`, `DeltaLineForce = 1.2984239500838157e-2 N`;
- budget 8: `MaxNodeDelta = 1.1067527926004256e-9 m`, `DeltaLineForce = 5.450192475109361e-8 N`;
- budget 16+: endpoint/node/force deltas are zero.

Historical-selected comparison:

- `FeedbackX - SelectedX = +12.460879990204916 m`
- relative to historical `SelectedX`: `+8.9883%`.

### Scenario E

Initial:

- `InitialX = 149.517022681814 m`
- `InitialZ = 379.99707269103715 m`
- `InitialQ0 = 4001.3380251126964 N`
- historical `SelectedX = 130.0129289611826 m`

Settled feedback state:

- `X = 150.3028041289927 m`
- `Z = 379.9922075244769 m`
- `Q0 = 3674.8176071523444 N`
- `LineForce = 1114.9437197014572 N`
- `DepthResidual = -0.007792475523103803 m`
- `NegativeDz = 0`
- `PointLoads = 4`
- `MaxPointJumpResidual = 3.7517733012589603e-13 N`

Convergence observations:

- budget 4: `MaxNodeDelta = 4.777493529571692e-3 m`, `DeltaLineForce = -7.951135659845932e-2 N`;
- budget 8: `MaxNodeDelta = 4.288471040298325e-8 m`, `DeltaLineForce = -1.1023996648873435e-6 N`;
- budget 16+: endpoint/node/force deltas are zero.

Historical-selected comparison:

- `FeedbackX - SelectedX = +20.289875167810095 m`
- relative to historical `SelectedX`: `+15.6060%`.

### Controlled buoyant-line case

The initial boundary state is intentionally not manufactured into a solution:

- `InitialClass = TautNonZeroHorizontalLoadNoFiniteRoot`
- `InitialSolved = false`
- historical `SelectedX = 27.429659239050817 m`
- terminal record: `Budget=0`, `Iteration=0`, `Reason=InitialBoundary:TautNonZeroHorizontalLoadNoFiniteRoot`.

This case validates explicit non-solvability propagation. It does not provide an iterative convergence trajectory and must not be counted as evidence that a buoyant solved case converges.

## 3. Cross-scenario convergence classification

For A-E the raw map settles rapidly under the exact experiment implemented by #478:

| Scenario | MaxNodeDelta at budget 4, m | MaxNodeDelta at budget 8, m | budget 16 state | Depth residual at settled state, mm |
|---|---:|---:|---|---:|
| A | 1.934e-4 | 5.654e-9 | zero deltas | +1.510 |
| B | 3.057e-4 | 6.740e-8 | roundoff only | -0.753 |
| C | 3.894e-4 | 1.835e-8 | zero/roundoff | -6.400 |
| D | 4.525e-4 | 1.107e-9 | zero deltas | -2.033 |
| E | 4.777e-3 | 4.288e-8 | zero deltas | -7.792 |

The budget-8 node-change reduction relative to budget 4 is approximately:

- A: `2.92e-5` of the budget-4 value;
- B: `2.20e-4`;
- C: `4.71e-5`;
- D: `2.45e-6`;
- E: `8.98e-6`.

No formal contraction ratio is claimed from these sparse budget snapshots. They are empirical decay measurements only.

`Stop=BudgetReached` must not be interpreted as a convergence failure: the validation harness intentionally runs a fixed budget and does not use convergence as its stop criterion.

## 4. Important result: convergence does not establish correctness

The converged feedback endpoint is not identical to the historical selected geometry:

| Scenario | Historical SelectedX, m | Settled feedback X, m | Difference, m | Difference vs SelectedX |
|---|---:|---:|---:|---:|
| A | 21.151843 | 21.853894 | +0.702051 | +3.3191% |
| B | 58.269198 | 58.610867 | +0.341670 | +0.5864% |
| C | 143.906063 | 140.646107 | -3.259956 | -2.2653% |
| D | 138.633696 | 151.094576 | +12.460880 | +8.9883% |
| E | 130.012929 | 150.302804 | +20.289875 | +15.6060% |

D and E are especially material differences. This is not itself evidence that either result is wrong. It is evidence that a production authority switch would materially change geometry and therefore requires the remaining independent/reference validation before any golden or selected-shape change.

## 5. Under-relaxation decision

For the tested A-E canonical scenarios:

- raw `alpha = 1` is numerically stable;
- no sustained oscillation is visible in the budget summaries;
- no divergence guard is required by the measured path;
- budget 16 is already beyond the observed settling point.

Accordingly, #407 should **not** introduce under-relaxation merely to improve convergence of these cases.

This does not prohibit a future relaxation study for a newly discovered unstable class. Such a study would require its own reproduced unstable input and validation-only evidence.

## 6. Acceptance status for #407

The acceptance list before any production geometry change is now classified as follows:

1. source-backed signed orientation convention — established by the #407 RFC/control marks;
2. synthetic quadrant tests — established;
3. no sign loss between cumulative H/V and tangent vector — established in diagnostic/validation path;
4. analytical limiting cases — established for the signed-orientation validation boundary;
5. convergence-study evidence — **established by #478/#479 and numerically classified by this control mark**;
6. independent/reference comparison — **still required**;
7. explicit review of every historical golden change — required only before any production proposal that changes those values.

The next allowed engineering package is therefore validation-only independent/reference comparison.

## 7. Next independent/reference comparison boundary

The next package must not compare the feedback solver to itself through a second wrapper around the same implementation.

It should use an independently coded/reference relation for a tractable planar case, with source provenance recorded. Suitable evidence should include at least one case where endpoint geometry or tension can be computed from a closed-form/static relation independently of `BoundaryConditionedFeedbackCouplingRegression`, and then compare the validation feedback result with explicit absolute/relative residuals.

The primary engineering source remains H. O. Berteaux, *Buoy Engineering* / Г. О. Берто, *Океанографические буи* (1979). The source describes static cable geometry as following the resultant tension vectors and preserves signed submerged load in the cable equilibrium formulation. The reference package must state exactly which equation/case is used and which simplifying assumptions make it independently solvable.

No acceptance tolerance should be invented merely to make the comparison pass. A tolerance must be justified by the reference discretization/numerical method before it becomes a gate.

## 8. Production authority remains unchanged

This convergence result does not authorize changes to:

- `BuoyCalculator`;
- `MooringShapeSolver`;
- production `MooringShapeForceAnalyzer` behavior;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate` or selected shape;
- selected X/Z;
- 2D/PDF/report geometry;
- force coefficients or drag equations;
- anchor or weak-link calculations;
- verdict;
- signed `WeightWaterKg` / `WeightWaterKgM` semantics;
- 0.20 m production segmentation target or unlimited segment count;
- profile-current production projection;
- JSON/DTO;
- golden baseline;
- 3D.

Production behavior remains frozen until the remaining #407 reference evidence is reviewed in a separate small package.
