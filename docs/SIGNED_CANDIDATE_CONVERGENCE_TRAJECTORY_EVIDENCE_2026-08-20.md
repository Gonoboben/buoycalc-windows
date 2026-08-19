# Signed-Candidate Convergence Trajectory Evidence

Date: 2026-08-20  
RFC: #497  
Package: A2  
Protocol: `SIGNED_CANDIDATE_CONVERGENCE_MEASUREMENT_PROTOCOL_2026-08-20.md`  
Status: validation evidence only; no production acceptance criterion or authority switch

## Purpose

Execute the Package A1 pre-registered convergence-trajectory protocol for the two canonical fixtures that currently produce a boundary-conditioned signed validation candidate:

```text
uniform-current-slack-line
discrete-payload
```

The measurement reuses the existing `BoundaryConditionedFeedbackCouplingRegression.RunBudget` feedback path and the canonical historical fixture definitions. It does not introduce a second solver, alter production calculations, tune a stopping threshold, or change the golden baseline.

## 1. Reproducibility

A1 fixed the measurement before extended results were inspected:

```text
Protocol budgets:
64, 128, 256, 512, 1024

Pre-registered trajectory samples:
1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024

New convergence tolerance:
None

Production acceptance state:
None
```

Validation implementation head before temporary audit-output instrumentation:

```text
5f84801f387184608a6e642947e9a1b5e379934d
```

The exact-output capture ran against the same validation implementation with temporary CI artifact instrumentation only:

```text
capture head: 0a94674913832f0d010d51688693f88851e82556
workflow run: 32306961977
artifact id: 9385075730
artifact digest: sha256:8ee91394d44c5a4760fe39d63f59550ab8cc0da89d22b8ee7e562b178f8db66a
```

The capture run passed `.NET Build`, `Selected Shape Consumer Scan`, and `Report Store Consumer Scan`. Temporary workflow instrumentation is not part of the intended final PR diff.

## 2. Main observation

Both measured feedback maps are **rapidly contracting toward a floating-point fixed point** under the existing deterministic calculation path.

For both fixtures:

- the boundary classification remains `SolvedByBoundedBisection` at every measured horizon;
- Q0 settles by the second iteration and does not change afterward;
- geometry/force successive-state deltas shrink by several orders of magnitude between iterations 1, 2, 4, and 8;
- by iteration 16, every recorded successive-state delta is exactly `0` in the emitted double-precision state;
- the state remains bit-for-bit unchanged at the measured horizons 32, 64, 128, 256, 512, and 1024;
- `NegativeDzSegmentCount=0` throughout;
- point-load crossings remain exactly `0` for `uniform-current-slack-line` and `2` for `discrete-payload`;
- point-load jump residual is `0` throughout the measured states.

However, the stabilized depth residual is **not zero**:

```text
uniform-current-slack-line: +0.000767005193502257 m
discrete-payload:          -0.0008541051931487686 m
```

Therefore this package establishes a deterministic feedback-state fixed point, but does **not** define whether that fixed point is acceptable for production. A later contract must decide which residuals/invariants own production acceptance and justify any numerical threshold independently of these observed outputs.

`BudgetReached` remains the emitted stop reason because the existing validation experiment intentionally runs to the requested horizon and has no convergence stop rule. It must not be relabeled as production convergence in A2.

## 3. `uniform-current-slack-line` trajectory

Initial state before feedback:

```text
InitialClass = SolvedByBoundedBisection
InitialQ0N   = 405.2784860229491
```

All rows below are exact emitted values. `Protocol` marks the budgets pre-registered as required A1 horizons; the other rows are the pre-registered within-trajectory samples.

