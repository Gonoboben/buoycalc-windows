# Control mark: main-window assembly item lifecycle boundary

Date: 2026-07-06
Scope: architecture stabilization / documentation only
Related issue: #115

This control mark records the current lifecycle behavior for `AssemblyItemViewModel` instances owned by `MainWindowViewModel.AssemblyItems`.

This document changes no production code, collection ownership, event wiring, commands, storage, JSON, default templates, project restore, solver physics, formulas, calculation inputs, reports, PDF, 2D, XAML, public API, or user-facing output.

## Owned collection

`MainWindowViewModel` creates one collection instance:

```text
AssemblyItems = new ObservableCollection<AssemblyItemViewModel>()
```

Required behavior:

- the collection instance is retained for the lifetime of the ViewModel;
- add, insert, move, remove, and clear mutate that existing instance;
- no lifecycle extraction may replace the collection;
- collection notifications remain the synchronous notifications produced by `ObservableCollection<T>`;
- the ViewModel does not subscribe to `AssemblyItems.CollectionChanged` itself;
- sequence/diagram/visualization publication is triggered explicitly by lifecycle methods and by item `PropertyChanged` subscriptions.

## Item-owned commands and events

Every `AssemblyItemViewModel` constructor creates four always-enabled `RelayCommand` objects:

```text
RemoveCommand    -> RemoveRequested?.Invoke(this)
MoveUpCommand    -> MoveUpRequested?.Invoke(this)
MoveDownCommand  -> MoveDownRequested?.Invoke(this)
DuplicateCommand -> DuplicateRequested?.Invoke(this)
```

The item exposes:

```text
RemoveRequested
MoveUpRequested
MoveDownRequested
DuplicateRequested
PropertyChanged
```

Required behavior:

- `RelayCommand.CanExecute(...)` remains `true` because no predicate is supplied;
- command execution invokes the event synchronously on the current thread;
- subscriber invocation-list order remains normal .NET event order;
- exceptions from subscribers propagate through the command call;
- commands do not directly mutate `AssemblyItems`;
- an unwired item command has no main-window effect.

## Add commands and construction timing

The main window creates new line, connector, and payload items through object initializers and then calls `AddAssemblyItem(...)`.

Current order:

```text
1. new AssemblyItemViewModel()
2. object-initializer property assignments
3. AddAssemblyItem(item)
```

Consequences:

- constructor commands exist before any properties are assigned;
- property setters and storage-backed preset resolution can run before main-window handlers are attached;
- item `PropertyChanged` events raised during the initializer do not reach `OnAssemblyItemChanged(...)`;
- an exception during construction or an initializer prevents `AddAssemblyItem(...)` from running;
- no partially constructed item is inserted by the main window.

The extraction must not move main-window wiring earlier than the current object-initializer work.

## `AddAssemblyItem(...)` exact order

Current order:

```text
1. evaluate item.IsConnector
2. if connector, assign item.Count = "1"
3. WireItem(item)
4. AssemblyItems.Add(item)
5. UpdateSequenceSummary()
```

Required behavior:

- connector count correction occurs before main-window wiring;
- if `Count` changes at this step, existing non-main-window subscribers can receive its `Count` and `Summary` events;
- the new main-window `PropertyChanged` handler does not receive those pre-wire events;
- all five main-window handlers are attached before the collection add;
- `ObservableCollection.Add(...)` notifications occur before the explicit summary update;
- collection observers can synchronously run while the item is already wired;
- the explicit summary update occurs only after `Add(...)` returns successfully;
- there is no duplicate-item or duplicate-subscription guard;
- there is no rollback if wiring succeeds and collection insertion later throws.

## Exact wire order

`WireItem(item)` currently attaches handlers in this exact order:

```text
1. item.RemoveRequested    += RemoveItem
2. item.MoveUpRequested    += MoveItemUp
3. item.MoveDownRequested  += MoveItemDown
4. item.DuplicateRequested += DuplicateItem
5. item.PropertyChanged    += OnAssemblyItemChanged
```

Required behavior:

- the order is not replaced by a set or unordered structure;
- repeated calls attach repeated delegate registrations;
- existing earlier external subscribers remain earlier in each individual event invocation list;
- no weak-event wrapper, disposable subscription object, or automatic deduplication is introduced in the first extraction.

## Item property-change handler

Every wired item property event reaches:

```text
if sender is a connector
    connector.Count = "1"
UpdateSequenceSummary()
```

Required behavior:

