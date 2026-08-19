# Signed-Geometry Golden Migration Decision

Date: 2026-08-20  
RFC: #487  
Package: F  
Status: audit/product-disposition control mark; golden baseline unchanged; no production authority switch

## Purpose

Close the RFC #487 golden-migration decision gate without changing runtime behavior or the committed engineering baseline.

This package records:

- an explicit product/migration disposition for all five canonical historical fixtures;
- exact historical-versus-validation-candidate geometry/force measurements for the two fixtures where the existing boundary-conditioned feedback audit can construct a candidate;
- the reason those two validation candidates are **not** yet production-solvable;
- the fields for which no signed-production value is defined or authorized;
- an explicit decision that no golden value is migrated in this package.

No solver, drag, wave, anchor/seabed physics, segmentation, elasticity, selected X/Z, selected source, downstream force/verdict authority, DTO, UI, report, PDF, 2D, or 3D behavior changes here.

## 1. Evidence basis

Package F is based on the calculation/validation state at main commit:

```text
d89d0941060f299605e537a4b66aba3add6e2eee
```

Historical baseline:

```text
validation/BuoyCalc.EngineeringRegression/baselines/engineering-baseline.json
```

Candidate measurement source:

```text
validation/BuoyCalc.EngineeringRegression/HistoricalGoldenImpactRegression.cs
FeedbackBudget = 64
```

The audit reuses the already-established boundary-conditioned feedback calculation path. No alternate physics calculation was introduced for this decision.

Package D authority rules remain binding (`SIGNED_GEOMETRY_AUTHORITY_CONTRACT_2026-08-20.md`), including truthful source identity, explicit candidate acceptance/convergence semantics, explicit discrete-load semantics, core-owned diagnostics, and no selected-X/Z switch before those gates are closed.

Package E ownership rules also remain binding (`SIGNED_GEOMETRY_DOWNSTREAM_AUTHORITY_CONTRACT_2026-08-20.md`): geometry promotion does not automatically transfer `TensionKn`, anchor/weak-link/verdict authority, legacy `EstimatedOffsetM`, or selected-node tension/angle authority.

## 2. `CandidateAvailable` is not production eligibility

The historical audit can currently construct a boundary-conditioned feedback candidate for two fixtures:

```text
uniform-current-slack-line
  CandidateAvailable = true
  InitialClass       = SolvedByBoundedBisection
  Iterations         = 64
  Stop               = BudgetReached

discrete-payload
  CandidateAvailable = true
  InitialClass       = SolvedByBoundedBisection
  Iterations         = 64
  Stop               = BudgetReached
```

`CandidateAvailable=true` means only that the validation audit can construct and iterate a candidate from the available boundary state. It is **not** a production acceptance result.

Both candidates end because the fixed 64-iteration audit budget is reached. There is no approved signed-candidate production convergence/acceptance contract, no explicit production signed source identity, and the required selected/discrete-load/downstream authority semantics are not yet closed. Therefore neither fixture is classified here as production-solvable.

The audit's historical `ProductionSwitchBlocker=False` label for these two fixtures must be read only in its original narrow sense: the initial boundary is available to the validation feedback experiment. It does not override Package D/E gates.

## 3. Five-fixture migration/product disposition

