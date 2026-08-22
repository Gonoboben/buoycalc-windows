# Control mark — downstream scalar authority disposition

Date: 2026-08-22  
RFC: #511  
Package: D — field-by-field authority disposition  
Base main: `e7b6eba7edc0253daa7f489b5d2296542d6f4f0d` (Package C merge)

## Purpose

This control mark classifies every downstream scalar/result field named by RFC #511 before any production authority migration is proposed.

This package is documentation-only. It does not change `CalculationResult`, solver equations, selected-shape arbitration, report/PDF/2D/UI behavior, baselines, persistence, 0.20 m segmentation, production feedback budget 64, signed `WeightWaterKgM` semantics, or 3D.

The governing boundary remains:

```text
selected geometry/source authority != downstream scalar-force authority
```

for the two canonical fixtures where `SignedBoundaryFeedback` is selected.

## Evidence already merged

The disposition is based on the merged #511 evidence chain:

- Package A / PR #512: current downstream ownership code-map and frozen mixed-authority evidence;
- Package B / PR #513, merge `40c518cc82d2c043096e1e608dc76916062905fb`: direct read-only selected signed boundary-state availability;
- Package C / PR #514, merge `e7b6eba7edc0253daa7f489b5d2296542d6f4f0d`: canonical side-by-side legacy/signed evidence.

Canonical signed-candidate truth table remains:

```text
Accepted         = 2
RejectedPhysical = 2
Indeterminate    = 1
```

Direct `MooringSelectedSignedBoundaryState` is available only when the actually selected core source is `SignedBoundaryFeedback` and the same candidate is `Accepted`.

Its direct fields are source identity, boundary classification, `Q0N`, `BuoySteadyDragN`, endpoint X/Z, endpoint H/V, min/max H/V, V-sign-change, point-load crossings, feedback iterations, discrete-load identity and diagnostics. It deliberately does not reconstruct a tension trace and does not replace `CalculationResult` scalar authority.

## Allowed disposition vocabulary

Each candidate field receives exactly one primary disposition:

```text
TransferableFromValidatedSignedState
RequiresIndependentValidation
IntentionallyLegacy
RequiresAdditionalPhysicalModel
NotSemanticallyComparable
```

`TransferableFromValidatedSignedState` means the existing signed selected state already carries the same physical quantity with sufficient validation for a production authority proposal. Package D finds no downstream `CalculationResult` field meeting that bar yet.

## Required downstream-field matrix

