# Control mark: main-window current profile point lifecycle boundary

Date: 2026-07-06
Scope: architecture stabilization / documentation only
Related issue: #121

This control mark records the current lifecycle behavior for `CurrentProfilePointViewModel` instances owned by `MainWindowViewModel.CurrentProfilePoints`.

This document changes no production code, collection ownership, commands, events, DTOs, parsing, default templates, project restore, solver physics, formulas, calculation inputs, reports, PDF, 2D, XAML, public API, or user-facing output.

## Owned collection

`MainWindowViewModel` creates one collection instance:

```text
CurrentProfilePoints = new ObservableCollection<CurrentProfilePointViewModel>()
```

Required behavior:

- the collection instance is retained for the lifetime of the ViewModel;
- add, remove, and clear mutate that existing instance;
- no lifecycle extraction may replace the collection;
- collection notifications remain the synchronous notifications produced by `ObservableCollection<T>`;
- the ViewModel does not subscribe to `CurrentProfilePoints.CollectionChanged` itself;
- `CurrentProfileSummary` publication is triggered explicitly by point lifecycle methods, current-profile property setters, and point `PropertyChanged` subscriptions.

## Point-owned command and event

Every `CurrentProfilePointViewModel` constructor creates one always-enabled `RelayCommand`:

```text
RemoveCommand -> RemoveRequested?.Invoke(this)
```

The point exposes:

```text
RemoveRequested
PropertyChanged
```

Required behavior:

- `RelayCommand.CanExecute(...)` remains `true` because no predicate is supplied;
- command execution invokes `RemoveRequested` synchronously on the current thread;
- subscriber invocation-list order remains normal .NET event order;
- exceptions from subscribers propagate through the command call;
- the command does not directly mutate `CurrentProfilePoints`;
- an unwired point command has no main-window effect.

## Editable point properties

A point owns these editable string properties:

```text
DepthM
EastCurrentMS
NorthCurrentMS
VerticalCurrentMS
WaterDensityKgM3
```

Each setter uses `SetProperty(...)` and therefore raises its normal synchronous `PropertyChanged` event only when the value changes according to the existing equality behavior.

Required behavior:

- values remain strings in the ViewModel;
- no validation, clamping, normalization, delayed notification, or error status is added;
- parsing remains in `ToInput()` and continues to replace comma with dot, use invariant `NumberStyles.Any`, and fall back to zero;
- property events raised before main-window wiring do not reach `OnCurrentProfilePointChanged(...)`.

## Add-command construction decision

`AddCurrentProfilePoint()` without arguments prepares a new point from current live state.

Current order:

```text
1. read CurrentProfilePoints.Count
2. if count is zero, choose depth = 0
3. otherwise enumerate CurrentProfilePoints, convert each row with ToInput(),
   take maximum parsed DepthM, and choose maximum + 10
4. construct a new CurrentProfilePointViewModel
5. assign DepthM = FormatDouble(depth)
6. assign EastCurrentMS = CurrentSpeed when the collection was empty,
   otherwise assign exact text "0.3"
7. assign NorthCurrentMS = "0"
8. assign VerticalCurrentMS = "0"
9. assign WaterDensityKgM3 = WaterDensity
10. call AddCurrentProfilePoint(point)
```

Required behavior:

- the empty decision is based on the count observed before construction;
- the nonempty depth decision converts every current row through `ToInput()`;
- maximum depth uses parsed numeric values, not raw string ordering;
- invalid or blank row depths therefore contribute parsed zero under the current parsing policy;
- `FormatDouble(...)` remains invariant `0.###` formatting;
- first-point east-current text is the current raw `CurrentSpeed` string;
- later-point east-current text remains exactly `"0.3"`;
- north and vertical current remain exactly `"0"`;
- water density is copied from the current raw `WaterDensity` string;
- all object-initializer setters run before main-window handlers are attached;
- an exception during construction, enumeration, conversion, or property assignment prevents the point from being wired or added.

## `AddCurrentProfilePoint(point)` exact order

Current behavior:

```text
1. point.RemoveRequested += RemoveCurrentProfilePoint
2. point.PropertyChanged += OnCurrentProfilePointChanged
3. CurrentProfilePoints.Add(point)
4. UpdateCurrentProfileSummary()
```

Required behavior:

- both main-window handlers are attached before collection insertion;
- wire order remains `RemoveRequested`, then `PropertyChanged`;
- the point can already have external subscribers before these handlers are attached;
- repeated calls with the same point attach repeated delegate registrations;
- the collection can contain the same point reference more than once;
- there is no duplicate-point or duplicate-subscription guard;
- synchronous collection observers run while the point is already wired;
- the explicit main-summary update occurs only after `Add(...)` returns successfully;
- if wiring succeeds and collection insertion or a collection observer throws, no rollback detaches the point;
- no row `RefreshSummary()` call is performed by this method before insertion.

## Exact wire order

The main window attaches handlers in this exact order:

```text
1. point.RemoveRequested += RemoveCurrentProfilePoint
2. point.PropertyChanged += OnCurrentProfilePointChanged
```

Required behavior:

- the order is not replaced by a set or unordered structure;
- one delegate registration is added per statement;
- existing earlier subscribers remain earlier in each individual event invocation list;
- no weak-event wrapper, disposable subscription object, automatic deduplication, or background dispatch is introduced.

## `RemoveCurrentProfilePoint(point)` exact order

Current behavior:

```text
1. point.RemoveRequested -= RemoveCurrentProfilePoint
2. point.PropertyChanged -= OnCurrentProfilePointChanged
3. CurrentProfilePoints.Remove(point)
4. UpdateCurrentProfileSummary()
```

Required behavior:

- unwire order remains `RemoveRequested`, then `PropertyChanged`;
- one matching delegate registration is removed per statement;
- the point is unwired before collection removal;
- `ObservableCollection.Remove(...)` uses normal collection equality behavior and removes the first matching entry;
- the returned boolean is ignored;
- when the point is absent, handlers are still detached and `UpdateCurrentProfileSummary()` still runs;
- with duplicate point references or duplicate registrations, one removal call removes one collection entry and one matching registration of each handler;
- synchronous collection observers run before the explicit main-summary update;
- if collection removal or an observer throws, the point remains unwired and the explicit summary update does not run;
- no automatic rewire, rollback, retry, or exception translation occurs.

## `ClearCurrentProfilePoints()` exact order

Current behavior:

```text
for each point in CurrentProfilePoints, in live collection order
    point.RemoveRequested -= RemoveCurrentProfilePoint
    point.PropertyChanged -= OnCurrentProfilePointChanged
CurrentProfilePoints.Clear()
```

`ClearCurrentProfilePoints()` does **not** call `UpdateCurrentProfileSummary()`.

Required behavior:

- the live collection is enumerated without snapshotting;
- each point is processed in current collection order;
- each entry removes one matching registration of each handler;
- all entries are unwired before `CurrentProfilePoints.Clear()` is called;
- `Clear()` raises its synchronous collection reset after unwiring;
- no point-specific remove command is invoked;
- no explicit main-summary update occurs inside this method;
- callers remain responsible for later additions and/or a final update;
- no rollback is performed if a synchronous collection observer throws during `Clear()`.

## `ResetCurrentProfile()` exact order

Current behavior:

```text
1. template = MainWindowDefaultProjectTemplateBuilder.Build()
2. ClearCurrentProfilePoints()
3. for each template.CurrentProfilePoints entry, in template order
       point = CreateDefaultCurrentProfilePoint(pointTemplate)
       AddCurrentProfilePoint(point)
4. UpdateCurrentProfileSummary()
```

Consequences:

- clear performs no main-summary update;
- every added default point performs its own main-summary update;
- one additional final main-summary update occurs after the loop;
- template order becomes collection order;
- point construction and all template-derived property setters run before wiring;
- default-value source resolution continues to read the current `CurrentSpeed`, `Depth`, and `WaterDensity` properties when `CreateDefaultCurrentProfilePoint(...)` is called;
- no publication is batched or suppressed.

## Reset callers

`ResetCurrentProfile()` is called by:

```text
ResetCurrentProfileCommand
ResetToDefaultProject()
FromDto(...) when the restored point list is empty
```

Required behavior:

- command execution remains synchronous;
- default-project reset retains its current surrounding clear/reset/property-assignment order;
- project restore uses reset only after it has cleared current points and found the restored point collection empty;
- no caller-specific shortcut bypasses the existing add helper;
- all repeated intermediate summary publications remain observable.

## Default-point construction timing

`CreateDefaultCurrentProfilePoint(...)` constructs a new point through an object initializer:

```text
DepthM
EastCurrentMS
NorthCurrentMS
VerticalCurrentMS
WaterDensityKgM3
```

Each value is resolved from the template in that assignment order.

Required behavior:

- constructor command creation occurs before property assignments;
- setters run before main-window wiring;
- point `PropertyChanged` events during the initializer do not reach the main window;
- current main-window source values are read at the same points in the workflow;
- no precomputed snapshot changes source timing;
- an exception before `AddCurrentProfilePoint(...)` leaves no inserted or main-window-wired point.

## Project-restore construction timing

`FromDto(...)` currently performs:

```text
ClearCurrentProfilePoints()
for each restored DTO row, in restored order
    point = CurrentProfilePointViewModel.FromDto(dto)
    AddCurrentProfilePoint(point)
if CurrentProfilePoints.Count == 0
    ResetCurrentProfile()
```

`CurrentProfilePointViewModel.FromDto(...)` assigns in this exact order:

```text
1. DepthM
2. EastCurrentMS
3. NorthCurrentMS
4. VerticalCurrentMS
5. WaterDensityKgM3
```

Required behavior:

- restored setters run before main-window wiring;
- every restored point is wired before insertion;
- every restored addition publishes its own main-summary update;
- restored order remains collection order;
- the empty-list fallback is evaluated after the restore loop;
- an empty restored list calls `ResetCurrentProfile()`, which performs its own clear, additions, and final update;
- restore exceptions continue to be handled only by the surrounding project-load workflow.

## Point `Summary` property

`Summary` is computed on demand by calling `ToInput()` and formatting the resolved numeric values.

Current text shape:

```text
z={DepthM} м · U={East} · V={North} · W={Vertical} · |U|={Speed} м/с · ρ={Density}
```

Required behavior:

- the getter continues to convert the point through `ToInput()` each time it is read;
- exact numeric formats remain unchanged;
- lifecycle extraction does not cache or precompute the summary;
- lifecycle extraction does not change point parsing or speed calculation.

## `RefreshSummary()` reentrancy behavior

`CurrentProfilePointViewModel.RefreshSummary()` currently performs:

```text
if _isRefreshingSummary
    return
try
    _isRefreshingSummary = true
    OnPropertyChanged(nameof(Summary))
finally
    _isRefreshingSummary = false
```

Required behavior:

- the reentrancy flag remains per point;
- the flag is set before publishing `PropertyChanged("Summary")`;
- the flag is reset in `finally`, including when a subscriber throws;
- a reentrant `RefreshSummary()` call while the flag is set returns without another event;
- the method publishes exactly one `Summary` property event when entered outside the guard;
- subscriber exceptions propagate after the flag is restored;
- no deferred, queued, coalesced, or background notification is introduced.

## `OnCurrentProfilePointChanged(...)` exact order

Every wired point property event reaches:

```text
if sender is CurrentProfilePointViewModel point
    point.RefreshSummary()
UpdateCurrentProfileSummary()
```

Required behavior:

- the handler does not filter by property name;
- row-summary refresh occurs before the handler's explicit main-summary update;
- a non-point sender skips row refresh but still calls `UpdateCurrentProfileSummary()`;
- lifecycle planning must not suppress, coalesce, delay, or reorder this handler.

## Nested `Summary` event and repeated main-summary publication

For a normal editable-property change on a wired point, the current synchronous flow is:

```text
editable property setter
  -> PropertyChanged(editable property)
     -> OnCurrentProfilePointChanged outer invocation
        -> RefreshSummary()
           -> PropertyChanged(Summary)
              -> OnCurrentProfilePointChanged nested invocation
                 -> RefreshSummary() returns because guard is set
                 -> UpdateCurrentProfileSummary()
        -> UpdateCurrentProfileSummary()
```

