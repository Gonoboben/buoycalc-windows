# Control mark: main-window sequence and visualization boundary

Date: 2026-07-04
Scope: architecture stabilization / documentation only
Related issue: #77

This control mark records the current construction and publication of the main-window sequence summary, textual sequence diagram, and compact visualization values.

This document changes no production code, solver physics, formulas, strings, UI, XAML, reports, PDF, 2D output, project persistence, libraries, presets, or public view-model API.

## Current duplicated paths

The live editable-state path is currently:

```text
MainWindowViewModel properties and AssemblyItems
  -> UpdateSequenceSummary(optional CalculationResult)
  -> UpdateSequenceDiagram()
  -> UpdateVisualizationSummary(optional CalculationResult)
  -> existing main-window properties and SequenceDiagramLines collection
```

The calculated-display path is currently:

```text
EnvironmentInput + AssemblyItemInput list + sequence display items + CalculationResult
  -> MainWindowCalculationDisplayBuilder.Build(...)
  -> MainWindowCalculationDisplay
  -> PublishCalculationDisplay(...)
  -> existing main-window properties and collections
```

Both paths currently contain equivalent calculated-path logic for:

```text
- active element count
- total active line length
- active connector count
- total active payload weight
- sequence summary wording
- diagram line order and labels
- visualization depth
- visualization line length
- L/Depth
- estimated offset
- visualization status precedence and wording
```

The first implementation should centralize deterministic read-model construction without changing publication timing.

## Existing live-path inputs

### `UpdateSequenceSummary(CalculationResult? result = null)`

The method reads:

```text
AssemblyItems
```

It filters rows with:

```text
x.IsEnabled
```

Every enabled row is converted at call time through:

```text
AssemblyItemViewModel.ToInput()
```

The conversion therefore keeps all existing parsing and library lookup behavior owned by `AssemblyItemViewModel`.

The method then calls, in this order:

```text
1. assign SequenceSummary
2. UpdateSequenceDiagram()
3. UpdateVisualizationSummary(result)
```

The first extraction must preserve this order and must not replace the existing observable collection.

### `UpdateSequenceDiagram()`

The method reads:

```text
BuoyName
AnchorName
AnchorType
AssemblyItems
```

It uses display-facing row values directly:

```text
IsEnabled
KindDisplayName
Title
Summary
```

It does not call `ToInput()` and does not parse numerical values.

### `UpdateVisualizationSummary(CalculationResult? result = null)`

The method reads:

```text
Depth
AssemblyItems
result?.EstimatedOffsetM
```

It parses `Depth` through the existing private `MainWindowViewModel.Parse(...)` method.

It filters enabled assembly rows, calls `ToInput()`, keeps only line elements, and sums their `LengthM`.

## Current sequence summary

After enabled rows have been converted to `AssemblyItemInput`, the live path calculates:

```text
enabledItems = all inputs where IsEnabled is true
lineLengthM = sum LengthM where Kind == Line
connectorCount = count where Kind == Connector
payloadWeightKg = sum PayloadWeightAirKg where Kind == Payload
```

The exact summary format is:

```text
Активных элементов: {enabledItems.Count} · линия: {lineLengthM:0.##} м · соединителей: {connectorCount} · приборы: {payloadWeightKg:0.##} кг
```

The separators, spaces, Russian wording, units, and `0.##` numeric formatting are existing output and must remain unchanged.

Disabled rows are excluded from every summary metric.

## Current sequence diagram

The existing `SequenceDiagramLines` collection is cleared before rebuilding.

The first line is:

```text
● Буй: {SafeText(BuoyName, "Буй")}
```

For every enabled assembly row in its current observable order, two lines are appended:

```text
↓
○ {KindDisplayName}: {SafeText(Title, "Элемент")} · {Summary}
```

After all enabled rows, the final two lines are:

```text
↓
■ Якорь: {SafeText(AnchorName, "Якорь")} · {SafeText(AnchorType, "тип не задан")}
```

Disabled rows do not produce an arrow or an element line.

No arrow is added between the buoy and the first element independently; the arrow belongs to each enabled-row iteration. Even with zero enabled rows, the final arrow before the anchor remains.

