# Control mark: alternative shape store consumer audit

Date: 2026-08-10  
Issue: #358  
Phase: architecture audit only

## Purpose

Classify every live `MooringAlternativeShapeStore` C# reference before making any retirement decision.

This audit does not change production behavior, solver physics, selected X/Z, rendering or report contents.

## Direct topology

`Services/MooringAlternativeShapeStore.cs` defines:

```text
MooringAlternativeShapeDisplayData
MooringAlternativeShapeStore.Current
MooringAlternativeShapeStore.Set(...)
MooringAlternativeShapeStore.Clear()
```

`MooringAlternativeDiscreteNodeProjector.Build(...)` returns `MooringAlternativeDiscreteNodeResult` directly and also performs compatibility writes:

```text
empty input -> MooringAlternativeShapeStore.Clear()
result built -> MooringAlternativeShapeStore.Set(alternativeShape, result)
return result
```

`TechnicalReportDataBuilder.Build(...)` consumes the returned result directly:

```text
var alternativeDiscreteNodes =
    MooringAlternativeDiscreteNodeProjector.Build(sequencePositions, discreteLoadShape, shape);

return new TechnicalReportData(..., alternativeDiscreteNodes, ...);
```

The immutable technical-report path therefore does not require a `MooringAlternativeShapeStore` read.

## Exact CI evidence

Evidence source: PR #359, Selected Shape Consumer Scan run #238 on head `b952a2d5bfabc73c15c3ec19c16871788336f8eb` before this evidence-only documentation update.

The scan reported:

```text
Alternative shape store audit: MooringAlternativeShapeStore
  Total textual C# references: 5
  Declarations: 1
  Set writes: 1
  Clear writes: 2
  Current reads: 0
```

Exact references:

```text
Services/MooringAlternativeDiscreteNodeProjector.cs:44
  MooringAlternativeShapeStore.Clear();

Services/MooringAlternativeDiscreteNodeProjector.cs:96
  MooringAlternativeShapeStore.Set(alternativeShape, result);

Services/MooringAlternativeShapeStore.cs:7
  public static class MooringAlternativeShapeStore

Services/PdfReportStructureGuide.cs:27
  textual report-guide mention only; no store API access

validation/BuoyCalc.EngineeringRegression/Program.cs:537
  MooringAlternativeShapeStore.Clear();
```

## Classification

Production store reads: **0**.

Production compatibility writes:

- one `Clear()` in `MooringAlternativeDiscreteNodeProjector` for empty projection input;
- one `Set(...)` in the same projector after building the returned result.

Validation-only writes:

- one `Clear()` before a regression scenario.

Non-API textual references:

- one historical source label in `PdfReportStructureGuide`.

The store's `Current` property has no consumer.

## Architectural conclusion

`MooringAlternativeShapeStore` does not provide data to any current product consumer. The actual alternative/discrete node result already travels directly from `MooringAlternativeDiscreteNodeProjector.Build(...)` into immutable `TechnicalReportData`.

The evidence supports a later documentation-first retirement package whose implementation may remove only the unused store side effects/type while preserving the projector result and all alternative/discrete calculations.

This audit PR itself still performs no retirement.

## Retirement boundary for a later work package

A separate retirement package may consider:

1. removing projector `MooringAlternativeShapeStore.Clear()` / `Set(...)` side effects while keeping the returned `MooringAlternativeDiscreteNodeResult` unchanged;
2. deleting `Services/MooringAlternativeShapeStore.cs` once no caller remains;
3. removing the validation-only `Clear()`;
4. updating the historical `PdfReportStructureGuide` source label separately only if required for truthful documentation;
5. making CI reject reintroduction of actual `MooringAlternativeShapeStore.` references.

Any such implementation must be a separate PR after a documentation-first retirement boundary is merged.

## Engineering invariants

Retirement must not change:

- `MooringAlternativeDiscreteNodeProjector` calculations or returned rows;
- `MooringDiscreteLoadShapeBuilder` calculations;
- iterative solver or primary-shape gate;
- selected X/Z source or coordinates;
- 2D/PDF geometry;
- force/tension/buoyancy/anchor formulas;
- fixed 0.20 m segmentation target or segment-count policy;
- signed `WeightWaterKgM` semantics;
- deterministic five-scenario golden engineering baseline;
- project JSON/version;
- no 3D.

## Merge gate

This audit PR may merge only when its **final exact head** has successful:

- `.NET Build` including golden engineering regression verification;
- `Selected Shape Consumer Scan`;
- `Report Store Consumer Scan`.
