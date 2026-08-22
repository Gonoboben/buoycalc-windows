# Control mark — F2-A anchor-end reaction ownership — 2026-08-22

Parent: #522  
Package: #532

## Purpose

F2-A fixes the force-direction and rigid-body contact semantics at the lower anchor boundary before any anchor holding-capacity or reserve authority is changed.

This package is evidence only. It does **not** replace `RequiredAnchorHoldingKg`, `AnchorHoldingKg`, or `AnchorReserve`.

## Coordinate and sequence contract

Production sequence is:

`surface/buoy at s=0 -> line and internal elements -> anchor/seabed at s=L`.

Signed geometry uses `+X` in the selected horizontal plane and `+Z` downward. Existing signed-orientation logic defines the top-to-bottom line tangent as:

`(tx, tz) = (H/T, V/T)`.

Therefore the selected signed lower-boundary `EndH/EndV` and the F1-B wave-aware `AnchorDesignH/AnchorDesignV` are internal line-end force-state components in the surface-to-anchor orientation.

## Line-on-anchor action/reaction

At the lower endpoint Newton's third law gives the force exerted by the line on the anchor as the opposite vector:

`F_line_on_anchor = (-AnchorDesignH, -AnchorDesignV)`

in the same `+X`, `+Z down` coordinate frame.

Thus:

- horizontal anchor demand magnitude is `abs(AnchorDesignH)`;
- positive `AnchorDesignV` means the line pulls the anchor upward;
- negative `AnchorDesignV` means the line contributes a downward push on the anchor.

The existing v1 design wave proxy changes only horizontal demand, so `AnchorDesignV = selected signed EndV`.

## Submerged anchor weight

Existing core semantics are independently restated as:

`m_anchor,water = m_air - rho * Volume`

and

`W_anchor = m_anchor,water * g`.

The F2-A regression reconstructs this from `AnchorInput` and effective water density before comparing it with the current `CalculationResult.AnchorWeightWaterKg` field.

## Rigid-body vertical contact balance

With downward positive:

- submerged weight on anchor: `+W_anchor`;
- line on anchor: `-AnchorDesignV`;
- seabed compressive normal on anchor: `-N`.

Static vertical balance is:

`W_anchor - AnchorDesignV - N = 0`

so the required compressive normal magnitude is:

`N = W_anchor - AnchorDesignV`.

Classification used as validation evidence:

- `N > 0`: `CompressiveContact`;
- `N = 0`: `ZeroNormalLimit`;
- `N < 0`: `UpliftSeparation`, with uplift excess `AnchorDesignV - W_anchor`.

This is a boundary rigid-body contact statement only. It is **not** a full seabed/soil holding model and does not model line touchdown, soil penetration, drag embedment, suction, friction evolution, or time-domain uplift.

## Why legacy horizontal holding is not migrated here

Current legacy holding is:

`AnchorHoldingKg = AnchorWeightWaterKg * BaseHoldingCoefficient * AnchorTypeMultiplier * SeabedHoldingMultiplier`

with required holding based on the legacy aggregate horizontal force. It does not presently consume selected anchor-end vertical line pull or remaining normal reaction.

F2-A deliberately does not assume that every anchor type's horizontal capacity scales directly with `N`. That relationship is anchor- and soil-model dependent and belongs to a later independently validated F2 capacity package.

## Explicitly unchanged

- selected geometry and selected source;
- signed solver equations and exact fixed-point acceptance;
- 0.20 m segmentation;
- feedback budget 64;
- signed submerged-weight semantics;
- wave formula;
- legacy `CalculationResult.TensionKn`;
- `RequiredAnchorHoldingKg`, `AnchorHoldingKg`, `AnchorReserve`;
- weak-link logic;
- checks/verdict/main risk;
- persistence, PDF, 2D and UI;
- 3D remains post-v1.

## Next boundary

F2-B may introduce a typed immutable selected anchor-reaction/contact state using only the semantics validated here. Horizontal anchor-capacity/reserve migration remains separate until an appropriate capacity model is independently justified.
