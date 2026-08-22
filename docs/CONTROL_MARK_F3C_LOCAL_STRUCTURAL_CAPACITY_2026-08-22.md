# Control mark — F3-C local structural capacity / weak-link authority — 2026-08-22

Parent milestone: #522  
Issue: #542

## Purpose

F3-B maps the retained exact-fixed-point signed force trace to each internal sequence element and provides wave-aware local design demand. F3-C compares that already-validated local demand with the product's existing MBL + SafetyFactor capacity contract.

This package introduces a typed selected local structural-capacity / governing weak-link authority, but it does **not** overwrite legacy `CalculationResult` weak-link scalars, per-element legacy reserve/status, Checks, Verdict or MainRisk.

## Capacity equation

No new material-strength or dynamic-capacity model is introduced:

```text
MBL_i = existing ElementCalculationRow.BreakingLoadKn
WLL_i = MBL_i / existing CalculationResult.SafetyFactor
Reserve_i = (WLL_i * 1000) / F3-B LocalDesignDemandN_i
```

The governing rated structural element is the minimum finite positive-demand reserve. Equal reserve is resolved by lower physical sequence number.

## Structural scope

The current product model supports explicit MBL for:

```text
line
connector
```

Payload/instrument inputs currently have no MBL property. Therefore payload rows retain F3-B demand provenance but are classified `NotRatedByCurrentModel` and cannot be silently promoted to a weak-link capacity candidate.

A line/connector with missing or non-positive MBL is `CapacityUnavailable`.

## Connector Count semantics

Production `AssemblyItemViewModel` fixes connector count to exactly one:

```text
EditorHint: quantity fixed 1
ToInput(): connector Count = 1
```

The lower-level calculation model historically accepts a `Count` integer and multiplies connector weight/projected area while leaving connector MBL unchanged. That does **not** define parallel or series strength scaling.

F3-C therefore requires:

```text
connector Count == 1
```

Any programmatic/legacy `Count != 1` is classified `UnsupportedConnectorCount`. F3-C does not multiply or divide connector MBL by Count.

## Availability / incomplete coverage

The selected capacity state exists only when the F3-B selected local-demand state exists. Non-Accepted / legacy-selected scenarios remain unavailable.

For expected structural line/connector rows:

- local demand missing -> `DemandUnavailable`;
- MBL missing/non-positive -> `CapacityUnavailable`;
- SafetyFactor non-positive/non-finite -> `SafetyFactorUnavailable`;
- connector Count != 1 -> `UnsupportedConnectorCount`;
- known zero local demand -> `NoPositiveDemand` and no fabricated finite reserve;
- valid positive demand with reserve >= 1 -> `Ok`;
- valid positive demand with reserve < 1 -> `Insufficient`.

`StructuralCapacityCoverageComplete` is true only when at least one expected structural element exists and every expected line/connector has usable demand/capacity semantics. Payload not-rated rows do not make structural coverage incomplete because the current model does not define payload as a capacity-rated load-path element.

A governing row may be reported among known rated elements even if coverage is incomplete, but incomplete coverage must never be interpreted as an overall safe/pass claim.

## Canonical evidence

Historical expectations remain:

```text
uniform-current-slack-line      Accepted         F3-C available
discrete-payload                Accepted         F3-C available; payload not rated
buoyant-line                    RejectedPhysical F3-C unavailable
depth-varying-current-profile   RejectedPhysical F3-C unavailable
vertical-zero-current           Indeterminate    F3-C unavailable
```

Dedicated regression independently recomputes WLL, local reserve and governing minimum reserve. Synthetic deterministic evidence covers:

- equal-reserve sequence tie;
- insufficient connector reserve;
- unsupported connector Count > 1;
- missing structural MBL;
- unavailable local demand;
- exactly zero local demand.

## Authority boundary

```text
F3-C local structural capacity / weak link = new typed selected authority
Legacy WeakLinkBreakingLoadKn              = unchanged compatibility scalar
Legacy WeakLinkName                        = unchanged
Legacy WorkingLoadKn                       = unchanged
Legacy TensionReserve                      = unchanged
Legacy ElementRows Reserve / Status        = unchanged
Legacy anchor holding / reserve            = unchanged
Checks / Verdict / MainRisk                = unchanged
Selected X/Z                               = unchanged
```

No solver equation, wave equation, segmentation, feedback, signed-weight, anchor-capacity, persistence, PDF/2D/UI or 3D behavior changes.

## Next

Physics Milestone F3 is complete after this authority is independently green and merged. F4 can then integrate validated F1/F2/F3 authorities into dependent Checks/Verdict/MainRisk through a separate focused package. Anchor horizontal holding-capacity remains explicitly compatibility-only until a separately validated soil/anchor capacity model exists; F4 must not pretend otherwise.
