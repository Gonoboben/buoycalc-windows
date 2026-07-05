# Control mark: main-window default project template boundary

Date: 2026-07-05
Scope: architecture stabilization / documentation only
Related issue: #81

This control mark records the current default-project data and the exact imperative workflow used to publish it into `MainWindowViewModel`.

This document changes no production code, solver physics, formulas, calculation inputs, UI, XAML, reports, PDF, 2D output, project JSON, libraries, presets, event wiring, collection ownership, or public view-model API.

## Current ownership

The default-project workflow is currently embedded in:

```text
MainWindowViewModel.ResetToDefaultProject()
MainWindowViewModel.ResetCurrentProfile()
MainWindowViewModel.AddCurrentProfilePoint(...)
MainWindowViewModel.AddAssemblyItem(...)
```

The workflow mixes two responsibilities:

```text
immutable standard-template data
  +
imperative application through setters, collections, and events
```

The first production extraction should move only the immutable template data. Application and publication remain in `MainWindowViewModel`.

## Constructor order

The constructor creates the existing observable collections and commands, then calls:

```text
1. RefreshLibraries()
2. ResetToDefaultProject()
```

This order is required because the default project selects buoy and anchor presets from the already populated library collections.

The first extraction must not construct or apply the default template before `RefreshLibraries()`.

## `NewProject()` behavior

`NewProject()` currently calls:

```text
1. ResetToDefaultProject()
2. ProjectStatusText = "Создан новый проект на основе стандартного шаблона."
```

The status assignment occurs after the complete reset workflow and must remain outside the immutable template unless a later issue explicitly changes status orchestration.

## Exact scalar assignment order

`ResetToDefaultProject()` currently assigns the following properties in this exact order:

```text
1. ProjectName = "Тестовый проект"
2. ProjectFilePath = ProjectJsonStorage.DefaultProjectPath
3. WaterDensity = "1025"
4. Depth = "50"
5. CurrentSpeed = "0.5"
6. UseCurrentProfile = false
7. WaveHeight = "1.0"
8. WavePeriod = "6.0"
9. SelectedSeabedPreset = SeabedCatalog.ById("unknown")
10. SelectedBuoyPreset = BuoyPresets.FirstOrDefault()
11. SelectedAnchorPreset = AnchorPresets.FirstOrDefault(x => x.Id == "built-in:concrete_500")
    ?? AnchorPresets.FirstOrDefault()
12. SafetyFactor = "5"
13. ResultText = "Нажмите «Рассчитать»."
14. ReportText = ""
15. ElementRows.Clear()
16. SequenceDiagramLines.Clear()
```

The order is behavior because several setters trigger updates.

### Setter side effects preserved by this order

```text
WaterDensity
  -> UpdateVisualizationSummary()

Depth
  -> UpdateVisualizationSummary()

CurrentSpeed
  -> UpdateCurrentProfileSummary()

UseCurrentProfile
  -> UpdateCurrentProfileSummary()

SelectedBuoyPreset
  -> ApplySelectedBuoyPreset()
  -> BuoyName setter
  -> diagram refreshes
  -> explicit UpdateSequenceDiagram()

SelectedAnchorPreset
  -> ApplySelectedAnchorPreset()
  -> AnchorName and AnchorType setters
  -> diagram refreshes
  -> explicit UpdateSequenceDiagram()
```

The first extraction must continue applying scalar values through these existing properties in the same order. It must not assign backing fields directly.

## Default preset rules

### Seabed

The default seabed is resolved by:

```text
SeabedCatalog.ById("unknown")
```

The template should carry the identifier `unknown`; the view model should continue resolving it through the existing catalog.

### Buoy

The default buoy is:

```text
BuoyPresets.FirstOrDefault()
```

There is no hard-coded buoy ID in the current reset workflow.

The first extraction must not invent a buoy ID or change library ordering semantics.

### Anchor

The preferred anchor ID is:

```text
built-in:concrete_500
```

Selection currently uses:

```text
AnchorPresets.FirstOrDefault(x => x.Id == "built-in:concrete_500")
    ?? AnchorPresets.FirstOrDefault()
```

The fallback to the first available anchor must remain.

## Calculated-display reset