- the handler does not filter by property name;
- connector count correction remains synchronous and can publish nested events;
- the explicit lifecycle updates described below remain in addition to property-driven updates;
- lifecycle planning must not suppress, coalesce, or delay this handler.

## `ClearAssemblyItems()` exact order

Current behavior:

```text
for each item in AssemblyItems, in live collection order
    detach five handlers in the current unwire order
AssemblyItems.Clear()
```

`ClearAssemblyItems()` does **not** call `UpdateSequenceSummary()`.

Required behavior:

- the live collection is enumerated without snapshotting;
- all items are unwired before `AssemblyItems.Clear()` is called;
- `Clear()` raises its synchronous collection reset after unwiring;
- no item-specific remove command is raised;
- no explicit summary update occurs inside this method;
- callers remain responsible for later additions and/or final updates;
- no rollback is performed if a synchronous collection observer throws during `Clear()`.

## Exact unwire order

Both clear and remove detach handlers in this exact order:

```text
1. item.PropertyChanged    -= OnAssemblyItemChanged
2. item.RemoveRequested    -= RemoveItem
3. item.MoveUpRequested    -= MoveItemUp
4. item.MoveDownRequested  -= MoveItemDown
5. item.DuplicateRequested -= DuplicateItem
```

This order is not the simple reverse of the wire order and must be preserved.

Required behavior:

- one matching delegate registration is removed per statement;
- no disposal abstraction changes event-removal timing;
- property changes raised after the first detach no longer reach the main-window property handler even while command handlers are still being detached;
- no exception translation or cleanup retry is introduced.

## `RemoveItem(...)` exact order

Current behavior:

```text
1. detach PropertyChanged
2. detach RemoveRequested
3. detach MoveUpRequested
4. detach MoveDownRequested
5. detach DuplicateRequested
6. AssemblyItems.Remove(item)
7. UpdateSequenceSummary()
```

Required behavior:

- the item is fully unwired before collection removal;
- `ObservableCollection.Remove(...)` uses the collection's normal equality behavior and removes the first matching entry;
- the returned boolean is ignored;
- when the item is absent, handlers are still detached and `UpdateSequenceSummary()` still runs;
- synchronous collection observers run before the explicit summary update;
- if collection removal or a collection observer throws, the item remains unwired and the explicit summary update does not run;
- no automatic rewire or rollback occurs.

## `MoveItemUp(...)` decision and order

Current behavior:

```text
index = AssemblyItems.IndexOf(item)
if index <= 0
    return
AssemblyItems.Move(index, index - 1)
UpdateSequenceSummary()
```

Required behavior:

- `IndexOf(...)` is evaluated once before the guard;
- an absent item produces `-1` and returns;
- the first item at index `0` returns;
- no-op branches perform no collection mutation and no summary update;
- a valid move targets exactly `index - 1`;
- item wiring is unchanged;
- synchronous collection move notifications occur before the explicit summary update;
- if a collection observer throws, the move can already be applied while the summary update is skipped.

## `MoveItemDown(...)` decision and order

Current behavior:

```text
index = AssemblyItems.IndexOf(item)
if index < 0 or index >= AssemblyItems.Count - 1
    return
AssemblyItems.Move(index, index + 1)
UpdateSequenceSummary()
```

Required behavior:

- the absent-item and last-item branches are no-ops;
- no-op branches perform no summary update;
- a valid move targets exactly `index + 1`;
- the current collection count is used by the guard;
- item wiring remains unchanged;
- collection notifications precede the explicit summary update;
- no selection or focus state is added.

## `DuplicateItem(...)` exact order

Current behavior:

```text
1. index = AssemblyItems.IndexOf(item)
2. copy = item.Clone()
3. WireItem(copy)
4. if index < 0 or index >= AssemblyItems.Count - 1
       AssemblyItems.Add(copy)
   else
       AssemblyItems.Insert(index + 1, copy)
5. UpdateSequenceSummary()
```

Required behavior:

- source index is captured before cloning;
- cloning completes before any main-window handler is attached to the copy;
- the copy is wired before collection insertion;
- an absent source index appends the copy;
- a source that was last at the placement decision appends the copy;
- otherwise the copy is inserted immediately after the captured index;
- collection observers run while the copy is already wired;
- the explicit summary update occurs after successful insertion;
- if cloning throws, no wiring or collection mutation occurs;
- if insertion throws after wiring, no rollback detaches the orphaned copy;
- the source object's event subscribers are not copied.

## Clone assignment order