| Field | Current production meaning/source | Package D disposition | Physical/model basis |
| --- | --- | --- | --- |
| `CalculationResult.TensionKn` | Global legacy resultant `hypot(HorizontalForceN, max(0, NetBuoyancyKg)*g)` computed in `BuoyCalculator.Calculate()` before snapshot/arbitration | **RequiresIndependentValidation** | The selected signed state exposes boundary H/V components and `Q0N`, but it does not expose an already-authoritative scalar with the same wave/load/policy meaning as legacy `TensionKn`. A reconstructed boundary tension trace is a separate diagnostic contract and is not selected scalar authority. |
| `CalculationResult.WeakLinkBreakingLoadKn` | Minimum structural MBL selected from enabled assembly rows | **IntentionallyLegacy** | This is a material/component capacity property, not a force solved by signed geometry. Geometry selection gives no replacement MBL. |
| `CalculationResult.WorkingLoadKn` | `WeakLinkBreakingLoadKn / SafetyFactor` | **IntentionallyLegacy** | This is capacity/policy arithmetic. It should not move with geometry authority; only the demand side may later be revalidated. |
| `CalculationResult.TensionReserve` | `WorkingLoadKn / TensionKn` | **RequiresIndependentValidation** | Numerator remains a valid capacity/policy quantity, but reserve authority cannot exceed the unresolved tension-demand authority in its denominator. |
| `CalculationResult.RequiredAnchorHoldingKg` | Legacy horizontal aggregate load converted by `HorizontalForceN / g` | **RequiresAdditionalPhysicalModel** | Direct signed endpoint H/V is not automatically an anchor holding demand in kg. Transfer requires an explicit anchor-end load/contact convention, including horizontal/vertical reaction ownership, uplift/contact treatment and the meaning of the current holding-capacity model. |
| `CalculationResult.AnchorReserve` | `AnchorHoldingKg / RequiredAnchorHoldingKg` | **RequiresAdditionalPhysicalModel** | The reserve depends on an anchor demand that is not yet validated against the signed anchor-end force vector and contact/uplift semantics. Capacity coefficients must not be silently reinterpreted. |
| `CalculationResult.EstimatedOffsetM` | Legacy small-angle/global estimate `HorizontalForceN / verticalForceN * DepthM` | **NotSemanticallyComparable** | It is not the same physical contract as selected signed endpoint X. Package C explicitly treats the numerical difference as evidence. Endpoint X must not be aliased into this field. |
| `CalculationResult.Checks` | Mixed legacy policy list consuming buoyancy, line length, weak-link reserve, anchor reserve, seabed and other diagnostics | **RequiresIndependentValidation** | Some checks are independent of signed force state, while weak-link/anchor checks depend on unresolved scalar families. The list cannot be migrated as one unit. |
| `CalculationResult.Verdict` | Policy aggregation over `Checks` (`FAILED`/`WARNING`/rock-note path) | **RequiresIndependentValidation** | Verdict cannot become more authoritative than the checks and force-demand fields feeding it. It must remain legacy until affected upstream families are independently validated. |
| `CalculationResult.MainRisk` | First hard failure/warning or legacy no-critical-risk text derived from `Checks` | **RequiresIndependentValidation** | Same dependency boundary as `Verdict`; it is a policy projection, not a signed solver output. |

## Upstream/supporting fields that constrain any later migration

These fields are not candidates for automatic signed replacement, but their ownership must remain explicit because they feed the required downstream matrix.

| Field/family | Disposition | Reason |
| --- | --- | --- |
| `CurrentForceN` | **IntentionallyLegacy** | Aggregate current-force result remains a separate global diagnostic/input to legacy scalar equations. Package B does not claim it is identical to any signed boundary component. |
| `WaveForceN` | **IntentionallyLegacy** | Existing result carries explicit wave drag. The Package B selected signed state exposes `BuoySteadyDragN`, not an explicit wave-inclusive signed boundary resultant. No silent loss or substitution of wave demand is allowed. |
| `HorizontalForceN` | **IntentionallyLegacy** | Defined as legacy `CurrentForceN + WaveForceN`; signed H/V components are not aliases for this aggregate. |
| `NetBuoyancyKg` | **IntentionallyLegacy** | Existing hydrostatic/global scalar remains valid as its own contract; selected signed geometry does not redefine net buoyancy. |
| `SafetyFactor` | **IntentionallyLegacy** | User/policy input; not solver-owned. |
| `AnchorWeightWaterKg` | **IntentionallyLegacy** | Capacity-side anchor property under the current model. |
| `AnchorBaseHoldingCoefficient` | **IntentionallyLegacy** | Existing anchor-capacity model parameter; no signed geometry replacement. |
| `AnchorTypeMultiplier` | **IntentionallyLegacy** | Existing anchor-capacity model parameter; changing it requires separate anchor-model validation. |
| `SeabedHoldingMultiplier` | **IntentionallyLegacy** | Existing seabed-capacity model parameter; changing it requires separate anchor/seabed validation. |
| `AnchorHoldingKg` | **IntentionallyLegacy** | Existing capacity result from weight-in-water and holding coefficients. Package D does not reinterpret it as a signed reaction capacity. |
| per-element MBL/WLL | **IntentionallyLegacy** | Component capacity data and safety-factor policy remain independent of geometry-source authority. |
| per-element reserve/status derived from legacy `TensionKn` | **RequiresIndependentValidation** | Their demand side inherits unresolved tension authority. |

