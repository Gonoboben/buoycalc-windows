# Control mark: signed geometry final acceptance evidence and production blockers — 2026-08-18

Issue: #407 — Physics RFC: preserve signed planar force orientation in tension-to-shape path.

This control mark closes the validation/evidence scope of #407. It does **not** authorize a production geometry switch.

## Governing invariants

The following remain unchanged:

- production segmentation target is exactly `0.20 m` with no segment-count cap;
- signed `WeightWaterKg` semantics are preserved;
- no production solver/gate/verdict/anchor/weak-link/2D/PDF geometry change is authorized here;
- committed historical golden baseline is unchanged;
- no new numerical tolerance is introduced to make a candidate pass.

## Acceptance evidence 1–7

### 1. Source-backed signed orientation convention — COMPLETE

Project planar convention remains:

```text
+X = horizontal offset direction
+Z = depth, positive downward
s  = line coordinate from buoy/top toward anchor/bottom
```

Signed tangent is represented by normalized signed H/V components, not by an authoritative unsigned scalar angle.

### 2. Synthetic quadrant tests — COMPLETE

Validation covers heavy/downward, buoyant/upward, pure vertical and degenerate-resultant cases. `V < 0` preserves `TangentZ < 0`; zero resultant is indeterminate rather than manufactured as a vertical direction.

### 3. No sign loss between H/V and tangent vector — COMPLETE

Boundary-conditioned trace stores signed `TangentX/TangentZ` derived from the shared physics-owned integration kernel. Validation geometry reconstructs each segment only as:

```text
dx = ds * TangentX
dz = ds * TangentZ
```

No `Abs(H)`, `Abs(V)` or first-quadrant angle clamp is used in the signed validation path.

### 4. Analytical limiting cases — COMPLETE

The surface-boundary work under #413 and the follow-on #407 validation packages established exact/analytical constant-load and point-load references, global force-accounting identities and signed tangent reconstruction identities.

### 5. Convergence-study evidence — COMPLETE

The raw boundary-conditioned fixed-point experiment used `alpha = 1` with independent budgets `4 / 8 / 16 / 32 / 64`.

Canonical A–E behavior:

- by budget 8, `MaxNodeDeltaM` is approximately `1e-8 ... 1e-9 m`;
- by budget 16, geometry/force deltas are zero or machine-noise scale;
- `NegativeDz = 0` for all solved A–E cases;
- point-load jump closure remains approximately `1e-13 N`;
- depth residual stays within about `0.75 ... 7.79 mm`.

Under-relaxation is therefore not required by the measured A–E stability evidence. This does not by itself authorize production use.

The converged candidate differs from the historical selected X sufficiently that correctness still required an independent reference check.

### 6. Independent/reference comparison — COMPLETE

Independent continuous analytical reference fixture:

```text
neutral line length = 100 m
target depth        = 80 m
uniform horizontal current = 0.5 m/s
line diameter       = 20 mm
Cd                  = 1.2
point loads         = 0
```

The analytical side does not use the production integration kernel, production shape-force analyzer or feedback harness.

Measured comparison:

```text
Reference Q0       = 160.1062112251539 N
Candidate Q0       = 160.1182215270996 N
Delta Q0           = +0.012010301945679203 N
Relative Q0        = +7.501459096292882e-05

Reference X        = 54.633504127415236 m
Candidate X        = 54.631743732148905 m
Delta X            = -0.001760395266330761 m
Relative X         = -3.2221899262130434e-05

Reference Z        = 80.0 m
Candidate Z        = 80.00134323139692 m
Delta Z            = +0.0013432313969303777 m

Reference end H    = 201.35025679395034 N
Candidate end H    = 201.35647834808202 N
Delta end H        = +0.00622155413168457 N

Reference line F   = 201.35025679395034 N
Candidate line F   = 201.35647834808202 N
Delta line F       = +0.00622155413168457 N
```

The comparison shows close independent agreement without introducing a post-hoc acceptance tolerance. The candidate depth residual is `+0.0013432313969161669 m` and `NegativeDz = 0`.

### 7. Explicit review of every historical golden change — COMPLETE, WITH PRODUCTION BLOCKERS

The five committed historical fixtures were audited without modifying the golden JSON and without introducing a tolerance.

Roll-up:

```text
Scenarios          = 5
Candidate available= 2
Candidate blocked  = 3
Golden modified    = false
Tolerance introduced = false
```

#### Blocker 1 — `vertical-zero-current`

```text
CandidateAvailable = false
InitialClass       = VerticalGeometryBoundaryNonUnique
Historical X/Z    = 0 / 50 m
ProductionSwitchBlocker = true
```

A production geometry switch cannot manufacture a signed candidate for this fixture while the boundary model classifies the vertical geometry as non-unique.

#### Measured candidate — `uniform-current-slack-line`

```text
Q0 = 379.810165863037 N
CurrentForceN: 251.12500000000048 -> 222.1144636219283 N
Delta current force = -29.010536378072175 N

Selected X: 22.904164818523228 -> 22.073605655669077 m
Delta X = -0.8305591628541507 m

Selected anchor Z: 50 -> 50.0007670051935 m
Depth residual = +0.000767005193502257 m

Max selected-sample X delta = 2.849246067463458 m
Max selected-sample Z delta = 1.0652539259144334 m
NegativeDz = 0
PointLoads = 0
ProductionSwitchBlocker = false
```

This fixture is measurable, but the historical result would materially change and therefore still requires an explicit production migration decision.

#### Blocker 2 — `buoyant-line`

```text
CandidateAvailable = false
InitialClass       = TautNonZeroHorizontalLoadNoFiniteRoot
Historical X/Z    = 27.429659239050817 / 30 m
Historical CurrentForceN = 62.72999999999993 N
ProductionSwitchBlocker = true
```

The current boundary-conditioned candidate does not provide a finite root for this historical fixture. No production value may be invented or substituted.

#### Measured candidate — `discrete-payload`

```text
Q0 = 720.8641923522947 N
CurrentForceN: 258.8125000000005 -> 230.6347002700199 N
Delta current force = -28.17779972998062 N

Selected X: 18.906914306513368 -> 19.583341922076137 m
Delta X = +0.6764276155627691 m

Selected anchor Z: 50 -> 49.99914589480685 m
Depth residual = -0.0008541051931487686 m

Max selected-sample X delta = 0.6764276155627691 m
Max selected-sample Z delta = 0.16742343041983077 m
NegativeDz = 0
PointLoads = 2
ProductionSwitchBlocker = false
```

This fixture is measurable, but downstream tension/anchor/verdict integration is still not defined by the validation-only candidate.

#### Blocker 3 — `depth-varying-current-profile`

```text
CandidateAvailable = false
InitialClass       = TautNonZeroHorizontalLoadNoFiniteRoot
Historical X/Z    = 24.284559370352596 / 50 m
Historical CurrentForceN = 195.9797868 N
ProductionSwitchBlocker = true
```

The profile fixture therefore prevents a production signed-geometry authority switch in the present model.

## Historical fields requiring production integration

The audit classified every historical `ScenarioSnapshot` field. The following cannot be declared migrated by validation geometry alone:

```text
TensionKn
AnchorReserve
EstimatedOffsetM
SelectedUsesDiscreteLoads
SelectedConverged
SelectedTensionSumKn
SelectedAngleSumDeg
IterativeConverged
IterativeStopReason
DiagnosticsSeverity
```

`SelectedSamples.TensionKn` and `SelectedSamples.AngleFromVerticalDeg` are likewise production-integration-required.

`SelectedSource` requires an explicit future source-identity decision.

The following remain provably unchanged by the validation-only study: input/environmental identity fields, buoyancy/weight aggregates, wave force, anchor holding input, line length, element/segment counts, production `0.20 m` segmentation metrics, signed line-water-weight aggregate, speed extrema and discrete-element count.

## Final #407 conclusion

All seven required pre-production evidence items have now been completed.

The evidence **does not authorize a production geometry switch**. Instead it establishes the next boundary clearly:

1. resolve the three candidate-unavailable historical fixtures;
2. define production ownership of signed geometry, tension, convergence/gate state and source identity;
3. integrate downstream anchor/weak-link/verdict/report consumers only through the calculation core;
4. review every resulting historical golden change explicitly before any baseline update;
5. keep the current selected X/Z authoritative until that separate Physics RFC is completed and approved.

Therefore #407 may be closed as a completed validation/source RFC with production blockers carried into a new, separately scoped Physics RFC.