### `SafeText(...)`

The current helper behavior is:

```text
blank/null/whitespace -> fallback
otherwise -> value.Trim()
```

The first extraction must preserve trimming and every fallback string exactly.

## Current visualization calculations

### Depth

The live path calculates:

```text
depthM = Parse(Depth)
```

The existing parser:

```text
- replaces comma with dot
- uses NumberStyles.Any
- uses CultureInfo.InvariantCulture
- returns 0 when parsing fails
```

The calculated-display path instead receives the already parsed value:

```text
environment.DepthM
```

A shared read model must accept the resolved numeric depth and must not introduce a new parser.

### Active line length

Both paths calculate total line length from enabled `AssemblyItemInput` rows only:

```text
lineLengthM = sum LengthM where IsEnabled and Kind == Line
```

Connector counts, payload dimensions, and disabled line rows do not contribute to line length.

### Slack ratio

The exact rule is:

```text
slackRatio = depthM > 0 ? lineLengthM / depthM : 0
```

The display text is:

```text
L/Depth: {slackRatio:0.###}
```

when depth is positive, otherwise:

```text
L/Depth: не определено
```

### Offset

The live path calculates:

```text
offsetM = result?.EstimatedOffsetM ?? 0
```

It always publishes the numeric property:

```text
VisualizationOffsetM = offsetM
```

When `result` is null, the text is exactly:

```text
Оценочный снос: после расчёта
```

When a result is supplied, the text is:

```text
Оценочный снос: {offsetM:0.##} м
```

The calculated-display builder always receives a result and always uses the numeric form.

A shared builder must preserve the distinction between `offsetM == 0` and `result/offset availability == false`. A zero calculated offset must display `0 м`; an unavailable offset must display `после расчёта`.

### Numeric and text properties

The live path assigns these numeric properties:

```text
VisualizationDepthM
VisualizationLineLengthM
VisualizationOffsetM
```

It then assigns these text properties:

```text
VisualizationDepthText = "Глубина: {depthM:0.##} м"
VisualizationLineLengthText = "Длина линии: {lineLengthM:0.##} м"
VisualizationOffsetText
VisualizationSlackRatioText
VisualizationStatusText
```

The first extraction must preserve assignment behavior and final values.

## Current visualization status

The raw status precedence is:

```text
if depthM <= 0:
    "WARNING: глубина не задана"
else if lineLengthM >= depthM:
    "OK: длина линии не меньше глубины"
else:
    "WARNING: линия короче глубины"
```

This order matters. A nonpositive depth takes precedence over line length.

`VisualizationStatusText` has a property setter that passes the raw value through:

```text
UserStatusPolicy.ToUserStatus(value)
```

Therefore the builder currently produces technical-prefix strings, while the public property stores the user-facing policy result. Both live publication and `PublishCalculationDisplay(...)` pass through the same setter.

The first extraction must not bypass `VisualizationStatusText` or pre-apply the policy a second time.

## Calculated-display duplication

`MainWindowCalculationDisplayBuilder.Build(...)` currently repeats the sequence and visualization logic using:

```text
EnvironmentInput environment
IReadOnlyList<AssemblyItemInput> assemblyItems
IReadOnlyList<MainWindowSequenceDisplayItem> sequenceItems
string buoyName
string anchorName
string anchorType
CalculationResult result
```

The builder:

```text
1. filters assembly inputs by IsEnabled
2. calculates the same summary metrics and exact summary string
3. builds the same diagram line sequence from enabled display items
4. uses environment.DepthM
5. uses result.EstimatedOffsetM
6. builds the same depth, line-length, offset, slack, and status values
7. returns them inside MainWindowCalculationDisplay
```

The report and element-row parts of `MainWindowCalculationDisplayBuilder` are outside this boundary and must remain there.

## Existing trigger behavior

Publication triggers are part of current behavior.

### Diagram-only triggers

These setters invoke `UpdateSequenceDiagram()` without recomputing summary or visualization:

```text
BuoyName
AnchorName
AnchorType
```