`AssemblyItemViewModel.Clone()` creates a new item and assigns properties in this exact order:

```text
1. IsEnabled
2. Kind
3. Title = source Title + " копия"
4. RopePresetStorageId
5. ConnectorPresetStorageId
6. PayloadPresetStorageId
7. LengthM
8. Count = "1" for a connector, otherwise source Count
9. PayloadWeightAirKg
10. PayloadVolumeM3
11. PayloadProjectedAreaM2
12. PayloadDragCoefficient
```

Required behavior:

- a fresh item owns fresh command objects and empty event subscriber lists;
- setter side effects occur in the listed order before main-window wiring;
- storage-backed preset resolution and payload application remain possible during cloning;
- the title suffix is exact and is appended without trimming or duplicate-suffix detection;
- connector copies always receive count text `"1"`;
- later payload field assignments continue to overwrite values that may have been applied by the payload preset setter;
- no shallow copy of commands, delegates, or backing fields replaces the setter-driven clone.

## Reset-to-default caller

`ResetToDefaultProject()` currently performs:

```text
ClearAssemblyItems()
for each default template in template order
    CreateDefaultAssemblyItem(template)
    AddAssemblyItem(item)
UpdateSequenceSummary()
```

Consequences:

- clear performs no summary update;
- every added default item performs its own summary update;
- one additional final summary update occurs after all defaults;
- default-item property assignment happens before wiring;
- template order becomes collection order.

The lifecycle extraction must not reduce or reorder these updates.

## Project-restore caller

`FromDto(...)` currently performs:

```text
ClearAssemblyItems()
for each restored row in restored order
    construct AssemblyItemViewModel through ordered object initializer
    AddAssemblyItem(item)
UpdateSequenceSummary()
UpdateCurrentProfileSummary()
```

Required behavior:

- restored object-initializer setters run before wiring;
- connector restored count is forced to `"1"` before `AddAssemblyItem(...)` and checked again there;
- each addition publishes its own sequence update;
- one additional final sequence update occurs;
- restore order remains collection order;
- exceptions continue to be handled only by the surrounding project-load workflow.

## Observable collection and partial-state behavior

All collection mutations are synchronous.

Required behavior:

- collection observers can execute between mutation and explicit summary update;
- observer exceptions propagate immediately;
- a mutation may already be complete when a later observer throws;
- an item may be wired but not inserted if add/insert fails;
- an item may be unwired before a failing remove;
- a clear may leave all former items unwired even when a reset observer throws;
- no transaction, rollback, retry, deferred dispatch, or background execution is introduced.

## Preferred production boundary

A later production PR may add a pure internal builder such as:

```text
MainWindowAssemblyItemLifecyclePlanBuilder
```

It may return immutable values for:

```text
WireRoutes
UnwireRoutes
ResolveMoveUpTarget(currentIndex)
ResolveMoveDownTarget(currentIndex, count)
ResolveDuplicateInsertionIndex(currentIndex, count)
```

A route enum may represent:

```text
RemoveRequested
MoveUpRequested
MoveDownRequested
DuplicateRequested
PropertyChanged
```

The builder must not:

- attach or detach handlers;
- call commands or events;
- clone items;
- mutate `AssemblyItems`;
- call `UpdateSequenceSummary()`;
- read storage or publish UI state.

`MainWindowViewModel` must retain actual subscriptions, collection operations, cloning, and display publication.

## First implementation invariants

```text
- one persistent ObservableCollection instance
- exact pre-wire connector count correction
- exact wire and unwire route order
- no duplicate-subscription guard
- synchronous command and collection events
- clear without an internal summary update
- remove update even when the item is absent
- exact move guards and target indices
- no update for invalid moves
- duplicate index capture before clone
- clone before wire, wire before insertion
- exact duplicate append/insert decision
- exact clone setter order, title suffix, and connector count
- existing per-add and final caller updates
- current exception propagation and partial states
```

## Non-goals

```text
- no collection replacement or snapshotting
- no event deduplication, weak events, or disposal framework
- no command CanExecute changes
- no clone data or setter changes
- no default-template or project-restore changes
- no summary, diagram, or visualization changes
- no solver, physics, formulas, calculation-input, report, PDF, 2D, XAML, or public API changes
- no user-facing string changes
- no 3D
```

## Safety gate

Every follow-up PR must explicitly state whether output changes and keep `BuoyCalc Windows Build` green.

The first implementation is architecture-only and must be **Output unchanged**.
