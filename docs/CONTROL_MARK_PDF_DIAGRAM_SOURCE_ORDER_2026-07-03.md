# Control mark: PDF diagram source order

Date: 2026-07-03
Scope: architecture stabilization / documentation only

This control mark records the current PDF diagram-source order before any behavior-preserving cleanup around PDF selected-shape inputs.

No production code, solver physics, numerical formulas, PDF output, 2D output, UI behavior, store behavior, generated Markdown output, or public app workflow are changed by this document.

## Current entry point

`Services/PdfReportBuilder.cs` currently builds the user PDF through:

```text
PdfReportBuilder.Build(...)
  -> SelectDiagramSource(reportText, visualizationOffsetM)
  -> NormalizeResultText(resultText, diagramSource.ShapeOffsetM)
```

The PDF chooses its diagram source before rendering the short user-facing diagram page.

## Current source priority

`SelectDiagramSource(...)` currently resolves `shapeOffsetM` using this priority order:

```text
1. MooringAlternativeShapeStore.Current
2. SelectedShapeStore.Current
3. TryReadReportMetric(reportText, "- Снос формы X/Z:")
4. TryReadReportMetric(reportText, "- Горизонтальный снос по узлам X/Z:")
5. visualizationOffsetM
```

In code terms, the method reads:

```text
var alternativeShape = MooringAlternativeShapeStore.Current;
var selectedShape = SelectedShapeStore.Current;
```

Then it prefers the alternative discrete shape offset when available, otherwise uses selected-shape offset, then report-text metrics, then the visualization offset argument.

## Current rendering behavior

When `MooringAlternativeShapeStore.Current` contains a shape with at least two rows, the user PDF renders the alternative discrete-shape diagram.

When no alternative discrete shape is available, the user PDF does not render the technical reserve diagram. It prints a user-facing explanation and a short line with depth, line length, and resolved offset.

This document does not change either path.

## Current report-text dependency

The current PDF path still has report-text metric parsing:

```text
TryReadReportMetric(reportText, "- Снос формы X/Z:")
TryReadReportMetric(reportText, "- Горизонтальный снос по узлам X/Z:")
```

This parsing exists only after explicit stores/read-model data are unavailable.

Boundary decision:

```text
PDF should move toward explicit renderer-facing/read-model input.
PDF reportText parsing should not be expanded.
```

## Relationship to selected-shape consumer map

The selected-shape consumer map already records that PDF is one of the first renderer consumers of `SelectedShapeStore`.

This document narrows that finding to the PDF-specific diagram-source selection path and records the exact remaining source chain.

## Allowed next cleanup

The next safe cleanup may document or prepare a behavior-preserving boundary such as:

```text
PdfReportBuilder.Build(...)
  -> explicit PDF diagram/read-model input
  -> SelectDiagramSource(pdfDiagramData)
```

or equivalent naming.

Any implementation PR must state explicitly whether output changes. The preferred first implementation PR should be behavior-preserving and should not alter PDF pages, wording, numbers, or diagram drawing.

## Non-goals

This document does not authorize changes to:

- solver physics;
- numerical formulas;
- iterative solver behavior;
- primary shape gate behavior;
- selected-shape gate behavior;
- PDF output;
- 2D output;
- UI behavior;
- generated Markdown output;
- public app workflow.

## Safety gate

Every follow-up PR must keep `.NET Build` green.

Every follow-up PR must state whether it changes output.

No solver physics changes are allowed in this architecture-stabilization phase.
