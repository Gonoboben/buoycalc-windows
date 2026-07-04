# Control mark: main-window calculation display boundary

Date: 2026-07-04
Scope: architecture stabilization / documentation only
Related issue: #67

This control mark records how one engineering calculation is currently published into the user-facing state of `MainWindowViewModel`.

No production code, solver physics, numerical formulas, UI, XAML, PDF output, 2D output, generated user-result text, generated technical-report text, project JSON, or public view-model API is changed by this document.

## Current calculation entry point

The calculation command reaches:

```text
MainWindowViewModel.Calculate()
```

The method prepares the current inputs and calls:

```text
BuoyCalculator.Calculate(environment, buoy, items, anchor, safetyFactor)
```

The returned `CalculationResult` remains the engineering-core output.

The view model does not currently publish that result through one explicit user-facing display/read model. Instead, it updates several properties and collections independently.

## Current publication path

The current path is:

```text
BuoyCalculator.Calculate(...)
  -> CalculationResult
  -> ElementRows.Clear/Add(ElementCalculationDisplayRow.From(...))
  -> ReportBuildBoundary.Build(...)
       -> ResultText
       -> ReportText
  -> UpdateSequenceSummary(result)
       -> SequenceSummary
       -> SequenceDiagramLines
       -> UpdateVisualizationSummary(result)
            -> VisualizationDepthM
            -> VisualizationLineLengthM
            -> VisualizationOffsetM
            -> VisualizationDepthText
            -> VisualizationLineLengthText
            -> VisualizationOffsetText
            -> VisualizationSlackRatioText
            -> VisualizationStatusText
  -> UpdateCurrentProfileSummary()
```

This sequence is the behavior baseline for a later extraction.

## Publication group 1: element calculation rows

Immediately after calculation, the view model clears and repopulates:

```text
ElementRows
```

The source is:

```text
CalculationResult.ElementRows
```

Each engineering row is converted through the existing display conversion:

```text
ElementCalculationDisplayRow.From(row)
```

The main-window XAML binds its element table directly to `ElementRows`.

A future boundary must preserve:

```text
- row order
- row count
- current display conversion
- current number formatting
- current status text
- existing ObservableCollection publication behavior
```

The first implementation must not reinterpret engineering values or change table content.

## Publication group 2: user result text

The current user-facing result text is produced through:

```text
ReportBuildBoundary.Build(...).UserResultText
```

`ReportBuildBoundary` delegates this value to:

```text
UserReportBuilder.Build(environment, result)
```

`UserReportBuilder` currently uses:

```text
VerdictDisplayAdvisor.Build(environment, result)
UserStatusPolicy
```

The resulting string is assigned to:

```text
ResultText
```

The main-window result text box binds directly to `ResultText`.

A future display/read model must carry this already-built text unchanged in its first implementation. It must not rebuild the verdict independently inside the view model.

## Publication group 3: technical report text

The current technical report is produced in the same boundary call:

```text
ReportBuildBoundary.Build(...).TechnicalReportText
```

It is assigned to:

```text
ReportText
```

`ReportText` is displayed in the technical report areas and remains an existing fallback input to the 2D diagram source selector.

A future main-window display/read model may carry the existing technical report text as a publication value, but it must not reinterpret or parse that text.

Important invariant:

```text
technical report generation remains owned by the technical report path
```

## Publication group 4: sequence summary and diagram lines

After publishing report text, the view model calls:

```text
UpdateSequenceSummary(result)
```

The summary itself is prepared from the currently enabled assembly items rather than from the calculated result rows:

```text
SequenceSummary
```

The text sequence is rebuilt through:

```text
UpdateSequenceDiagram()
SequenceDiagramLines
```

These lines are used by:

```text
- sequence preview UI
- existing synthetic 2D fallback markers
```

The first extraction must preserve the current assembly-item source, ordering, labels, arrow rows, buoy row, and anchor row.

It must not replace the sequence with calculated element rows in the same PR.

## Publication group 5: visualization values

`UpdateSequenceSummary(result)` calls:

```text
UpdateVisualizationSummary(result)
```

Current numeric values are prepared from mixed sources:

