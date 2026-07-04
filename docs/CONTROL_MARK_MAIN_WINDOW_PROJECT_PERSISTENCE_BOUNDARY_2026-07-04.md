# Control mark: main-window project persistence boundary

Date: 2026-07-04
Scope: architecture stabilization / documentation only
Related issue: #73

This control mark records the current mapping between `MainWindowViewModel` state and `BuoyProjectDto`, together with the current save/load orchestration around `ProjectJsonStorage`.

This document changes no production code, JSON schema, persistence behavior, solver physics, formulas, UI, XAML, reports, PDF, 2D output, project defaults, library data, or public view-model API.

## Current persistence path

The current save path is:

```text
SaveProjectCommand
  -> SaveProjectAsync(promptForPath)
  -> choose or reuse target path
  -> ProjectJsonStorage.NormalizeJsonPath(...)
  -> MainWindowViewModel.ToDto()
  -> ProjectJsonStorage.Save(dto, targetPath)
  -> ProjectFilePath = targetPath
  -> ProjectStatusText
```

The current load path is:

```text
LoadProjectCommand
  -> LoadProjectAsync()
  -> choose or reuse source path
  -> ProjectJsonStorage.Load(sourcePath)
  -> MainWindowViewModel.FromDto(dto)
  -> ProjectFilePath = sourcePath
  -> ProjectStatusText
```

`ProjectJsonStorage` currently owns only file-system and JSON serialization concerns. `MainWindowViewModel` owns the complete DTO mapping and restoration policy.

## File-dialog and path behavior

### Save

`SaveProjectAsync(promptForPath)` currently starts with:

```text
targetPath = ProjectFilePath
```

A path is requested when either condition is true:

```text
promptForPath == true
or
ProjectFilePath is null/blank/whitespace
```

The suggested filename is:

```text
MakeSafeFileName(ProjectName) + ".json"
```

When a dialog service exists, the save path comes from:

```text
IProjectFileDialogService.PickSavePathAsync(suggestedFileName)
```

When no dialog service exists, the fallback is:

```text
ProjectJsonStorage.DefaultProjectPath
```

A null dialog result becomes an empty string. An empty/blank target cancels saving and sets:

```text
Сохранение отменено.
```

Before writing, the path is passed through:

```text
ProjectJsonStorage.NormalizeJsonPath(targetPath)
```

The normalized target becomes `ProjectFilePath` only after a successful save.

Any exception is converted into:

```text
Ошибка сохранения: {exception message}
```

### Load

When a dialog service exists, the load path comes from:

```text
IProjectFileDialogService.PickOpenPathAsync()
```

When no dialog service exists, the source path is the current:

```text
ProjectFilePath
```

A null dialog result becomes an empty string. An empty/blank source cancels loading and sets:

```text
Загрузка отменена.
```

`ProjectJsonStorage.Load(...)` returns null when the path is empty or the file does not exist. The view model then sets:

```text
Файл проекта не найден: {selectedPath}
```

After successful `FromDto(...)`, the original selected path becomes `ProjectFilePath`. The load path is not normalized in `LoadProjectAsync()`.

Any exception is converted into:

```text
Ошибка загрузки: {exception message}
```

The first boundary extraction must not change file-dialog timing, fallback paths, path normalization, cancellation handling, or status wording.

## Current `ProjectJsonStorage` behavior

`ProjectJsonStorage.Save(...)` currently:

```text
1. normalizes the extension with NormalizeJsonPath(...)
2. creates the parent directory when present
3. serializes with System.Text.Json
4. uses WriteIndented = true
5. writes the entire JSON text with File.WriteAllText(...)
```

`ProjectJsonStorage.Load(...)` currently:

```text
1. returns null for a blank path
2. returns null when the file does not exist
3. reads the entire file with File.ReadAllText(...)
4. deserializes directly to BuoyProjectDto
```

The first mapping-boundary extraction must not modify `ProjectJsonStorage` or JSON serializer options.

## Current save mapping: `ToDto()`

`ToDto()` creates one `BuoyProjectDto` and writes the following main-window values.

### Project and environment fields

```text
ProjectName
  <- ProjectName

WaterDensity
  <- WaterDensity

Depth
  <- Depth

CurrentSpeed
  <- CurrentSpeed

UseCurrentProfile
  <- "true" when UseCurrentProfile is true
  <- "false" when UseCurrentProfile is false

WaveHeight
  <- WaveHeight

WavePeriod
  <- WavePeriod

SelectedSeabedPresetId
  <- SelectedSeabedPreset.Id
  <- "unknown" when SelectedSeabedPreset is null
```