| Fixture | Current signed evidence | Package F product disposition | Golden migration |
|---|---|---|---|
| `vertical-zero-current` | Exact `L=depth=50 m`, `H=0`; geometry is uniquely straight vertical, but validated Q0/vertical force state is a non-unique family. Production INFO classification is `VerticalGeometryUniqueForceStateFamily`; `Solved=false`, `Q0N=null`, `SolutionState=null`. | Preserve the current selected-shape/downstream authority. Expose the existing INFO semantics only. Do not invent a unique Q0 or claim a unique signed equilibrium state. | **No** |
| `uniform-current-slack-line` | Validation feedback candidate is measurable, but stops at `BudgetReached` after 64 iterations. | Preserve current production authority. Candidate values below are migration-review evidence only; a later production package must first define/validate signed source, acceptance/convergence, discrete-load and downstream semantics. | **No** |
| `buoyant-line` | Exact `L=depth=30 m` with non-zero horizontal load; analytically classified `PhysicallyInfeasibleUnderCurrentInextensibleModel`. | Treat the signed inextensible candidate as physically infeasible/rejected. Do not manufacture X/Z or silently add stretch. Existing production compatibility authority may remain until a separate product-state/physics package explicitly changes behavior, but it must not be described as a signed solved state. | **No** |
| `discrete-payload` | Validation feedback candidate is measurable, with two point-load crossings, but stops at `BudgetReached` after 64 iterations. | Preserve current production authority. Candidate values below are migration-review evidence only; explicit discrete-load and downstream ownership gates are especially binding for this fixture. | **No** |
| `depth-varying-current-profile` | Exact `L=depth=50 m` with non-zero horizontal load; analytically classified `PhysicallyInfeasibleUnderCurrentInextensibleModel`. | Treat the signed inextensible candidate as physically infeasible/rejected. Do not manufacture X/Z, alter the current profile model, or silently add stretch. Existing production compatibility authority may remain until a separate product-state/physics package explicitly changes behavior, but it must not be described as a signed solved state. | **No** |

Result: all five historical fixtures have an explicit Package F disposition, and **zero** fixtures are authorized for golden migration by this package.

## 4. Exact measured old/new audit — `uniform-current-slack-line`

These are validation-candidate measurements only, not proposed production baseline values.

```text
InitialClass = SolvedByBoundedBisection
Iterations   = 64
Stop         = BudgetReached
Q0N          = 379.810165863037
NegativeDz   = 0
PointLoads   = 0
```

| Measured field | Historical | Validation candidate | Delta |
|---|---:|---:|---:|
| `CurrentForceN` | 251.12500000000048 | 222.1144636219283 | -29.010536378072175 |
| `HorizontalForceN` | 341.04806232103687 | 312.0375259429647 | -29.010536378072175 |
| `SegmentCurrentForceSumN` | 169.12500000000048 | 140.1144636219283 | -29.010536378072175 |
| `SelectedNodeCount` | 276 | 276 | 0 |
| `SelectedHorizontalOffsetM` | 22.904164818523228 | 22.073605655669077 | -0.8305591628541507 |
| `SelectedAnchorDepthM` | 50 | 50.0007670051935 | 0.000767005193502257 |
| `SelectedVerticalResidualM` | 0 | 0.000767005193502257 | 0.000767005193502257 |
| `SelectedXSumM` | 3160.774744956185 | 2600.496839053319 | -560.2779059028658 |
| `SelectedZSumM` | 6900.546928698937 | 7094.615871254673 | 194.0689425557357 |
| `SelectedXSquaredSumM2` | 48351.02187599841 | 36100.122883036216 | -12250.898992962197 |

### Selected sample X/Z measurements

| Index | Historical X | Candidate X | Delta X | Historical Z | Candidate Z | Delta Z |
|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 69 | 5.746863172647611 | 3.6360485212167197 | -2.1108146514308914 | 12.546456219780406 | 13.305468676939448 | 0.7590124571590415 |
| 138 | 11.493726345295169 | 8.644480277831711 | -2.849246067463458 | 25.092912439560788 | 26.15816636547522 | 1.0652539259144334 |
| 207 | 17.240589517942812 | 14.881566033206427 | -2.359023484736385 | 37.63936865934122 | 38.46265911520251 | 0.8232904558612901 |
| 275 | 22.904164818523228 | 22.073605655669077 | -0.8305591628541507 | 50 | 50.0007670051935 | 0.000767005193502257 |

The sample extrema reproduce the committed historical audit summary:

```text
MaxSelectedSampleXDeltaM = 2.849246067463458
MaxSelectedSampleZDeltaM = 1.0652539259144334
```

