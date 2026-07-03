# Control mark: PDF read-model boundary

Date: 2026-07-03
Scope: architecture stabilization / documentation only

This control mark records the next behavior-preserving boundary for PDF diagram source selection.

No production code, solver physics, numerical formulas, PDF output, 2D output, UI behavior, store behavior, generated Markdown output, or public app workflow are changed by this document.

## Current code state

`PdfReportBuilder.Build(...)` currently calls:

```text
SelectDiagramSource(reportText, visualizationOffsetM)
NormalizeResultText(resultText, diagramSource.ShapeOffsetM)
```

`SelectDiagramSource(...)` currently lives inside `PdfReportBuilder` and reads these inputs directly:

```text
MooringAlternativeShapeStore.Current
SelectedShapeStore.Current
reportText metric lines
visualizationOffsetM
```

The current priority must remain unchanged:

```text
1. alternative shape store
2. selected shape store
3. report text metric
4. visualization offset argument
```

`PdfDiagramSource` is currently a private nested record inside `PdfReportBuilder`.

## Boundary decision

The next code cleanup should introduce an explicit PDF diagram source/read-model boundary.

Target direction:

```text
current stores and report text
  -> PDF diagram source selector / read-model builder
  -> PdfReportBuilder rendering
```

The renderer should move toward receiving a prepared diagram source object rather than owning the selection policy.

## First allowed implementation PR

The first implementation PR may extract the current selection logic into a dedicated service such as:

```text
PdfDiagramSourceSelector
PdfDiagramSourceBuilder
PdfDiagramReadModelBuilder
```

Equivalent naming is acceptable.

The first implementation PR must be behavior-preserving:

```text
- same source priority
- same shape offset value
- same PDF pages
- same PDF text
- same diagram rendering
- same reportText parsing behavior
```

## What must not happen yet

Do not remove `reportText` metric parsing in the first implementation PR.

Do not change `PdfReportBuilder.Build(...)` public signature in the first implementation PR unless the PR explicitly documents why output remains unchanged.

Do not move PDF to solver-facing objects directly.

Do not introduce new physics or new numerical formulas.

## Why this boundary is needed

PDF currently mixes rendering and selection policy.

That makes it harder to prove that PDF, 2D, and UI are all displaying the same selected engineering state.

The goal is to make this chain explicit:

```text
Calculation/report data
  -> renderer-facing read model
  -> PDF / 2D / UI rendering
```

## Safety gate

Every follow-up PR must keep `.NET Build` green.

Every implementation PR must state whether output changes.

No solver physics changes are allowed in this architecture-stabilization phase.