No numeric parsing or normalization occurs during `ToDto()`. Editable strings are stored as they currently exist.

### Buoy fields

```text
BuoyName
  <- BuoyName

SelectedBuoyPresetId
  <- SelectedBuoyPreset.Id
  <- empty string when SelectedBuoyPreset is null

BuoyVolume
  <- BuoyVolume

BuoyWeight
  <- BuoyWeight

BuoyArea
  <- BuoyArea

BuoyCd
  <- BuoyCd
```

All buoy numeric fields are saved as strings without parsing.

### Anchor fields

```text
SelectedAnchorPresetId
  <- SelectedAnchorPreset.Id
  <- empty string when SelectedAnchorPreset is null

AnchorName
  <- AnchorName

AnchorType
  <- AnchorType

AnchorMaterial
  <- AnchorMaterial

AnchorWeight
  <- AnchorWeight

AnchorVolume
  <- AnchorVolume

AnchorCoefficient
  <- AnchorCoefficient
```

All anchor numeric fields are saved as strings without parsing.

### Safety factor

```text
SafetyFactor
  <- SafetyFactor
```

The value is saved as the current editable string.

### Current-profile rows

The profile collection is saved in its current observable order:

```text
CurrentProfilePoints.Select(x => x.ToDto()).ToList()
```

Each row continues to own its DTO conversion through `CurrentProfilePointViewModel.ToDto()`.

The first extraction must not reorder, filter, sort, or parse profile rows while saving.

### Assembly rows

Every assembly row is saved in its current observable order, including disabled rows.

Each `AssemblyItemDto` receives:

```text
IsEnabled
  <- AssemblyItemViewModel.IsEnabled

Kind
  <- Kind

Title
  <- Title

RopePresetId
  <- RopePresetStorageId

ConnectorPresetId
  <- ConnectorPresetStorageId

PayloadPresetId
  <- PayloadPresetStorageId

LengthM
  <- LengthM

Count
  <- "1" when IsConnector is true
  <- Count otherwise

PayloadWeightAirKg
  <- PayloadWeightAirKg

PayloadVolumeM3
  <- PayloadVolumeM3

PayloadProjectedAreaM2
  <- PayloadProjectedAreaM2

PayloadDragCoefficient
  <- PayloadDragCoefficient
```

The connector count override is part of the current persistence format produced by the application and must be preserved by the first extraction.

## Current load mapping: `FromDto()`

The current restoration order is significant because property setters and library selection have side effects.

### Initial scalar assignment order

The current order begins with:

```text
1. ProjectName = dto.ProjectName
2. WaterDensity = dto.WaterDensity
3. Depth = dto.Depth
4. CurrentSpeed = dto.CurrentSpeed
5. UseCurrentProfile = dto.UseCurrentProfile equals "true", case-insensitive
6. WaveHeight = dto.WaveHeight
7. WavePeriod = dto.WavePeriod
8. SelectedSeabedPreset lookup
9. BuoyName assignment with fallback
10. RefreshLibraries()
11. SelectedBuoyPreset lookup
12. SelectedAnchorPreset lookup
13. conditional anchor field overrides
14. SafetyFactor = dto.SafetyFactor
```

Property setters may update summaries or visualization values during this sequence. The first extraction must not reorder publication in a way that changes visible intermediate or final state.

### Current-profile boolean decoding

`UseCurrentProfile` becomes true only when:

```text
string.Equals(dto.UseCurrentProfile, "true", StringComparison.OrdinalIgnoreCase)
```

All other strings, including blank values, become false.

The first extraction must not replace this with a broader boolean parser.

### Seabed lookup

The selected seabed is restored as:

```text
SeabedPresets.FirstOrDefault(x => x.Id == dto.SelectedSeabedPresetId)
  ?? SeabedCatalog.ById("unknown")
```

The ID comparison is the existing exact string equality used by `FirstOrDefault`.

### Buoy-name fallback

Before library refresh, the view model assigns:

```text
BuoyName = "Буй" when dto.BuoyName is null/blank/whitespace
BuoyName = dto.BuoyName otherwise
```

### Library refresh timing

