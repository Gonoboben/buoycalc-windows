# Контрольная отметка: диагностика силы линии при агрегации сегментов

Дата: 2026-08-06
Issue: #235
Scope: documentation only

## Причина

Распределённая сила течения линии представлена в двух расчётных read models:

```text
SegmentCalculationRow.CurrentForceN
ElementCalculationRow.CurrentForceN для Kind = Линия
```

Локальные силы рассчитываются в `BuoyCalculator.BuildSegmentRows(...)`.

Для каждого участка линии там одновременно формируется агрегат:

```text
itemCurrentForce = Σ CurrentForceN сегментов участка
```

Список агрегатов передаётся как:

```text
SegmentBuildResult.LineCurrentForces
```

Затем `BuildAssemblyRows(...)` записывает соответствующий агрегат в строку участка линии.

## Требуемая согласованность

```text
Σ SegmentRows.CurrentForceN
≈
Σ ElementRows.CurrentForceN, где Kind = Линия
```

## Почему общего векторного контроля недостаточно

Существующая диагностика сравнивает:

```text
CalculationResult.HorizontalForceN
MooringVectorBalance.SumExternalFxN
```

Она проверяет всю горизонтальную нагрузку постановки:

```text
буй + линия + соединители + приборы + волна
```

Новая строка изолирует именно границу:

```text
локальные сегменты линии
→ агрегированные строки участков линии
```

## Вычисления

```text
lineElementForceN =
    Σ result.ElementRows.CurrentForceN
    для Kind = Линия

segmentForceN =
    Σ result.SegmentRows.CurrentForceN

absoluteResidualN =
    abs(segmentForceN - lineElementForceN)

relativeResidual =
    absoluteResidualN / max(1 Н, abs(lineElementForceN))
```

## Критерий

```text
relativeResidual ≤ 1e-6
```

Это программный допуск согласованности представлений `double`, а не новый физический критерий.

## Строка диагностики

Название:

```text
Согласованность силы линии и расчётных сегментов
```

Значение:

```text
ΔF={absoluteResidualN} Н ({relativeResidual})
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
Сила участков линии={lineElementForceN} Н; Σ сил сегментов={segmentForceN} Н. Проверяется сохранение распределённой силы течения линии при агрегации сегментов.
```

## Размещение

Логический порядок строк сегментации:

```text
согласованность длины
→ согласованность веса
→ согласованность силы
→ накопление Fx/Fz
→ полный векторный контроль
```

## Разрешённый production-diff

Только:

```text
Services/EngineeringDiagnostics.cs
```

Разрешено:

```text
- вычислить две суммы силы;
- вычислить абсолютную и относительную невязку;
- добавить одну EngineeringDiagnosticRow.
```

## Инварианты

Не меняются:

```text
- CurrentForceN сегментов и элементов;
- формула drag;
- профиль течения, плотность, площадь и Cd;
- длина и вес сегментов;
- натяжения, углы и формы;
- существующие Fx/Fz accumulation checks;
- HorizontalForceN/vector-ledger check;
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