## 5. Exact measured old/new audit — `discrete-payload`

These are validation-candidate measurements only, not proposed production baseline values.

```text
InitialClass = SolvedByBoundedBisection
Iterations   = 64
Stop         = BudgetReached
Q0N          = 720.8641923522947
NegativeDz   = 0
PointLoads   = 2
```

| Measured field | Historical | Validation candidate | Delta |
|---|---:|---:|---:|
| `CurrentForceN` | 258.8125000000005 | 230.6347002700199 | -28.17779972998062 |
| `HorizontalForceN` | 291.1848024355736 | 263.007002705593 | -28.17779972998062 |
| `SegmentCurrentForceSumN` | 169.1250000000005 | 140.9472002700199 | -28.17779972998062 |
| `SelectedNodeCount` | 276 | 276 | 0 |
| `SelectedHorizontalOffsetM` | 18.906914306513368 | 19.583341922076137 | 0.6764276155627691 |
| `SelectedAnchorDepthM` | 50 | 49.99914589480685 | -0.0008541051931487686 |
| `SelectedVerticalResidualM` | 0 | -0.0008541051931487686 | -0.0008541051931487686 |
| `SelectedXSumM` | 1823.5192899894134 | 1882.6704123430843 | 59.15112235367087 |
| `SelectedZSumM` | 7227.297933107509 | 7238.744643245494 | 11.446710137985065 |
| `SelectedXSquaredSumM2` | 20364.313397982285 | 22036.994361978304 | 1672.6809639960193 |

### Selected sample X/Z measurements

| Index | Historical X | Candidate X | Delta X | Historical Z | Candidate Z | Delta Z |
|---:|---:|---:|---:|---:|---:|---:|
| 0 | 0 | 0 | 0 | 0.004369922406397109 | 0 | -0.004369922406397109 |
| 69 | 2.3511520235124848 | 1.9675015936113487 | -0.38365042990113607 | 13.601763228978758 | 13.656954903546382 | 0.05519167456762375 |
| 138 | 4.151724956275655 | 4.745945540888112 | 0.5942205846124571 | 27.282735454621093 | 27.172279090933163 | -0.11045636368793055 |
| 207 | 10.964556748319001 | 11.41836368265296 | 0.4538069343339579 | 38.96011972810396 | 39.12754315852379 | 0.16742343041983077 |
| 275 | 18.906914306513368 | 19.583341922076137 | 0.6764276155627691 | 50 | 49.99914589480685 | -0.0008541051931487686 |

The sample extrema reproduce the committed historical audit summary:

```text
MaxSelectedSampleXDeltaM = 0.6764276155627691
MaxSelectedSampleZDeltaM = 0.16742343041983077
```

## 6. Blocked fixtures — no arbitrary candidate numbers

The existing audit reports no available signed feedback candidate for these three fixtures:

```text
vertical-zero-current
  InitialClass = VerticalGeometryUniqueForceStateFamily
  Stop         = InitialBoundary:VerticalGeometryUniqueForceStateFamily
  Historical X/Z = 0 / 50 m

buoyant-line
  InitialClass = TautNonZeroHorizontalLoadNoFiniteRoot
  Stop         = InitialBoundary:TautNonZeroHorizontalLoadNoFiniteRoot
  Historical X/Z = 27.429659239050817 / 30 m
  Historical CurrentForceN = 62.72999999999993 N

depth-varying-current-profile
  InitialClass = TautNonZeroHorizontalLoadNoFiniteRoot
  Stop         = InitialBoundary:TautNonZeroHorizontalLoadNoFiniteRoot
  Historical X/Z = 24.284559370352596 / 50 m
  Historical CurrentForceN = 195.9797868 N
```

Package F does not fill the missing candidate columns with guessed or reconstructed coordinates. The vertical fixture has known unique geometry but lacks a unique approved force state; the two taut non-zero-horizontal fixtures are physically infeasible under the current inextensible/no-stretch model.

## 7. Fields that remain undefined for signed production migration

