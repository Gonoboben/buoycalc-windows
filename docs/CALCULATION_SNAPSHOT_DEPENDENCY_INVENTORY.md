# Calculation snapshot dependency inventory

Date: 2026-08-11  
Phase: Optimization Phase 1–4  
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
           -> MooringAlternativeDiscreteNodeProjector   [immutable result only]
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
- `MooringAlternativeShapeStore` — retired after CI-backed audit proved `Current reads = 0`, production `Set/Clear` publication was removed, and the validation-only clear was proven unnecessary by the unchanged golden regression baseline.

`MooringShapeResult`, `MooringShapePoint`, `MooringIterativeSolverResult`, `MooringAlternativeDiscreteNodeResult`, `MooringPrimaryShapeGate` and `MooringPrimaryShapeSelector` remain active calculation/result types and algorithms. Their retirement is **not** part of the store cleanup.

## Alternative/discrete result path after store retirement

`MooringAlternativeDiscreteNodeProjector` remains an active calculation step. It returns immutable `MooringAlternativeDiscreteNodeResult` directly into `TechnicalReportData`:

```text
MooringAlternativeDiscreteNodeProjector.Build(...)
  -> MooringAlternativeDiscreteNodeResult
TechnicalReportDataBuilder
  -> TechnicalReportData.AlternativeDiscreteNodes
```

No compatibility store is used or required by this path.

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

### Technical Markdown

`TechnicalReportMarkdownBuilder` receives a completed `CalculationSnapshot` and reads:

```text
snapshot.Result
snapshot.TechnicalReportData
```

It does not execute calculation builders or publish mutable shape state.

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
- **#353** — retired the remaining mutable primary/fallback shape/report publication loop.
- **#358 / #360** — audited and retired `MooringAlternativeShapeStore` while preserving the immutable alternative/discrete calculation result path.

## Next migrations

1. Move `BuoyCalculator.Calculate` plus snapshot creation into a dedicated application calculation use-case so `MainWindowViewModel` no longer owns calculation orchestration.
2. Keep deterministic engineering regressions green for all architecture work.
3. Begin intentional solver/physics changes only through the Physics RFC process and explicit golden-baseline review.
4. Physical roadmap remains: force/shape consistency, global equilibrium residuals, integrated discrete-load equilibrium, seabed/anchor reaction, mode-aware wave physics and reference validation.

## Engineering invariant

Architecture cleanup does not prove physical validity. All numerical/physics changes remain separate, explicitly reviewed work packages.
