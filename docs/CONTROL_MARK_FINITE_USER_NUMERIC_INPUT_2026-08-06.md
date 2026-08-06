# Контрольная отметка: конечная граница числового ввода расчёта

Дата: 2026-08-06
Issue: #259
Scope: documentation only

## Причина

`MainWindowCalculationInputBuilder` является последней границей между пользовательскими/ViewModel-данными и расчётным ядром.

В современных версиях .NET успешный `Double.TryParse` не гарантирует конечный результат: переполненная числовая строка может дать `PositiveInfinity` или `NegativeInfinity`. Уже созданные объекты профиля или пользовательской библиотеки также могут содержать `NaN`/infinity до входа в builder.

Неконечный результат опасен до стадии отчёта и autocheck, потому что может попасть в:

```text
- глубину, плотность, течение и волну
- U/V/W профиля течения
- длину линии и сегментацию
- параметры буя, якоря, соединителя, линии и прибора
- drag, вес в воде, плавучесть, удержание и натяжения
- X/Z solver и координаты выбранной формы
```

## Выбранная архитектурная граница

Не размножать одинаковую политику по UI/ViewModel parser-ам.

Поставить один авторитетный finite-gate в:

```text
ViewModels/MainWindowCalculationInputBuilder.cs
```

Он должен нормализовать как прямые строки, так и уже построенные вложенные calculation inputs.

## Прямые строковые поля

Существующая политика сохраняется:

```text
нечисловой пользовательский ввод → 0
```

Она расширяется на неконечный parsed result:

```text
TryParse success AND double.IsFinite(result) → result
иначе → 0
```

Эта граница охватывает:

```text
WaterDensity
Depth
CurrentSpeed
WaveHeight
WavePeriod
Buoy Volume / Weight / Area / Cd
Anchor Weight / Volume / BaseHoldingCoefficient
SafetyFactor
```

## Уже построенные значения

### CurrentProfilePointInput

Каждое числовое поле проходит `FiniteOrZero`:

```text
DepthM
EastCurrentMS
NorthCurrentMS
VerticalCurrentMS
WaterDensityKgM3
```

### AssemblyItemInput

Каждое числовое поле проходит `FiniteOrZero`:

```text
LengthM
PayloadWeightAirKg
PayloadVolumeM3
PayloadProjectedAreaM2
PayloadDragCoefficient
```

`Count` является `int` и не меняется.

### RopePreset

При наличии пресета сохраняются текстовые поля и нормализуются:

```text
DiameterMm
BreakingLoadKn
WeightWaterKgM
DragCoefficient
```

### ConnectorPreset

При наличии пресета сохраняются текстовые поля и нормализуются:

```text
WeightAirKg
VolumeM3
BreakingLoadKn
ProjectedAreaM2
DragCoefficient
```

Это защищает расчёт даже при уже существующей пользовательской библиотечной записи с неконечным числом.

## Private helpers

Разрешены только private helpers внутри builder:

```text
FiniteOrZero(double)
SanitizeCurrentProfilePoint(...)
SanitizeAssemblyItem(...)
SanitizeRopePreset(...)
SanitizeConnectorPreset(...)
```

Публичный parser или новый глобальный сервис не вводится.

## Инварианты

```text
- все конечные значения сохраняются без изменения
- comma-to-dot normalization сохраняется
- NumberStyles.Any сохраняется
- InvariantCulture сохраняется
- отрицательные конечные значения не clamp-ятся
- дополнительные диапазоны и физические критерии не вводятся
- names, IDs, types, materials and notes не меняются
- IsEnabled, Kind and Count не меняются
- SelectedSeabedPreset не меняется
- формулы расчётного ядра не меняются
- current-profile interpolation не меняется
- segmentation, drag, weight, tension and shape formulas не меняются
- solver, gate, stores and selected-shape routing не меняются
- 2D and PDF remain consumers
- JSON/DTO schema, XAML, commands, version and 3D не меняются
```

## Ожидаемое поведение

```text
"1025"      → 1025
"-0.2"      → -0.2
"1,25"      → 1.25
нечисловое   → 0
NaN          → 0
+Infinity    → 0
-Infinity    → 0
переполнение → 0, если TryParse возвращает infinity
```

## Разрешённый production diff

```text
ViewModels/MainWindowCalculationInputBuilder.cs
```

## Проверки

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
