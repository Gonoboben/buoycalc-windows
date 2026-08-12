# Calculation snapshot dependency inventory

Date: 2026-08-12  
Phase: Optimization Phase 1–5  
Issues: #333, #335, #337, #339, #343, #347, #349, #351, #353, #358, #360

## Purpose

Track the active calculation/read-model path after migration away from historical mutable shape/report state.

## Engineering rule

The only user-facing geometry with engineering meaning is the selected calculated X/Z shape.

The discrete/iterative path remains the preferred physical direction because it includes discrete loads. `MooringShapeSolver` remains a fallback/initialization/diagnostic calculation path; gate/fallback indicate numerical reliability and do not create a second product geometry.

2D, PDF and Markdown reports are consumers of calculated/read-model data. They do not initiate solver work, invent coordinates or recover engineering geometry from report text.

## Current calculation and selected-X/Z flow

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
           -> MooringAlternativeDiscreteNodeProjector   [immutable returned result]
           -> MooringIterativeSolver                    [iterative candidate X/Z]
           -> EngineeringDiagnostics
           -> MooringVectorBalance
        -> SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver)
           -> MooringPrimaryShapeSelector.Select(...)
              -> MooringPrimaryShapeGate.Evaluate(...)
           -> SelectedShapeReadModel
        -> immutable CalculationSnapshot
     -> MainWindowCalculationDisplay.SelectedShape = snapshot.SelectedShape
     -> ReportBuildBoundary.Build(..., snapshot)
        -> UserReportBuilder consumes snapshot.Result
        -> TechnicalReportBuilder / Markdown consume snapshot
  -> MainWindowViewModel.SelectedShape = display.SelectedShape
     -> Mooring2DCanvas receives selected X/Z explicitly
     -> PDF export receives selected X/Z explicitly
```

There is no mutable selected-shape/report-store publication in this path.

## Retired mutable compatibility state

The following compatibility holders are retired and guarded against reintroduction:

- `SelectedShapeStore` — retired after explicit selected-X/Z handoff to 2D/PDF;
- `TechnicalReportStorePublisher` — retired after snapshot selection became fully stateless;
- `MooringIterativeSolverStore` — retired after its publication state was proven unread;
- `MooringPrimaryShapeSelectionStore` — retired after the stateless selector became authoritative;
- `MooringShapeStore` — retired after its only remaining use was the closed compatibility publication loop;
- `MooringAlternativeShapeStore` — retired after PR #359 proved `Current reads = 0`; its projector result already flowed directly into immutable `TechnicalReportData`.

`MooringShapeResult`, `MooringShapePoint`, `MooringIterativeSolverResult`, `MooringAlternativeDiscreteNodeResult`, `MooringPrimaryShapeGate` and `MooringPrimaryShapeSelector` remain active calculation/result types and algorithms. Their retirement is **not** part of the store cleanup.

## Remaining mutable shape/report state

None of the audited historical selected-shape/report compatibility stores remains in the active calculation path.

This does **not** mean the application is globally stateless. UI/project state still exists where appropriate. It means engineering shape/report publication no longer depends on static mutable compatibility stores.

## User-facing geometry consumers

### 2D

```text
CalculationSnapshot.SelectedShape
  -> MainWindowCalculationDisplay.SelectedShape
  -> MainWindowViewModel.SelectedShape
  -> Mooring2DDiagramSourceSelector.Select(selectedShape)
  -> Mooring2DCanvas
```

Properties preserved:

- selected X/Z only;
- real `xScale = zScale`;
- no Markdown parsing;
- no visualization-offset fallback geometry;
- no alternative-shape comparison;
- no engineering line when selected X/Z is unavailable.

### PDF

```text
CalculationSnapshot.SelectedShape
  -> MainWindowCalculationDisplay.SelectedShape
  -> MainWindowViewModel.SelectedShape
  -> PdfReportBuilder.Build(..., selectedShape)
  -> PdfDiagramSourceSelector.Select(selectedShape)
```

PDF renders `SelectedShapeReadModel.Shape.Nodes` directly and does not select between solver candidates.

The PDF structure guide now identifies `CalculationSnapshot.SelectedShape / SelectedShapeReadModel.Shape.Nodes` as the selected-X/Z source; it no longer attributes geometry to a retired store.

### Technical Markdown

`TechnicalReportMarkdownBuilder` receives a completed `CalculationSnapshot` and reads:

```text
snapshot.Result
snapshot.TechnicalReportData
```

It does not execute calculation builders or publish mutable shape state.

## Alternative/discrete projection

`MooringAlternativeDiscreteNodeProjector.Build(...)` remains an active calculation/report component. It returns `MooringAlternativeDiscreteNodeResult` directly to `TechnicalReportDataBuilder`.

Retiring `MooringAlternativeShapeStore` does not retire or alter:

- `MooringDiscreteLoadShapeBuilder`;
- alternative X/Z candidate calculations;
- projected discrete-node rows;
- iterative solver input/output;
- primary-shape gate behavior.

## Regression boundary

The deterministic engineering regression harness protects five canonical scenarios:

1. vertical line / zero current;
2. uniform current with slack line;
3. intentionally buoyant line with negative signed `WeightWaterKgM`;
4. discrete connector + payload sequence;
5. depth-varying current profile.

Architecture-only PRs must keep the committed golden baseline unchanged.

## Completed optimization boundaries

- **#333** — immutable `CalculationSnapshot` boundary.
- **#335** — stateless `SelectedMooringShapeProvider`.
- **#337** — 2D selected-X/Z-only rendering.
- **#339** — PDF selected-X/Z-only rendering.
- **#343** — snapshot creation before report assembly; reports became passive consumers.
- **#347** — deterministic engineering golden regression harness.
- **#349** — explicit selected X/Z handoff from snapshot state to 2D/PDF.
- **#351** — retired `SelectedShapeStore` facade.
- **#353** — retired the remaining mutable main-shape/report publication loop.
- **#358** — exact CI-backed audit of `MooringAlternativeShapeStore`; `Current reads = 0`.
- **#360** — retirement sequence for unread alternative-shape compatibility state and stale PDF provenance.

## Next migrations

1. Define a dedicated application calculation use-case that owns `BuoyCalculator.Calculate(...)` plus `CalculationSnapshotBuilder.Build(...)`, without moving physics out of the calculation core.
2. Keep `MainWindowViewModel` as an application/UI consumer rather than an orchestration owner where practical.
3. Keep deterministic engineering regressions green for all architecture work.
4. Begin intentional solver/physics changes only through the Physics RFC process and explicit golden-baseline review.

## Engineering invariant

Architecture cleanup does not prove physical validity. The physical roadmap still requires force/shape consistency, global equilibrium residuals, integrated discrete-load equilibrium, seabed/anchor reaction, mode-aware wave physics and validation against reference cases.
