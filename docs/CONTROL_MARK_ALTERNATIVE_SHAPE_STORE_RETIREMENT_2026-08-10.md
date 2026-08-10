# Control mark: alternative shape store retirement boundary

Date: 2026-08-10  
Issue: #360  
Evidence: #358 / PR #359

## Purpose

Define the behavior-preserving implementation boundary for retiring the unread `MooringAlternativeShapeStore` compatibility state.

This control mark is documentation-only. It does not change production code, calculations, selected X/Z, rendering, report output or the engineering golden baseline.

## Exact audit evidence

PR #359 completed an exact CI-backed consumer audit.

Selected Shape Consumer Scan reported:

```text
Total textual C# references: 5
Declarations: 1
Set writes: 1
Clear writes: 2
Current reads: 0
```

Classified references:

1. `Services/MooringAlternativeDiscreteNodeProjector.cs` — `Clear()` on empty input;
2. `Services/MooringAlternativeDiscreteNodeProjector.cs` — `Set(alternativeShape, result)` after the projection result is built;
3. `Services/MooringAlternativeShapeStore.cs` — store declaration;
4. `Services/PdfReportStructureGuide.cs` — textual provenance label only, no API access;
5. `validation/BuoyCalc.EngineeringRegression/Program.cs` — validation-only `Clear()`.

There are **zero** `MooringAlternativeShapeStore.Current` readers.

## Existing immutable data path

The real alternative/discrete projection result already flows without reading the store:

```text
MooringAlternativeDiscreteNodeProjector.Build(...)
  -> MooringAlternativeDiscreteNodeResult
TechnicalReportDataBuilder.Build(...)
  -> TechnicalReportData.AlternativeDiscreteNodes
```

`MooringAlternativeShapeStore.Set(...)` therefore publishes an unread duplicate of data that already exists in the immutable calculation/report result.

## Allowed implementation scope

A later implementation PR for #360 may only:

1. remove `MooringAlternativeShapeStore.Clear()` from the empty-input branch of `MooringAlternativeDiscreteNodeProjector.Build(...)`;
2. remove `MooringAlternativeShapeStore.Set(alternativeShape, result)` after result construction;
3. delete `Services/MooringAlternativeShapeStore.cs` after those writes are gone;
4. remove the validation-only `MooringAlternativeShapeStore.Clear()` from the deterministic engineering regression harness;
5. correct the stale `PdfReportStructureGuide` source label so it names the actual selected-X/Z snapshot/read-model source rather than a retired store;
6. update selected-shape consumer scans/maps so an actual `MooringAlternativeShapeStore` API reference cannot be reintroduced silently;
7. update the calculation snapshot dependency inventory.

## Required truth correction in PDF provenance text

`PdfReportStructureGuide` currently labels the PDF 2D source as:

```text
MooringAlternativeShapeStore
```

That has been architecturally stale since PDF migrated to explicit `SelectedShapeReadModel` selected X/Z.

The implementation PR may correct only this provenance label. It must not change PDF geometry, selection behavior, calculations or page structure.

The truthful source chain is:

```text
CalculationSnapshot.SelectedShape
  -> MainWindowViewModel.SelectedShape
  -> PdfReportBuilder
  -> SelectedShapeReadModel.Shape.Nodes
```

## Engineering invariants

The implementation must preserve:

- all `MooringAlternativeDiscreteNodeProjector` calculations and returned rows;
- all `MooringDiscreteLoadShapeBuilder` calculations;
- iterative solver behavior and convergence criteria;
- `MooringPrimaryShapeGate` / selector behavior;
- selected X/Z source, coordinates, gate metadata and `UsesDiscreteLoads`;
- 2D/PDF geometry and real X=Z scale;
- force, tension, buoyancy and anchor formulas;
- production segmentation `segmentCount = max(1, ceil(itemLength / 0.20))` with no segment-count cap;
- signed line `WeightWaterKgM`, including negative buoyant-line values;
- deterministic five-scenario engineering golden baseline;
- project JSON and application version;
- no 3D.

## Explicit non-goals

This package does not alter or remove:

- `MooringAlternativeDiscreteNodeResult`;
- `MooringAlternativeDiscreteNodeProjector` as a calculation/report component;
- `MooringDiscreteLoadShapeResult`;
- technical report alternative/discrete tables;
- the selected-shape gate or solver;
- any physics formula.

## Merge gate

Both this documentation PR and the later implementation PR require successful exact-head:

- `.NET Build` including deterministic engineering golden regression verification;
- `Selected Shape Consumer Scan`;
- `Report Store Consumer Scan`.
