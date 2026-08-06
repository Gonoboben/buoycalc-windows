# Контрольная отметка: диагностика сохранения длины линии при сегментации

Дата: 2026-08-06
Issue: #229
Scope: documentation only

## Причина

`CalculationResult` содержит две формы представления длины распределённой линии:

```text
LineLengthM
SegmentRows[].SegmentLengthM
```

`LineLengthM` вычисляется из включённых участков линии до сегментации.

`SegmentRows` создаются в `BuoyCalculator.BuildSegmentRows(...)`, где каждый участок разбивается на сегменты не длиннее 1 м.

Сумма длин сегментов должна восстанавливать исходную длину линии:

```text
Σ SegmentRows.SegmentLengthM
≈
CalculationResult.LineLengthM
```

## Почему существующей проверки недостаточно

`EngineeringDiagnostics` уже содержит строку:

```text
Длина расчётной линии
```

Она сравнивает:

```text
shape.AnchorPoint.AlongLineM
LineLengthM
```

Это проверка конечного узла построенной формы.

Она не является независимой проверкой сегментации, потому что сама форма строится по `SegmentRows`.

Если построитель сегментов потеряет или задвоит длину, downstream-форма может воспроизвести ту же ошибочную сумму.

## Новая независимая проверка

Вычисления:

```text
segmentLengthM = Σ result.SegmentRows.SegmentLengthM

absoluteResidualM =
    abs(segmentLengthM - result.LineLengthM)

relativeResidual =
    absoluteResidualM / max(1 м, abs(result.LineLengthM))
```

Критерий:

```text
relativeResidual ≤ 1e-6
```

Порог используется как программный допуск согласованности представлений `double`.

## Строка диагностики

Название:

```text
Согласованность длины линии и расчётных сегментов
```

Значение:

```text
ΔL={absoluteResidualM} м ({relativeResidual})
```

Допуск:

```text
relative ≤ 1e-6
```

Severity:

```text
OK    — relativeResidual ≤ 1e-6
ERROR — relativeResidual > 1e-6
```

Примечание:

```text
Длина линии={LineLengthM} м; Σ длин сегментов={segmentLengthM} м. Проверяется сохранение длины распределённой линии при сегментации.
```

## Размещение

Строка размещается рядом с существующей проверкой веса линии и сегментов.

Логический порядок:

```text
геометрические граничные проверки
→ согласованность длины сегментации
→ согласованность веса сегментации
→ силовые проверки
```

## Разрешённый production-diff

Только:

```text
Services/EngineeringDiagnostics.cs
```

Разрешено:

```text
- вычислить сумму длин сегментов;
- вычислить абсолютную и относительную невязку;
- добавить одну EngineeringDiagnosticRow.
```

## Инварианты

Не меняются:

```text
- LineLengthM и SegmentLengthM;
- количество и нумерация сегментов;
- WeightWaterKg и CurrentForceN;
- профиль течения и плотность;
- натяжения, углы и формы;
- существующая проверка shape.AnchorPoint.AlongLineM;
- CalculationResult.Verdict и MainRisk;
- solver, gate и selected-shape routing;
- stores, 2D и PDF;
- JSON/DTO, XAML, команды и версия;
- 3D не добавляется.
```

Новая строка может влиять только на существующий `EngineeringDiagnosticsResult.OverallSeverity` при фактической несогласованности read models.

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