```text
VisualizationDepthM
  <- parsed current Depth input

VisualizationLineLengthM
  <- sum of enabled assembly line lengths

VisualizationOffsetM
  <- CalculationResult.EstimatedOffsetM when result is available
```

Current display strings are then derived from those values:

```text
VisualizationDepthText
VisualizationLineLengthText
VisualizationOffsetText
VisualizationSlackRatioText
VisualizationStatusText
```

The status is a display-level geometry check:

```text
depth <= 0
  -> WARNING: depth is not specified

line length >= depth
  -> OK: line length is not less than depth

otherwise
  -> WARNING: line is shorter than depth
```

The `VisualizationStatusText` property passes assigned values through the existing `UserStatusPolicy.ToUserStatus(...)` mapper.

This status is not a replacement for the engineering-core verdict.

## Publication group 6: current-profile summary

After the result publication, the view model calls:

```text
UpdateCurrentProfileSummary()
```

This summary is prepared from current profile inputs and view-model state. It is not calculated from `CalculationResult`.

A later boundary must keep this distinction explicit:

```text
calculation-result publication
!=
current input/profile summary publication
```

The first implementation should not move current-profile editing or input summaries into the calculation display model.

## Current main-window consumers

The main-window XAML currently binds directly to:

```text
ResultText
ElementRows
ReportText
SequenceSummary
```

Other existing consumers include:

```text
SequenceDiagramLines
  -> sequence preview
  -> synthetic 2D fallback markers

VisualizationDepthM / VisualizationLineLengthM / VisualizationOffsetM
  -> Mooring2DCanvas display inputs

ReportText
  -> technical report display
  -> Mooring2DDiagramSourceSelector fallback parser
  -> PDF boundary inputs through the existing export path
```

The first implementation must preserve these public properties and consumers.

## Calculated values versus display projections

Engineering-core output:

```text
CalculationResult
CalculationResult.ElementRows
CalculationResult.EstimatedOffsetM
```

Existing report outputs:

```text
UserResultText
TechnicalReportText
```

Existing display projections:

```text
ElementCalculationDisplayRow items
SequenceSummary
SequenceDiagramLines
Visualization* numeric values
Visualization* text values
VisualizationStatusText
```

Existing input summaries:

```text
CurrentProfileSummary
project and library status text
```

A future boundary must not blur these categories.

## Preferred implementation boundary

A later production-code PR may introduce a small internal model and builder, for example:

```text
MainWindowCalculationDisplay
MainWindowCalculationDisplayBuilder
```

Equivalent naming is acceptable.

Target direction:

```text
current project name + environment + buoy + anchor + assembly inputs + CalculationResult
  -> existing report boundary
  -> prepared main-window calculation display/read model
  -> publish to existing MainWindowViewModel properties and collections
```

The builder may prepare immutable values and row lists. The view model should remain responsible for assigning properties and mutating its existing `ObservableCollection` instances so current bindings remain intact.

## First implementation invariants

The first implementation PR must preserve:

```text
- same call to BuoyCalculator.Calculate(...)
- same CalculationResult instance used for all publications
- same ElementCalculationDisplayRow conversion and order
- same UserReportBuilder output
- same TechnicalReportBuilder output
- same ResultText and ReportText values
- same sequence-summary calculation
- same SequenceDiagramLines content and order
- same visualization numeric values
- same visualization text and status values
- same UserStatusPolicy mapping
- same public MainWindowViewModel properties
- same ObservableCollection instances
- same XAML bindings
- same PDF and 2D inputs
- same UI behavior and output
```

## Non-goals

Do not change in the first implementation PR:

```text
- solver physics
- numerical formulas
- CalculationResult
- ReportBuildBoundary behavior
- UserReportBuilder wording
- technical report wording
- element-row display wording
- XAML layout or bindings
- project persistence format
- selected-shape logic
- PDF or 2D source priority
- current-profile calculation behavior
- public view-model API
- 3D functionality
```

## Safety gate

Every follow-up PR must state whether output changes.

Every follow-up PR must keep `BuoyCalc Windows Build` green.

No solver physics changes are allowed in this architecture-stabilization phase.