`RefreshLibraries()` is called after the initial buoy-name assignment and before restoring selected buoy and anchor preset IDs.

`RefreshLibraries()` currently:

```text
1. reloads the buoy collection
2. assigns a buoy selection and applies that preset
3. reloads the anchor collection
4. assigns an anchor selection and applies that preset
5. refreshes sequence library options
```

Because selected-preset setters apply preset values, this refresh can overwrite editable buoy and anchor fields before the later restoration steps.

The first extraction must preserve this timing unless a separate behavior-changing issue explicitly addresses it.

### Selected buoy preset lookup

After refresh, the view model assigns:

```text
SelectedBuoyPreset =
    BuoyPresets.FirstOrDefault(x => x.Id == dto.SelectedBuoyPresetId)
    ?? SelectedBuoyPreset
```

Selecting a buoy preset invokes the current preset-application behavior.

### Existing buoy numeric restore behavior

Although `ToDto()` writes these fields:

```text
BuoyVolume
BuoyWeight
BuoyArea
BuoyCd
```

`FromDto()` currently does not directly assign the corresponding DTO strings back to the editable properties.

After load, buoy numeric values are therefore governed by the selected buoy preset and its existing setter side effects, not directly by the saved numeric strings.

This is recorded as existing behavior. Fixing or changing it is outside the first architecture extraction because it would change project-load output and compatibility behavior.

### Selected anchor preset lookup

After refresh, the view model assigns:

```text
SelectedAnchorPreset =
    AnchorPresets.FirstOrDefault(x => x.Id == dto.SelectedAnchorPresetId)
    ?? SelectedAnchorPreset
```

Selecting an anchor preset applies its current values before the conditional DTO overrides below.

### Conditional anchor overrides

Each anchor DTO field is applied only when it is not null/blank/whitespace:

```text
AnchorName
AnchorType
AnchorMaterial
AnchorWeight
AnchorVolume
AnchorCoefficient
```

Blank DTO fields leave the value produced by the selected anchor preset or current selection unchanged.

The first extraction must preserve independent per-field blank checks and assignment order.

### Safety factor

`SafetyFactor` is assigned directly from:

```text
dto.SafetyFactor
```

No blank fallback or numeric validation is performed in `FromDto()`.

## Display reset after scalar restoration

After scalar and preset restoration, the current calculated display is reset:

```text
ResultText = "Проект загружен. Нажмите «Рассчитать»."
ReportText = empty string
ElementRows.Clear()
SequenceDiagramLines.Clear()
```

The first extraction must keep the same strings and must clear the existing collection instances rather than replace them.

## Current-profile collection restoration

The current profile collection is rebuilt as follows:

```text
1. ClearCurrentProfilePoints()
2. for every dto.CurrentProfilePoints row:
     AddCurrentProfilePoint(CurrentProfilePointViewModel.FromDto(row))
3. when the resulting collection is empty:
     ResetCurrentProfile()
```

`ClearCurrentProfilePoints()` unsubscribes existing row events before clearing the collection.

`AddCurrentProfilePoint(...)` wires row events, adds the row to the existing observable collection, and updates the current-profile summary.

When no rows exist, `ResetCurrentProfile()` creates the current standard profile rows from current main-window values. It also performs its existing summary-update side effects.

The first extraction must not replace collection instances, bypass event wiring, change row order, filter rows, or alter the empty-list fallback.

## Assembly collection restoration

The current assembly collection is rebuilt as follows:

```text
1. ClearAssemblyItems()
2. for every dto.AssemblyItems row:
     create AssemblyItemViewModel
     AddAssemblyItem(viewModel)
3. UpdateSequenceSummary()
4. UpdateCurrentProfileSummary()
```

`ClearAssemblyItems()` unsubscribes item events before clearing the existing observable collection.

`AddAssemblyItem(...)` enforces the current connector count behavior, wires events, appends the item, and updates sequence summaries.

Every `AssemblyItemDto` maps to a new `AssemblyItemViewModel` with:

```text
IsEnabled
  <- item.IsEnabled

Kind
  <- item.Kind

Title
  <- item.Title

RopePresetStorageId
  <- NormalizeRopeId(item.RopePresetId)

ConnectorPresetStorageId
  <- NormalizeConnectorId(item.ConnectorPresetId)

PayloadPresetStorageId
  <- NormalizePayloadId(item.PayloadPresetId)

LengthM
  <- item.LengthM

Count
  <- "1" when item.Kind == "Connector"
  <- item.Count otherwise

PayloadWeightAirKg
  <- item.PayloadWeightAirKg

PayloadVolumeM3
  <- item.PayloadVolumeM3

PayloadProjectedAreaM2
  <- item.PayloadProjectedAreaM2

PayloadDragCoefficient
  <- item.PayloadDragCoefficient
```

