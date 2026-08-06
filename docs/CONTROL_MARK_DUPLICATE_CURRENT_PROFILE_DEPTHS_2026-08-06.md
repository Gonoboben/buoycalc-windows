# Контрольная отметка: диагностика дублированных глубин профиля течения

Дата: 2026-08-06
Issue: #265
Scope: documentation only

## Причина

`BuoyCalculator.CurrentAtDepth(...)` сортирует точки профиля по `DepthM` и интерполирует между соседними точками.

Интерполяционная модель требует единственного набора U/V/W/ρ для каждой опорной глубины. При точном повторении глубины профиль становится зависимым от порядка строк.

Пример:

```text
z=100 м, U=0.2
z=100 м, U=0.8
z=200 м, U=0.4
```

Действующее поведение:

```text
- на depth <= первой точке 100 м возвращается первая строка
- нулевой интервал 100–100 м пропускается
- сразу ниже 100 м интерполяция начинается от второй строки 100 м
- перестановка двух строк меняет скачок без изменения глубин
```

Это не ошибка формулы `Lerp`, а неоднозначность входного read model.

## Выбранная граница

Физика и интерполяция не исправляются автоматически.

В `EngineeringDiagnostics` добавляется строка:

```text
Уникальные глубины активного профиля течения
```

## Условия активности

Проверка конфликта применяется только когда:

```text
environment.UseCurrentProfile == true
и profile point count >= 2
```

Если профиль отключён или содержит меньше двух точек, локальный инвариант считается выполненным. Другие проверки сохраняют ответственность за отсутствие профиля и общие входные условия.

## Расчёт

Точки группируются по точному значению `DepthM`:

```text
duplicate depth group = group.Count > 1
duplicateDepthCount = number of duplicate groups
duplicatePointCount = Σ(group.Count - 1)
```

Используется точное равенство `double`.

Не вводятся:

```text
- epsilon
- округление
- автоматическое объединение
- выбор первой или последней точки
- усреднение U/V/W/ρ
```

## Вывод

Без дублей:

```text
значение: точек N; дублированных глубин 0
статус: OK
```

При дублях:

```text
значение: глубин-дублей D; лишних точек P; z=...
статус: ERROR
```

Глубины выводятся по возрастанию. Для ограничения длины строки допускается показать первые 8 уникальных дублированных глубин и добавить `...` при большем количестве.

## Размещение

Строка добавляется рядом с проверками качества входной среды и профиля, до геометрических и суммарных сегментных проверок.

Порядок существующих строк относительно друг друга не меняется.

## Инварианты

```text
- profile collection and DTO не меняются
- sorting and CurrentAtDepth не меняются
- Lerp and zero-span behavior не меняются
- density fallback and averaging не меняются
- U/V/W values не меняются
- drag, weight, force, tension, anchor and shape formulas не меняются
- CalculationResult.Verdict and MainRisk не меняются
- only EngineeringDiagnostics rows/OverallSeverity may change
- solver, gate, stores and selected-shape routing не меняются
- 2D and PDF coordinates не меняются
- XAML, commands, version and 3D не меняются
```

## Разрешённый production diff

```text
Services/EngineeringDiagnostics.cs
```

Разрешены:

```text
- локальное построение duplicate depth groups
- компактная строка duplicate depth values
- одна EngineeringDiagnosticRow
```

## Проверки

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
