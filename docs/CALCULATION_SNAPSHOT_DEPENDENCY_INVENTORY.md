# Calculation snapshot dependency inventory

Date: 2026-08-08  
Phase: Optimization Phase 1–2  
Issues: #333, #335, #337, #339

## Purpose

Track migration from historical mutable shape/report state toward one immutable calculation snapshot and one selected engineering X/Z geometry for every user-facing renderer.

## Engineering rule

The only user-facing geometry with engineering meaning is the selected calculated X/Z shape.

The discrete/iterative path remains the preferred physical direction because it includes discrete loads. `MooringShapeSolver` is a fallback/initialization/diagnostic path until stronger equilibrium validation exists. Gate/fallback indicate numerical reliability; they do not create a second independent product geometry.

2D and PDF are presentation layers. They must not invent coordinates or reconstruct engineering geometry from report text or visualization helper values.

## Current engineering pipeline

```text
TechnicalReportMarkdownBuilder
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
```

The snapshot derives selected X/Z directly from immutable pipeline data through the existing selector/gate semantics. Compatibility stores remain temporarily because renderer wiring has not yet been converted to explicit snapshot/read-model injection.

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

`PdfDiagramSourceSelector` now exposes only:

```text
SelectedShapeStore.Current -> SelectedShapeReadModel
```

and uses only:

```text
SelectedShapeReadModel.Shape.HorizontalOffsetM
```

for the X/Z offset published into the user PDF.

Removed from PDF source selection/rendering:

- `MooringAlternativeShapeStore.Current`;
- alternative-shape priority;
- technical-report metric parsing;
- `visualizationOffsetM` engineering fallback;
- `AlternativeShapeDiagram(...)`;
- approximate diagram generation when selected X/Z is unavailable.

`PdfReportBuilder.SelectedShapeDiagram(...)` renders `SelectedShapeReadModel.Shape.Nodes` directly with a single uniform X/Z scale. When selected X/Z is unavailable, the PDF states that no engineering diagram is available and shows only non-geometric input context.

## Current report responsibility problem

`TechnicalReportMarkdownBuilder` still causes `CalculationSnapshotBuilder` to execute. Calculation orchestration therefore has not yet fully moved out of report generation.

Target:

```text
application orchestration
  -> one completed CalculationSnapshot
     -> technical report read model
     -> user PDF read model
     -> 2D read model
```

## Completed optimization boundaries

- **#333** — introduced `CalculationSnapshot` / `CalculationSnapshotBuilder`.
- **#335** — introduced stateless `SelectedMooringShapeProvider`; snapshot no longer reads `SelectedShapeStore.Current`.
- **#337** — removed alternative/report/fallback geometry from 2D.
- **#339** — removes alternative/report/visualization geometry sources from PDF and renders selected X/Z directly.

## Next migrations

1. Move calculation snapshot creation out of the Markdown renderer into application orchestration.
2. Pass explicit selected-shape/read-model data to PDF and 2D instead of letting renderer adapters read `SelectedShapeStore.Current`.
3. Prevent new direct consumers of historical stores.
4. Retire `MooringAlternativeShapeStore` when remaining non-renderer consumers are proven unnecessary.
5. Retire selected/fallback stores only after consumer counts reach zero.
6. Add deterministic calculation/selected-X/Z regression scenarios before intentional solver changes.

## Engineering invariant

Architecture cleanup does not prove physical validity. The physical roadmap still requires force/shape consistency, global equilibrium residuals, integrated discrete-load equilibrium, seabed/anchor reaction, mode-aware wave physics and validation against reference cases.