The historical audit classifies the following as `ProductionIntegrationRequired`:

```text
TensionKn
AnchorReserve
EstimatedOffsetM
SelectedUsesDiscreteLoads
SelectedConverged
SelectedTensionSumKn
SelectedAngleSumDeg
SelectedSamples.TensionKn
SelectedSamples.AngleFromVerticalDeg
IterativeConverged
IterativeStopReason
DiagnosticsSeverity
```

It separately classifies `SelectedSource` as future source identity.

Package F deliberately does **not** publish signed-production replacements for these fields.

For the two measurable validation candidates:

| Field family | Current historical authority/value | Signed-production value in Package F |
|---|---|---|
| `SelectedSource` | `MooringIterativeSolver.FinalShape` | **Not defined / not authorized** — explicit signed source identity is still a Package D implementation gate. |
| `SelectedConverged` | historical selected iterative/fallback semantics | **Not defined / not authorized** — `BudgetReached` is an audit stop, not an approved production convergence result. |
| `SelectedUsesDiscreteLoads` | current selected-path semantics (`true` in both fixtures) | **Not defined / not authorized** — signed discrete-load meaning remains an explicit authority gate. |
| `TensionKn`, `AnchorReserve`, weak-link/verdict provenance | current scalar `CalculationResult` equilibrium | **Not migrated** — geometry evidence does not transfer downstream force authority. |
| `EstimatedOffsetM` | legacy scalar `H/V * depth` estimate | **Not migrated** and must not be silently redefined as signed endpoint X. |
| selected tension/angle sums and sample tension/angles | current selected `MooringShapeResult.Nodes` | **Not defined / not authorized** for a signed-selected production result until per-node signed equilibrium semantics are validated. |
| iterative convergence/status | facts about the existing iterative solver | Remain iterative-solver facts; they must not be relabeled as signed acceptance metadata. |
| diagnostics severity | current core diagnostic result | No signed-production replacement is defined until candidate acceptance/rejection and severity semantics are explicitly integrated. |

Historical scalar values therefore remain exactly as committed. The validation-candidate force/geometry differences in Sections 4–5 are evidence for future migration review, not permission to mix them with historical downstream fields and call the result a coherent signed solution.

## 8. Golden baseline decision

Package F decision:

```text
GoldenBaselineModified = False
ToleranceIntroduced    = False
SelectedAuthoritySwitch = False
DownstreamAuthoritySwitch = False
NumericalGoldenChanges = 0
```

No entry in `engineering-baseline.json` is changed.

This is intentional. At this point:

- three fixtures have no admissible unique signed production equilibrium result to migrate under the current model/state semantics;
- two fixtures have measurable validation feedback candidates, but neither has an approved production acceptance/convergence/source/discrete-load/downstream contract.

Updating the golden file now would convert validation evidence into production authority without closing the RFC gates and would therefore be semantically incorrect.

## 9. Package F exit and future migration rule

Package F closes the **decision/disposition** requirement of RFC #487 for all five current historical fixtures. It does not authorize the selected-X/Z authority switch.

Before any future numerical golden migration for a signed-production result, a later package must:

1. close the applicable Package D candidate/result/source/acceptance/discrete-load/diagnostic gates;
2. close or explicitly isolate the Package E downstream equilibrium/tension/anchor/weak-link/verdict gates;
3. run the candidate from the exact production integration path rather than treating `BudgetReached` audit output as production convergence;
4. reproduce a reviewed old/new table for every field whose committed value would change, including exact sampled coordinates and all downstream fields whose authority is actually transferred;
5. leave physically infeasible and non-unique-force-state fixtures represented by truthful product diagnostics rather than arbitrary golden coordinates or invented Q0 values;
6. obtain green `.NET Build`, `Selected Shape Consumer Scan`, and `Report Store Consumer Scan` on the exact final head.

Until those conditions are met, the committed baseline and current production selected-shape/downstream authority remain unchanged.