| Budget | Protocol | Q0N | X m | Z m | Depth residual m | Line force N | Δ line force N | Max segment Δ force N | ΔX m | ΔZ m | ΔQ0 N | Max node Δ m |
|---:|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | no | 380.9174841308593 | 22.060831307254382 | 50.0069053450306 | 0.006905345030602916 | 140.1589967359365 | -28.96600326406397 | 0.20753889596822334 | 0.14179451615434147 | 0.010516037816209689 | -24.361001892089803 | 0.33991939533384286 |
| 2 | no | 379.810165863037 | 22.07463777571866 | 50.0002665233718 | 0.00026652337179911 | 140.14721765007837 | -0.01177908585813725 | 0.013628782504179204 | 0.013806468464277799 | -0.006638821658803806 | -1.1073182678222793 | 0.015319677688270725 |
| 4 | no | 379.810165863037 | 22.073614337481043 | 50.00076199404042 | 0.0007619940404168801 | 140.11476659975656 | 0.003023916908659885 | 0.00004202165925504264 | 0.0001134292054665309 | -0.00006148519883453218 | 0 | 0.0001290217591280263 |
| 8 | no | 379.810165863037 | 22.07360565571636 | 50.00076700516367 | 0.0007670051636665676 | 140.11446362604235 | 9.75641967215779e-8 | 2.2082016770674784e-9 | 1.3119390018800914e-9 | -8.202363233067445e-10 | 0 | 1.5472464479603425e-9 |
| 16 | no | 379.810165863037 | 22.073605655669077 | 50.0007670051935 | 0.000767005193502257 | 140.1144636219283 | 0 | 0 | 0 | 0 | 0 | 0 |
| 32 | no | 379.810165863037 | 22.073605655669077 | 50.0007670051935 | 0.000767005193502257 | 140.1144636219283 | 0 | 0 | 0 | 0 | 0 | 0 |
| 64 | **yes** | 379.810165863037 | 22.073605655669077 | 50.0007670051935 | 0.000767005193502257 | 140.1144636219283 | 0 | 0 | 0 | 0 | 0 | 0 |
| 128 | **yes** | 379.810165863037 | 22.073605655669077 | 50.0007670051935 | 0.000767005193502257 | 140.1144636219283 | 0 | 0 | 0 | 0 | 0 | 0 |
| 256 | **yes** | 379.810165863037 | 22.073605655669077 | 50.0007670051935 | 0.000767005193502257 | 140.1144636219283 | 0 | 0 | 0 | 0 | 0 | 0 |
| 512 | **yes** | 379.810165863037 | 22.073605655669077 | 50.0007670051935 | 0.000767005193502257 | 140.1144636219283 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1024 | **yes** | 379.810165863037 | 22.073605655669077 | 50.0007670051935 | 0.000767005193502257 | 140.1144636219283 | 0 | 0 | 0 | 0 | 0 | 0 |

At every row:

```text
Stop             = BudgetReached
Class            = SolvedByBoundedBisection
NegativeDz       = 0
PointLoads       = 0
MaxPointJumpResidualN = 0
Acceptance       = None
```

The 64-step values exactly preserve the Package F candidate evidence for Q0 and endpoint X/Z.

### Behavior interpretation

The first two feedback updates produce the large correction. The remaining updates rapidly damp the geometry/force change. There is a small sign change in some deltas around iterations 2–4, but the magnitude collapses rather than maintaining an oscillation. By iteration 16 the emitted state is a fixed point at double precision and remains unchanged through 1024.

The fixed point itself retains the non-zero `+0.000767005193502257 m` depth residual. A2 does not judge that residual acceptable or unacceptable.

## 4. `discrete-payload` trajectory

Initial state before feedback:

```text
InitialClass = SolvedByBoundedBisection
InitialQ0N   = 741.3495803070066
```

| Budget | Protocol | Q0N | X m | Z m | Depth residual m | Line force N | Δ line force N | Max segment Δ force N | ΔX m | ΔZ m | ΔQ0 N | Max node Δ m |
|---:|:---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | no | 721.4178514862058 | 19.579295939458774 | 50.000973008548016 | 0.0009730085480157413 | 141.01082905229677 | -28.11417094770374 | 0.2585686498849661 | 0.10210396981842607 | -0.000912515449044804 | -19.9317288208008 | 0.22815781348439843 |
| 2 | no | 720.8641923522947 | 19.583564481477847 | 49.99901427262295 | -0.0009857273770492725 | 140.95653810158146 | -0.05429095071531265 | 0.01386860058963485 | 0.004268542019072896 | -0.001958735925065014 | -0.5536591339110828 | 0.004696498418261334 |
| 4 | no | 720.8641923522947 | 19.58334300651755 | 49.99914508801281 | -0.0008549119871901212 | 140.94726322486576 | 0.0007364493755801504 | 0.000012491396571889801 | 0.0000189922764306516 | -0.000013441745089437518 | 0 | 0.000023267726039897022 |
| 8 | no | 720.8641923522947 | 19.58334192207751 | 49.999145894805736 | -0.0008541051942643207 | 140.94720027021978 | 6.80910261507961e-9 | 2.3815627248069404e-10 | 5.439559913611447e-11 | -4.228439820508356e-11 | 0 | 6.889739862246565e-11 |
| 16 | no | 720.8641923522947 | 19.583341922076137 | 49.99914589480685 | -0.0008541051931487686 | 140.9472002700199 | 0 | 0 | 0 | 0 | 0 | 0 |
| 32 | no | 720.8641923522947 | 19.583341922076137 | 49.99914589480685 | -0.0008541051931487686 | 140.9472002700199 | 0 | 0 | 0 | 0 | 0 | 0 |
| 64 | **yes** | 720.8641923522947 | 19.583341922076137 | 49.99914589480685 | -0.0008541051931487686 | 140.9472002700199 | 0 | 0 | 0 | 0 | 0 | 0 |
| 128 | **yes** | 720.8641923522947 | 19.583341922076137 | 49.99914589480685 | -0.0008541051931487686 | 140.9472002700199 | 0 | 0 | 0 | 0 | 0 | 0 |
| 256 | **yes** | 720.8641923522947 | 19.583341922076137 | 49.99914589480685 | -0.0008541051931487686 | 140.9472002700199 | 0 | 0 | 0 | 0 | 0 | 0 |
| 512 | **yes** | 720.8641923522947 | 19.583341922076137 | 49.99914589480685 | -0.0008541051931487686 | 140.9472002700199 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1024 | **yes** | 720.8641923522947 | 19.583341922076137 | 49.99914589480685 | -0.0008541051931487686 | 140.9472002700199 | 0 | 0 | 0 | 0 | 0 | 0 |

