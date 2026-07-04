# Control mark: main-window calculation input boundary

Date: 2026-07-04
Scope: architecture stabilization / documentation only
Related issue: #70

This control mark records how editable main-window values are currently converted into the engineering inputs passed to `BuoyCalculator.Calculate(...)`.

No production code, solver physics, numerical formula, input fallback, UI, XAML, report, PDF, 2D output, project JSON, or public view-model API is changed by this document.

## Current calculation path

The calculation command reaches:

```text
MainWindowViewModel.Calculate()
```

The current preparation path is:

```text
CurrentProfilePoints
  -> CurrentProfilePointViewModel.ToInput()
  -> OrderBy(DepthM)
  -> currentProfile

WaterDensity / Depth / CurrentSpeed / WaveHeight / WavePeriod
  -> MainWindowViewModel.Parse(...)

SelectedSeabedPreset
  -> selected value
  -> fallback SeabedCatalog.ById("unknown")

current parsed values + seabed + profile flags/profile
  -> EnvironmentInput

BuoyName + parsed BuoyVolume / BuoyWeight / BuoyArea / BuoyCd
  -> BuoyInput

AnchorName / AnchorType / AnchorMaterial
+ parsed AnchorWeight / AnchorVolume / AnchorCoefficient
  -> AnchorInput

AssemblyItems
  -> AssemblyItemViewModel.ToInput()
  -> items

SafetyFactor
  -> MainWindowViewModel.Parse(...)

EnvironmentInput + BuoyInput + items + AnchorInput + safety factor
  -> BuoyCalculator.Calculate(...)
```

The returned `CalculationResult` then enters the existing main-window calculation display boundary introduced by PR #69.

## Main-window numeric parsing

`MainWindowViewModel.Parse(string value)` currently performs exactly these operations:

```text
1. null becomes an empty string
2. every comma is replaced with a dot
3. parsing uses NumberStyles.Any
4. parsing uses CultureInfo.InvariantCulture
5. unsuccessful parsing returns 0
```

This parser is currently used for:

```text
WaterDensity
Depth
CurrentSpeed
WaveHeight
WavePeriod
BuoyVolume
BuoyWeight
BuoyArea
BuoyCd
AnchorWeight
AnchorVolume
AnchorCoefficient
SafetyFactor
```

A later extraction must preserve the same accepted input forms and the same zero fallback. It must not introduce validation errors, exceptions, clamping, defaults, or locale-dependent behavior in the same PR.

## Environment input sources

The current `EnvironmentInput` is constructed from:

```text
WaterDensityKgM3
  <- Parse(WaterDensity)

DepthM
  <- Parse(Depth)

CurrentSpeedMS
  <- Parse(CurrentSpeed)

WaveHeightM
  <- Parse(WaveHeight)

WavePeriodS
  <- Parse(WavePeriod)

Seabed
  <- SelectedSeabedPreset
  <- SeabedCatalog.ById("unknown") when selection is null

UseCurrentProfile
  <- current main-window boolean

CurrentProfile
  <- every CurrentProfilePointViewModel.ToInput()
  <- sorted ascending by DepthM
```

The extraction must not reinterpret profile points or replace the selected-seabed fallback.

## Current-profile conversion ownership

Each profile row owns its current conversion through:

```text
CurrentProfilePointViewModel.ToInput()
```

The row converts these strings:

```text
DepthM
EastCurrentMS
NorthCurrentMS
VerticalCurrentMS
WaterDensityKgM3
```

Its private parser has the same essential behavior as the main-window parser:

```text
- comma replaced with dot
- NumberStyles.Any
- invariant culture
- invalid input becomes 0
```

The first implementation of the calculation input boundary should continue calling `CurrentProfilePointViewModel.ToInput()` rather than duplicating or relocating row conversion.

The current profile list is sorted only after conversion:

```text
CurrentProfilePoints.Select(ToInput).OrderBy(DepthM).ToList()
```

This order must be preserved.

## Buoy input sources

The current `BuoyInput` is constructed from:

```text
Name
  <- BuoyName, unchanged

VolumeM3
  <- Parse(BuoyVolume)

WeightKg
  <- Parse(BuoyWeight)

ProjectedAreaM2
  <- Parse(BuoyArea)

DragCoefficient
  <- Parse(BuoyCd)
```

