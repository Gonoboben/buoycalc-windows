# Контрольная отметка: конечные пользовательские числовые значения

Дата: 2026-08-06
Issue: #259
Scope: documentation only

## Причина

Пользовательские числовые поля преобразуются через `Double.TryParse` и при неудаче получают значение `0`.

В современных версиях .NET успешный `TryParse` не гарантирует конечный результат: переполненная числовая строка может дать `PositiveInfinity` или `NegativeInfinity`. Специальные представления плавающей точки также не должны проходить в инженерную модель как обычные числа.

Неконечный результат опасен до стадии отчёта и autocheck, потому что может попасть в:

```text
- глубину, плотность, течение и волну
- U/V/W профиля течения
- длину линии и сегментацию
- параметры буя, якоря, соединителя, линии и прибора
- drag, вес в воде, плавучесть, удержание и натяжения
- X/Z solver и координаты выбранной формы
- пользовательские библиотеки элементов
```

## Действующая политика parser-а

Существующая политика сохраняется:

```text
нечисловой пользовательский ввод → 0
```

Она расширяется на неконечные результаты:

```text
TryParse success AND double.IsFinite(result) → result
иначе → 0
```

## Точный инвентарь production-границ

### MainWindowCalculationInputBuilder

Поля:

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

### CurrentProfilePointViewModel

Поля:

```text
DepthM
EastCurrentMS
NorthCurrentMS
VerticalCurrentMS
WaterDensityKgM3
```

### AssemblyItemViewModel

Поля:

```text
LengthM
PayloadWeightAirKg
PayloadVolumeM3
PayloadProjectedAreaM2
PayloadDragCoefficient
```

### MainWindowViewModel

Локальный parser используется только для оперативной визуальной сводки глубины и последовательности. Его конечность должна совпадать с основным input builder.

### ElementLibraryViewModel

Parser используется при сохранении всех пользовательских библиотечных элементов:

```text
буй
линия
соединитель
якорь
прибор
```

### MainWindowUserBuoySaveBuilder

Отдельный путь сохранения пользовательского буя из главного окна должен использовать ту же границу.

## Разрешённое production-изменение

В каждом перечисленном private `Parse` заменить условие:

```csharp
TryParse(...) ? result : 0
```

на эквивалент:

```csharp
TryParse(...) && double.IsFinite(result) ? result : 0
```

Многострочное форматирование допускается без изменения логики.

## Инварианты

```text
- все конечные значения сохраняются без изменения
- comma-to-dot normalization сохраняется
- NumberStyles.Any сохраняется
- InvariantCulture сохраняется
- отрицательные конечные значения не clamp-ятся
- дополнительные диапазоны и физические критерии не вводятся
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

## Проверки

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
