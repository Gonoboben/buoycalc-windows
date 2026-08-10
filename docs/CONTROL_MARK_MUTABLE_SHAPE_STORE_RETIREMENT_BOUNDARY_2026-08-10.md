# Control mark: mutable shape-store retirement boundary

Date: 2026-08-10  
Issue: #353  
Phase: architecture stabilization / immutable calculation snapshot migration

## Purpose

Record the exact evidence and implementation boundary for retiring the remaining mutable shape/report compatibility publication loop without changing engineering physics or selected X/Z behavior.

This document is documentation-only. It does not change solver code, formulas, gate decisions, rendering, report values, segmentation or project data.

## Current authoritative selected-shape path

The selected engineering X/Z read model is already produced without reading any mutable selected-shape store:

```text
TechnicalReportDataBuilder.Build(environment, result)
  -> TechnicalReportData.Shape
  -> TechnicalReportData.IterativeSolver

SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver)
  -> MooringPrimaryShapeSelector.Select(fallbackShape, iterativeSolver)
  -> SelectedShapeReadModel
  -> CalculationSnapshot.SelectedShape
```

`CalculationSnapshot.SelectedShape` is then passed explicitly through application/UI state to 2D and PDF.

## Remaining compatibility publication loop

`CalculationSnapshotBuilder` still calls:

```text
TechnicalReportStorePublisher.Publish(data)
```

The publisher performs:

```text
MooringShapeStore.Set(data.Shape)
MooringIterativeSolverStore.Set(data.IterativeSolver)
```

`MooringIterativeSolverStore.Set(...)` then performs a second selection side-effect:

```text
fallbackShape = MooringShapeStore.Current
selection = MooringPrimaryShapeSelector.Select(fallbackShape, result)
MooringPrimaryShapeSelectionStore.Set(selection)
MooringShapeStore.Set(selection.Shape)
```

After that publication, `CalculationSnapshotBuilder` independently computes the selected read model from `data.Shape` and `data.IterativeSolver` through `SelectedMooringShapeProvider`.

## Exact green-CI evidence before retirement

Evidence source: PR #352 exact head `ab099cfc8f5ca73ce404ef2ad4091de75f3d2d34`.

### Selected Shape Consumer Scan

`SelectedShapeStore`:

```text
Reference count: 0
```

`MooringPrimaryShapeSelectionStore`:

```text
Reference count: 5
Services/MooringIterativeSolver.cs: Clear
Services/MooringIterativeSolver.cs: Set
Services/MooringIterativeSolver.cs: Clear
Services/MooringPrimaryShapeGate.cs: declaration
validation/BuoyCalc.EngineeringRegression/Program.cs: Clear
```

There is no production consumer read of `MooringPrimaryShapeSelectionStore.Current`.

`MooringShapeStore.Current`:

```text
Reference count: 1
Services/MooringIterativeSolver.cs: var fallbackShape = MooringShapeStore.Current;
```

`MooringShapeStore.Set(`:

```text
Reference count: 2
Services/MooringIterativeSolver.cs: MooringShapeStore.Set(selection.Shape);
Services/TechnicalReportStorePublisher.cs: MooringShapeStore.Set(data.Shape);
```

### Report Store Consumer Scan

`MooringIterativeSolverStore`:

```text
Total references: 3
Declarations: 1
Write references: 1
Explicit reads: 0
```

Its references are the declaration, `TechnicalReportStorePublisher.Set(...)`, and validation `Clear()`.

`MooringShapeStore` has one explicit read, and that read is internal to `MooringIterativeSolverStore.Set(...)` as described above.

## Architectural conclusion

The remaining mutable stores form a closed compatibility loop. Their published state is not consumed by 2D, PDF, Markdown, the ViewModel, or the immutable snapshot selected-shape provider.

Therefore the compatibility loop can be retired as one behavior-preserving architecture package, provided the deterministic engineering golden baseline remains unchanged.

## Implementation boundary for the next PR

The implementation PR may only:

1. Remove `TechnicalReportStorePublisher.Publish(data)` from `CalculationSnapshotBuilder`.
2. Delete `TechnicalReportStorePublisher` when no caller remains.
3. Remove the `MooringIterativeSolverStore` compatibility class from `MooringIterativeSolver.cs` without changing iterative solver equations or result construction.
4. Remove the `MooringPrimaryShapeSelectionStore` compatibility class from `MooringPrimaryShapeGate.cs` without changing `MooringPrimaryShapeGate` or `MooringPrimaryShapeSelector` behavior.
5. Remove the `MooringShapeStore` compatibility class from `MooringShapeSolver.cs` without changing `MooringShapeSolver`, `MooringShapeResult`, nodes, offsets or buoy-state calculations.
6. Remove validation-only store-clearing calls that become impossible after retirement.
7. Update selected-shape/report-store scans and map guards so retired store symbols/files cannot silently return.
8. Update the dependency inventory to describe the snapshot-only selected X/Z path.

## Engineering invariants

The implementation must preserve all of the following:

- no solver equation changes;
- no iterative convergence-criterion changes;
- no primary-shape gate decision changes;
- no force, drag, buoyancy, tension or anchor formula changes;
- selected X/Z source, coordinates, horizontal offset, gate metadata and `UsesDiscreteLoads` unchanged;
- deterministic five-scenario engineering golden baseline unchanged;
- production segmentation remains `segmentCount = max(1, ceil(itemLength / 0.20))` with no segment-count cap;
- signed line `WeightWaterKgM` semantics remain unchanged, including negative buoyant-line values;
- 2D keeps real `xScale = zScale` and selected-X/Z-only behavior;
- PDF remains a renderer of selected X/Z only;
- no 3D.

## Explicit non-goals

This work package does not retire or change `MooringAlternativeShapeStore`.

It does not move `BuoyCalculator.Calculate` out of `MainWindowViewModel`.

It does not introduce a new global/static store as a replacement.

It does not modify report wording or UI behavior.

It does not make any claim that the current solver is physically validated. Physics improvements still require a separate Physics RFC and explicit golden-baseline review.

## Merge gate

Both this documentation PR and the later implementation PR require successful exact-head:

- `.NET Build` including deterministic engineering golden regression verification;
- `Selected Shape Consumer Scan`;
- `Report Store Consumer Scan`.
