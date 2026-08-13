# Control mark: profile planar projection Stage C

Date: 2026-08-13
Issue: #430
Scope: validation only; no production calculation change.

## Metric

For each non-zero horizontal-current sample measure:

`discarded fraction = |U_out| / sqrt(East^2 + North^2)`.

This is a velocity-component diagnostic, not a force-loss percentage.

## Canonical profile

Canonical points are `(z, East, North)` = `(0, 0.6, 0)`, `(25, 0.3, 0)`, `(50, 0.1, 0)`.

For +X azimuth `90 deg` (East): max horizontal speed `0.6 m/s`, max `|U_out| = 0`, max discarded fraction `0`.

For +X azimuth `0 deg` (North): max `|U_out| = 0.6 m/s`, max discarded fraction `1`, mean discarded fraction `1`.

The same profile can therefore be fully retained or fully discarded horizontally depending on the explicit project axis. This supports the rule that no default axis is manufactured.

## Rotating synthetic profile

Four unit vectors East, North, West and South are projected onto fixed +X = East.

Measured result: max `|U_out| = 1 m/s`, max discarded fraction `1`, mean discarded fraction `0.5`.

A directional profile can therefore alternate between fully retained and fully discarded horizontal velocity even when speed magnitude is unchanged.

## Conclusion

These cases show that out-of-plane loss is axis-sensitive and can be large. They do not justify a universal warning/failure threshold.

Any future physics consumer switch must separately justify the governing acceptance quantity and threshold (for example velocity-component, force-weighted or integrated-load loss) with source-backed assumptions and validation evidence.

Preserved: solver formulas, force coefficients, selected-shape/gate behavior, anchor and weak-link verdicts, 2D/PDF geometry, engineering baseline file, and the legacy path when axis is absent.
