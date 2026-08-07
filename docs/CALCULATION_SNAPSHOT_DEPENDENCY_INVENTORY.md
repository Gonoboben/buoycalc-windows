# Calculation snapshot dependency inventory

Date: 2026-08-08  
Phase: Optimization Phase 1–2  
Issues: #333, #335, #337, #339, #343, #349

## Purpose

Track migration from historical mutable shape/report state toward one immutable calculation snapshot and one selected engineering X/Z geometry for every user-facing renderer.

## Engineering rule

The only user-facing geometry with engineering meaning is the selected calculated X/Z shape.

The discrete/iterative path remains the preferred physical direction because it includes discrete loads. `MooringShapeSolver` is a fallback/initialization/diagnostic path until stronger equilibrium validation exists. Gate/fallback indicate numerical reliability; they do not create a second independent product geometry.

2D, PDF and Markdown reports are consumers of calculated/read-model data. They must not initiate solver work, invent coordinates or reconstruct engineering geometry from report text or visualization helper values.

## Current calculation and presentation flow

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
     -> MainWindowCalculationDisplay.SelectedShape = snapshot.SelectedShape
     -> ReportBuildBoundary.Build(..., snapshot)
        -> UserReportBuilder consumes snapshot.Result
        -> TechnicalReportBuilder / Markdown consume snapshot
  -> MainWindowViewModel.SelectedShape = display.SelectedShape
     -> Mooring2DCanvas passes vm.SelectedShape explicitly
     -> PDF export passes viewModel.SelectedShape explicitly
```

The selected X/Z read model therefore comes from the completed calculation snapshot and is retained as state of the latest completed calculation. User renderers no longer need to discover the selected geometry through a global selected-shape store.

`MainWindowViewModel.SelectedShape` is cleared when a default/new project is restored and when a project DTO is loaded, so geometry from a previous calculation cannot silently survive project replacement.

## Mutable compatibility state still present

| State holder | Current role | Target direction |
|---|---|---|
| `MooringShapeStore` | compatibility fallback/selected state | retire after consumer audit |
| `MooringIterativeSolverStore` | compatibility iterative state and selection side effect | keep solver result in immutable snapshot |
| `MooringPrimaryShapeSelectionStore` | compatibility selection state | retire after consumer audit |
| `SelectedShapeStore` | legacy facade retained temporarily for non-renderer/history compatibility | remove after scan proves no required production consumers |
| `MooringAlternativeShapeStore` | historical alternative display state | no longer a 2D/PDF engineering geometry source |

No new global/static store is introduced by Issue #349.

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

### 2D

After Issues #337 and #349:

```text
CalculationSnapshot.SelectedShape
  -> MainWindowCalculationDisplay.SelectedShape
  -> MainWindowViewModel.SelectedShape
  -> Mooring2DDiagramSourceSelector.Select(selectedShape)
  -> Mooring2DCanvas
```

The 2D selector no longer reads `SelectedShapeStore.Current`.

2D remains selected-X/Z only:

- no alternative store;
- no technical-report parsing;
- no `VisualizationOffsetM` synthesized engineering geometry;
- no sequence-derived synthesized geometry;
- no main/alternative comparison;
- real `xScale = zScale`;
- no engineering line when selected X/Z is unavailable.

### PDF

After Issues #339 and #349:

```text
CalculationSnapshot.SelectedShape
  -> MainWindowCalculationDisplay.SelectedShape
  -> MainWindowViewModel.SelectedShape
  -> MainWindow PDF export
  -> PdfReportBuilder.Build(..., selectedShape)
  -> PdfDiagramSourceSelector.Select(selectedShape)
```

The PDF selector no longer reads `SelectedShapeStore.Current`.

PDF remains selected-X/Z only:

- no alternative-shape priority;
- no report-text X/Z metric parsing;
- no visualization-derived engineering offset;
- no approximate diagram when selected X/Z is unavailable;
- `SelectedShapeReadModel.Shape.Nodes` are rendered directly.

### Technical Markdown

`TechnicalReportMarkdownBuilder` receives a completed `CalculationSnapshot` and reads:

```text
snapshot.Result
snapshot.TechnicalReportData
```

It does not call calculation/snapshot builders or store publishers.

## Completed optimization boundaries

- **#333** — introduced `CalculationSnapshot` / `CalculationSnapshotBuilder`.
- **#335** — introduced stateless `SelectedMooringShapeProvider`.
- **#337** — removed alternative/report/fallback geometry from 2D.
- **#339** — removed alternative/report/visualization geometry sources from PDF.
- **#343** — moved snapshot creation before report assembly; reports became consumers.
- **#347** — added deterministic calculation and selected-X/Z golden regression scenarios in CI.
- **#349** — passes selected X/Z explicitly from snapshot/display state to 2D and PDF, removing user-renderer reads of `SelectedShapeStore.Current`.

## Next migrations

1. Run a focused consumer audit of `SelectedShapeStore`, `MooringAlternativeShapeStore`, `MooringPrimaryShapeSelectionStore`, `MooringIterativeSolverStore` and `MooringShapeStore`.
2. Retire stores only when required production consumer count is proven zero or their compatibility side effect has been replaced explicitly.
3. Move `BuoyCalculator.Calculate` plus snapshot creation into a dedicated application calculation use-case once store retirement boundaries are clear.
4. Keep deterministic engineering regressions green for all architecture work.
5. Begin intentional solver/physics changes only through the Physics RFC process and explicit golden-baseline review.

## Engineering invariant

Architecture cleanup does not prove physical validity. The physical roadmap still requires force/shape consistency, global equilibrium residuals, integrated discrete-load equilibrium, seabed/anchor reaction, mode-aware wave physics and validation against reference cases.
