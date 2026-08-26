# CONTROL MARK — RC smoke round 4

Date: 2026-08-27
Issue: #569
Parent release gate: #522

## Scope

Presentation/UI-only follow-up from Windows v1.0.0 RC manual review.

- Only the left-column cards `Условия постановки`, `Буй`, and `Якорь и запас` are collapsible. Their collapsed state exposes only the Expander header/title and is not project data.
- `Полный отчёт` can export the exact retained `ReportText` to UTF-8 `.txt`; no PDF guide, renderer, solver, or recalculation participates in that export.
- Interactive 2D and PDF 2D use smaller/thinner presentation markers and lines while continuing to consume the same retained `Mooring2DDiagramReadModel` / selected X/Z geometry.

## Explicit non-changes

- no solver or engineering-physics changes;
- no F1–F4 authority changes;
- no anchor/soil capacity model added;
- no engineering baseline regeneration;
- no project/library serialization format changes;
- no 3D;
- production segmentation remains exactly 0.20 m;
- signed feedback budget remains exactly 64;
- signed `WeightWaterKgM` semantics remain unchanged;
- accepted signed candidate remains an exact deterministic fixed point.

A fresh exact-main Windows RC is required after merge and must pass manual Windows smoke before any `v1.0.0` tag or GitHub Release.
