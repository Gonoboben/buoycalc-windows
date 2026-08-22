# F1-A — wave-load ownership before local design tension

Date: 2026-08-22
Parent: #522
Child: #523

## Purpose

Freeze the exact current wave-load meaning before introducing any wave-aware local design-demand path.

This checkpoint changes no production formula or authority.

## Current production wave model

`BuoyCalculator.Calculate(...)` currently evaluates:

```text
waveVelocity = WavePeriodS > 0 ? pi * WaveHeightM / WavePeriodS : 0
WaveForceN   = 0.5 * rho * waveVelocity^2 * buoy.ProjectedAreaM2 * buoy.DragCoefficient
HorizontalForceN = CurrentForceN + WaveForceN
```

Therefore current `CalculationResult.WaveForceN` is an application design-wave proxy with these exact properties:

```text
horizontal scalar magnitude
buoy projected-area/Cd ownership only
drag-only
no vertical wave component
no inertia/added-mass term
no wave phase/time history
no distributed wave load on line/connectors/payloads
```

The existing legacy global `TensionKn` and anchor demand already consume this wave term through `HorizontalForceN`.

## Primary source boundary

Primary project source: H. O. Berteaux, *Buoy Engineering* / G. O. Berteaux, `Океанографические буи`, 1979, Chapter 1 §1.2.

The source distinguishes wave loading from steady-current loading:

- wave-induced forces vary with time;
- a body in oscillatory flow receives both drag and inertia contributions;
- horizontal and vertical wave-force / particle-kinematic components are treated separately in Eqs. (1.20)-(1.23).

The book also develops true cable dynamics separately. That dynamic scope is not the v1 quasi-static model.

Consequently the existing BuoyCalc `WaveForceN` must not be described as a complete Berteaux dynamic-wave force. It is a narrower legacy design proxy.

## v1 design-envelope ownership to validate next

The smallest compatibility-preserving candidate is:

```text
H_surface_design = H_surface_steady_signed + WaveForceN
V_surface_design = V_surface_steady_signed
```

Then the already validated signed current/weight/point-load ownership is propagated toward the anchor without adding the same wave load again.

This candidate means:

```text
wave-aware local design envelope under the existing BuoyCalc wave proxy
```

It does NOT mean:

```text
dynamic tension
time-domain wave response
Morison inertia model
vertical wave response
distributed line-wave dynamics
```

## Hard non-ownership rules

Do not:

1. add `WaveForceN` independently to every segment;
2. add it again at connector/payload crossings;
3. invent a vertical wave-force component;
4. infer wave load from selected X/Z geometry;
5. modify selected geometry to fit the design-envelope load in F1-A;
6. rename the envelope as a dynamic solution.

## Limiting-case evidence required

Validation must prove the present production contract before any runtime migration:

```text
WaveHeight=0                       -> WaveForceN=0
WaveForceN                         -> independent buoy drag formula
HorizontalForceN                  -> CurrentForceN + WaveForceN
Current=0, WaveForceN>0           -> HorizontalForceN=WaveForceN
adding connector/payload current  -> WaveForceN unchanged for unchanged buoy/environment
changing line properties          -> WaveForceN unchanged for unchanged buoy/environment
```

These are ownership/identity checks, not a validation that the legacy wave proxy equals full physical wave loading.

## Frozen invariants

No changes to:

- existing wave equation;
- existing global `TensionKn`, weak-link, anchor or verdict authority;
- Accepted `SignedBoundaryFeedback` selected geometry/source authority;
- non-Accepted selected fallback behavior;
- exact 0.20 m segmentation;
- signed feedback budget 64;
- signed `WeightWaterKgM` semantics;
- JSON/persistence;
- PDF/2D renderer physics;
- 3D before v1.

## Next package

F1-B may introduce an immutable calculation-core **design-envelope force state** only after the ownership regression is green. It must remain separate from dynamic-wave terminology and from selected geometry authority.
