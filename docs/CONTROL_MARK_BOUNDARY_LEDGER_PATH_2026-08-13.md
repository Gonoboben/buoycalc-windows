# Control mark: boundary load ledger path validation

Date: 2026-08-13  
Issue: #407  
Base main: `5f1982820933f31a4d7ee5e15d6b627689e82b36`

## Purpose

This control mark records the validation-only refinement of the boundary load ownership check after PR #412.

PR #412 proved aggregate ownership closure. The present step proves the same ownership **in top-to-bottom line-coordinate order** without constructing cable geometry.

## Source boundary

Primary project source: H. O. Berteaux / Г. О. Берто, *Океанографические буи*, 1979, Chapter 2 §2.1.

For a surface buoy with unknown vertical tension, Berteaux states that another physical constraint such as horizontal excursion or holding-line length must be specified and the problem is then solved by successive approximations. The source therefore does not justify treating full-volume buoyancy capacity as an already solved surface equilibrium force.

## Validation ledger

The regression reconstructs the current-model buoy contribution by conservation residuals, then crosses loads in increasing `s` from buoy toward anchor.

The ledger stores only vector quantities:

```text
DeltaFxN
DeltaFzN
CumulativeFxN
CumulativeFzN
```

No scalar cable angle and no X/Z geometry are constructed.

### Boundary row

At `s = 0`, the validation-only boundary contribution is reconstructed as:

```text
BuoySteadyDragN =
    result.CurrentForceN
    - sum(SegmentRows.CurrentForceN)
    - sequence.DiscreteCurrentForceN

BuoySignedWeightWaterKg =
    -result.NetBuoyancyKg
    - sum(SegmentRows.WeightWaterKg)
    - sequence.DiscreteWeightWaterKg
```

The vertical term is ownership evidence under the existing full-volume capacity model only. It is not claimed to be actual equilibrium `B_b` for a free surface buoy.

### Distributed rows

Each segment contributes exactly once at its lower-end coordinate:

```text
DeltaFxN = Segment.CurrentForceN
DeltaFzN = Segment.WeightWaterKg * g
```

Signed negative `WeightWaterKg` remains negative in `DeltaFzN`.

### Point rows

Connector/payload rows at the same `s` are grouped into one mechanical point event and added exactly once.

Buoy and anchor remain boundary rows and are not internal point loads.

## Terminal identities

After all segment and internal point events are crossed:

```text
CumulativeFxN(bottom-) = result.CurrentForceN
CumulativeFzN(bottom-) = -result.NetBuoyancyKg * g
```

For Chapter-2 steady-current validation, `WaveForceN` remains excluded from this ledger.

## Numerical / physics impact

None in production.

This change affects validation only. It does not modify:

- solver equations;
- shape builders;
- selected X/Z;
- primary shape gate or verdict;
- anchor or weak-link calculations;
- 0.20 m segmentation or segment count;
- signed `WeightWaterKg` semantics;
- reports/PDF/2D;
- JSON/DTO;
- golden baseline;
- 3D.

## Next physical question

Once this ordered ownership ledger is green, the next source-backed validation problem is the **surface-buoy vertical boundary unknown**.

Under the current static assumptions, the next work must treat actual surface-buoy displacement / vertical cable tension as an unknown constrained by buoyancy capacity and line geometry. It must not silently substitute `rho * full Volume` as actual equilibrium buoyancy.
