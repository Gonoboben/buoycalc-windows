# Контрольная отметка: подписи скоростей shape-based силы X/Z

Дата: 2026-08-06
Issue: #256
Scope: documentation only

## Причина

В таблице `Силы линии по форме X/Z и ориентации сегментов` выводятся два существующих значения:

```text
MooringShapeForceRow.LocalSpeedMS
MooringShapeForceRow.NormalSpeedMS
```

После решения Issue #252 их смысл определён однозначно:

```text
Uxz = sqrt(|Uгор|² + W²)
Uнорм,XZ = составляющая Uxz, нормальная к X/Z-направлению сегмента
```

Текущие заголовки:

```text
U, м/с
Uнорм, м/с
```

не различают эти значения с `U East`, базовым `|Uгор|` и информационным `|U3D|` профиля.

## Требуемые подписи

```text
Uxz, м/с
Uнорм,XZ, м/с
```

Вводная строка раздела должна прямо пояснять:

```text
Uxz = sqrt(|Uгор|² + W²)
сила по форме X/Z рассчитывается по компоненте Uнорм,XZ
```

## Источники значений

Подписи меняются без изменения источников:

```text
Uxz column       → row.LocalSpeedMS
Uнорм,XZ column  → row.NormalSpeedMS
```

## Инварианты

```text
- значения строк не меняются
- MooringShapeForceRow не меняется
- MooringShapeForceAnalyzer не меняется
- формулы speed, dot, normalSpeed and ShapeForceN не меняются
- количество, порядок и sampling строк не меняются
- единицы и numeric format 0.#### не меняются
- shape tensions and iterative feedback не меняются
- gate and selected-shape routing не меняются
- 2D and PDF coordinates не меняются
- JSON/DTO, XAML, commands, version and 3D не меняются
```

## Разрешённый production diff

```text
Services/TechnicalReportMarkdownMovedSections.cs
```

Разрешены только:

```text
- вводная строка AppendShapeForceRows
- два заголовка колонок этой таблицы
```

## Проверки

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
