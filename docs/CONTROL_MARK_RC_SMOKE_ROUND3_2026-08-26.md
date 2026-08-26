# CONTROL MARK — RC smoke round 3

Date: 2026-08-26
Issue: #567
Parent release gate: #522

## Scope

Presentation-only corrections found during the third manual Windows v1.0.0 RC smoke:

- interactive 2D and PDF 2D retain the selected calculated X/Z shape and the real calculated element marker positions, but no longer render element-name callouts or leader lines;
- buoy and anchor remain visually distinct shapes without element-name text on the diagram;
- sequence cards remain editable when expanded, while collapsed state keeps only the compact title/participation/control row; summary and detailed parameters are hidden;
- library Export/Import commands use the same dark command palette as the main-window header;
- expanded Cd help retains the practical data-source/formula guidance but begins with the earlier concise definition of Cd for each element class.

## Frozen engineering invariants

No solver or engineering physics changes.
No F1–F4 authority changes.
No engineering baseline regeneration.
Production segmentation remains exactly 0.20 m.
Signed feedback budget remains exactly 64.
Signed `WeightWaterKgM` semantics remain unchanged.
Accepted signed candidate remains an exact deterministic fixed point.
No 3D.

A fresh exact-main Windows RC is required after merge and must pass manual Windows smoke before any `v1.0.0` tag or GitHub Release.
