# Контрольная отметка: горизонтальный и трёхмерный модуль скорости профиля

Дата: 2026-08-06
Issue: #232
Scope: documentation only

## Причина

Точка профиля течения содержит компоненты:

```text
U = EastCurrentMS
V = NorthCurrentMS
W = VerticalCurrentMS
```

и два разных модуля скорости:

```text
HorizontalSpeedMS = sqrt(U² + V²)
SpeedMS = sqrt(U² + V² + W²)
```

Эти величины имеют разную роль.

## Расчётная роль

Текущая модель сопротивления линии использует горизонтальный модуль:

```text
|Uгор| = sqrt(U² + V²)
```

`EnvironmentInput.EffectiveCurrentSpeedMS` для буя, соединителей и приборов также выбирает:

```text
max HorizontalSpeedMS
```

Компонента `W`:

```text
- сохраняется в проекте;
- интерполируется по глубине;
- присутствует в расчётных read models;
- не используется в текущей drag-модели.
```

## Текущий отчётный дефект

`TechnicalReportMarkdownBuilder.AppendEnvironment(...)` выводит таблицу:

```text
Глубина | U East | V North | W Vertical | |U| | ρ
```

В колонку `|U|` записывается:

```csharp
p.SpeedMS
```

то есть трёхмерный модуль:

```text
sqrt(U² + V² + W²)
```

Обозначение `|U|` неоднозначно и не показывает величину, которая фактически используется в расчётах сопротивления.

## Выбранное отображение

Таблица показывает оба уже рассчитанных значения:

```text
|Uгор|, м/с
|U3D|, м/с
```

Источники:

```text
|Uгор| = p.HorizontalSpeedMS
|U3D|  = p.SpeedMS
```

## Новый порядок колонок

```text
Глубина, м
U East, м/с
V North, м/с
W Vertical, м/с
|Uгор|, м/с
|U3D|, м/с
ρ, кг/м³
```

Горизонтальный модуль расположен первым, поскольку именно он используется текущей моделью сопротивления.

## Разрешённый production-diff

Только:

```text
Services/TechnicalReportMarkdownBuilder.cs
```

Внутри `AppendEnvironment(...)` разрешено изменить только:

```text
- заголовок таблицы профиля;
- Markdown-разделитель;
- строку вывода одной точки профиля.
```

## Инварианты

Не меняются:

```text
- CurrentProfilePointInput;
- HorizontalSpeedMS и SpeedMS;
- сортировка и интерполяция профиля;
- EffectiveCurrentSpeedMS;
- локальная скорость сегмента;
- использование W;
- плотность воды;
- силы, веса, натяжения и формы;
- solver, gate и selected-shape routing;
- stores, 2D и PDF diagram sources;
- JSON/DTO, XAML, команды и версия;
- 3D-визуализация не добавляется.
```

## Влияние

Правка изменяет только Markdown технического отчёта.

Пользователь сможет отличить:

```text
- расчётный горизонтальный модуль;
- справочный трёхмерный модуль с учётом W.
```

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