## Signed quantities that are valid but are not aliases for the fields above

For an Accepted selected `SignedBoundaryFeedback` source, Package B has already validated direct availability of:

```text
Q0N
BuoySteadyDragN
EndpointXM
EndpointZM
EndHN
EndVN
MinHN / MaxHN
MinVN / MaxVN
VSignChange
PointLoadCrossings
FeedbackIterations
ContainsDiscreteLoads
```

These values may be used as source-backed evidence in later validation packages. Their existence does not by itself authorize:

```text
TensionKn = hypot(EndHN, EndVN)
RequiredAnchorHoldingKg = abs(EndHN) / g
EstimatedOffsetM = EndpointXM
AnchorReserve = AnchorHoldingKg / signed-derived-demand
Verdict = policy(signed-derived-demand)
```

Any such equation would be a new production authority/model decision and requires its own evidence package.

## Package E gates by scalar family

### E1 — tension demand / weak-link demand side

Status: **eligible for independent validation work, not yet eligible for production switch**.

Required before a behavior-change PR:

1. define exactly which physical location/resultant `TensionKn` is intended to represent;
2. state wave inclusion/exclusion explicitly;
3. define whether boundary endpoint, top-of-line, maximum-local, or another tension is the design demand;
4. compare against an independent analytical/reference implementation on canonical and additional fixtures;
5. verify discrete point-load handling and signed vertical-force semantics;
6. only then evaluate `TensionReserve` and element reserve/status behavior.

### E2 — anchor demand / anchor reserve

Status: **blocked by additional physical-model definition**.

Required before authority transfer:

1. define the actual anchor-end line force vector and sign convention;
2. define seabed-contact and uplift behavior;
3. define how horizontal and vertical demand interact with each anchor type;
4. validate whether the existing scalar holding-coefficient model is compatible with that vector demand;
5. independently validate against a physical/reference model.

No Package E anchor behavior change should be opened before those questions are resolved.

### E3 — offset

Status: **no authority transfer proposed**.

`EstimatedOffsetM` and selected signed `EndpointXM` are different contracts. Keep `EstimatedOffsetM` legacy unless a later explicit deprecation/rename removes ambiguity. Do not write endpoint X into the legacy field.

### E4 — checks / verdict / main risk

Status: **blocked on upstream validated families**.

Policy migration must occur only after the relevant tension/anchor demand families are validated. Unaffected checks may remain legacy. A verdict cannot be upgraded merely because geometry is signed-selected.

## Review conclusion

At the end of Package D:

```text
TransferableFromValidatedSignedState downstream CalculationResult fields = 0
RequiresIndependentValidation = TensionKn, TensionReserve, Checks, Verdict, MainRisk (+ demand-derived element reserve/status)
IntentionallyLegacy = WeakLinkBreakingLoadKn, WorkingLoadKn and current capacity/global-supporting fields
RequiresAdditionalPhysicalModel = RequiredAnchorHoldingKg, AnchorReserve
NotSemanticallyComparable = EstimatedOffsetM
```

This is a deliberate safety result, not a failure to migrate. The signed geometry/source switch has progressed farther than downstream scalar-force validation, so scalar authority remains legacy until each physical family earns a separate transfer.

## Non-change statement

Package D changes documentation only. It does not authorize or implement any Package E production authority change. Specifically unchanged:

- solver equations;
- signed candidate acceptance;
- exact 0.20 m segmentation;
- production feedback budget 64;
- signed `WeightWaterKgM` semantics;
- selected geometry/source authority;
- `CalculationResult` formulas and values;
- weak-link/WLL policy;
- anchor/seabed equations or coefficients;
- PDF/2D/UI physics;
- persistence/DTO schema;
- 3D.
