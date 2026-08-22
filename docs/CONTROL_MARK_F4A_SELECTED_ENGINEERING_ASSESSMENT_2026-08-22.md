# Control mark — F4-A selected engineering assessment — 2026-08-22

Parent milestone: #522  
Issue: #544

## Purpose

F1, F2 and F3 now provide independently validated typed selected authorities for wave-aware design tension, anchor-end reaction/contact and local structural weak-link capacity. F4-A composes those authorities into one immutable selected engineering assessment retained in `CalculationSnapshot`.

This package does **not** migrate PDF, 2D, UI, user-report or technical-report presentation. Legacy `CalculationResult.Checks`, `Verdict`, `MainRisk`, weak-link scalars and anchor holding fields remain unchanged.

## Availability

The assessment is available only when the selected calculation chain exposes all of:

```text
SignedBoundaryFeedback selected source
F1 selected design-tension demand
F2 selected anchor reaction/contact
F3 selected local structural capacity
```

The three non-Accepted canonical scenarios retain no fabricated F1-F4 selected assessment state.

## Direct hard preconditions

The following preconditions remain calculation-core facts independent of weak-link or soil-capacity migration:

```text
NetBuoyancyKg <= 0                       -> HardFailure
DepthM > 0 and LineLengthM < DepthM     -> HardFailure
AnchorWeightWaterKg <= 0                -> HardFailure
```

They are evaluated from typed calculation inputs/results. F4-A does not parse report/check strings to recover engineering state.

Selected verdict policy:

```text
any HardFailure                         -> Не подходит
otherwise any RequiresReview            -> Требуется проверка
otherwise                               -> Подходит
```

## Anchor contact

F2-B selected reaction/contact is consumed directly:

```text
CompressiveContact -> Ok
ZeroNormalLimit     -> RequiresReview
UpliftSeparation    -> RequiresReview
```

`UpliftSeparation` is the validated rigid-body boundary/contact classification. It is **not** reinterpreted as a validated geotechnical uplift-capacity model for plate, mushroom or embedded anchors.

## Local structural capacity

F3-C is consumed directly:

```text
coverage incomplete          -> RequiresReview
coverage complete + reserve<1 row(s) -> RequiresReview
coverage complete + no insufficient row -> Ok
```

The selected assessment retains F3-C governing element identity/reserve. Legacy global `TensionReserve` does not authorize the selected assessment.

## Horizontal anchor-capacity disposition

F2-C validated the selected anchor demand/contact boundary but explicitly did **not** validate the old holding multipliers as Coulomb friction `mu` or as a soil/embedment capacity model.

Therefore F4-A always records:

```text
AnchorHorizontalCapacityDisposition = RequiresAdditionalPhysicalModel
AnchorHorizontalCapacity check      = RequiresReview
```

The selected horizontal demand comes from F2. Legacy fields remain compatibility diagnostics only:

```text
AnchorHoldingKg
RequiredAnchorHoldingKg
AnchorReserve
```

They cannot produce a selected-authority `Подходит` verdict.

This deliberately means that, under the current pre-v1 physics boundary, an otherwise acceptable `SignedBoundaryFeedback` design normally remains `Требуется проверка` until a separately validated anchor/soil horizontal-capacity model exists. This is preferable to a false engineering pass.

## MainRisk priority

Risk selection is deterministic and does not depend on translated report strings:

```text
1. non-positive net buoyancy
2. line shorter than depth
3. non-positive anchor submerged weight
4. anchor zero-normal/uplift contact state
5. incomplete or insufficient local structural capacity
6. missing validated horizontal anchor-capacity model
```

## Snapshot boundary

For one completed application calculation, `CalculationSnapshot` now retains the selected authority chain once:

```text
SelectedDesignEnvelope
SelectedDesignTensionDemand
SelectedAnchorReaction
SelectedLocalElementDemand
SelectedLocalStructuralCapacity
SelectedEngineeringAssessment
```

All are projections from the same completed `CalculationResult`, sequence positions and selected Accepted signed candidate. Downstream F4-B consumers must read these snapshot states rather than recompute tension, reaction, reserve or verdict.

## Canonical evidence

Expected historical availability remains:

```text
uniform-current-slack-line      Accepted         F1-F4 selected assessment available
discrete-payload                Accepted         F1-F4 selected assessment available
buoyant-line                    RejectedPhysical selected assessment unavailable
depth-varying-current-profile   RejectedPhysical selected assessment unavailable
vertical-zero-current           Indeterminate    selected assessment unavailable
```

Dedicated regression also exercises deterministic assessment-policy fixtures for:

- non-positive buoyancy;
- line shorter than depth;
- non-positive anchor submerged weight;
- zero-normal contact;
- uplift separation;
- incomplete local structural coverage;
- insufficient local structural reserve;
- otherwise-clean selected state blocked only by missing horizontal anchor-capacity model;
- hard-failure risk priority over review conditions.

## Preserved authority / behavior

```text
CalculationResult.Checks                   unchanged
CalculationResult.Verdict                  unchanged
CalculationResult.MainRisk                 unchanged
legacy WeakLinkBreakingLoadKn/Name         unchanged
legacy WorkingLoadKn/TensionReserve        unchanged
legacy ElementRows Reserve/Status          unchanged
legacy anchor holding/reserve fields       unchanged
selected X/Z                               unchanged
UserReportBuilder / ReportBuildBoundary    unchanged presentation behavior
PDF / 2D / UI                              unchanged
```

No solver equation, wave equation, anchor-capacity equation, 0.20 m segmentation, feedback budget 64, signed `WeightWaterKgM`, exact fixed-point rule, persistence or 3D change.

## Next

F4-B may migrate user-facing read models to `SelectedEngineeringAssessment` where available and preserve explicit compatibility diagnostics where no selected physical model exists. PDF, 2D, UI and reports remain consumers only; no renderer-local engineering formulas are allowed.
