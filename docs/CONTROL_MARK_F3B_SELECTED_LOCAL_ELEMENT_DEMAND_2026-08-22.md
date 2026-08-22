# Control mark — F3-B selected local element demand — 2026-08-22

Parent milestone: #522  
Issue: #540

## Purpose

F3-A retained the exact final steady-current tension trace from the Accepted signed feedback fixed point. F3-B projects that solver provenance onto the actual physical assembly sequence so each internal line, connector and payload has a location-specific quasi-static design demand.

This package does **not** calculate WLL, reserve or governing weak-link status.

## Authority prerequisites

The local map exists only when all of the following are true:

```text
selected source = SignedBoundaryFeedback
candidate status = Accepted
exact fixed point = true
FinalTensionTrace retained = true
```

Legacy/fallback selected geometry receives no fabricated signed local-demand map.

## Wave semantics

F3-B reuses the already validated F1 v1 design-envelope policy and does not introduce a new wave model:

```text
H_design = H_steady + CalculationResult.WaveForceN
V_design = V_steady
T_design = sqrt(H_design^2 + V_design^2)
```

The existing wave proxy is added exactly once to H and never fed back into X/Z geometry.

## Distributed line demand

Each line owns its `MooringSequencePositionRow` interval `[s0,s1]`. Every retained final-trace segment must map to exactly one line interval.

For every segment belonging to a line, F3-B evaluates design demand at the three direct retained states:

```text
segment start
segment midpoint
segment end
```

The line demand is the maximum over all such states in that line range. The state records:

- governing resultant;
- start/mid/end location kind;
- governing segment number;
- governing coordinate `s`;
- steady H/V;
- wave-aware design H/V.

Tie breaking is deterministic: larger demand first, then lower physical `s`, then start -> midpoint -> end, then segment number.

A zero-length enabled line with no production segments is explicitly marked unavailable (`NoProductionSegmentsInLineRange`) instead of inventing demand.

## Discrete point demand

Internal connectors/payloads are ordered exactly as the existing integration kernel:

```text
PositionAlongLineM
then sequence Number
```

The retained trace provides the force state around each group of point-load crossings. Within each group, F3-B applies only the already-existing explicit point jump for each element:

```text
H_after = H_before + point.CurrentForceN
V_after = V_before - point.WeightWaterKg * g
```

No distributed segment force and no hidden feedback state is reconstructed.

The local structural demand of one discrete point element is conservatively:

```text
max(T_design_before_own_jump, T_design_after_own_jump)
```

Both side states/resultants are retained. Ties govern on the `PointBefore` side deterministically.

Point-load closure back to retained trace start states/terminal state is checked against the existing 1e-6 N point-load identity contract already used by the Accepted evaluator. This is numerical identity checking, not an engineering acceptance tolerance.

## Scope of rows

F3-B creates one row per **internal** sequence item:

```text
line
connector
payload
```

The surface buoy and seabed anchor remain separate boundary authorities already handled by F1/F2 and are not duplicated as local weak-link rows.

## Canonical evidence

Historical canonical expectations remain:

```text
uniform-current-slack-line      Accepted        local map available, no point loads
discrete-payload                Accepted        local map available, point sides resolved
buoyant-line                    RejectedPhysical local map unavailable
depth-varying-current-profile   RejectedPhysical local map unavailable
vertical-zero-current           Indeterminate    local map unavailable
```

Regression independently checks line maxima and every discrete H/V jump/resultant, element ownership, trace segment ownership and wave identity.

## Authority boundary

```text
Selected local element demand          = typed evidence/authority where available
Legacy TensionKn                       = unchanged
Legacy WeakLinkBreakingLoadKn/Name     = unchanged
Legacy WorkingLoadKn                   = unchanged
Legacy TensionReserve                  = unchanged
Legacy per-element Reserve/Status      = unchanged
Anchor capacity/reserve                = unchanged
Checks / Verdict / MainRisk            = unchanged
Selected X/Z                           = unchanged
```

## Next package

F3-C may now validate structural capacity against actual local demand:

```text
WLL_i = MBL_i / SafetyFactor
Reserve_i = WLL_i / LocalDesignDemand_i
GoverningWeakLink = minimum valid local Reserve_i
```

That package must first audit connector `Count` semantics and unavailable/local-demand handling. No legacy weak-link scalar should be overwritten until that validation passes independently.
