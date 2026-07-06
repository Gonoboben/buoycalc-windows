# Control mark: assembly library option notification boundary

Date: 2026-07-06
Scope: architecture stabilization / documentation only
Related issue: #110

This control mark records the current behavior of `AssemblyItemViewModel.RefreshLibraryOptions()` and its orchestration through `MainWindowViewModel.RefreshSequenceLibraryOptions()`.

This document changes no production code, storage, JSON, collections, event wiring, bindings, solver physics, formulas, calculation inputs, reports, PDF, 2D, XAML, public API, or user-facing output.

## Current top-level orchestration

`MainWindowViewModel.RefreshSequenceLibraryOptions()` currently performs:

```text
for each item in AssemblyItems, in current collection order
    item.RefreshLibraryOptions()

UpdateSequenceSummary()
```

Required behavior:

- `AssemblyItems` is enumerated directly through its live `ObservableCollection` enumerator;
- the collection is not copied, sorted, filtered, or replaced;
- one item completes all of its notifications before the next item begins;
- one additional `UpdateSequenceSummary()` runs after the loop;
- when the collection is empty, the final update still runs once;
- if enumeration or an event handler throws, remaining items and the final update do not run.

## Exact notification plan

`AssemblyItemViewModel.RefreshLibraryOptions()` currently calls `OnPropertyChanged(...)` exactly eight times and in this order:

```text
1. RopePresetOptions
2. RopePresetId
3. ConnectorPresetOptions
4. ConnectorPresetId
5. PayloadPresetOptions
6. PayloadPresetId
7. EditorHint
8. Summary
```

Required behavior:

- every name is published exactly once by the outer refresh call;
- no name is skipped based on item kind or enabled state;
- no duplicate elimination is introduced;
- no wildcard or empty property name replaces the eight explicit names;
- all eight calls remain synchronous;
- the next name is not published until all subscribers for the current name have returned;
- `OnPropertyChanged(propertyName)` continues to create a new `PropertyChangedEventArgs` for each call.

## Notification plan boundary

A later pure builder may return only the ordered property-name plan, for example:

```text
AssemblyItemLibraryOptionNotificationPlanBuilder.Build()
    -> IReadOnlyList<string>
```

The builder may own the immutable sequence of eight names.

It must not:

- call `OnPropertyChanged(...)`;
- inspect item kind, enabled state, IDs, or titles;
- read library storage;
- compute option lists or display names;
- invoke main-window updates;
- snapshot `AssemblyItems`;
- publish events or mutate state.

`AssemblyItemViewModel` must remain responsible for one `OnPropertyChanged(...)` call per plan entry.

## Synchronous subscriber behavior

`ViewModelBase.OnPropertyChanged(...)` invokes the current `PropertyChanged` delegate synchronously:

```text
PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName))
```

Required behavior:

- subscribers run on the caller's current thread;
- invocation-list order remains unchanged;
- a subscriber may read the notified property before the next notification;
- a subscriber may publish nested events before the outer event returns;
- a subscriber exception propagates immediately;
- after an exception, later subscribers, later plan entries, later items, and the final summary update follow current .NET delegate behavior and are not recovered by this workflow.

No try/catch, retry, deferred dispatch, queue, throttle, debounce, or batch notification may be added in the first extraction.

## Main-window subscriber

Every assembly item added through `AddAssemblyItem(...)` or restored through the existing project workflow is wired as:

```text
item.PropertyChanged += OnAssemblyItemChanged
```

For each received event, `OnAssemblyItemChanged(...)` currently performs:

```text
if sender is a connector
    connector.Count = "1"

UpdateSequenceSummary()
```

The handler does not filter by `PropertyChangedEventArgs.PropertyName`.

Therefore every one of the eight outer notifications normally causes one `UpdateSequenceSummary()` call through this subscriber.

## Connector count reentrancy

When the item is a connector and `Count` is already exactly `"1"`:

```text
outer property event
  -> connector.Count = "1"
  -> SetProperty sees equality and emits nothing
  -> one UpdateSequenceSummary()
```

When the item is a connector and `Count` is not exactly `"1"`, the first outer notification that reaches the handler causes this synchronous nested sequence:

```text
outer property event
  -> connector.Count = "1"
     -> SetProperty changes the field
     -> nested PropertyChanged("Count")
        -> OnAssemblyItemChanged
        -> Count assignment is equality-suppressed
        -> nested UpdateSequenceSummary()
     -> nested PropertyChanged("Summary")
        -> OnAssemblyItemChanged
        -> Count assignment is equality-suppressed
        -> nested UpdateSequenceSummary()
  -> outer UpdateSequenceSummary()
```

Consequences:

- the first applicable outer event can cause three `UpdateSequenceSummary()` calls;
- it also publishes nested `Count` and `Summary` events before the original subscriber returns;
- after correction, the remaining seven outer notifications normally cause one update each;
- the final top-level update still occurs after all items;
- no attempt may be made to pre-correct connector count, suppress nested events, or collapse these updates in the first extraction.

## Work triggered by notified getters

The notification method itself does not evaluate property values. Subscribers and bindings may evaluate them synchronously after each event.

Current getters can trigger storage work:

```text
RopePresetOptions       -> RopeLibraryStorage.LoadAllRopes()
ConnectorPresetOptions  -> ConnectorLibraryStorage.LoadAllConnectors()
PayloadPresetOptions    -> PayloadLibraryStorage.LoadAllPayloads()
RopePresetId            -> rope display-name lookup and rope load
ConnectorPresetId       -> connector display-name lookup and connector load
PayloadPresetId         -> payload display-name lookup and payload load
Summary                 -> kind-dependent display-name lookup and library load
EditorHint              -> no library load
```

Required behavior:

- values are not precomputed before publication;
- storage results are not cached by the notification-plan builder;
- notification order continues to determine the order in which bindings may request these values;
- storage exceptions raised during subscriber getter evaluation continue to interrupt the current notification chain.

## Repeated main-window display updates

Each `UpdateSequenceSummary()` currently performs:

```text
1. convert enabled AssemblyItems to calculation inputs
2. build SequenceSummary
3. clear and rebuild SequenceDiagramLines
4. rebuild visualization summary
```

This can itself call rope, connector, or payload storage through `ToInput()` and display-name access.

Required behavior:

- every event-driven update remains synchronous;
- update order is unchanged;
- `CalculationResult` remains absent for these calls;
- no memoization, coalescing, background dispatch, or delayed final update is introduced;
- existing public property setters and collection instances remain in use.

## Per-item and multi-item ordering

For two items `A` then `B`, the outer order remains:

```text
A.RopePresetOptions
A.RopePresetId
A.ConnectorPresetOptions
A.ConnectorPresetId
A.PayloadPresetOptions
A.PayloadPresetId
A.EditorHint
A.Summary
B.RopePresetOptions
...
B.Summary
final UpdateSequenceSummary()
```

Nested connector events may occur inside the handling of any outer event, but the next outer plan entry is not reached until the nested work returns.

The first extraction must not change this to property-by-property processing across all items.

## Collection mutation and partial completion

Because `AssemblyItems` is enumerated live:

- external synchronous event handling that mutates the collection can invalidate enumeration;
- an exception can leave some items fully refreshed, one item partially refreshed, and later items untouched;
- the final summary update may be skipped;
- no rollback or restart occurs.

The first extraction must not snapshot the collection as a way to avoid this behavior.

## Current callers

`RefreshSequenceLibraryOptions()` is reached through `RefreshLibraries()`, which is currently used by:

```text
- MainWindowViewModel constructor
- RefreshBuoyLibraryCommand
- SaveCurrentBuoyToLibrary() after storage upsert
- DeleteSelectedBuoyPreset() after every allowed delete attempt
- FromDto(...) during project restore
```

The notification-plan extraction must not change any caller or move this refresh relative to buoy/anchor loading, storage operations, status publication, or project restoration.

## Preferred production shape

A later production PR may add an internal type such as:

```text
AssemblyItemLibraryOptionNotificationPlanBuilder
```

A suitable first implementation is:

```text
Build()
  -> immutable ordered list of the exact eight property names
```

A later live hookup may remain structurally equivalent to:

```text
foreach propertyName in builder.Build()
    OnPropertyChanged(propertyName)
```

The outer main-window loop remains unchanged.

## First implementation invariants

```text
- exact eight names and exact order
- one synchronous OnPropertyChanged call per name
- fresh PropertyChangedEventArgs per call
- no value precomputation
- all subscribers complete before the next name
- all eight outer events for one item before the next item
- live AssemblyItems enumeration
- main-window handler receives every event
- exact connector Count correction and nested Count/Summary events
- repeated UpdateSequenceSummary calls
- one final UpdateSequenceSummary after the loop
- empty-collection final update
- current exception propagation and partial completion
- existing caller order and collection ownership
```

## Non-goals

```text
- no storage or JSON changes
- no batching, throttling, deduplication, or temporary unsubscription
- no collection snapshot or replacement
- no connector count-policy changes
- no summary, diagram, or visualization changes
- no solver, physics, formulas, calculation-input, report, PDF, 2D, XAML, or public API changes
- no user-facing string changes
- no 3D
```

## Safety gate

Every follow-up PR must explicitly state whether output changes and keep `BuoyCalc Windows Build` green.

The first implementation is architecture-only and must be **Output unchanged**.