The first extraction must not trim, normalize, substitute, validate, or otherwise modify `BuoyName`.

## Anchor input sources

The current `AnchorInput` is constructed from:

```text
Name
  <- AnchorName, unchanged

Type
  <- AnchorType, unchanged

Material
  <- AnchorMaterial, unchanged

WeightAirKg
  <- Parse(AnchorWeight)

VolumeM3
  <- Parse(AnchorVolume)

BaseHoldingCoefficient
  <- Parse(AnchorCoefficient)
```

The first extraction must not modify the anchor text fields or introduce material/type mappings.

## Assembly-item conversion ownership

The current assembly list is prepared through:

```text
AssemblyItems.Select(x => x.ToInput()).ToList()
```

Each `AssemblyItemViewModel.ToInput()` currently owns:

```text
- kind parsing
- connector count forced to 1
- rope lookup for line items
- connector lookup for connector items
- numeric parsing of length and payload values
- invalid numeric fallback to zero
- propagation of title and enabled state
```

The first implementation of the calculation input boundary must continue calling `AssemblyItemViewModel.ToInput()`.

It must not move library lookup, item-kind parsing, connector-count policy, or item numeric parsing into the new boundary.

All assembly items are converted, including disabled items. The solver itself selects enabled items. This must remain unchanged.

## Safety factor

The current safety factor is prepared as:

```text
Parse(SafetyFactor)
```

It uses the same main-window parser and therefore the same zero fallback.

The first extraction must not add a minimum, maximum, default, or validation rule.

## Current solver call

The exact engineering call remains:

```text
BuoyCalculator.Calculate(
    environment,
    buoy,
    items,
    anchor,
    safetyFactor)
```

The new boundary must only prepare arguments for this existing call. It must not wrap, replace, subclass, or change `BuoyCalculator`.

## Preferred implementation boundary

A later production-code PR may introduce a small internal immutable command/input model and builder, for example:

```text
MainWindowCalculationInput
MainWindowCalculationInputBuilder
```

Equivalent naming is acceptable.

A suitable internal model would carry:

```text
EnvironmentInput Environment
BuoyInput Buoy
AnchorInput Anchor
IReadOnlyList<AssemblyItemInput> AssemblyItems
double SafetyFactor
```

The builder should receive already identified main-window values and existing row objects, perform only the current conversions, and return the prepared object.

Target direction:

```text
editable main-window values and row collections
  -> MainWindowCalculationInputBuilder.Build(...)
  -> MainWindowCalculationInput
  -> BuoyCalculator.Calculate(
       input.Environment,
       input.Buoy,
       input.AssemblyItems,
       input.Anchor,
       input.SafetyFactor)
```

The view model remains responsible for command orchestration and for passing the resulting `CalculationResult` into the existing display boundary.

## First implementation invariants

The first implementation PR must preserve:

```text
- same source properties
- same string values passed without normalization for names/type/material
- same comma-to-dot replacement
- same NumberStyles.Any parsing
- same invariant culture
- same invalid/blank/null numeric fallback to zero
- same selected seabed and unknown fallback
- same CurrentProfilePointViewModel.ToInput() calls
- same ascending profile sort after conversion
- same AssemblyItemViewModel.ToInput() calls
- same conversion of disabled assembly items
- same connector count and library-resolution behavior
- same safety-factor value
- same BuoyCalculator.Calculate(...) call
- same CalculationResult
- same display builder inputs and output
- same public MainWindowViewModel properties
- same UI, report, PDF, and 2D behavior
```

## Non-goals

Do not change in the first implementation PR:

```text
- solver physics
- numerical formulas
- CalculationResult
- validation policy
- parse fallback behavior
- locale behavior
- library lookup behavior
- assembly item conversion behavior
- profile row conversion behavior
- profile interpolation or segmentation
- selected seabed behavior
- report builders
- PDF or 2D boundaries
- XAML or UI layout
- project persistence format
- public view-model API
- 3D functionality
```

## Safety gate

Every follow-up PR must explicitly state whether output changes.

Every follow-up PR must keep `BuoyCalc Windows Build` green.

No solver physics changes are allowed in this architecture-stabilization phase.