`SelectedBuoyPreset` and `SelectedAnchorPreset` apply preset values and also explicitly invoke `UpdateSequenceDiagram()`. Their applied name/type property setters may already invoke diagram updates.

This selective behavior is critical after a calculation: changing only a displayed buoy or anchor label must not accidentally replace the calculated offset text with `после расчёта`.

A later shared builder may expose a diagram-only method/read model, or the view model may selectively publish only diagram lines. It must not turn these triggers into full live refreshes.

### Visualization-only triggers

These setters invoke `UpdateVisualizationSummary()` directly:

```text
WaterDensity
Depth
```

`WaterDensity` is not used by `UpdateVisualizationSummary()` itself, but its current setter still triggers the update. This existing trigger must remain unless a separate behavior-changing issue removes it.

A direct visualization refresh with no result resets numeric offset to `0` and offset text to `после расчёта`.

### Current-profile-only triggers

These setters invoke `UpdateCurrentProfileSummary()` and are outside the first sequence/visualization extraction:

```text
CurrentSpeed
UseCurrentProfile
```

### Full sequence/visualization triggers

The following paths invoke `UpdateSequenceSummary()` and therefore update summary, diagram, and visualization:

```text
- RefreshSequenceLibraryOptions()
- AddAssemblyItem(...)
- OnAssemblyItemChanged(...)
- RemoveItem(...)
- MoveItemUp(...)
- MoveItemDown(...)
- DuplicateItem(...)
- final reset/default-project refreshes
- final project-load refreshes
```

Some reset and load paths also call `AddAssemblyItem(...)` repeatedly, causing repeated intermediate refreshes before a final refresh. The first extraction must not change collection ownership or event timing.

### Calculation publication

`Calculate()` currently:

```text
1. builds calculation input
2. calls the existing solver
3. creates sequence display-item snapshots
4. calls MainWindowCalculationDisplayBuilder.Build(...)
5. calls PublishCalculationDisplay(...)
6. updates current-profile summary
```

`PublishCalculationDisplay(...)` clears and refills existing collections and assigns the calculation display fields directly. The first sequence/visualization extraction must not change solver invocation or report construction.

## Preferred implementation boundary

A later production PR may add an internal read model such as:

```text
MainWindowSequenceVisualizationDisplay
MainWindowSequenceVisualizationDisplayBuilder
```

Equivalent naming is acceptable.

A suitable input boundary may include:

```text
- resolved numeric depth
- AssemblyItemInput snapshots
- MainWindowSequenceDisplayItem snapshots
- buoy name
- anchor name
- anchor type
- optional offset availability
- resolved offset value
```

The builder may expose a complete read model and a diagram-only function, or smaller deterministic functions under one boundary. Publication remains the responsibility of `MainWindowViewModel` and `MainWindowCalculationDisplayBuilder`.

## First implementation invariants

The first implementation PR must preserve:

```text
- enabled-item filtering
- AssemblyItemViewModel.ToInput() timing in the live path
- assembly observable order
- exact sequence summary string and formats
- exact diagram lines, arrows, symbols, spaces, fallbacks, and trimming
- final anchor arrow with zero enabled items
- exact depth parsing ownership
- active line-length sum
- slack formula and formats
- distinction between unavailable offset and calculated zero offset
- exact raw status precedence and strings
- publication through VisualizationStatusText and UserStatusPolicy
- diagram-only trigger behavior
- visualization-only trigger behavior
- full-refresh trigger behavior
- existing collection instances
- report and element-row construction
- solver call and all physics
```

## Non-goals

Do not change in the first implementation PR:

```text
- solver physics or formulas
- calculation inputs
- current-profile summary
- project persistence
- library or preset behavior
- event subscriptions
- observable collection ownership
- public view-model API
- XAML or UI layout
- report construction
- PDF or 2D boundaries
- JSON schema
- status policy
- user-facing strings
- 3D functionality
```

## Safety gate

Every follow-up PR must explicitly state whether output changes.

Every follow-up PR must keep `BuoyCalc Windows Build` green.

The first implementation is architecture-only and must be **Output unchanged**.
