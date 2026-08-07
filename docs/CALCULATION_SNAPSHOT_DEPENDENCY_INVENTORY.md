# Calculation snapshot dependency inventory

Date: 2026-08-07  
Phase: Optimization Phase 1  
Issues: #333, #335

## Purpose

Freeze the calculation/report/shape dependency path while responsibilities are moved toward an immutable calculation snapshot and one selected engineering X/Z source.

## Engineering rule

The only user-facing geometry with engineering meaning is the selected calculated X/Z shape.

The discrete/iterative path is the preferred physical direction because it includes discrete loads. `MooringShapeSolver` remains a fallback/initialization/diagnostic path until stronger equilibrium validation is implemented. Gate/fallback indicate numerical reliability; they do not make the fallback geometry a second independent product answer.

## Current engineering pipeline

`TechnicalReportMarkdownBuilder.Build(...)` currently initiates the following transitional path:

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
        -> MooringShapeStore.Set(data.Shape)
        -> MooringIterativeSolverStore.Set(data.IterativeSolver)
           -> MooringPrimaryShapeSelector.Select(...)
           -> MooringPrimaryShapeSelectionStore.Set(...)
           -> MooringShapeStore.Set(selection.Shape)
     -> SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver)
        -> MooringPrimaryShapeSelector.Select(...)
        -> SelectedShapeReadModel
     -> immutable CalculationSnapshot
```

The snapshot no longer reads selected X/Z from a mutable store. It derives the same selection directly from the immutable fallback shape and iterative solver result using the existing selector/gate semantics.

The compatibility publisher remains temporarily because 2D/PDF and historical consumers still read stores.

## Current mutable shape state

| State holder | Current role | Target direction |
|---|---|---|
| `MooringShapeStore` | stores fallback first, then selected shape may overwrite it | internal migration state only |
| `MooringIterativeSolverStore` | stores iterative result and triggers selection as a side effect | solver result should remain immutable snapshot data |
| `MooringPrimaryShapeSelectionStore` | stores gate/selector result for legacy consumers | retire after consumers move to snapshot/provider |
| `SelectedShapeStore` | legacy read-model adapter over selection/fallback stores | keep only until 2D/PDF consumers migrate |
| `MooringAlternativeShapeStore` | separate alternative display state | retire as a user-facing geometry source |

## Stateless selected X/Z boundary

`ApplicationModel/SelectedMooringShapeProvider.cs` is the first store-independent selected-shape boundary.

Inputs:

```text
MooringShapeResult fallbackShape
MooringIterativeSolverResult iterativeSolver
```

Selection:

```text
MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver)
```

Output:

```text
SelectedShapeReadModel
```

The provider does not read or write:

```text
SelectedShapeStore
MooringShapeStore
MooringPrimaryShapeSelectionStore
```

It does not alter gate criteria, candidate promotion, coordinates, forces or solver math.

## Current user-facing geometry consumers

### 2D

`Mooring2DDiagramSourceSelector` still reads:

1. `SelectedShapeStore.Current`;
2. `MooringAlternativeShapeStore.Current`;
3. report Markdown parsed X/Z nodes as fallback.

Target: 2D becomes a pure renderer of the selected X/Z read model and has no independent geometry/source-selection logic.

### PDF

`PdfDiagramSourceSelector` still reads:

1. `MooringAlternativeShapeStore.Current`;
2. `SelectedShapeStore.Current`;
3. X/Z metrics parsed from technical report text;
4. visualization offset fallback.

Target: PDF consumes the same selected X/Z read model as 2D. Report text and visualization offsets must not be engineering geometry sources.

## Current report responsibility problem

The Markdown renderer still causes `CalculationSnapshotBuilder` to execute. The renderer no longer directly executes the technical data builder or publisher, but calculation orchestration has not yet moved fully out of report generation.

Target: application orchestration creates one completed `CalculationSnapshot`; reports, PDF and 2D consume read models derived from that snapshot.

## Completed Phase 1 boundaries

### Issue #333

Introduced:

```text
CalculationSnapshot
CalculationSnapshotBuilder
```

and moved direct technical-data build/store publication out of the Markdown renderer.

### Issue #335

Introduces:

```text
SelectedMooringShapeProvider
```

and removes `SelectedShapeStore.Current` from `CalculationSnapshotBuilder`.

The current compatibility order remains:

```text
TechnicalReportDataBuilder.Build
-> TechnicalReportStorePublisher.Publish
-> SelectedMooringShapeProvider.Build
-> immutable CalculationSnapshot
```

No numerical behavior changes are intended.

## Next migrations

1. Move 2D to selected X/Z read-model input and remove alternative-store/report-text fallback selection.
2. Move PDF to the same selected X/Z read-model input and remove report-text/visualization geometry fallback.
3. Move calculation snapshot creation out of the Markdown renderer into application orchestration.
4. Prevent new direct consumers of historical shape stores.
5. Retire mutable shape stores only after direct consumer counts reach zero.

## Engineering invariant

Architecture cleanup does not prove physical validity. The physical roadmap still requires force/shape consistency, global equilibrium residuals, integrated discrete-load equilibrium, seabed/anchor reaction, mode-aware wave physics and validation against reference cases.
