# Calculation snapshot dependency inventory

Date: 2026-08-08  
Phase: Optimization Phase 1–2  
Issues: #333, #335, #337

## Purpose

Track the migration from historical mutable shape/report state toward one immutable calculation snapshot and one selected engineering X/Z geometry for all user-facing renderers.

## Engineering rule

The only user-facing geometry with engineering meaning is the selected calculated X/Z shape.

The discrete/iterative path is the preferred physical direction because it includes discrete loads. `MooringShapeSolver` remains a fallback/initialization/diagnostic path until stronger equilibrium validation is implemented. Gate/fallback indicate numerical reliability; they do not make the fallback geometry a second independent product answer.

2D and PDF are presentation layers. They must not invent coordinates or reconstruct engineering geometry from text or visualization helper values.

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

The snapshot does not read selected X/Z from a mutable store. It derives the same selection directly from immutable fallback-shape and iterative-solver data using existing selector/gate semantics.

The compatibility publisher remains temporarily because PDF and some historical consumers still read stores.

## Current mutable shape state

| State holder | Current role | Target direction |
|---|---|---|
| `MooringShapeStore` | stores fallback first, then selected shape may overwrite it | internal migration state only |
| `MooringIterativeSolverStore` | stores iterative result and triggers selection as a side effect | solver result should remain immutable snapshot data |
| `MooringPrimaryShapeSelectionStore` | stores gate/selector result for legacy consumers | retire after consumers move to snapshot/provider |
| `SelectedShapeStore` | legacy selected-shape facade; still used by renderer adapters during migration | retire direct global reads after explicit snapshot/read-model wiring |
| `MooringAlternativeShapeStore` | separate alternative display state | no longer a 2D source after Issue #337; PDF migration remains |

## Stateless selected X/Z boundary

`ApplicationModel/SelectedMooringShapeProvider.cs` is the store-independent selected-shape boundary.

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

The provider does not read or write `SelectedShapeStore`, `MooringShapeStore` or `MooringPrimaryShapeSelectionStore`. It does not alter gate criteria, candidate promotion, coordinates, forces or solver math.

## User-facing geometry consumers

### 2D — selected X/Z only after Issue #337

`Mooring2DDiagramSourceSelector` now exposes only:

```text
SelectedShapeStore.Current -> SelectedShapeReadModel
```

This remaining global store read is transitional wiring; the 2D selector does not choose between engineering models.

Removed from the 2D path:

```text
MooringAlternativeShapeStore.Current
technical-report Markdown X/Z parsing
VisualizationOffsetM-based synthesized mooring geometry
SequenceDiagramLines-based synthesized element geometry
main/alternative comparison drawing
```

`Mooring2DCanvas` now:

- draws only selected calculated X/Z nodes;
- uses `xScale = zScale`;
- uses `shape.HorizontalOffsetM` for the displayed calculated offset;
- displays an explicit unavailable state when selected X/Z nodes do not exist;
- does not draw an approximate engineering line when calculation geometry is unavailable.

This is an intentional presentation change, not a solver/physics change.

### PDF — still transitional

`PdfDiagramSourceSelector` still reads:

1. `MooringAlternativeShapeStore.Current`;
2. `SelectedShapeStore.Current`;
3. X/Z metrics parsed from technical report text;
4. visualization offset fallback.

Target: PDF consumes one selected X/Z read model. Report text and visualization offsets must not be engineering geometry sources.

## Current report responsibility problem

The Markdown renderer still causes `CalculationSnapshotBuilder` to execute. The renderer no longer directly executes the technical data builder or publisher, but calculation orchestration has not yet moved fully out of report generation.

Target: application orchestration creates one completed `CalculationSnapshot`; reports, PDF and 2D consume read models derived from that snapshot.

## Completed optimization boundaries

### Issue #333 — CalculationSnapshot

Introduced `CalculationSnapshot` / `CalculationSnapshotBuilder` and moved direct technical-data build/store publication out of the Markdown renderer.

### Issue #335 — stateless selected X/Z provider

Introduced `SelectedMooringShapeProvider` and removed `SelectedShapeStore.Current` from `CalculationSnapshotBuilder`.

### Issue #337 — 2D selected X/Z renderer

Removes alternative/report/fallback geometry from the 2D path. 2D becomes a single selected-X/Z renderer and shows no engineering line when selected calculated nodes are unavailable.

## Next migrations

1. Move PDF to one selected X/Z source and remove alternative-store/report-text/visualization geometry fallback.
2. Move calculation snapshot creation out of the Markdown renderer into application orchestration.
3. Replace renderer global store reads with explicit snapshot/read-model input.
4. Prevent new direct consumers of historical shape stores.
5. Retire mutable shape stores only after direct consumer counts reach zero.

## Engineering invariant

Architecture cleanup does not prove physical validity. The physical roadmap still requires force/shape consistency, global equilibrium residuals, integrated discrete-load equilibrium, seabed/anchor reaction, mode-aware wave physics and validation against reference cases.
