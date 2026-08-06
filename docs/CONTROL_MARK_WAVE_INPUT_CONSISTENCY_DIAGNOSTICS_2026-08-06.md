# Контрольная отметка: согласованность высоты и периода волны

Дата: 2026-08-06  
Issue: #280

## Контекст

Расчёт волновой добавки использует:

```text
waveVelocity = WavePeriodS > 0
    ? π × WaveHeightM / WavePeriodS
    : 0

waveForce = DragForce(..., waveVelocity, ...)
```

Input-граница удаляет `NaN` и infinity, но не исправляет физический диапазон конечных значений.

Из-за этого:

- отрицательная высота при положительном периоде создаёт ту же положительную силу, что и её модуль, поскольку скорость возводится в квадрат;
- положительная высота при `T <= 0` молча даёт нулевую волновую добавку;
- отрицательный период физически неверен даже при нулевой высоте.

Формула не изменяется в рамках этой задачи. Несогласованный ввод показывается диагностикой.

## Диагностика 1: высота волны

```text
CheckName: Неотрицательная высота волны
Value: H=<environment.WaveHeightM> м
Tolerance: H >= 0 и конечна
```

Severity:

```text
OK    double.IsFinite(environment.WaveHeightM) && environment.WaveHeightM >= 0
ERROR otherwise
```

## Диагностика 2: период и высота

```text
CheckName: Согласованность периода и высоты волны
Value: H=<environment.WaveHeightM> м; T=<environment.WavePeriodS> с
```

### При положительной высоте

```text
WaveHeightM > 0
Tolerance: T > 0 при H > 0
OK: double.IsFinite(WavePeriodS) && WavePeriodS > 0
```

### При нулевой или отрицательной высоте

```text
WaveHeightM <= 0
Tolerance: T >= 0 при H = 0
OK: double.IsFinite(WavePeriodS) && WavePeriodS >= 0
```

Отрицательная высота уже получает `ERROR` в первой строке. Вторая строка независимо проверяет, что период не отрицателен, не дублируя условие высоты.

## Проверяемые сценарии

### Волна задана корректно

```text
H = 1,0 м
T = 6,0 с
height = OK
period = OK
```

### Волна отключена

```text
H = 0
T = 0
height = OK
period = OK
```

### Положительная высота без периода

```text
H = 1,0 м
T = 0
height = OK
period = ERROR
```

### Отрицательная высота

```text
H = -1,0 м
T = 6,0 с
height = ERROR
period = OK
```

### Отрицательный период

```text
H = 0
T = -6,0 с
height = OK
period = ERROR
```

## Архитектурная граница

Production-изменение ограничивается:

```text
Services/EngineeringDiagnostics.cs
```

Допустимо изменить только:

- `EngineeringDiagnosticsResult.Rows`;
- `EngineeringDiagnosticsResult.OverallSeverity`;
- производный diagnostic summary.

Не изменяются:

- `EnvironmentInput`;
- `CalculationResult.Verdict`;
- `CalculationResult.MainRisk`;
- `waveVelocity`;
- `waveForce`;
- результаты solver и координаты.

## Запрещённые изменения

В рамках Issue #280 запрещено:

- применять `Math.Abs` к высоте или периоду;
- нормализовать, ограничивать или заменять ввод;
- блокировать команду расчёта;
- менять формулу волновой скорости или силы;
- менять профиль течения, плотность, drag, вес, плавучесть, натяжение или якорь;
- менять solver, gate, stores или selected-shape routing;
- менять 2D/PDF координаты;
- менять JSON/DTO, XAML или команды;
- добавлять 3D.

## Порядок строк

Обе строки размещаются в начальной группе физических входных инвариантов после активной скалярной скорости течения и до проверок плотности.

## Условия merge

Сначала документационный PR, затем отдельный production PR.

Оба PR объединяются только после успешных проверок:

- `.NET Build`;
- `Selected Shape Consumer Scan`;
- `Report Store Consumer Scan`.
