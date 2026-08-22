# F1-B — selected quasi-static design-envelope force state

Date: 2026-08-22  
Parent milestone: #522  
Package issue: #526

## Purpose

Introduce a read-only calculation-core state for **local wave-aware design demand** without changing selected X/Z geometry or any existing production scalar authority.

F1-A established that current `CalculationResult.WaveForceN` is a legacy horizontal buoy-only drag proxy, not a full dynamic-wave model.

F1-B combines that existing proxy with the already-selected Accepted signed steady force state.

## Availability boundary

The state exists only when the actual selected calculation-core source is:

```text
SignedBoundaryFeedback
```

and it is the exact same Accepted signed candidate.

For every non-Accepted / non-signed selected case the projector returns unavailable (`null`).

This preserves the existing five-fixture selection truth:

```text
Accepted signed selected: 2 -> design envelope available
non-Accepted legacy selected: 3 -> design envelope unavailable
```

## Boundary resultants

The existing wave proxy is added once in the horizontal design direction:

```text
SurfaceDesignH = SurfaceSteadyH + WaveForceN
SurfaceDesignV = SurfaceSteadyV

AnchorDesignH  = AnchorSteadyH + WaveForceN
AnchorDesignV  = AnchorSteadyV
```

Surface and anchor steady components come directly from `MooringSelectedSignedBoundaryState`.

## Local midpoint resultants

An Accepted signed shape already stores, for each production segment:

- final exact-fixed-point midpoint tension magnitude;
- X/Z segment geometry created from the final signed midpoint force direction.

F1-B reconstructs the signed steady midpoint direction from adjacent selected X/Z nodes and combines it with the stored midpoint tension magnitude:

```text
Tsteady = stored SegmentTensionKn * 1000
tangent = selected segment ΔX/ΔZ normalized by projected segment length
Hsteady = Tsteady * tangentX
Vsteady = Tsteady * tangentZ
```

The design envelope is then:

```text
Hdesign = Hsteady + WaveForceN
Vdesign = Vsteady
Tdesign = sqrt(Hdesign^2 + Vdesign^2)
```

`WaveForceN` is not re-applied by segment, connector or payload. It is one boundary-load increment expressed at each section of the force envelope.

## Geometry boundary

F1-B does **not** solve a new geometry and does not feed the design load back into selected X/Z.

```text
selected signed X/Z -> source for steady local direction
design envelope      -> derived demand state only
```

No `MooringShapeResult` is mutated or created by the projector.

## Terminology boundary

Correct:

```text
quasi-static design envelope
wave-aware under existing BuoyCalc WaveForceN proxy
local design resultant
```

Incorrect:

```text
dynamic tension
time-domain response
Morison wave load
vertical wave force
added-mass / inertia response
distributed wave dynamics
```

Those dynamic models remain explicitly post-v1.

## Canonical validation

`SelectedDesignEnvelopeStateRegression` verifies all five historical canonical fixtures and requires:

- exactly 2 design-envelope states available;
- exactly 3 unavailable;
- exactly one Accepted case with internal point loads and one without;
- wave horizontal increment applied once at surface, every local midpoint and anchor;
- vertical components unchanged;
- zero-wave case collapses exactly to the steady stored midpoint/boundary tension state;
- selected shape object identity remains unchanged;
- legacy `CalculationResult.TensionKn` remains unchanged.

## Authority status after F1-B

```text
Selected X/Z authority                  = unchanged
CalculationResult.TensionKn authority   = legacy unchanged
Weak-link reserve authority             = legacy unchanged
Anchor reserve authority                = legacy unchanged
Checks / Verdict / MainRisk authority   = legacy unchanged
F1-B DesignEnvelope state               = shadow/read-only evidence
```

## Next

F1-C must supply independent/reference evidence for the design-envelope vector addition and compare canonical local design resultants to the legacy global demand before any production tension authority decision.