Therefore, under the normal main-window-only path, one changed editable property produces:

```text
1 row Summary notification
2 UpdateCurrentProfileSummary() calls
```

A direct external call to `RefreshSummary()` on a wired point produces one `Summary` event and one main-summary update from the nested main-window handler.

Required behavior:

- nested event delivery remains synchronous;
- the current two-update editable-property path is preserved;
- the reentrancy guard prevents an infinite Summary-event loop;
- external subscriber ordering can affect how far execution proceeds if a subscriber throws;
- no event-name filter, deduplication, batching, or publication suppression is added in the first extraction.

## Main-summary publication ownership

`UpdateCurrentProfileSummary()` remains owned by `MainWindowViewModel`.

The existing summary builder boundary remains unchanged:

```text
UseCurrentProfile
CurrentSpeed
resolved point inputs when required
  -> MainWindowCurrentProfileSummaryBuilder.Build(...)
  -> CurrentProfileSummary
```

Required behavior:

- lifecycle planning does not move summary-string construction into the lifecycle builder;
- disabled and enabled-empty behavior remains unchanged;
- populated-state point conversion and ordering remain unchanged;
- all lifecycle-triggered call locations and repetition remain unchanged.

## Observable collection and partial-state behavior

All point lifecycle and collection operations are synchronous.

Required behavior:

- collection observers can execute between mutation and explicit main-summary publication;
- event subscribers can execute inside commands, editable setters, and row-summary refresh;
- subscriber and observer exceptions propagate immediately;
- a collection mutation may already be complete when a later observer throws;
- a point may be wired but not inserted if add or an add observer throws;
- a point may be unwired before a failing remove;
- clear may leave all former points unwired even when a clear observer throws;
- reset or restore may leave a partially rebuilt collection and already published intermediate summaries;
- no transaction, rollback, retry, deferred dispatch, background execution, or exception translation is introduced.

## Preferred production boundary

A later production PR may add a pure internal builder such as:

```text
MainWindowCurrentProfilePointLifecyclePlanBuilder
```

It may return immutable values for:

```text
WireRoutes
UnwireRoutes
BuildNewPointDefaults(existingParsedDepths, currentSpeedText, waterDensityText)
```

A route enum may represent:

```text
RemoveRequested
PropertyChanged
```

A deterministic new-point defaults record may carry only:

```text
DepthM
EastCurrentMS
NorthCurrentMS
VerticalCurrentMS
WaterDensityKgM3
```

The builder must not:

- attach or detach handlers;
- call commands or events;
- construct or mutate `CurrentProfilePointViewModel` objects;
- enumerate or mutate `CurrentProfilePoints` directly;
- call `ToInput()`, `RefreshSummary()`, or `UpdateCurrentProfileSummary()`;
- read DTOs, storage, UI controls, or publish UI state.

`MainWindowViewModel` must retain actual subscriptions, collection operations, live snapshot timing, point construction, reset/restore orchestration, and display publication.

## First implementation invariants

```text
- one existing ObservableCollection instance
- one synchronous RemoveCommand event
- wire order: RemoveRequested, PropertyChanged
- unwire order: RemoveRequested, PropertyChanged
- wire before add
- unwire before remove
- update after add and remove, including absent remove
- no update inside clear
- reset: clear, add each with updates, final update
- restore: clear, add each with updates, empty fallback reset
- point setters before main-window wiring
- row RefreshSummary before outer main-summary update
- nested Summary event preserved
- two main-summary updates for a normal editable-property change
- no duplicate guard
- no rollback or batching
- output unchanged
```

## Explicit exclusions

This boundary does not authorize changes to:

- current-profile interpolation or engineering physics;
- solver selection or numerical formulas;
- point parsing, DTO fields, or JSON format;
- default project content;
- current-profile summary text or calculation-input ordering;
- reports, PDF, 2D, XAML, commands, or public API;
- collection replacement, asynchronous processing, or 3D.
