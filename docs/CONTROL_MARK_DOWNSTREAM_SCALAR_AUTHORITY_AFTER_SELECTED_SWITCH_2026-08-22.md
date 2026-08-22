# Control mark — downstream scalar authority after selected-shape switch

Date: 2026-08-22  
Issue: #511  
Base main: `114c523af374aee03e211d922a950bbd9f17e603`  
Package: A — ownership code-map and frozen evidence  
Behavior change: **none**

## 1. Purpose

Package 5 of #505 / PR #510 changed the selected user-facing geometry/source boundary for an `Accepted` signed candidate:

```text
Accepted SignedBoundaryFeedback
    -> selected X/Z/source may come from signed core result

non-Accepted signed candidate
    -> complete legacy selected read model is preserved
```

That merge deliberately kept downstream scalar-force authority unchanged.

This control mark records the production ownership boundary that exists immediately after #510. It does not propose a replacement equation and does not authorize a scalar migration.

The central fact is:

```text
selected geometry/source authority
    is not the same production path as
legacy scalar tension / weak-link / anchor / EstimatedOffsetM / verdict authority
```

For the two canonical `Accepted` fixtures this mixed authority is now an explicit transitional architecture state, not an accidental renderer behavior.

## 2. Application calculation order

`ApplicationModel/ApplicationCalculationRunner.cs` owns the top-level ordering:

```text
BuoyCalculator.Calculate(...)
    -> CalculationResult

CalculationSnapshotBuilder.Build(..., CalculationResult)
    -> TechnicalReportData
    -> legacy selected shape
    -> signed candidate
    -> typed selected-core arbitration
    -> SelectedShapeReadModel projection
```

The source order is explicit:

```csharp
var result = BuoyCalculator.Calculate(...);
var snapshot = CalculationSnapshotBuilder.Build(environment, buoy, result);
```

Therefore every scalar already stored in `CalculationResult` is calculated before Package-5 selected-shape arbitration occurs.

`CalculationSnapshot` later retains the same `CalculationResult` instance together with `TechnicalReportData`, `SelectedShape`, `SignedCandidate` and the typed selected-core result.

No Package-5 projector writes back into `CalculationResult`.

## 3. Legacy scalar owner — `BuoyCalculator.Calculate`

Production owner:

```text
Models/EngineeringModels.cs
BuoyCalculator.Calculate(...)
```

### 3.1 Aggregate horizontal load

The legacy scalar path calculates:

```text
currentForce =
    buoyCurrentForce
  + lineCurrentForce
  + connectorCurrentForce
  + payloadCurrentForce

waveForce = DragForce(... waveVelocity ...)

horizontalForce = currentForce + waveForce
```

This `HorizontalForceN` is not selected from `MooringSelectedShapeResult`.

### 3.2 Aggregate vertical term and scalar `TensionKn`

The current scalar tension is:

```text
verticalForceN = max(0, netBuoyancyKg) * g

tensionN  = hypot(horizontalForce, verticalForceN)
TensionKn = tensionN / 1000
```

Owner classification:

```text
CalculationResult.TensionKn
    owner = legacy aggregate-force path
    selected-shape arbitration dependency = none
```

This is not automatically the top, bottom or local tension of an Accepted signed boundary-feedback trace.

## 4. Weak-link family

The current weak-link family is derived from the same legacy scalar `TensionKn`.

Structural elements are identified from assembly rows and the weakest positive breaking load is selected:

```text
WeakLinkBreakingLoadKn = minimum positive structural BreakingLoadKn
WeakLinkName           = matching element identity
```

Then:

```text
WorkingLoadKn  = WeakLinkBreakingLoadKn / SafetyFactor
TensionReserve = WorkingLoadKn / TensionKn
```

Ownership table:

| Field | Current production owner | Selected signed geometry dependency |
|---|---|---|
| `WeakLinkBreakingLoadKn` | structural element inventory | none |
| `WeakLinkName` | structural element inventory | none |
| `WorkingLoadKn` | weak-link MBL + safety factor | none |
| `TensionReserve` | legacy `TensionKn` + `WorkingLoadKn` | none |

The identity of the structurally weakest element is a separate concept from the force state used to load it. A future force-authority change must not silently alter element strength data.

## 5. Anchor family

The current holding capacity is calculated from anchor properties and seabed/type multipliers:

```text
AnchorHoldingKg =
    AnchorWeightWaterKg
  * AnchorBaseHoldingCoefficient
  * AnchorTypeMultiplier
  * SeabedHoldingMultiplier
```

Current demand is calculated only from aggregate horizontal force:

```text
RequiredAnchorHoldingKg = HorizontalForceN / g
AnchorReserve = AnchorHoldingKg / RequiredAnchorHoldingKg
```

Ownership table:

| Field | Current production owner | Selected signed geometry dependency |
|---|---|---|
| `AnchorHoldingKg` | legacy anchor/seabed coefficient model | none |
| `RequiredAnchorHoldingKg` | legacy aggregate `HorizontalForceN` | none |
| `AnchorReserve` | legacy holding capacity / legacy horizontal demand | none |

No anchor-end signed tension vector, uplift/contact reaction or seabed reaction is substituted by Package 5.

Consequently an Accepted signed selected geometry does **not** by itself justify changing `RequiredAnchorHoldingKg` or `AnchorReserve`.

## 6. Legacy `EstimatedOffsetM`

Current formula:

```text
EstimatedOffsetM =
    verticalForceN > 0
        ? HorizontalForceN / verticalForceN * DepthM
        : 0
```

This value is a legacy aggregate-force estimate.

It is not the selected-shape endpoint X coordinate and must not be aliased to it merely because Package 5 now has a signed selected endpoint.

Current authority classification:

```text
CalculationResult.EstimatedOffsetM
    owner = legacy aggregate-force estimate
    SelectedShape.Shape.HorizontalOffsetM = separate geometry quantity
```

The two values may differ substantially and are not assumed semantically interchangeable.

## 7. Checks, verdict and main risk

`BuoyCalculator.Calculate(...)` constructs `CalculationResult.Checks` from legacy calculation state including:

```text
net buoyancy
line length versus depth
weak-link availability
legacy TensionReserve
anchor weight in water
legacy AnchorReserve
seabed identity
segment/current informational rows
```

`CalculationResult.Verdict` and `CalculationResult.MainRisk` are then derived from those checks.

Therefore:

```text
CalculationResult.Verdict
CalculationResult.MainRisk
    owner = legacy core check policy fed by legacy scalar values
```

They are not recomputed by `MooringSelectedShapeArbitrator` or `SelectedMooringShapeReadModelProjector`.

This is an important trust boundary: selected signed geometry cannot make a legacy verdict more physically authoritative than the scalar force state feeding that verdict.

## 8. Display policy does not change physics ownership

`Services/VerdictDisplayAdvisor.cs` can adapt the wording/status shown to a user, but it consumes the already-built `CalculationResult`.

Examples of its inputs include:

```text
AnchorWeightWaterKg
AnchorHoldingKg
LineLengthM
AnchorReserve
TensionReserve
RequiredAnchorHoldingKg
Checks
Verdict
MainRisk
```

It does not solve a signed force state.

`Services/UserReportBuilder.cs` similarly renders existing values:

```text
result.TensionKn
result.WeakLinkName
result.TensionReserve
result.AnchorReserve
```

This confirms that presentation code is a consumer of scalar authority, not its owner.

## 9. Selected signed geometry/source owner

The Package-5 geometry path is later in `CalculationSnapshotBuilder`.

First, the complete legacy selected read model is built:

```csharp
var selectedShape = SelectedMooringShapeProvider.Build(
    data.Shape,
    data.IterativeSolver);
```

The typed current selection is represented as `MooringSelectedShapeResult` where possible.

Then the production signed candidate is built:

```csharp
var signedCandidate = MooringSignedCandidateEvaluator.Build(
    environment,
    buoy,
    result,
    data.SequencePositions);
```

Typed arbitration is centralized in:

```text
MooringSelectedShapeArbitrator
```

and the user-facing read model is projected only after arbitration:

```csharp
selectedShape = SelectedMooringShapeReadModelProjector.Project(
    selectedShape,
    shadowSelectedCore);
```

For an `Accepted` signed candidate, selected geometry/source can therefore be:

```text
SourceIdentity = SignedBoundaryFeedback
Shape          = signed accepted candidate shape
```

while all scalar fields in `CalculationResult` remain the pre-existing values described above.

## 10. Technical force-state families already calculated before selection

`TechnicalReportDataBuilder` currently computes several distinct diagnostic/model-stage families from `CalculationResult`, including:

```text
SegmentTensionAnalyzer
MooringSignedOrientationAnalyzer
MooringShapeSolver
MooringShapeProjection
MooringShapeForceAnalyzer
MooringShapeTensionAnalyzer
MooringForceShapeConsistencyAnalyzer
MooringSurfaceBoundaryInfoAnalyzer
MooringSurfaceBoundaryTensionTraceBuilder
MooringDiscreteLoadTensionAnalyzer
MooringDiscreteLoadShapeBuilder
MooringSignedNodeEquilibriumAnalyzer
MooringIterativeSolver
final-iteration signed-node equilibrium
MooringVectorBalance
```

