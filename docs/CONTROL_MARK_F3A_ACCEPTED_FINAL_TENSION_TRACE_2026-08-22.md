# Control mark — F3-A Accepted final tension trace retention — 2026-08-22

Parent milestone: #522  
Issue: #538

## Purpose

Local weak-link demand requires force state at actual element locations, including both sides of discrete point-load jumps. The Accepted signed candidate already has that information at the exact deterministic fixed point, but before this package only its shape and solved boundary were retained.

This package closes that provenance gap without changing any solver equation or production scalar authority.

## Exact ownership

At the Accepted branch of `MooringSignedCandidateEvaluator` the evaluator already owns, simultaneously:

```text
nextResult
nextBoundary
nextTrace
nextGeometry
```

`IsExactFixedPoint(...)` compares the current/next trace row-by-row with exact equality. `TryBuildAcceptedShape(...)` then builds the Accepted X/Z shape directly from `nextTrace`.

F3-A therefore stores **that same `nextTrace` object** in `MooringSignedCandidateResult.FinalTensionTrace` when the production candidate is Accepted.

There is no downstream trace reconstruction and no second integration pass.

## Contract identity

When a final trace is supplied to `CreateAccepted`, the contract verifies:

- trace is available;
- trace parent classification equals the Accepted boundary classification;
- trace point-load crossings equal Accepted candidate/boundary identity;
- trace row count equals `shape.Nodes.Count - 1`;
- trace start H/V exactly equal `BuoySteadyDragN / Q0N`;
- trace end H/V exactly equal solved boundary `EndH / EndV`;
- for each segment, trace segment number and end `s` exactly equal the Accepted shape node;
- `trace.MidTensionN / 1000` exactly equals the Accepted node `SegmentTensionKn` copied from that trace.

Production `MooringSignedCandidateEvaluator` always supplies the retained trace for Accepted candidates.

The factory parameter remains optional only to preserve existing validation/structural factory compatibility. Such manually constructed fixtures do not fabricate provenance; `FinalTensionTrace` stays `null` unless an actual trace is supplied.

## Canonical result

Expected historical classification remains unchanged:

```text
uniform-current-slack-line      Accepted       final trace retained
 discrete-payload                Accepted       final trace retained
buoyant-line                    RejectedPhysical no final Accepted trace
depth-varying-current-profile   RejectedPhysical no final Accepted trace
vertical-zero-current           Indeterminate   no final Accepted trace
```

The Accepted pair covers one case with no internal point loads and one with point-load crossings.

## Authority boundary

```text
Selected X/Z/source authority              unchanged
Selected design-tension authority          unchanged
Selected anchor H/V/contact authority      unchanged
Final Accepted steady tension trace        retained provenance
Legacy CalculationResult.TensionKn         unchanged
Legacy weak-link/WLL/reserve               unchanged
Legacy anchor capacity/reserve             unchanged
Checks / Verdict / MainRisk                unchanged
```

The retained trace is steady-current signed fixed-point evidence. F1 wave semantics are still applied only in the separate design-demand layer and are not fed back into geometry.

## Next package

F3-B may now construct a typed local element-demand map using only:

1. this retained exact-fixed-point trace;
2. existing `MooringSequencePositionResult` element ownership along `s`;
3. the already validated horizontal wave increment policy.

For a distributed line range, local design demand must include relevant boundary/midpoint states over that range. For an internal discrete item, the point-load jump must be resolved explicitly and demand must conservatively account for both sides of the point. No hidden solver-state reconstruction is permitted.