At every row:

```text
Stop             = BudgetReached
Class            = SolvedByBoundedBisection
NegativeDz       = 0
PointLoads       = 2
MaxPointJumpResidualN = 0
Acceptance       = None
```

The 64-step values exactly preserve the Package F candidate evidence for Q0 and endpoint X/Z.

### Behavior interpretation

This trajectory shows the same broad pattern as the uniform-current fixture: a large initial correction, rapidly shrinking feedback changes, a small sign reversal in some deltas during damping, and an exact emitted fixed point by iteration 16. The two discrete point-load crossings remain present and close with zero reported point-jump residual throughout the sampled trajectory.

The fixed point retains the non-zero `-0.0008541051931487686 m` depth residual. A2 does not judge that residual acceptable or unacceptable.

## 5. Pre-registered protocol-budget summary

For all required A1 protocol budgets `64, 128, 256, 512, 1024`, each fixture emits exactly the same final state as its iteration-16 fixed point.

| Fixture | State at 64 through 1024 | Q0N | Endpoint X m | Endpoint Z m | Depth residual m | Line force N | All recorded successive deltas |
|---|---|---:|---:|---:|---:|---:|---|
| `uniform-current-slack-line` | unchanged | 379.810165863037 | 22.073605655669077 | 50.0007670051935 | 0.000767005193502257 | 140.1144636219283 | exactly `0` |
| `discrete-payload` | unchanged | 720.8641923522947 | 19.583341922076137 | 49.99914589480685 | -0.0008541051931487686 | 140.9472002700199 | exactly `0` |

This rules out continued drift, a sustained limit cycle, or divergence over the pre-registered 1024-step horizon for these two fixtures in the current deterministic validation path. It does **not** establish the production acceptance criterion for the residual fixed point.

## 6. What A2 proves

A2 provides evidence for the following statements only:

1. The two currently measurable signed validation candidates have deterministic feedback trajectories under the existing calculation path.
2. Both trajectories contract rapidly toward a stable floating-point fixed point.
3. At every measured state, finite-state, negative-dz, fixture point-load-count, and point-load jump-closure hard checks remain valid.
4. Every recorded successive-state delta is exactly zero by iteration 16 and remains zero at all measured horizons through 1024.
5. The fixed points retain small but non-zero depth residuals of opposite sign.
6. The historical Package F 64-step candidate values are reproducible exactly for Q0 and endpoint geometry.

## 7. What A2 does not prove

A2 does **not** prove or authorize:

```text
ProductionCandidateAccepted = true
SelectedConverged            = true for a signed production source
SelectedSource               = signed production source
Selected X/Z authority       = switched
GoldenBaselineModified       = true
DownstreamAuthoritySwitch    = true
NewConvergenceTolerance      = any value
```

In particular, `successive deltas == 0` is evidence that the feedback map has reached a numerical fixed point. It is not by itself evidence that the fixed point satisfies the complete physical/product acceptance contract. The stabilized depth residual is exactly why the later independent-reference and acceptance-contract packages remain necessary.

## 8. Next gate

The next RFC #497 package is independent termination/convergence evidence.

It should compare the stabilized feedback result against already validated analytical/reference boundary invariants where possible, especially the relationship between:

- the fixed-point Q0 and boundary solution;
- endpoint/depth residual behavior;
- line-force/current-force closure;
- discrete point-load closure for `discrete-payload`;
- the meaning and numerical origin of the stable non-zero depth residual.

No production acceptance threshold should be chosen until that independent evidence is reviewed and a separate docs contract pre-registers the acceptance semantics.

## 9. Frozen production state

```text
GoldenBaselineModified     = False
SelectedAuthoritySwitch    = False
SelectedSourceSchemaChange = False
DownstreamAuthoritySwitch  = False
ProductionAcceptance       = NotDefined
NewConvergenceTolerance    = None
```
