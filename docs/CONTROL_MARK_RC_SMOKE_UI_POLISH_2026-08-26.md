# Control mark — RC smoke UI polish — 2026-08-26

Issue: #563
Parent release gate: #522

Manual Windows 11 smoke of the first complete v1.0.0 RC found presentation defects. This step changes only UI/read-model projection and invalidates that RC as a final-release candidate; a new exact-main RC is required after merge.

## Scope

- Current-profile window: move the long U/V/W explanation to a `?` tooltip and add a vertical GridSplitter between controls and point table.
- Main-window and element-library help marks restored with current v1 semantics.
- Sequence editor displays Russian kind names while canonical internal/persisted values remain `Line`, `Connector`, `Payload`.
- Main-window inline right-column report panel removed; `Полный отчёт...` and report generation remain intact.
- 2D view no longer draws ordinary segmentation-node circles.
- 2D element markers are projected from retained calculated element identity/length state onto the retained selected shape using its `AlongLineM/X/Z` nodes.
- Exact unformatted source line length is retained in `ElementCalculationDisplayRow.SourceLengthM` solely so visual element boundaries do not depend on formatted table strings.

## Explicit non-scope

No solver or engineering-physics change. No F1–F4 authority change. No canonical engineering baseline regeneration. No change to 0.20 m production segmentation, signed feedback budget 64, signed `WeightWaterKgM` semantics, exact fixed-point acceptance, PDF/report engineering authority, persistence schema, or 3D.

## Release consequence

The RC from source `9d2e478327a81e204c3d06b61ce3c831915b6311` remains historical smoke evidence only. After #563 merges, all exact-main gates must pass, `release-candidate/v1.0.0` must be advanced to the new exact main, a new deterministic RC artifact must be built and verified, and the user must repeat Windows 11 smoke before any v1.0.0 tag or GitHub Release.