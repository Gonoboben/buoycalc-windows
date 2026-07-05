# Control mark: main-window current profile summary boundary

Date: 2026-07-05
Scope: architecture stabilization / documentation only
Related issue: #85

This control mark records the current behavior of `MainWindowViewModel.UpdateCurrentProfileSummary()` and defines invariants for a later display-only read-model extraction.

This document changes no production code, solver physics, current-profile calculation policy, formulas, calculation inputs, profile row summaries, default templates, UI, XAML, reports, PDF, 2D, project JSON, event wiring, collection ownership, public view-model API, or user-facing strings.

## Current responsibility

The current method combines:

```text
UseCurrentProfile
+ CurrentSpeed raw text
+ CurrentProfilePoints live collection
  -> conditional row conversion
  -> depth ordering
  -> aggregate values
  -> CurrentProfileSummary text
```

The output is display-only. It is not the calculation result and it must not become a source of engineering physics.

## Public destination

The result is assigned through:

```text
CurrentProfileSummary = <text>
```

The existing property setter and public binding surface must remain unchanged in the first production extraction.

## Exact three-state behavior

### State 1 — profile disabled

Condition:

```text
UseCurrentProfile == false
```

Exact output:

```text
Профиль течения отключён. Используется одно значение скорости: {CurrentSpeed} м/с.
```

Important details:

- `CurrentSpeed` is inserted as the current raw string;
- no numeric parse or normalization occurs for this text;
- no profile point is converted with `ToInput()`;
- the method returns immediately;
- profile collection contents do not affect the text.

Examples of raw-value behavior that must remain possible:

```text
CurrentSpeed = "0,5" -> text contains "0,5"
CurrentSpeed = ""    -> text contains an empty value before " м/с"
```

The first extraction must not reformat this value through invariant culture or a numeric format string.

### State 2 — profile enabled but collection empty

Conditions:

```text
UseCurrentProfile == true
CurrentProfilePoints.Count == 0
```

Exact output:

```text
Профиль включён, но точки не заданы. Будет использовано одно значение скорости.
```

Important details:

- no profile point conversion occurs;
- `CurrentSpeed` is not inserted into this text;
- the method returns immediately.

### State 3 — profile enabled and populated

Conditions:

```text
UseCurrentProfile == true
CurrentProfilePoints.Count > 0
```

The method currently evaluates:

```text
inputs = CurrentProfilePoints
    .Select(x => x.ToInput())
    .OrderBy(x => x.DepthM)
    .ToList()

maxSpeed = inputs.Max(x => x.HorizontalSpeedMS)
minDepth = inputs.Min(x => x.DepthM)
maxDepth = inputs.Max(x => x.DepthM)
```

Exact output:

```text
Профиль включён: {inputs.Count} точек, глубины {minDepth:0.##}–{maxDepth:0.##} м, max |Uгор|={maxSpeed:0.###} м/с. В v0.19 расчёт использует эту max-скорость как переходную оценку.
```

Exact formatting:

```text
point count: default integer formatting
depth minimum: 0.##
depth maximum: 0.##
maximum horizontal speed: 0.###
depth separator: en dash “–”
```

The wording mentioning `v0.19` may look historical, but changing it is outside this architecture-only boundary.

## Snapshot and ordering semantics

In the populated state:

- each current row is converted once by `CurrentProfilePointViewModel.ToInput()`;
- conversion follows the current observable collection enumeration order;
- the resolved input snapshots are then ordered by numeric `DepthM`;
- ordering does not mutate `CurrentProfilePoints`;
- aggregates operate on the ordered snapshot list;
- point count is the snapshot-list count;
- the maximum uses `HorizontalSpeedMS`, not `SpeedMS`, `EastCurrentMS`, or the raw `CurrentSpeed` field.

The first extraction must not read formatted row `Summary` strings or reconstruct numeric values from display text outside `ToInput()`.

## `CurrentProfilePointViewModel.ToInput()` boundary

Each row currently resolves these editable strings:

```text
DepthM
EastCurrentMS
NorthCurrentMS
VerticalCurrentMS
WaterDensityKgM3
```

through the row's existing parse behavior into `CurrentProfilePointInput`.

The summary boundary must consume these resolved input snapshots. It must not duplicate row parsing rules.

## Direct property triggers

### `CurrentSpeed`

The setter currently performs:

```text
SetProperty(...)
  -> UpdateCurrentProfileSummary()
```

only when the value changes.

This trigger remains even when the profile is enabled and populated, although the populated summary does not read the raw `CurrentSpeed` property.

The first extraction must preserve this trigger rather than optimizing it away.

### `UseCurrentProfile`

The setter currently performs:

```text
SetProperty(...)
  -> UpdateCurrentProfileSummary()
```

only when the boolean value changes.

No collection mutation occurs in this setter.

## Row collection triggers

### Add row

`AddCurrentProfilePoint(CurrentProfilePointViewModel point)` currently performs:

```text
1. subscribe point.RemoveRequested
2. subscribe point.PropertyChanged
3. CurrentProfilePoints.Add(point)
4. UpdateCurrentProfileSummary()
```

The refresh occurs after collection mutation and after event wiring.

### Remove row

`RemoveCurrentProfilePoint(CurrentProfilePointViewModel point)` currently performs:

```text
1. unsubscribe point.RemoveRequested
2. unsubscribe point.PropertyChanged
3. CurrentProfilePoints.Remove(point)
4. UpdateCurrentProfileSummary()
```

The refresh occurs after removal.

### Edit row

`OnCurrentProfilePointChanged(...)` currently performs:

```text
1. point.RefreshSummary()
2. UpdateCurrentProfileSummary()
```

The individual row's `Summary` notification occurs before the main profile summary is rebuilt.

The first extraction must not reverse this order or remove the row-level refresh.

## Command and workflow triggers

### Add-current-profile-point command

`AddCurrentProfilePoint()` creates one row and then delegates to the event-wiring overload. Its default values depend on current collection state and live main-window values.

The summary boundary must not absorb row creation logic.

### Reset current profile

`ResetCurrentProfile()` currently:

```text
1. builds the default project template
2. clears existing points
3. adds each default profile row through AddCurrentProfilePoint(...)
4. calls UpdateCurrentProfileSummary() again
```

Each row addition produces an intermediate summary refresh, followed by one final explicit refresh.

The first extraction must not batch or suppress these publications.

### Default project reset

`ResetToDefaultProject()` causes additional profile summary updates through:

```text
CurrentSpeed setter
UseCurrentProfile setter
ClearCurrentProfilePoints()
ResetCurrentProfile()
second UseCurrentProfile assignment
```

The first extraction must not change this orchestration.

### Project load

`FromDto(...)` currently causes summary updates through:

```text
CurrentSpeed setter
UseCurrentProfile setter
each AddCurrentProfilePoint(...)
optional ResetCurrentProfile() when no points were restored
final UpdateCurrentProfileSummary()
```

The first extraction must preserve load timing and the existing repeated publications.

### Calculation

After calculation display publication, `Calculate()` currently calls:

```text
UpdateCurrentProfileSummary()
```

This refresh remains even though calculation display publication does not change the profile rows.

The first extraction must preserve this call.

## Separation from calculation input

The main-window profile summary is not the calculation-input boundary.

The calculation path continues to obtain profile inputs through `MainWindowCalculationInputBuilder` and the existing current-profile policy. The summary builder must not:

- choose the current speed used by the solver;
- modify `EnvironmentInput`;
- change enabled/disabled profile policy;
- change maximum-current selection used by calculation;
- publish data into the solver.

It only returns the existing user-facing summary string.

## Preferred production boundary

A later production PR may add an internal deterministic builder such as:

```text
MainWindowCurrentProfileSummaryBuilder
```

A suitable pure input surface is:

```text
Build(
    bool useCurrentProfile,
    string currentSpeedText,
    IReadOnlyList<CurrentProfilePointInput> points)
    -> string
```

Equivalent naming is acceptable.

To preserve lazy row conversion, `MainWindowViewModel` should only create the input snapshot list when:

```text
UseCurrentProfile == true
&& CurrentProfilePoints.Count > 0
```

For disabled and enabled-empty states it may pass an empty list to the builder, but it must not call row `ToInput()`.

The builder may sort the supplied resolved snapshots, or the view model may preserve the existing ordering step before the call. Whichever location is chosen, the observable collection must not be reordered.

## First implementation invariants

The first production PR must preserve:

```text
- all direct property triggers
- all add/remove/edit triggers
- point.RefreshSummary() before main summary refresh
- reset, load, and calculation refresh calls
- repeated intermediate publications
- no ToInput() calls in disabled state
- no ToInput() calls in enabled-empty state
- one ToInput() call per row in enabled-populated state
- collection enumeration before numeric depth ordering
- no mutation of CurrentProfilePoints order
- use of HorizontalSpeedMS
- exact point count, minimum depth, maximum depth, and maximum speed behavior
- exact strings, punctuation, spaces, en dash, and format strings
- raw CurrentSpeed display in disabled state
- publication through CurrentProfileSummary
- existing ObservableCollection instance
- existing event wiring
```

## Non-goals

Do not change in the first production PR:

```text
- solver physics or formulas
- current-profile calculation policy
- calculation-input construction
- CurrentProfilePointViewModel parsing
- CurrentProfilePointViewModel.Summary
- default project template data
- add/remove/reset/load workflows
- event subscriptions
- collection ownership
- reports, PDF, or 2D
- XAML or public view-model API
- project JSON
- user-facing strings
- 3D functionality
```

## Safety gate

Every follow-up PR must explicitly state whether output changes.

Every follow-up PR must keep `BuoyCalc Windows Build` green.

The first implementation is architecture-only and must be **Output unchanged**.