The connector kind comparison during restoration is the existing exact case-sensitive comparison with `"Connector"`.

## Preset-ID compatibility normalization

### Rope IDs

`NormalizeRopeId(value)` currently applies this order:

```text
1. blank -> "built-in:polyester_20"
2. exact DisplayName match in RopeLibraryStorage.LoadAllRopes() -> matched Id
3. value already begins with "user:" or "built-in:", case-insensitive -> unchanged
4. otherwise -> "built-in:" + value
```

### Connector IDs

`NormalizeConnectorId(value)` currently applies this order:

```text
1. blank -> "built-in:shackle_55"
2. exact DisplayName match in ConnectorLibraryStorage.LoadAllConnectors() -> matched Id
3. value already begins with "user:" or "built-in:", case-insensitive -> unchanged
4. otherwise -> "built-in:" + value
```

### Payload IDs

`NormalizePayloadId(value)` currently applies this order:

```text
1. blank -> "built-in:adcp_40"
2. exact DisplayName match in PayloadLibraryStorage.LoadAllPayloads() -> matched Id
3. value already begins with "user:" or "built-in:", case-insensitive -> unchanged
4. otherwise -> value unchanged
```

The asymmetry between payload normalization and rope/connector normalization is existing compatibility behavior and must be preserved in the first extraction.

## Preferred implementation boundary

A later production PR may introduce small internal models and mappers, for example:

```text
MainWindowProjectPersistenceSource
MainWindowProjectDtoMapper
MainWindowProjectRestoreModel
```

Equivalent naming is acceptable.

A safe first extraction can separate deterministic mapping from view-model mutation:

```text
current editable values and row snapshots
  -> MainWindowProjectDtoMapper.ToDto(...)
  -> BuoyProjectDto
```

For loading, a mapper may prepare normalized values and row models:

```text
BuoyProjectDto + current library snapshots
  -> MainWindowProjectDtoMapper.FromDto(...)
  -> internal restore model
  -> MainWindowViewModel publishes values and rebuilds existing collections
```

The view model should remain responsible for:

```text
- file dialogs
- ProjectJsonStorage.Save/Load
- ProjectFilePath
- ProjectStatusText
- observable collection ownership
- event unsubscription and subscription
- selected-preset property assignment
- display clearing
- final summary updates
```

The first code extraction should be smaller than a complete save/load service and must not introduce an application-wide persistence mega-model.

## First implementation invariants

The first implementation PR must preserve:

```text
- same BuoyProjectDto fields and JSON property behavior
- same editable strings stored without parsing
- same UseCurrentProfile "true"/"false" encoding
- same seabed and selected-preset ID fallbacks
- same profile and assembly row order
- same inclusion of disabled assembly rows
- same connector count override when saving
- same load assignment order
- same case-insensitive "true" decoding
- same library refresh timing
- same selected preset lookup fallbacks
- same current lack of direct buoy numeric DTO restoration
- same per-field nonblank anchor overrides
- same safety-factor assignment
- same display reset strings and collection clearing
- same profile empty-list ResetCurrentProfile() fallback
- same event wiring through existing add/clear methods
- same preset-ID normalization rules and defaults
- same final UpdateSequenceSummary() and UpdateCurrentProfileSummary() calls
- same file-dialog, path, and status behavior
- same ProjectJsonStorage implementation
```

## Non-goals

Do not change in the first implementation PR:

```text
- JSON schema
- JSON property names
- serializer options
- file-format versioning
- migration policy
- load compatibility policy
- saved or restored values
- buoy numeric restoration behavior
- preset lookup behavior
- default project content
- collection instances
- event wiring
- status wording
- solver physics
- numerical formulas
- reports
- PDF or 2D boundaries
- XAML or UI layout
- public view-model API
- 3D functionality
```

## Safety gate

Every follow-up PR must explicitly state whether output changes.

Every follow-up PR must keep `BuoyCalc Windows Build` green.

No project JSON behavior changes are allowed in the first architecture-stabilization extraction.
