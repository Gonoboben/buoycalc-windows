# Контрольная отметка: диагностика положительной плотности воды

Дата: 2026-08-06
Issue: #262
Scope: documentation only

## Причина

После finite-gate Issue #259 в расчёт не проходят `NaN` и infinity через основную UI-границу. Значения `0` и отрицательные конечные числа намеренно не исправляются автоматически, поскольку input boundary не должна вводить физические диапазоны.

При этом плотность воды участвует непосредственно в:

```text
- плавучести буя
- весе элементов в воде
- базовой силе течения и волны
- fallback плотности профиля
- сегментной силе сопротивления
- shape-based X/Z силе линии
```

Существующие проверки могут показать производные последствия, но не называют первичную ошибку плотности.

## Диагностика 1: эффективная плотность среды

Источник:

```text
environment.EffectiveWaterDensityKgM3
```

Строка:

```text
Положительная эффективная плотность воды
```

Значение:

```text
ρэфф=... кг/м³
```

Допуск:

```text
ρэфф > 0 и конечна
```

Статус:

```text
OK    when double.IsFinite(ρэфф) && ρэфф > 0
ERROR otherwise
```

Эта проверка относится к плотности, используемой общей базовой моделью и fallback-семантикой профиля.

## Диагностика 2: плотность сегментного read model

Для `CalculationResult.SegmentRows` вычисляются:

```text
nonPositiveOrNonFiniteCount
minimumDensityKgM3, если сегменты существуют
```

Строка:

```text
Положительная плотность расчётных сегментов
```

Значение:

```text
min ρ=... кг/м³; нарушений N
```

Допуск:

```text
каждый ρ > 0 и конечен
```

Статус:

```text
OK    when violations = 0
ERROR otherwise
```

Пустая коллекция сегментов не является ошибкой этого локального инварианта. Наличие расчётной линии контролируется другими геометрическими и сегментными проверками.

## Размещение

Обе строки добавляются рядом с геометрическими/input-quality проверками до проверок согласованности сумм сегментов.

Порядок существующих строк относительно друг друга не меняется.

## Инварианты

```text
- input values не нормализуются и не clamp-ятся
- EffectiveWaterDensityKgM3 не меняется
- profile average and fallback semantics не меняются
- CurrentAtDepth interpolation не меняется
- SegmentCalculationRow не меняется
- drag, buoyancy, weight, force, tension, anchor and shape formulas не меняются
- CalculationResult.Verdict and MainRisk не меняются
- only EngineeringDiagnostics rows/OverallSeverity may reflect the new errors
- solver, gate, stores and selected-shape routing не меняются
- 2D and PDF coordinates не меняются
- JSON/DTO, XAML, commands, version and 3D не меняются
```

## Разрешённый production diff

```text
Services/EngineeringDiagnostics.cs
```

Разрешены:

```text
- локальные вычисления effective density and segment density integrity
- две EngineeringDiagnosticRow
```

## Проверки

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
