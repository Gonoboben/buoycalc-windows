# Control mark: current-profile guidance accuracy

Date: 2026-07-07
Related issue: #156
Scope: documentation only

This record compares current-profile user guidance with the current calculation core before correcting stale UI text.

No production code, XAML, profile data, parsing, sorting, interpolation, segmentation, forces, density policy, calculation result, solver, report, PDF, 2D, project JSON, DTO, command, binding, layout, version, or engineering physics is changed by this document.

## Stale user guidance

`Views/CurrentProfileWindow.axaml` currently says:

```text
Задайте компоненты течения U/V/W и плотность воды по слоям. В v0.19 профиль уже сохраняется и участвует как переходная оценка скорости; послойная интеграция сил будет следующим этапом.
```

The populated branch of `MainWindowCurrentProfileSummaryBuilder.Build(...)` currently ends with:

```text
В v0.19 расчёт использует эту max-скорость как переходную оценку.
```

Both statements describe the original v0.19 policy, not the current line-force calculation.

## Verified persistence behavior

`MainWindowViewModel.ToDto()` includes:

```text
UseCurrentProfile
CurrentProfilePoints
```

`FromDto()` restores the flag and reconstructs profile rows. An empty restored list falls back to the existing default profile template.

Therefore the statement that the profile is saved remains correct, but the historical version reference is no longer useful.

## Verified input boundary

`MainWindowCalculationInputBuilder`:

```text
- converts every profile row through ToInput()
- orders snapshots by DepthM
- passes UseCurrentProfile and the ordered profile into EnvironmentInput
```

No proposed text correction changes this boundary.

## Verified environment fallback policy

For an enabled populated profile:

```text
EffectiveCurrentSpeedMS = maximum HorizontalSpeedMS over profile points
EffectiveWaterDensityKgM3 = average valid profile density
```

For a disabled or empty profile, the scalar current speed and scalar water density remain the fallback values.

## Verified line-force policy

`BuoyCalculator.BuildSegmentRows(...)` currently:

```text
- processes enabled line elements
- divides each line element into equal segments no longer than 1 m
- maps cumulative line position proportionally onto water depth
- obtains current and density at each estimated segment depth
- linearly interpolates U, V, W, and density between profile points
- uses the first profile point above the profile range
- uses the last profile point below the profile range
- computes local drag from horizontal speed sqrt(U² + V²)
- sums segment forces into total line current force
```

The vertical component W is stored, interpolated, and published in segment rows, but it is not included in the current drag velocity.

This is already segmented depth-dependent integration of line drag. It is incorrect to describe it as a future step.

## Verified non-line policy

The current force on these elements still uses `EffectiveCurrentSpeedMS`, which is `max |Uгор|` for an enabled populated profile:

```text
- buoy
- connectors
- payloads / instruments
```

`EffectiveWaterDensityKgM3`, the average profile density, is still used for buoyancy and weight-in-water calculations outside the segmented line calculation.

Therefore it would also be incorrect to imply that every element is fully integrated through the profile.

## Report audit

The current technical report already:

```text
- identifies whether the profile is enabled
- lists profile points
- renders line segment rows
- reports the number of line calculation segments
- reports summed line force by segments
```

It does not retain the old v0.19 sentence that layer integration is a future step. No report or PDF change is required by issue #156.

## Exact replacement: profile window guidance

Replace only the introductory TextBlock text with:

```text
Задайте U/V/W и плотность воды по глубине. Линия разбивается на сегменты не длиннее 1 м; U/V и плотность интерполируются на глубине каждого сегмента. W сохраняется, но в текущей модели сопротивления не используется. Для буя, соединителей и приборов применяется max |Uгор| профиля, а для расчётов веса в воде — средняя плотность профиля.
```

## Exact replacement: populated summary

Preserve the existing dynamic prefix:

```text
Профиль включён: {count} точек, глубины {minDepth}–{maxDepth} м, max |Uгор|={maxSpeed} м/с.
```

Replace only its historical suffix with:

```text
Сила течения на линии интегрируется по интерполированным сегментам ≤1 м; для буя, соединителей и приборов используется max |Uгор|.
```

## Required unchanged summary branches

The disabled branch remains exactly:

```text
Профиль течения отключён. Используется одно значение скорости: {currentSpeedText} м/с.
```

The enabled-empty branch remains exactly:

```text
Профиль включён, но точки не заданы. Будет использовано одно значение скорости.
```

Point count, depth ordering, minimum/maximum depth, maximum horizontal-speed calculation, and numeric formats `0.##` / `0.###` remain unchanged.

## Allowed production diff

A production PR may modify only:

```text
Views/CurrentProfileWindow.axaml
ViewModels/MainWindowCurrentProfileSummaryBuilder.cs
```

Only the two identified strings may change.

## Explicit exclusions

```text
- no Models/EngineeringModels.cs changes
- no MainWindowCalculationInputBuilder changes
- no MainWindowViewModel changes
- no profile interpolation or segmentation changes
- no force, density, buoyancy, weight, or tension formula changes
- no report, PDF, 2D, JSON, DTO, binding, command, or layout changes
- no version bump
- no 3D
```
