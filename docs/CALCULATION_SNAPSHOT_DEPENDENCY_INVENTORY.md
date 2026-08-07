# Calculation snapshot dependency inventory

Date: 2026-08-08  
Phase: Optimization Phase 1–2  
Issues: #333, #335, #337, #339, #343

## Purpose

Track migration from historical mutable shape/report state toward one immutable calculation snapshot and one selected engineering X/Z geometry for every user-facing renderer.

## Engineering rule

The only user-facing geometry with engineering meaning is the selected calculated X/Z shape.

The discrete/iterative path remains the preferred physical direction because it includes discrete loads. `MooringShapeSolver` is a fallback/initialization/diagnostic path until stronger equilibrium validation exists. Gate/fallback indicate numerical reliability; they do not create a second independent product geometry.

2D, PDF and Markdown reports are consumers of calculated/read-model data. They must not initiate solver work, invent coordinates or reconstruct engineering geometry from report text or visualization helper values.

## Current main calculation/display pipeline after Issue #343

```text
MainWindowViewModel.Calculate
  -> BuoyCalculator.Calculate(...)
     -> CalculationResult
  -> MainWindowCalculationDisplayBuilder.Build(..., result)
     -> CalculationSnapshotBuilder.Build(environment, result)
        -> TechnicalReportDataBuilder.Build(environment, result)
           -> SegmentTensionAnalyzer
           -> MooringShapeSolver                         [fallback / initial X/Z]
           -> MooringShapeProjection
           -> MooringShapeForceAnalyzer
           -> MooringShapeTensionAnalyzer
           -> MooringSequencePositioner
           -> MooringDiscreteLoadTensionAnalyzer
           -> MooringDiscreteLoadShapeBuilder           [discrete-load X/Z candidate]
           -> MooringAlternativeDiscreteNodeProjector
           -> MooringIterativeSolver                    [iterative candidate X/Z]
           -> EngineeringDiagnostics
           -> MooringVectorBalance
        -> TechnicalReportStorePublisher.Publish(data) [compatibility side effects]
        -> SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver)
           -> MooringPrimaryShapeSelector.Select(...)
           -> SelectedShapeReadModel
        -> immutable CalculationSnapshot
     -> ReportBuildBoundary.Build(..., snapshot)
        -> UserReportBuilder.Build(environment, snapshot.Result)
        -> TechnicalReportBuilder.Build(..., snapshot)
           -> TechnicalReportMarkdownBuilder.Build(..., snapshot)
              -> render snapshot.Result + snapshot.TechnicalReportData
```

The key boundary is now explicit: the calculation snapshot exists before report builders are called. Markdown rendering no longer executes `CalculationSnapshotBuilder`, `TechnicalReportDataBuilder` or store publication.

The remaining transitional responsibility is that `MainWindowCalculationDisplayBuilder` still creates the snapshot. A later work package can move core `BuoyCalculator.Calculate` plus snapshot creation into a dedicated application run/use-case without touching report renderers again.

## Mutable state still present

| State holder | Current role | Target direction |
|---|---|---|
| `MooringShapeStore` | compatibility fallback/selected state | retire after consumers migrate |
| `MooringIterativeSolverStore` | compatibility iterative state and selection side effect | keep solver result in immutable snapshot |
| `MooringPrimaryShapeSelectionStore` | compatibility selection state | retire after consumers migrate |
| `SelectedShapeStore` | legacy selected-shape facade used by 2D/PDF adapters | replace with explicit snapshot/read-model input |
| `MooringAlternativeShapeStore` | historical alternative display state | no longer a 2D or PDF engineering geometry source after Issues #337/#339 |

## Stateless selected X/Z boundary

`ApplicationModel/SelectedMooringShapeProvider.cs` accepts:

```text
MooringShapeResult fallbackShape
MooringIterativeSolverResult iterativeSolver
```

and calls:

```text
MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver)
```

to produce `SelectedShapeReadModel` without reading or writing mutable stores.

## User-facing geometry consumers

### 2D — selected X/Z only after Issue #337

`Mooring2DDiagramSourceSelector` exposes only `SelectedShapeStore.Current` as transitional wiring.

Removed from 2D:

- `MooringAlternativeShapeStore.Current`;
- technical-report Markdown X/Z parsing;
- `VisualizationOffsetM` synthesized geometry;
- `SequenceDiagramLines` synthesized geometry;
- main/alternative comparison drawing.

`Mooring2DCanvas` draws only selected X/Z nodes, uses real `xScale = zScale`, displays `shape.HorizontalOffsetM`, and draws no engineering line when selected X/Z is unavailable.

### PDF — selected X/Z only after Issue #339

`PdfDiagramSourceSelector` exposes only:

```text
SelectedShapeStore.Current -> SelectedShapeReadModel
```

and uses only `SelectedShapeReadModel.Shape.HorizontalOffsetM` for the X/Z offset published into the user PDF.

Removed from PDF source selection/rendering:

- `MooringAlternativeShapeStore.Current`;
- alternative-shape priority;
- technical-report metric parsing;
- `visualizationOffsetM` engineering fallback;
- `AlternativeShapeDiagram(...)`;
- approximate diagram generation when selected X/Z is unavailable.

`PdfReportBuilder.SelectedShapeDiagram(...)` renders `SelectedShapeReadModel.Shape.Nodes` directly with a single uniform X/Z scale.

### Technical Markdown — passive renderer after Issue #343

`TechnicalReportMarkdownBuilder` now receives a completed `CalculationSnapshot` and reads:

```text
snapshot.Result
snapshot.TechnicalReportData
```

It no longer calls:

```text
CalculationSnapshotBuilder.Build
TechnicalReportDataBuilder.Build
TechnicalReportStorePublisher.Publish
```

`ReportBuildBoundary` and `TechnicalReportBuilder` also accept the completed snapshot instead of creating calculation state.

## Completed optimization boundaries

- **#333** — introduced `CalculationSnapshot` / `CalculationSnapshotBuilder`.
- **#335** — introduced stateless `SelectedMooringShapeProvider`; snapshot no longer reads `SelectedShapeStore.Current`.
- **#337** — removed alternative/report/fallback geometry from 2D.
- **#339** — removed alternative/report/visualization geometry sources from PDF and rendered selected X/Z directly.
- **#343** — creates the immutable calculation snapshot before report assembly; technical/user report builders are consumers rather than analysis triggers.

## Next migrations

1. Pass explicit selected-shape/read-model data to PDF and 2D instead of letting renderer adapters read `SelectedShapeStore.Current`.
2. Retain the completed `CalculationSnapshot` in calculation/application state so renderers can receive it explicitly.
3. Move `BuoyCalculator.Calculate` plus snapshot creation from ViewModel/display orchestration into a dedicated application calculation use-case once the snapshot handoff is stable.
4. Prevent new direct consumers of historical stores.
5. Retire `MooringAlternativeShapeStore` when remaining non-renderer consumers are proven unnecessary.
6. Retire selected/fallback stores only after consumer counts reach zero.
7. Add deterministic calculation/selected-X/Z regression scenarios before intentional solver changes.

## Engineering invariant

Architecture cleanup does not prove physical validity. The physical roadmap still requires force/shape consistency, global equilibrium residuals, integrated discrete-load equilibrium, seabed/anchor reaction, mode-aware wave physics and validation against reference cases.
