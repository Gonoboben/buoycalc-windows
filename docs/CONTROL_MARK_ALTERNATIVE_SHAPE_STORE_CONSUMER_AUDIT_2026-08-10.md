# Control mark: alternative shape store consumer audit

Date: 2026-08-10  
Issue: #358  
Phase: architecture audit only

## Purpose

Classify every live `MooringAlternativeShapeStore` C# reference before making any retirement decision.

This control mark does not authorize production changes. The audit PR may change only documentation and the existing selected-shape consumer scan.

## Known direct topology before exact CI classification

`Services/MooringAlternativeShapeStore.cs` defines:

```text
MooringAlternativeShapeDisplayData
MooringAlternativeShapeStore.Current
MooringAlternativeShapeStore.Set(...)
MooringAlternativeShapeStore.Clear()
```

`MooringAlternativeDiscreteNodeProjector.Build(...)` currently returns `MooringAlternativeDiscreteNodeResult` directly to its caller and also performs compatibility writes:

```text
empty input -> MooringAlternativeShapeStore.Clear()
result built -> MooringAlternativeShapeStore.Set(alternativeShape, result)
return result
```

`TechnicalReportDataBuilder.Build(...)` receives the returned value directly:

```text
var alternativeDiscreteNodes =
    MooringAlternativeDiscreteNodeProjector.Build(sequencePositions, discreteLoadShape, shape);

return new TechnicalReportData(..., alternativeDiscreteNodes, ...);
```

Therefore the immutable technical-report path does not require a store read at this call boundary.

The deterministic regression harness currently clears `MooringAlternativeShapeStore` before each scenario as compatibility cleanup.

## Exact audit mechanism

`tools/scan-selected-shape-consumers.ps1` is extended in this audit PR to print:

- total textual C# references;
- declaration count;
- `Set(...)` write count;
- `Clear()` write count;
- `Current` read count;
- every matching file, line and source line.

The exact green CI artifact from this PR is the source of truth for the retirement decision. No reference count is assumed in advance.

## Decision rule after CI

If the audit reports one or more production `MooringAlternativeShapeStore.Current` reads, those consumers must be classified and migrated first in separate small work packages.

If the audit reports zero `Current` reads and all remaining references are only:

- the store declaration;
- projector `Set/Clear` compatibility writes;
- validation-only `Clear()`;

then a later documentation-first retirement boundary may be created, followed by a separate implementation PR.

## Engineering invariants

The audit must not change:

- `MooringAlternativeDiscreteNodeProjector` behavior;
- alternative/discrete shape calculation;
- iterative solver or primary-shape gate;
- selected X/Z source or coordinates;
- 2D/PDF rendering;
- technical report contents;
- force/tension/buoyancy/anchor formulas;
- fixed 0.20 m segmentation target or segment count policy;
- signed `WeightWaterKgM` semantics;
- deterministic five-scenario golden engineering baseline;
- project JSON/version;
- 3D remains excluded.

## Merge gate

This audit PR may merge only when the exact head has successful:

- `.NET Build` including golden engineering regression verification;
- `Selected Shape Consumer Scan`;
- `Report Store Consumer Scan`.
