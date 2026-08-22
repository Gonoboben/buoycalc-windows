# F1-C — design-envelope independent/reference evidence

Date: 2026-08-22  
Parent milestone: #522  
Package issue: #528

## Purpose

Measure the F1-B quasi-static design-envelope resultants against independent vector algebra and against the existing legacy global `CalculationResult.TensionKn` before any downstream authority decision.

This package changes no runtime authority or calculation result.

## Independent vector identity

The design-envelope definition is:

```text
Hdesign = Hsteady + Hwave
Vdesign = Vsteady
Tdesign = sqrt(Hdesign^2 + Vdesign^2)
```

Reference validation includes known deterministic vector cases independent of the projector:

```text
Hsteady=0, Vsteady=4, Hwave=3 -> Tdesign=5
Hsteady=3, Vsteady=4, Hwave=0 -> Tdesign=5
signed V +/-4 with the same H terms -> equal resultant magnitude
```

These checks validate the vector-combination identity only. They do not claim that the legacy BuoyCalc wave proxy is a full dynamic wave model; F1-A explicitly established the opposite.

## Canonical evidence

For all five historical fixtures F1-C records:

```text
legacy global tension
selected source/candidate status
design-envelope availability
```

For the two Accepted signed-selected fixtures it additionally records:

```text
WaveForceN
surface design resultant
anchor-end design resultant
maximum local midpoint design resultant + segment
governing design resultant + physical location
delta and ratio versus legacy global tension
```

The three non-Accepted fixtures remain design-envelope unavailable.

## Midpoint independent reconstruction

For each Accepted segment the validation separately reconstructs the steady local force direction from adjacent selected X/Z nodes and the stored final midpoint tension magnitude, then applies the F1-A horizontal wave increment and compares that independent result to F1-B.

This is a source-backed geometry/tension reconstruction and does not re-run the feedback solver.

## Interpretation

A difference between:

```text
legacy CalculationResult.TensionKn
```

and:

```text
governing selected design-envelope resultant
```

is not an automatic defect and is not an equality gate.

They have different semantics:

- legacy tension = one pre-selection aggregate current+wave + net-buoyancy resultant;
- selected design envelope = location-specific resultants on an Accepted signed steady force/geometry state plus the existing wave proxy exactly once.

No numerical tolerance may be adjusted merely to make the two agree.

## F1-D decision boundary

F1-C prepares two possible policies:

1. redefine/overwrite the old global `TensionKn`; or
2. keep the legacy compatibility scalar and introduce a separately named selected design-demand authority for future local weak-link, anchor and checks.

Semantic separation is preferred if evidence confirms that these are physically different quantities.

## Frozen authority

After F1-C:

```text
selected X/Z                           unchanged
CalculationResult.TensionKn            legacy authoritative
F1-B design envelope                   evidence/shadow only
weak-link reserve                      legacy
anchor demand/reserve                  legacy
checks/verdict/MainRisk                legacy
```

No solver equation, selected geometry, wave equation, 0.20 m segmentation, feedback budget 64, signed weight, persistence, PDF/2D or 3D changes are made here.
