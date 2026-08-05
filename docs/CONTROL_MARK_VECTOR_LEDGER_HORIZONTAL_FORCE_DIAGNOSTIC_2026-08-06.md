# Контрольная отметка: диагностика согласованности горизонтальной нагрузки и векторной ведомости

Дата: 2026-08-06
Issue: #208
Scope: documentation only

## Причина

Основной расчёт и векторная ведомость представляют одну базовую горизонтальную нагрузку двумя путями.

`BuoyCalculator` вычисляет:

```text
HorizontalForceN = CurrentForceN + WaveForceN
```

`MooringVectorBalance` повторно собирает горизонтальные силы из строк элементов и волновой добавки:

```text
SumExternalFxN = Σ ElementRows.CurrentForceN + WaveForceN
```

После исправления идентичности сил отдельных участков линии в Issue #205 эти значения должны совпадать с точностью вычислений `double`.

Сейчас `EngineeringDiagnostics` выводит оба значения только как информационные строки и не формирует автоматическую проверку их согласованности.

## Назначение проверки

Новая строка является контролем целостности расчётных представлений:

```text
основной CalculationResult
↔ ElementRows
↔ MooringVectorBalance
```

Это не новый физический критерий проектирования и не замена проверок якоря или натяжения.

## Вычисление невязки

```text
absoluteResidualN = abs(SumExternalFxN - HorizontalForceN)
relativeResidual = absoluteResidualN / max(1 Н, abs(HorizontalForceN))
```

Критерий:

```text
relativeResidual ≤ 1e-6
```

Порог совпадает с уже используемым в `EngineeringDiagnostics` программным относительным допуском внутренних силовых невязок.

## Результат диагностики

Название проверки:

```text
Согласованность базовой нагрузки и векторной ведомости
```

Значение:

```text
ΔFx={absoluteResidualN} Н ({relativeResidual})
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
HorizontalForceN={...} Н; ΣFx ведомости={...} Н. Проверяется восстановление базовой горизонтальной нагрузки из строк элементов и волновой добавки.
```

## Влияние на результаты

Новая строка не меняет:

```text
- CalculationResult.Verdict;
- CalculationResult.MainRisk;
- CurrentForceN, WaveForceN и HorizontalForceN;
- RequiredAnchorHoldingKg и AnchorReserve;
- SumExternalFxN и RequiredReactionFxN;
- AnchorHorizontalReserve;
- форму, натяжения, solver и selected-shape routing.
```

Она участвует только в существующем вычислении `EngineeringDiagnosticsResult.OverallSeverity`, поскольку любая несогласованность представлений должна быть видна в разделе инженерной диагностики отчёта.

## Разрешённый production-diff

Только файл:

```text
Services/EngineeringDiagnostics.cs
```

Разрешено:

```text
- вычислить абсолютную и относительную невязку после MooringVectorBalance.Build(result);
- добавить одну EngineeringDiagnosticRow;
- использовать существующие типы и DisplaySeverity pipeline.
```

## Запрещённые изменения

```text
- не менять EngineeringDiagnosticRow или EngineeringDiagnosticsResult;
- не менять MooringVectorBalance;
- не менять BuoyCalculator и формулы сил;
- не менять расчётный verdict;
- не менять отчётный renderer и порядок существующих разделов;
- не менять solver, stores, selected shape, 2D или PDF;
- не менять JSON, DTO, XAML, команды или версию;
- не добавлять 3D.
```

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