Before rebuilding profile and assembly rows, the workflow publishes:

```text
ResultText = "Нажмите «Рассчитать»."
ReportText = ""
ElementRows.Clear()
SequenceDiagramLines.Clear()
```

It does not directly assign `SequenceSummary` or individual visualization fields at this point. Those are rebuilt through later helper calls and setter side effects.

The existing `ElementRows` and `SequenceDiagramLines` collection instances must not be replaced.

## Current-profile reset workflow

After the scalar and calculated-display reset, `ResetToDefaultProject()` performs:

```text
1. ClearCurrentProfilePoints()
2. ResetCurrentProfile()
3. UseCurrentProfile = false
```

`ResetCurrentProfile()` itself begins with another:

```text
ClearCurrentProfilePoints()
```

This means the current workflow clears the profile collection twice during a default-project reset. The first production extraction must not silently optimize this away.

### Current-profile event behavior

`ClearCurrentProfilePoints()` unsubscribes each existing row from:

```text
RemoveRequested
PropertyChanged
```

and then clears the existing collection.

Every new row is applied through:

```text
AddCurrentProfilePoint(CurrentProfilePointViewModel point)
```

which:

```text
1. subscribes RemoveRequested
2. subscribes PropertyChanged
3. adds the row to CurrentProfilePoints
4. calls UpdateCurrentProfileSummary()
```

The first extraction must keep this helper-based application and the repeated intermediate summary updates.

## Exact default current-profile rows

`ResetCurrentProfile()` adds these rows in order:

### Row 1

```text
DepthM = "0"
EastCurrentMS = CurrentSpeed
NorthCurrentMS = "0"
VerticalCurrentMS = "0"
WaterDensityKgM3 = WaterDensity
```

With the standard scalar values already applied, the final values are normally:

```text
0 m, east 0.5 m/s, north 0, vertical 0, density 1025 kg/m³
```

The source dependencies remain significant: the row reads the current `CurrentSpeed` and `WaterDensity` properties at application time.

### Row 2

```text
DepthM = "10"
EastCurrentMS = "0.45"
NorthCurrentMS = "0"
VerticalCurrentMS = "0"
WaterDensityKgM3 = WaterDensity
```

### Row 3

```text
DepthM = "25"
EastCurrentMS = "0.3"
NorthCurrentMS = "0"
VerticalCurrentMS = "0"
WaterDensityKgM3 = WaterDensity
```

### Row 4

```text
DepthM = Depth
EastCurrentMS = "0.1"
NorthCurrentMS = "0"
VerticalCurrentMS = "0"
WaterDensityKgM3 = WaterDensity
```

With the standard scalar values, the final depth is normally `50` m.

The row must continue reading the current `Depth` and `WaterDensity` properties at application time.

After adding all four rows, `ResetCurrentProfile()` calls:

```text
UpdateCurrentProfileSummary()
```

Then `ResetToDefaultProject()` assigns:

```text
UseCurrentProfile = false
```

again. The second assignment and its existing setter behavior must remain.

## Assembly reset workflow

After the current profile is rebuilt, the workflow performs:

```text
1. ClearAssemblyItems()
2. AddAssemblyItem(default row 1)
3. AddAssemblyItem(default row 2)
4. AddAssemblyItem(default row 3)
5. AddAssemblyItem(default row 4)
6. AddAssemblyItem(default row 5)
7. UpdateSequenceSummary()
```

### Assembly clear behavior

`ClearAssemblyItems()` unsubscribes each existing row from:

```text
PropertyChanged
RemoveRequested
MoveUpRequested
MoveDownRequested
DuplicateRequested
```

and then clears the existing `AssemblyItems` collection.

The collection instance must remain unchanged.

### Assembly add behavior

Each row is applied through:

```text
AddAssemblyItem(AssemblyItemViewModel item)
```

which currently performs:

```text
1. if item.IsConnector, item.Count = "1"
2. WireItem(item)
3. AssemblyItems.Add(item)
4. UpdateSequenceSummary()
```

`WireItem(...)` subscribes the existing remove, move, duplicate, and property-change handlers.

The first extraction must not bulk-add un-wired rows or bypass connector count normalization.

## Exact default assembly rows

