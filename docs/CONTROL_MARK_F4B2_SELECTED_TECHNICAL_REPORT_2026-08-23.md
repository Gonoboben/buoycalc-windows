# Control mark — F4-B2 selected technical report authority

Date: 2026-08-23
Parent: #522
Package: #548
Base main: `c4046a62cac7fd4cbe56da01c89fd51fcd151f7b`

## Scope

F4-B2 migrates only the user-conclusion portions of the full technical report to retained selected F1/F2/F3/F4 snapshot authorities when `SelectedEngineeringAssessment` exists.

The existing `TechnicalReportMarkdownBuilder` remains the legacy renderer and exact fallback. `TechnicalReportBuilder` first obtains that unchanged Markdown and then applies `SelectedTechnicalReportProjector` only for an Accepted selected-authority snapshot.

Selected technical-report presentation owns no engineering equations. It reads retained snapshot state only.

## Selected authority

The selected report presents:

- F4 selected verdict, main risk and typed checks;
- F1 selected design-tension demand and governing location;
- F3 local structural-capacity coverage, governing element and local reserve;
- F2 anchor contact, horizontal demand and normal reaction;
- explicit `RequiresAdditionalPhysicalModel` disposition for horizontal anchor/soil capacity.

The selected element table is projected through the existing F4-B1 `SelectedElementCalculationDisplayProjector` and therefore does not re-derive demand, MBL, WLL or reserve.

## Compatibility-only legacy evidence

Legacy weak-link and anchor-holding evidence remains available for traceability in the old diagnostic/report sections, but selected presentation marks capacity-like legacy lines `compatibility-only`, including legacy weak-link demand/reserve, legacy anchor holding/reserve and old holding multipliers. These values cannot authorize the selected verdict.

## Exact fallback

If selected F4 assessment is absent, `SelectedTechnicalReportProjector` returns the original legacy Markdown string unchanged.

## Preserved invariants

- no solver equation change;
- no wave equation change;
- no anchor-capacity equation added;
- selected X/Z unchanged;
- production segmentation remains 0.20 m;
- signed feedback budget remains 64;
- signed `WeightWaterKgM` semantics unchanged;
- Accepted candidate remains an exact deterministic fixed point;
- `CalculationResult` remains unchanged;
- F4-B1 compact user summary and UI/PDF element-table read models remain unchanged;
- PDF renderer and 2D own no engineering physics;
- no 3D work in this package.

## Validation target

The five historical canonical scenarios must prove two Accepted selected reports and three exact legacy fallbacks, with no mutation of legacy result fields or selected geometry.
