# Control mark — user PDF executive layout — 2026-08-27

Parent issue: #575  
Implementation issue: #578

## Scope

The user-facing PDF now consumes only the retained `UserEngineeringReportReadModel` produced by the completed calculation run.

The renderer no longer uses `ResultText`, `ReportText`, `PdfReportStructureGuide` or technical-report text as engineering data sources.

## First-half document structure

1. Engineering executive summary with verdict, main risk and key indicators.
2. Calculation/environment conditions including current profile when enabled.
3. Buoy and anchor input characteristics.
4. Ordered calculated mooring composition table.
5. Selected X/Z geometry through the existing shared `Mooring2DDiagramReadModel` only.

## Authority

The PDF performs no engineering calculations and does not choose a shape candidate. It formats retained calculated values and presentation-only geometry markers.

The report explicitly states that horizontal anchor/soil holding capacity is not a validated selected-capacity model in v1 and therefore requires separate physical review.

## Frozen invariants

No solver or F1-F4 authority change. No engineering baseline regeneration. Production segmentation remains 0.20 m. Signed feedback budget remains 64. Signed `WeightWaterKgM` and exact deterministic fixed-point semantics are unchanged. No 3D.