These are not automatically equivalent model states.

Package A deliberately does **not** declare any one of these diagnostics to be the new production owner of `CalculationResult.TensionKn`, anchor demand, weak-link reserve or verdict.

Issue #511 Package B must first determine which signed quantities are actually available and semantically tied to an Accepted `SignedBoundaryFeedback` result without reconstructing or mixing model stages.

## 11. Canonical Package-5 selected-authority evidence

Merged #510 exact-head evidence:

```text
vertical-zero-current
  CandidateStatus = Indeterminate
  OldSource = MooringIterativeSolver.FinalShape
  NewSource = MooringIterativeSolver.FinalShape
  OldX/NewX = 0 / 0
  OldZ/NewZ = 50 / 50
  Switched = False

uniform-current-slack-line
  CandidateStatus = Accepted
  OldSource = MooringIterativeSolver.FinalShape
  NewSource = SignedBoundaryFeedback
  OldX/NewX = 22.904164818523228 / 22.07360565566908
  OldZ/NewZ = 50 / 50.00076700519351
  Switched = True

discrete-payload
  CandidateStatus = Accepted
  OldSource = MooringIterativeSolver.FinalShape
  NewSource = SignedBoundaryFeedback
  OldX/NewX = 18.906914306513368 / 19.583341922076137
  OldZ/NewZ = 50 / 49.99914589480685
  Switched = True

buoyant-line
  CandidateStatus = RejectedPhysical
  OldSource = MooringIterativeSolver.FinalShape
  NewSource = MooringIterativeSolver.FinalShape
  OldX/NewX = 27.429659239050817 / 27.429659239050817
  OldZ/NewZ = 30 / 30
  Switched = False

depth-varying-current-profile
  CandidateStatus = RejectedPhysical
  OldSource = MooringIterativeSolver.FinalShape
  NewSource = MooringIterativeSolver.FinalShape
  OldX/NewX = 24.284559370352596 / 24.284559370352596
  OldZ/NewZ = 50 / 50
  Switched = False
```

Rollup:

```text
Scenarios=5
Switched=2
Preserved=3
Accepted=2
RejectedPhysical=2
Indeterminate=1
DownstreamScalarAuthority=LegacyUnchanged
```

## 12. Current ownership matrix

| Quantity | Current authority | Package-5 signed geometry switch changes it? | Package #511 disposition now |
|---|---|---:|---|
| selected X/Z | typed selected-core arbitration | yes for Accepted | already switched in #510 |
| selected source identity | typed selected-core arbitration | yes for Accepted | already switched in #510 |
| `TensionKn` | `BuoyCalculator` aggregate force | no | frozen pending evidence |
| weak-link identity/MBL | structural inventory | no | frozen |
| `WorkingLoadKn` | MBL / safety factor | no | frozen |
| `TensionReserve` | legacy `TensionKn` | no | frozen pending force authority |
| `AnchorHoldingKg` | current anchor/seabed coefficient model | no | frozen |
| `RequiredAnchorHoldingKg` | legacy aggregate horizontal force | no | frozen pending anchor-end evidence |
| `AnchorReserve` | legacy holding / legacy demand | no | frozen pending anchor semantics |
| `EstimatedOffsetM` | legacy aggregate-force estimate | no | intentionally distinct from endpoint X |
| `Checks` | `BuoyCalculator` policy | no | frozen pending upstream authority |
| `Verdict` / `MainRisk` | `BuoyCalculator` checks | no | frozen pending upstream authority |
| user display verdict | `VerdictDisplayAdvisor` projection/advice | no physics ownership | presentation consumer only |

## 13. Package A conclusion

The post-#510 architecture is internally explicit:

```text
ApplicationCalculationRun.Result
    = legacy scalar/result authority

ApplicationCalculationRun.Snapshot.SelectedShape
    = selected geometry/source projection
      that may use Accepted SignedBoundaryFeedback
```

This is a mixed-authority state by design.

The next safe step is **not** to replace scalar values. It is Issue #511 Package B: inventory the signed boundary/tension-trace state attached to an Accepted signed candidate, classify what is truly available, and identify what would require additional physical modeling or independent validation.

## 14. Frozen non-changes

This Package A changes no runtime code and authorizes no change to:

```text
solver equations
selected arbitration
candidate acceptance
0.20 m segmentation
64-step signed feedback budget
signed WeightWaterKgM semantics
drag/wave equations
anchor/seabed equations
TensionKn
TensionReserve
AnchorReserve
EstimatedOffsetM
Checks
Verdict
JSON/DTO
PDF/2D/UI physics
3D
```
