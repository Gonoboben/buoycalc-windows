# Calculation snapshot dependency inventory

Date: 2026-08-07  
Phase: Optimization Phase 1  
Issue: #333

## Purpose

Freeze the current calculation/report/shape dependency path before moving responsibilities. This document describes the behavior that the first `CalculationSnapshot` boundary must preserve.

## Current engineering pipeline

`TechnicalReportMarkdownBuilder.Build(...)` currently initiates the following path:

```text
TechnicalReportMarkdownBuilder
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
  -> TechnicalReportStorePublisher.Publish(data)
     -> MooringShapeStore.Set(data.Shape)
     -> MooringIterativeSolverStore.Set(data.IterativeSolver)
        -> MooringPrimaryShapeSelector.Select(...)
        -> MooringPrimaryShapeSelectionStore.Set(...)
        -> MooringShapeStore.Set(selection.Shape)
  -> SelectedShapeStore.Current
     -> SelectedShapeReadModel
```

The publisher order is significant in the current implementation: the fallback shape is published first, then the iterative solver store performs primary-shape selection and may replace `MooringShapeStore` with the selected candidate.

## Current mutable shape state

| State holder | Current role | Target direction |
|---|---|---|
| `MooringShapeStore` | stores fallback first, then selected shape may overwrite it | internal migration state only |
| `MooringIterativeSolverStore` | stores iterative result and triggers selection as a side effect | solver result should become immutable snapshot data |
| `MooringPrimaryShapeSelectionStore` | stores gate/selector result | selection belongs in snapshot/application result |
| `SelectedShapeStore` | read-model adapter over selection/fallback stores | become explicit selected-shape provider/read model over snapshot |
| `MooringAlternativeShapeStore` | separate alternative display state | retire as user-facing geometry source after consumers migrate |

## Current user-facing geometry consumers

### 2D

`Mooring2DDiagramSourceSelector` currently reads:

1. `SelectedShapeStore.Current`;
2. `MooringAlternativeShapeStore.Current`;
3. report Markdown parsed X/Z nodes as fallback.

Target: 2D consumes only selected X/Z read-model data.

### PDF

`PdfDiagramSourceSelector` currently reads:

1. `MooringAlternativeShapeStore.Current`;
2. `SelectedShapeStore.Current`;
3. X/Z metrics parsed from technical report text;
4. visualization offset fallback.

Target: PDF consumes the same selected X/Z read model as 2D; report text must not be an engineering geometry source.

## Current report responsibility problem

The Markdown renderer currently both causes the technical analysis pipeline to execute and renders the resulting data. This couples presentation to calculation orchestration and makes mutable stores part of the report path.

## Phase 1 boundary introduced by Issue #333

Introduce:

```text
CalculationSnapshot
  CalculationResult Result
  TechnicalReportData TechnicalReportData
  SelectedShapeReadModel? SelectedShape
```

and:

```text
CalculationSnapshotBuilder.Build(environment, result)
```

The builder must preserve the current sequence exactly:

```text
TechnicalReportDataBuilder.Build
-> TechnicalReportStorePublisher.Publish
-> SelectedShapeStore.Current
-> immutable CalculationSnapshot
```

For this transitional step, no store is removed and no numerical behavior changes.

## Next migrations after Issue #333

1. Create a single selected X/Z provider over `CalculationSnapshot`.
2. Move 2D to that provider and remove report parsing / alternative-store selection.
3. Move PDF to the same provider and remove report parsing / visualization geometry fallback.
4. Move calculation snapshot creation out of the Markdown renderer into application orchestration.
5. Retire mutable shape stores only after direct consumer counts reach zero.

## Engineering invariant

This inventory does not declare the current fallback form physically authoritative. Per `docs/OPTIMIZATION_PLAN.md`, the selected X/Z geometry is the only user-facing engineering geometry, and the discrete/iterative path is the preferred physical direction while solver validation is strengthened.
