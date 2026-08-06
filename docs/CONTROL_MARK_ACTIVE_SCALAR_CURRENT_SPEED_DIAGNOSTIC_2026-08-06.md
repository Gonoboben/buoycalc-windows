# Контрольная отметка: активная скалярная скорость течения

Дата: 2026-08-06  
Issue: #277

## Контекст

Приложение поддерживает два способа задания течения.

### Скалярное значение

`EnvironmentInput.CurrentSpeedMS` используется, когда:

```text
!UseCurrentProfile || EffectiveCurrentProfile.Count == 0
```

В этой ветви поле представляет модуль скорости и должно быть неотрицательным.

### Компонентный профиль

Если профиль включён и содержит хотя бы одну точку, расчёт использует подписанные компоненты U/V/W. Отрицательная компонента в этом режиме описывает направление и не является ошибкой сама по себе.

Текущая input-граница удаляет неконечные числа, но не исправляет знак конечного скалярного значения. В формуле сопротивления скорость возводится в квадрат, поэтому отрицательное скалярное значение молча даёт ту же положительную силу, что и его модуль.

## Цель

Добавить строку:

```text
CheckName: Неотрицательная активная скалярная скорость течения
```

## Определение активной ветви

```text
scalarCurrentIsActive =
    !environment.UseCurrentProfile ||
    environment.EffectiveCurrentProfile.Count == 0
```

Это определение совпадает с действующей логикой `EffectiveCurrentSpeedMS` и `BuildSegmentRows(...)`.

## Скалярная ветвь

Когда `scalarCurrentIsActive == true`:

```text
Value: Uскал=<environment.CurrentSpeedMS> м/с
Tolerance: Uскал >= 0 и конечна
```

Severity:

```text
OK    double.IsFinite(environment.CurrentSpeedMS) && environment.CurrentSpeedMS >= 0
ERROR otherwise
```

## Профильная ветвь

Когда включён непустой профиль:

```text
Value: скалярное значение не используется; активных точек <N>
Tolerance: локальный инвариант не применяется
Severity: OK
```

Строка не проверяет знак U/V/W и не ограничивает направление течения в профиле.

## Проверяемые сценарии

### Одиночная скорость 0,5 м/с

```text
UseCurrentProfile = false
CurrentSpeedMS = 0.5
result = OK
```

### Одиночная скорость 0 м/с

```text
UseCurrentProfile = false
CurrentSpeedMS = 0
result = OK
```

### Одиночная скорость -0,5 м/с

```text
UseCurrentProfile = false
CurrentSpeedMS = -0.5
result = ERROR
```

### Профиль включён, но пуст

```text
UseCurrentProfile = true
EffectiveCurrentProfile.Count = 0
CurrentSpeedMS = -0.5
result = ERROR
```

Пустой профиль не заменяет скалярную fallback-ветвь.

### Активный непустой профиль

```text
UseCurrentProfile = true
EffectiveCurrentProfile.Count > 0
CurrentSpeedMS = -0.5
result = OK для этого локального инварианта
```

Скалярное поле не участвует в расчёте текущей нагрузки.

## Архитектурная граница

Production-изменение ограничивается:

```text
Services/EngineeringDiagnostics.cs
```

Новая строка может изменить только инженерные diagnostic rows и общий diagnostic severity.

Она не изменяет:

- `EnvironmentInput`;
- выбор scalar/profile ветви;
- `EffectiveCurrentSpeedMS`;
- U/V/W;
- `CalculationResult.Verdict`;
- `CalculationResult.MainRisk`;
- рассчитанные силы и координаты.

## Запрещённые изменения

В рамках Issue #277 запрещено:

- применять `Math.Abs` к скорости;
- ограничивать или заменять пользовательский ввод;
- запрещать отрицательные U/V/W профиля;
- менять интерполяцию или fallback профиля;
- менять drag, волну, вес, плавучесть, натяжение или якорь;
- менять solver, gate, stores или selected-shape routing;
- менять 2D/PDF координаты;
- менять JSON/DTO, XAML или команды;
- добавлять 3D.

## Порядок строки

Строка размещается в начальной группе входных физических инвариантов после положительной проектной глубины и до проверок плотности.

## Условия merge

Сначала документационный PR, затем отдельный production PR.

Оба PR объединяются только после успешных проверок:

- `.NET Build`;
- `Selected Shape Consumer Scan`;
- `Report Store Consumer Scan`.