### Row 1 — connector

```text
Kind = "Connector"
Title = "Скоба под буем"
ConnectorPresetStorageId = "built-in:shackle_55"
Count = "1"
```

### Row 2 — line

```text
Kind = "Line"
Title = "Верхний буйреп"
RopePresetStorageId = "built-in:polyester_20"
LengthM = "45"
```

### Row 3 — connector

```text
Kind = "Connector"
Title = "Вертлюг"
ConnectorPresetStorageId = "built-in:swivel_60"
Count = "1"
```

### Row 4 — payload

```text
Kind = "Payload"
Title = "ADCP"
PayloadPresetStorageId = "built-in:adcp_40"
```

### Row 5 — line

```text
Kind = "Line"
Title = "Нижняя цепь"
RopePresetStorageId = "built-in:chain_10"
LengthM = "10"
```

Fields not explicitly initialized continue using `AssemblyItemViewModel` defaults and preset-selection side effects. The first extraction must not eagerly fill those fields with newly invented values.

## Repeated intermediate publication

The current default workflow intentionally or incidentally produces repeated updates:

```text
- scalar setters update profile, diagram, or visualization fields
- each current-profile row addition updates CurrentProfileSummary
- ResetCurrentProfile() updates CurrentProfileSummary again
- the second UseCurrentProfile assignment may update CurrentProfileSummary
- each assembly row addition updates sequence summary, diagram, and visualization
- the final UpdateSequenceSummary() repeats the full sequence/visualization publication
```

The first production extraction must move data only. It must not introduce batching, suppression flags, direct collection replacement, or a single final publication pass.

Reducing repeated updates would be a separate behavior/performance issue.

## Existing field initializers are not the template boundary

`MainWindowViewModel` also has backing-field initializers such as:

```text
_projectName = "Тестовый проект"
_waterDensity = "1025"
_depth = "50"
_currentSpeed = "0.5"
...
```

These initializers exist before constructor publication and may support binding state during construction. The first default-template extraction should not remove or rewrite them unless compilation requires a mechanical change and final behavior is proven identical.

The authoritative standard project is the data applied by `ResetToDefaultProject()` and `ResetCurrentProfile()` after libraries are refreshed.

## Preferred production boundary

A later production PR may add internal immutable records such as:

```text
MainWindowDefaultProjectTemplate
MainWindowDefaultEnvironmentTemplate
MainWindowDefaultCurrentProfilePointTemplate
MainWindowDefaultAssemblyItemTemplate
MainWindowDefaultProjectTemplateBuilder
```

Equivalent naming is acceptable.

The template may contain:

```text
- project/environment scalar strings
- seabed ID
- preferred anchor ID
- safety factor
- result and report reset text
- profile row descriptors, including value-source markers for CurrentSpeed, WaterDensity, and Depth
- assembly row descriptors
```

The template should not contain live observable collections, commands, view-model event handlers, selected library objects, or calculated solver results.

## First implementation invariants

The first production PR must preserve:

```text
- constructor call order
- NewProject status assignment order
- exact scalar assignment order
- all exact scalar strings
- seabed lookup through SeabedCatalog
- first-buoy selection semantics
- preferred-anchor ID and first-anchor fallback
- setter side effects
- calculated collection clearing order
- duplicate profile clearing
- all profile rows and application-time source dependencies
- profile event subscriptions and repeated summary updates
- second UseCurrentProfile = false assignment
- assembly clear/unwire behavior
- all assembly rows and order
- connector count normalization
- assembly event wiring
- repeated intermediate sequence/visualization updates
- final UpdateSequenceSummary() call
- existing ObservableCollection instances
- all final user-facing values
```

## Non-goals

Do not change in the first production PR:

```text
- solver physics or formulas
- calculation input construction
- report construction
- PDF or 2D boundaries
- XAML or UI layout
- project JSON or persistence
- library storage
- preset loading or preset application behavior
- event subscriptions
- collection ownership
- public view-model API
- user-facing strings
- 3D functionality
```

## Safety gate

Every follow-up PR must explicitly state whether output changes.

Every follow-up PR must keep `BuoyCalc Windows Build` green.

The first implementation is architecture-only and must be **Output unchanged**.
