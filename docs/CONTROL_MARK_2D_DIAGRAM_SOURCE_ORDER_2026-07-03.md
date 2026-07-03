# Control mark: 2D diagram source order

Date: 2026-07-03
Scope: architecture stabilization / documentation only
Related issue: #64

This control mark records the current source-selection and fallback behavior of `Views/Mooring2DCanvas.cs` before any production-code extraction.

No production code, solver physics, numerical formulas, 2D visual output, PDF output, UI behavior, generated Markdown output, store behavior, or public workflow is changed by this document.

## Current entry point

The 2D view is rendered through:

```text
Mooring2DCanvas.Render(DrawingContext context)
```

Before selecting a shape source, the renderer reads these display values from `MainWindowViewModel`:

```text
VisualizationDepthM
VisualizationOffsetM
VisualizationLineLengthM
BuoyName
AnchorName
ReportText
SequenceDiagramLines
```

These values are display inputs. They are not recalculated by the 2D renderer.

## Current source-selection path

The current path is:

```text
Mooring2DCanvas.Render(...)
  -> SelectedShapeStore.Current
  -> MooringAlternativeShapeStore.Current
  -> ParseCalculatedNodes(vm.ReportText)
  -> DrawFallbackLine(...) from Visualization* and sequence labels
```

The effective source order must be understood with the branch conditions below.

## Source 1: selected engineering shape

The renderer first reads:

```text
var selectedShape = SelectedShapeStore.Current;
var shape = selectedShape?.Shape;
```

The selected engineering branch is used only when:

```text
selectedShape is not null
shape.Nodes.Count >= 2
```

When this condition is true, the renderer calls:

```text
DrawEngineeringComparison(...)
```

The selected shape supplies the primary X/Z line, selected-shape status, buoy state, depth, labels, and primary horizontal offset.

The renderer returns immediately after drawing this branch. Report-text nodes and the synthetic fallback line are not considered.

## Source 2: optional alternative-shape overlay

The renderer also reads:

```text
MooringAlternativeShapeStore.Current
```

The alternative shape is not an independent top-level fallback source.

It is used only as an optional overlay inside the selected engineering-shape branch. In other words:

```text
valid selected shape
  -> draw primary selected shape
  -> optionally draw alternative shape and discrete nodes
```

If the selected shape is unavailable or has fewer than two nodes, the renderer does not draw the alternative shape by itself. It continues to the report-text branch.

This behavior must remain unchanged in the first implementation refactor.

## Source 3: X/Z nodes parsed from report text

When no valid selected shape is available, the renderer calls:

```text
ParseCalculatedNodes(vm?.ReportText)
```

The parser searches the existing Markdown report for either section heading:

```text
## Расчётная форма постановки X/Z
## Расчётные узлы линии X/Z
```

It reads table rows beginning with `|`, ignores separator rows, parses the existing number, X, and Z columns, and orders nodes by number.

This branch is used only when at least two parsed nodes are available:

```text
parsedNodes.Count >= 2
```

The renderer then calls:

```text
DrawCalculatedLine(..., fromEngineeringCore: false)
```

The node geometry comes from the report-text table. Existing view-model display values still supply depth, offset, line length, buoy name, and anchor name.

The first implementation refactor must not remove, expand, reinterpret, or reorder this parsing behavior.

## Source 4: synthetic fallback line

When neither a valid selected shape nor at least two parsed report-text nodes are available, the renderer calls:

```text
DrawFallbackLine(...)
```

The fallback line uses existing view-model values:

```text
VisualizationDepthM
VisualizationOffsetM
BuoyName
AnchorName
SequenceDiagramLines
```

The fallback line is a display-only schematic. It does not run the solver and does not create new engineering results.

Internal element markers are distributed along the fallback line from existing `SequenceDiagramLines` labels.

## Exact current priority

The effective priority is:

```text
1. valid SelectedShapeStore.Current shape with at least two nodes
   + optional MooringAlternativeShapeStore.Current overlay
2. at least two X/Z nodes parsed from existing ReportText
3. synthetic fallback line from existing Visualization* values and sequence labels
```

Important invariant:

```text
alternative shape alone is not a fallback branch
```

## Rendering behavior owned by the canvas

`Mooring2DCanvas` currently owns both source selection and Avalonia drawing.

It also owns existing presentation behavior including:

```text
- equal X/Z scale
- centering and clamping of the drawing span
- water, bottom, buoy, anchor, node, and legend drawing
- selected and alternative shape labels
- horizontal-offset annotations
- fallback element-marker placement
- report-text node parsing
```

The first boundary refactor should separate source preparation from drawing without changing any of these behaviors.

## Preferred implementation boundary

The next production-code PR may introduce a small internal boundary such as:

```text
Mooring2DDiagramSource
Mooring2DDiagramSourceSelector
```

Equivalent naming is acceptable.

Target direction:

```text
stores + current view-model display values + existing report text
  -> prepared 2D diagram source/read model
  -> Mooring2DCanvas drawing
```

The first implementation should remain local to the 2D path. It should not introduce a shared PDF/2D abstraction yet.

## First implementation invariants

The first implementation PR must preserve:

```text
- same source priority
- same selected-shape validity condition
- same alternative-overlay condition
- same report-text headings and table parsing
- same synthetic fallback behavior
- same depth, offset, and line-length display values
- same labels and status text
- same X/Z scaling and geometry
- same colors and drawing order
- same 2D visual output
```

## Non-goals

Do not change in the first implementation PR:

```text
- solver physics
- numerical formulas
- shape-selection gate behavior
- SelectedShapeStore behavior
- alternative-shape calculations
- PDF output
- UI workflow
- generated Markdown output
- public view-model properties
- report-text format
- 3D functionality
```

## Safety gate

Every follow-up PR must state whether output changes.

Every follow-up PR must keep `BuoyCalc Windows Build` green.

No solver physics changes are allowed in this architecture-stabilization phase.
