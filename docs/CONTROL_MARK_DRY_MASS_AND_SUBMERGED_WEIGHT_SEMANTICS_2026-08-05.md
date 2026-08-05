# Control mark: dry mass and submerged weight semantics

Date: 2026-08-05
Related issue: #168
Scope: documentation only

This record fixes the input and result semantics for mass, displaced volume, and apparent weight in water before clarifying the UI and technical report.

No production code, engineering formula, preset value, storage schema, JSON, DTO, parser, validation, calculation result, solver, selected-shape source, 2D, PDF diagram source, application version, or 3D behavior is changed by this document.

## Core conversion

For anchors, connectors, and payloads, the existing core uses:

```text
apparent mass-equivalent in water, kg
= mass in air, kg − water density, kg/m³ × displaced volume, m³
```

The implementation is:

```csharp
private static double WeightInWaterKg(
    double weightAirKg,
    double volumeM3,
    double waterDensityKgM3)
{
    return weightAirKg - volumeM3 * waterDensityKgM3;
}
```

The returned value is expressed in kilograms-force equivalent for the current engineering model. Multiplication by `g` is applied where a force in newtons is required.

## Buoy semantics and no-double-counting rule

Buoy input fields mean:

```text
BuoyInput.WeightKg = dry mass / mass in air
BuoyInput.VolumeM3 = displaced volume
```

The core calculates:

```text
BuoyancyKg = water density × buoy volume
buoy dry-mass contribution = BuoyInput.WeightKg
NetBuoyancyKg = BuoyancyKg − all weight contributions in water
```

The buoy is intentionally not converted by calling `WeightInWaterKg(buoy.WeightKg, buoy.VolumeM3, ...)`. Its displaced volume is already represented by the separate positive buoyancy term. Applying both would subtract the same buoyancy twice.

## Anchor semantics

Anchor input fields mean:

```text
AnchorInput.WeightAirKg = mass in air
AnchorInput.VolumeM3 = displaced volume
```

The current core calculates:

```text
AnchorWeightWaterKg
= AnchorInput.WeightAirKg
− water density × AnchorInput.VolumeM3

AnchorHoldingKg
= AnchorWeightWaterKg
× base holding coefficient
× anchor-type multiplier
× seabed multiplier
```

Therefore the holding calculation uses apparent weight in water, not the dry mass.

Example for a 500 kg concrete deadweight with an assumed displaced volume of 0.208 m³ in water of density 1025 kg/m³:

```text
500 − 1025 × 0.208 ≈ 286.8 kg in water
```

This example illustrates why the two values must not be confused. It does not change any built-in preset.

## Connector and payload semantics

Connector presets store:

```text
WeightAirKg
VolumeM3
```

Payload/instrument presets store:

```text
WeightAirKg
VolumeM3
```

The core converts each active connector and payload to apparent weight in water through the same `WeightInWaterKg(...)` function.

## Line semantics

Line presets are intentionally different. They store:

```text
WeightWaterKgM = linear weight already in water, kg/m
```

The line contribution is:

```text
line length × WeightWaterKgM
```

No second buoyancy subtraction is applied. Changing this field to dry linear mass would require a different data model including displaced volume or material/geometry policy and is outside issue #168.

## Current result semantics

These current result labels are already correct and must remain unchanged:

```text
Вес постановки в воде
Вес якоря в воде
Вес в воде (element table)
Чистая плавучесть
```

`ElementCalculationRow.WeightWaterKg` contains the calculated water-weight contribution for anchors, connectors, payloads, and lines. For the buoy row, a negative value represents its net upward contribution after buoyancy.

## Current UI ambiguity

The main window and element-library editors currently use generic labels such as:

```text
Масса, кг
Объём, м³
```

Those labels do not tell the user whether to enter dry mass or submerged weight. They are especially misleading for low-density or bulky anchors such as concrete deadweights.

The line editor already uses the correct explicit label:

```text
Вес в воде, кг/м
```

## Allowed production changes

Presentation-only changes may be made in:

```text
Views/MainWindow.axaml
Views/ElementLibraryWindow.axaml
ViewModels/AssemblyItemViewModel.cs
Services/TechnicalReportMarkdownBuilder.cs
```

Allowed changes:

```text
- replace ambiguous mass labels with “Масса на воздухе, кг”
- replace ambiguous volume labels with “Вытесняемый объём, м³”
- add one concise UI explanation of the input policy
- clarify line, connector, and payload editor hints
- change payload summary “вес” to “масса на воздухе”
- clarify buoy and anchor input labels in the technical report
```

## Exact terminology boundary

Input terminology:

```text
buoy, anchor, connector, payload:
Масса на воздухе, кг
Вытесняемый объём, м³

line:
Вес в воде, кг/м
```

Calculated-result terminology:

```text
Вес в воде, кг
Вес якоря в воде, кг
Вес постановки в воде, кг
```

## Required invariants

```text
- Models/EngineeringModels.cs unchanged
- WeightInWaterKg(...) unchanged
- buoyancy and net-buoyancy accounting unchanged
- no double subtraction of buoy displacement
- anchor holding formula unchanged
- connector, payload, and line weight calculations unchanged
- all preset numeric values unchanged
- JSON and DTO formats unchanged
- report numbers and table columns unchanged
- solver, selected shape, 2D, and PDF diagram behavior unchanged
- no version bump
- no 3D
```

## Deferred block

The report terminology cleanup for `Top T старая / shape` remains deferred until this higher-priority mass/weight ambiguity is closed.
