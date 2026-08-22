# Control mark — F4-B1 selected user presentation — 2026-08-22

Parent milestone: #522  
Issue: #546

## Purpose

F4-A retained one selected engineering assessment in `CalculationSnapshot`. F4-B1 migrates only the compact user summary and the common UI/PDF element-table read model to that selected snapshot authority when it exists.

This is a presentation-only package. It does not change the calculation result, solver, selected geometry, technical report, PDF renderer or persistence.

## User summary

When `SelectedEngineeringAssessment` exists, `UserReportBuilder` renders:

- F4 selected Verdict and MainRisk;
- existing direct net-buoyancy information;
- F1 selected design-tension demand;
- F3 selected governing local structural element and reserve;
- F2 selected anchor contact classification and horizontal demand;
- explicit statement that horizontal anchor holding capacity requires a separately validated anchor/soil model.

The selected summary does **not** present legacy global `TensionKn`, global weak-link reserve or `AnchorReserve` as selected-authority conclusions.

When selected assessment is unavailable, the previous `UserReportBuilder.Build(EnvironmentInput, CalculationResult)` output is retained exactly.

## Element table

`SelectedElementCalculationDisplayProjector` consumes the completed immutable snapshot and maps it into the existing `ElementCalculationDisplayRow` contract.

Selected path:

```text
buoy
  -> F4 positive-buoyancy check status
  -> no structural MBL/WLL/reserve fabricated

line / connector
  -> F3-C MBL
  -> F3-C WLL
  -> F3-C local reserve
  -> F3-C typed status rendered as user text

payload / instrument
  -> NotRatedByCurrentModel
  -> no fake MBL/WLL/reserve

anchor
  -> F2 contact classification
  -> F4 RequiresAdditionalPhysicalModel horizontal-capacity disposition
  -> no legacy AnchorReserve shown as selected reserve
  -> old K-type/K-ground holding factors explicitly prefixed compatibility-only
```

The projector performs identity joins and string formatting only. It does not recompute demand, WLL, reserve, reaction or verdict.

## UI and PDF ownership

`MainWindowCalculationDisplayBuilder` is the common read-model boundary for the main UI and exported user PDF element table.

`ReportBuildBoundary` is the common user-summary boundary.

Therefore `PdfReportBuilder` is intentionally unchanged. It still receives already-built summary text, already-projected element rows and selected X/Z. No selected-authority decision or engineering formula is added inside PDF rendering.

## Technical report

The full technical report is intentionally **not** migrated in F4-B1. F4-B2 will update its headline/check/capacity sections separately and will label retained legacy weak-link/anchor calculations compatibility-only.

## Canonical regression

Historical expectation:

```text
uniform-current-slack-line      Accepted         selected summary/table
discrete-payload                Accepted         selected summary/table
buoyant-line                    RejectedPhysical exact legacy fallback
depth-varying-current-profile   RejectedPhysical exact legacy fallback
vertical-zero-current           Indeterminate    exact legacy fallback
```

Regression proves:

- selected summary uses F4/F3/F2 authorities;
- legacy weak-link and anchor-reserve authority labels do not leak into selected summary;
- selected internal MBL/WLL/local reserve matches F3-C exactly;
- payload remains not capacity-rated;
- selected anchor row suppresses legacy AnchorReserve and marks old holding factors compatibility-only;
- three non-selected cases are byte-for-byte legacy summary and display-row fallback;
- technical report remains unchanged pending F4-B2;
- `CalculationResult` fields and selected X/Z identities are not mutated.

## Preserved invariants

No change to:

- solver equations;
- wave equation;
- 0.20 m production segmentation;
- signed feedback budget 64;
- signed `WeightWaterKgM` semantics;
- exact deterministic fixed-point acceptance;
- anchor-capacity physics;
- selected X/Z;
- technical-report presentation;
- persistence;
- PDF renderer physics;
- 2D;
- 3D.

## Next

F4-B2: migrate the full technical report to the retained selected assessment/capacity/reaction states while preserving legacy diagnostics only under explicit compatibility labeling.
