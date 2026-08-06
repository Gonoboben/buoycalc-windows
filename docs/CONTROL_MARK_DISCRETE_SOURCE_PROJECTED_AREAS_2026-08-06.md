# Control mark: discrete source projected-area boundary

Date: 2026-08-06

Issue: #326

## Purpose

Record a behavior-preserving boundary that allows engineering diagnostics to distinguish:

- the projected area of one source element;
- the projected area published by the calculated element row after multiplicity is applied.

This control mark does not change drag, force, solver, shape, report coordinates or any project verdict.

## Current calculation semantics

`ElementCalculationRow.ProjectedAreaM2` is the area used by the current drag calculation.

For discrete elements it is populated as follows:

```text
buoy       = BuoyInput.ProjectedAreaM2
connector  = normalized Count × ConnectorPreset.ProjectedAreaM2
payload    = AssemblyItemInput.PayloadProjectedAreaM2
anchor     = 0
```

The connector value is therefore a row total, while the buoy and payload values are single-element values because their published `Count` is one.

Distributed line rows use a different identity:

```text
ProjectedAreaM2 = calculated line length × RopePreset.DiameterMm / 1000
```

Line rows and line segments are outside this control mark. Their source length, source diameter and segment/element conservation are already separate diagnostic boundaries.

## Observability gap

The calculated read model currently publishes only the resulting `ProjectedAreaM2`.

It does not publish the source projected area of one connector, buoy or payload independently from multiplicity.

Consequently, downstream diagnostics cannot determine whether a connector row correctly preserves:

```text
ProjectedAreaM2 = Count × source unit projected area
```

A future constructor or routing regression could alter either value while leaving a superficially plausible area and force.

## Result boundary

Add one provenance field to `ElementCalculationRow`:

```text
SourceUnitProjectedAreaM2
```

Required population:

```text
buoy       => BuoyInput.ProjectedAreaM2
connector  => ConnectorPreset.ProjectedAreaM2
payload    => AssemblyItemInput.PayloadProjectedAreaM2
anchor     => 0
line       => 0
```

The field is observational only.

It must not be used to replace or recalculate the existing `ProjectedAreaM2` value.

For connectors it remains a per-unit source value. Existing `Count` remains the calculated row multiplicity.

## Diagnostic boundary

After the result boundary is merged, add two rows to `EngineeringDiagnostics`.

### 1. Source-area physical sign

Check name:

```text
Неотрицательные исходные площади дискретных элементов
```

Scope:

```text
ElementRows where Kind != "Линия"
```

Invalid source area:

```text
!double.IsFinite(SourceUnitProjectedAreaM2)
|| SourceUnitProjectedAreaM2 < 0
```

Zero is allowed for:

- the anchor row;
- idealized point elements;
- elements intentionally omitted from drag.

Severity:

```text
OK    when invalid count = 0
ERROR when invalid count > 0
```

The row should display minimum source area, invalid count and inspected row count.

### 2. Published area identity

Check name:

```text
Согласованность исходной и расчётной площади дискретных элементов
```

For each non-line element row calculate locally inside diagnostics:

```text
expected = Count × SourceUnitProjectedAreaM2
absoluteResidual = abs(ProjectedAreaM2 - expected)
relativeResidual = absoluteResidual / max(1, abs(expected))
```

The diagnostic must use the already published `Count`.

It must not repeat the calculation-layer normalization of connector count.

A row is invalid when:

```text
Count < 0
or any used value/residual is non-finite
or relativeResidual > 1e-6
```

Severity:

```text
OK      when inspected row count > 0 and invalid count = 0
ERROR   when invalid count > 0
WARNING when inspected row count = 0
```

The displayed value should include:

```text
maximum absolute residual
maximum relative residual
invalid count
inspected row count
```

## Required implementation sequence

1. Merge this documentation-only control mark.
2. Add `SourceUnitProjectedAreaM2` in a result-boundary PR limited to `Models/EngineeringModels.cs`.
3. Add the two diagnostic rows in a PR limited to `Services/EngineeringDiagnostics.cs`.
4. Merge each PR only after all required checks are green.

Required checks:

- `.NET Build`
- `Selected Shape Consumer Scan`
- `Report Store Consumer Scan`

## Preserved behavior

The implementation must not change:

- source input values;
- calculated `ProjectedAreaM2` values;
- connector count normalization or scaling;
- any `DragForce` invocation;
- current or wave forces;
- water density handling;
- mass, displaced volume or weight-in-water calculations;
- buoyancy;
- tension, weak-link, WLL or reserve calculations;
- anchor holding calculations;
- segment generation or the fixed `0.20 m` target step;
- unlimited segment-count policy;
- shape or solver behavior;
- selected-shape stores;
- 2D or PDF coordinates;
- `CalculationResult.Verdict`;
- `CalculationResult.MainRisk`;
- `CalculationResult.Checks`;
- project JSON/DTO contracts;
- XAML, commands or application version;
- the project prohibition on 3D.

Only the existing diagnostics table may show new rows after the final diagnostic PR.
