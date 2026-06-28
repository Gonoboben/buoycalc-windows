using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class ReportBuilder
{
    public static string Build(string projectName, EnvironmentInput environment, BuoyInput buoy, AnchorInput anchor, CalculationResult result)
    {
        var sb = new StringBuilder();
        var tensionRows = SegmentTensionAnalyzer.Build(result);
        var shape = MooringShapeSolver.Build(environment, result);
        var shapeProjection = MooringShapeProjection.Build(shape);
        var shapeForces = MooringShapeForceAnalyzer.Build(result, shapeProjection);
        var diagnostics = EngineeringDiagnostics.Build(environment, result, shape, tensionRows);
        var vectorBalance = MooringVectorBalance.Build(result);
        MooringShapeStore.Set(shape);

        sb.AppendLine("# BuoyCalc Windows — предварительный отчёт");
        sb.AppendLine();
        sb.AppendLine($"Проект: {projectName}");
        sb.AppendLine($"Вердикт: {result.Verdict}");
        sb.AppendLine($"Главный риск: {result.MainRisk}");
        sb.AppendLine($"Инженерная диагностика: {diagnostics.Summary}");
        sb.AppendLine();

        AppendEnvironment(sb, environment);
        AppendBuoy(sb, buoy, shape);
        AppendAnchor(sb, anchor, result);
        AppendTotals(sb, result, tensionRows, shape, shapeProjection, shapeForces, diagnostics);
        AppendDiagnostics(sb, diagnostics);
        AppendVectorBalanceRows(sb, vectorBalance);
        AppendElementRows(sb, result);
        AppendSegmentRows(sb, result);
        AppendTensionRows(sb, tensionRows);
        AppendShapeRows(sb, shape);
        AppendShapeProjectionRows(sb, shapeProjection);
        AppendShapeForceRows(sb, shapeForces);
        AppendChecks(sb, result);

        sb.AppendLine("## Ограничения");
        sb.AppendLine(shape.MethodNote);
        sb.AppendLine(shapeProjection.MethodNote);
        sb.AppendLine(shapeForces.MethodNote);
        sb.AppendLine(vectorBalance.MethodNote);
        sb.AppendLine("v0.31 считает shape-based силы по ориентации сегментов, но ещё не подставляет их обратно в итерационный solver формы.");

        return sb.ToString();
    }

    private static void AppendEnvironment(StringBuilder sb, EnvironmentInput environment)
    {
        sb.AppendLine("## Условия");
        sb.AppendLine($"- Плотность воды базовая: {environment.WaterDensityKgM3:0.####} кг/м³");
        sb.AppendLine($"- Плотность воды расчётная: {environment.EffectiveWaterDensityKgM3:0.####} кг/м³");
        sb.AppendLine($"- Глубина: {environment.DepthM:0.####} м");
        sb.AppendLine($"- Течение базовое: {environment.CurrentSpeedMS:0.####} м/с");
        sb.AppendLine($"- Течение расчётное: {environment.EffectiveCurrentSpeedMS:0.####} м/с");
        sb.AppendLine($"- Профиль течения: {(environment.UseCurrentProfile ? "используется" : "не используется")}");
        sb.AppendLine($"- Волна: {environment.WaveHeightM:0.####} м / {environment.WavePeriodS:0.####} с");
        sb.AppendLine($"- Грунт: {environment.Seabed.Name}");
        sb.AppendLine($"- Множитель грунта: {environment.Seabed.HoldingMultiplier:0.####}");
        sb.AppendLine($"- Примечание по грунту: {environment.Seabed.Note}");
        sb.AppendLine();

        if (environment.EffectiveCurrentProfile.Count == 0)
        {
            return;
        }

        sb.AppendLine("## Профиль течения по глубине");
        sb.AppendLine("| Глубина, м | U East, м/с | V North, м/с | W Vertical, м/с | |U|, м/с | ρ, кг/м³ |");
        sb.AppendLine("|---:|---:|---:|---:|---:|---:|");
        foreach (var p in environment.EffectiveCurrentProfile)
        {
            sb.AppendLine($"| {p.DepthM:0.####} | {p.EastCurrentMS:0.####} | {p.NorthCurrentMS:0.####} | {p.VerticalCurrentMS:0.####} | {p.SpeedMS:0.####} | {p.WaterDensityKgM3:0.####} |");
        }
        sb.AppendLine();
    }

    private static void AppendBuoy(StringBuilder sb, BuoyInput buoy, MooringShapeResult shape)
    {
        sb.AppendLine("## Буй");
        sb.AppendLine($"- Название: {buoy.Name}");
        sb.AppendLine($"- Объём: {buoy.VolumeM3:0.####} м³");
        sb.AppendLine($"- Масса: {buoy.WeightKg:0.####} кг");
        sb.AppendLine($"- Площадь: {buoy.ProjectedAreaM2:0.####} м²");
        sb.AppendLine($"- Cd: {buoy.DragCoefficient:0.####}");
        sb.AppendLine($"- Состояние по форме: {DisplayBuoyState(shape.BuoyState)}");
        sb.AppendLine($"- Глубина узла буя: {shape.BuoyPoint?.ZDepthM ?? 0:0.####} м");
        sb.AppendLine();
    }

    private static void AppendAnchor(StringBuilder sb, AnchorInput anchor, CalculationResult result)
    {
        sb.AppendLine("## Якорь");
        sb.AppendLine($"- Название: {anchor.Name}");
        sb.AppendLine($"- Тип: {anchor.Type}");
        sb.AppendLine($"- Материал: {anchor.Material}");
        sb.AppendLine($"- Масса: {anchor.WeightAirKg:0.####} кг");
        sb.AppendLine($"- Объём: {anchor.VolumeM3:0.####} м³");
        sb.AppendLine($"- Вес якоря в воде: {result.AnchorWeightWaterKg:0.####} кг");
        sb.AppendLine($"- Базовый коэф. удержания якоря: {result.AnchorBaseHoldingCoefficient:0.####}");
        sb.AppendLine($"- Множитель типа якоря: {result.AnchorTypeMultiplier:0.####}");
        sb.AppendLine($"- Множитель грунта: {result.SeabedHoldingMultiplier:0.####}");
        sb.AppendLine("- Формула удержания: вес в воде × K якоря × K типа × K грунта");
        sb.AppendLine();
    }

    private static void AppendTotals(StringBuilder sb, CalculationResult result, IReadOnlyList<SegmentTensionRow> tensionRows, MooringShapeResult shape, MooringShapeProjectionResult shapeProjection, MooringShapeForceResult shapeForces, EngineeringDiagnosticsResult diagnostics)
    {
        sb.AppendLine("## Итоги");
        sb.AppendLine($"- Плавучесть: {result.BuoyancyKg:0.####} кг");
        sb.AppendLine($"- Вес в воде: {result.TotalWeightWaterKg:0.####} кг");
        sb.AppendLine($"- Чистая плавучесть: {result.NetBuoyancyKg:0.####} кг");
        sb.AppendLine($"- Сила течения: {result.CurrentForceN:0.####} Н");
        sb.AppendLine($"- Волновая составляющая: {result.WaveForceN:0.####} Н");
        sb.AppendLine($"- Горизонтальная сила: {result.HorizontalForceN:0.####} Н");
        sb.AppendLine($"- Натяжение: {result.TensionKn:0.####} кН");
        sb.AppendLine($"- Слабое звено: {result.WeakLinkName}");
        sb.AppendLine($"- MBL слабого звена: {result.WeakLinkBreakingLoadKn:0.####} кН");
        sb.AppendLine($"- WLL слабого звена: {result.WorkingLoadKn:0.####} кН");
        sb.AppendLine($"- Запас по слабому звену: {result.TensionReserve:0.####}");
        sb.AppendLine($"- Требуемое удержание якоря: {result.RequiredAnchorHoldingKg:0.####} кг");
        sb.AppendLine($"- Удержание якоря: {result.AnchorHoldingKg:0.####} кг");
        sb.AppendLine($"- Запас якоря: {result.AnchorReserve:0.####}");
        sb.AppendLine($"- Длина линии: {result.LineLengthM:0.####} м");
        sb.AppendLine($"- Оценочный снос: {result.EstimatedOffsetM:0.####} м");
        sb.AppendLine($"- Диагностика: {diagnostics.Summary}");

        if (tensionRows.Count > 0)
        {
            var maxTension = tensionRows.OrderByDescending(x => x.TensionKn).First();
            sb.AppendLine($"- Макс. натяжение по сегментной оценке: {maxTension.TensionKn:0.####} кН, сегмент №{maxTension.Number}, z≈{maxTension.EstimatedDepthM:0.####} м");
        }

        if (shape.Nodes.Count > 0)
        {
            sb.AppendLine($"- Горизонтальный снос по узлам X/Z: {shape.HorizontalOffsetM:0.####} м");
            sb.AppendLine($"- Глубина якорного узла X/Z: {shape.AnchorPoint?.ZDepthM ?? 0:0.####} м");
            sb.AppendLine($"- Невязка якорной глубины: {shape.VerticalResidualM:0.####} м");
            sb.AppendLine($"- Невязка solver формы: {shape.ConvergenceResidualM:0.####} м");
            sb.AppendLine($"- Итераций solver формы: {shape.IterationCount}");
            sb.AppendLine($"- ΣdX формы: {shapeProjection.SumDeltaXM:0.####} м");
            sb.AppendLine($"- ΣdZ формы: {shapeProjection.SumDeltaZM:0.####} м");
            sb.AppendLine($"- Статус проекций формы: {(shapeProjection.GeometryClosed ? "OK" : "WARNING")}");
            sb.AppendLine($"- Shape-based сила линии: {shapeForces.ShapeLineForceN:0.####} Н");
            sb.AppendLine($"- Отличие shape-based силы линии: {shapeForces.DifferenceN:0.####} Н ({shapeForces.RelativeDifference:0.####})");
            sb.AppendLine($"- Статус shape-based сил: {(shapeForces.WithinTolerance ? "OK" : "INFO: отличается от старой оценки")}");
            sb.AppendLine($"- Узлов формы: {shape.Nodes.Count}");
        }
        sb.AppendLine();
    }

    private static void AppendDiagnostics(StringBuilder sb, EngineeringDiagnosticsResult diagnostics)
    {
        sb.AppendLine("## Контроль инженерной модели");
        sb.AppendLine($"Общий статус: {EngineeringDiagnostics.DisplaySeverity(diagnostics.OverallSeverity)} — {diagnostics.Summary}.");
        sb.AppendLine();
        sb.AppendLine("| Проверка | Значение | Допуск / критерий | Статус | Примечание |");
        sb.AppendLine("|---|---:|---|---|---|");
        foreach (var row in diagnostics.Rows)
        {
            sb.AppendLine($"| {Escape(row.CheckName)} | {Escape(row.Value)} | {Escape(row.Tolerance)} | {EngineeringDiagnostics.DisplaySeverity(row.Severity)} | {Escape(row.Note)} |");
        }
        sb.AppendLine();
    }

    private static void AppendVectorBalanceRows(StringBuilder sb, MooringVectorBalanceResult balance)
    {
        sb.AppendLine("## Векторная ведомость сил постановки");
        sb.AppendLine("Положительное направление X принято по направлению горизонтальной нагрузки. Положительное направление Z принято вверх.");
        sb.AppendLine("Эта таблица показывает силовые члены, уже переданные расчётным ядром. Она не является результатом итерационного solver равновесной формы.");
        sb.AppendLine();
        sb.AppendLine("| № | Тип | Элемент | Fx, Н | Fz, Н | Примечание |");
        sb.AppendLine("|---:|---|---|---:|---:|---|");
        var number = 1;
        foreach (var term in balance.Terms)
        {
            sb.AppendLine($"| {number++} | {Escape(term.Kind)} | {Escape(term.Name)} | {term.FxN:0.####} | {term.FzN:0.####} | {Escape(term.Note)} |");
        }
        sb.AppendLine($"|  | **Σ** | **Сумма учтённых сил** | **{balance.SumExternalFxN:0.####}** | **{balance.SumExternalFzN:0.####}** | Сумма строк таблицы. |");
        sb.AppendLine($"|  | **R** | **Требуемая реакция для ΣF=0** | **{balance.RequiredReactionFxN:0.####}** | **{balance.RequiredReactionFzN:0.####}** | Вычислена как минус сумма учтённых сил; пока не найдена solver-ом. |");
        sb.AppendLine();
        sb.AppendLine($"- Горизонтальная удерживающая способность якоря: {balance.AnchorHorizontalCapacityN:0.####} Н");
        sb.AppendLine($"- Запас горизонтального удержания по требуемой реакции Rx: {balance.AnchorHorizontalReserve:0.####}");
        sb.AppendLine($"- Статус баланса: {(balance.IsSolved ? "решён" : "каркас собран; реакции не решены")}");
        sb.AppendLine($"- Методическое примечание: {balance.MethodNote}");
        sb.AppendLine();
    }

    private static void AppendElementRows(StringBuilder sb, CalculationResult result)
    {
        sb.AppendLine("## Таблица элементов");
        sb.AppendLine("| № | Тип | Элемент | Пресет | Длина, м | Кол-во | Вес в воде, кг | Площадь, м² | Cd | Сила, Н | MBL, кН | WLL, кН | Запас | Статус |");
        sb.AppendLine("|---:|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in result.ElementRows)
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.Kind)} | {Escape(row.Title)} | {Escape(row.PresetName)} | {row.LengthM:0.####} | {row.Count} | {row.WeightWaterKg:0.####} | {row.ProjectedAreaM2:0.####} | {row.DragCoefficient:0.####} | {row.CurrentForceN:0.####} | {row.BreakingLoadKn:0.####} | {row.WorkingLoadKn:0.####} | {row.Reserve:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
    }

    private static void AppendSegmentRows(StringBuilder sb, CalculationResult result)
    {
        if (result.SegmentRows.Count == 0) return;
        sb.AppendLine("## Расчётные сегменты линии");
        sb.AppendLine($"Линия разбита на {result.SegmentRows.Count} сегментов. В таблице ниже показаны первые {System.Math.Min(80, result.SegmentRows.Count)} сегментов.");
        sb.AppendLine();
        sb.AppendLine("| № | Элемент | Пресет | s0, м | s1, м | L, м | z, м | U | V | W | |Uгор| | ρ | A, м² | Cd | Сила, Н |");
        sb.AppendLine("|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var row in result.SegmentRows.Take(80))
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.SourceElement)} | {Escape(row.RopePresetName)} | {row.StartLengthM:0.####} | {row.EndLengthM:0.####} | {row.SegmentLengthM:0.####} | {row.EstimatedDepthM:0.####} | {row.EastCurrentMS:0.####} | {row.NorthCurrentMS:0.####} | {row.VerticalCurrentMS:0.####} | {row.LocalSpeedMS:0.####} | {row.WaterDensityKgM3:0.####} | {row.ProjectedAreaM2:0.####} | {row.DragCoefficient:0.####} | {row.CurrentForceN:0.####} |");
        }
        sb.AppendLine();
        sb.AppendLine($"Суммарная сила течения по сегментам линии: {result.SegmentRows.Sum(x => x.CurrentForceN):0.####} Н");
        sb.AppendLine();
    }

    private static void AppendTensionRows(StringBuilder sb, IReadOnlyList<SegmentTensionRow> rows)
    {
        if (rows.Count == 0) return;
        var maxTension = rows.OrderByDescending(x => x.TensionKn).First();
        sb.AppendLine("## Натяжения по сегментам линии");
        sb.AppendLine($"Расчёт ведётся снизу вверх: от якоря к бую. Показаны первые {System.Math.Min(80, rows.Count)} сегментов.");
        sb.AppendLine($"Максимальное оценочное натяжение: {maxTension.TensionKn:0.####} кН на сегменте №{maxTension.Number}.");
        sb.AppendLine();
        sb.AppendLine("| № | Элемент | z, м | L, м | Вес в воде, кг | Fтек, Н | ΣFгор, Н | ΣFверт, Н | T, кН | Угол от вертикали, ° | Статус |");
        sb.AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in rows.Take(80))
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.SourceElement)} | {row.EstimatedDepthM:0.####} | {row.SegmentLengthM:0.####} | {row.WeightWaterKg:0.####} | {row.SegmentCurrentForceN:0.####} | {row.CumulativeHorizontalForceN:0.####} | {row.CumulativeVerticalForceN:0.####} | {row.TensionKn:0.####} | {row.AngleFromVerticalDeg:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
    }

    private static void AppendShapeRows(StringBuilder sb, MooringShapeResult shape)
    {
        if (shape.Nodes.Count == 0) return;
        sb.AppendLine("## Расчётная форма постановки X/Z");
        sb.AppendLine("Координаты являются выходом инженерного слоя MooringShapeSolver. Визуализация должна только отображать эти точки.");
        sb.AppendLine($"Состояние буя: {DisplayBuoyState(shape.BuoyState)}. Сходимость: {(shape.Converged ? "да" : "нет")}.");
        sb.AppendLine($"Итерации solver: {shape.IterationCount}, невязка: {shape.ConvergenceResidualM:0.####} м, scale={shape.AngleScale:0.####}. Критерий: {shape.ConvergenceCriterion}.");
        sb.AppendLine($"Показаны первые {System.Math.Min(90, shape.Nodes.Count)} узлов из {shape.Nodes.Count}.");
        sb.AppendLine();
        sb.AppendLine("| Узел | Сегмент | Элемент | s, м | X, м | Z, м | Lсег, м | Угол, ° | T, кН | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in shape.Nodes.Take(90))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.Label)} | {row.AlongLineM:0.####} | {row.XOffsetM:0.####} | {row.ZDepthM:0.####} | {row.SegmentLengthM:0.####} | {row.SegmentAngleFromVerticalDeg:0.####} | {row.SegmentTensionKn:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
    }

    private static void AppendShapeProjectionRows(StringBuilder sb, MooringShapeProjectionResult projection)
    {
        if (projection.Rows.Count == 0) return;
        sb.AppendLine("## Проекции формы X/Z по сегментам");
        sb.AppendLine("Этот раздел связывает узлы MooringShapeSolver с геометрией сегментов: dX, dZ, фактическая длина по координатам и угол от вертикали.");
        sb.AppendLine($"ΣdX={projection.SumDeltaXM:0.####} м; ΣdZ={projection.SumDeltaZM:0.####} м; Lпо координатам={projection.TotalProjectedLengthM:0.####} м; Lсегментов={projection.TotalSegmentLengthM:0.####} м.");
        sb.AppendLine($"Невязка длины={projection.LengthResidualM:0.####} м; невязка X конечных точек={projection.EndpointResidualXM:0.####} м; невязка Z конечных точек={projection.EndpointResidualZM:0.####} м; статус={(projection.GeometryClosed ? "OK" : "WARNING")}.");
        sb.AppendLine($"Максимальный угол от вертикали={projection.MaxAngleFromVerticalDeg:0.####}°; средний угол={projection.AverageAngleFromVerticalDeg:0.####}°.");
        sb.AppendLine();
        sb.AppendLine("| № | Сегмент | Элемент | L, м | dX, м | dZ, м | L по X/Z, м | Невязка L, м | Угол, ° | T, кН | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in projection.Rows.Take(90))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.Label)} | {row.SegmentLengthM:0.####} | {row.DeltaXM:0.####} | {row.DeltaZM:0.####} | {row.ProjectedLengthM:0.####} | {row.LengthResidualM:0.####} | {row.AngleFromVerticalDeg:0.####} | {row.TensionKn:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(projection.MethodNote);
        sb.AppendLine();
    }

    private static void AppendShapeForceRows(StringBuilder sb, MooringShapeForceResult forces)
    {
        if (forces.Rows.Count == 0) return;
        sb.AppendLine("## Shape-based силы линии по ориентации сегментов");
        sb.AppendLine("Эта таблица сравнивает старую силу сегмента с оценкой по нормальной составляющей скорости к фактической X/Z-ориентации сегмента.");
        sb.AppendLine($"Старая ΣFлинии={forces.OriginalLineForceN:0.####} Н; shape-based ΣFлинии={forces.ShapeLineForceN:0.####} Н; Δ={forces.DifferenceN:0.####} Н ({forces.RelativeDifference:0.####}).");
        sb.AppendLine($"Максимальное отличие строки={forces.MaxRowDifferenceN:0.####} Н; статус={(forces.WithinTolerance ? "OK" : "INFO: заметное отличие от старой оценки")}.");
        sb.AppendLine();
        sb.AppendLine("| № | Сегмент | Элемент | L, м | U, м/с | Uнорм, м/с | Угол, ° | F старая, Н | F shape, Н | ΔF, Н | Ratio | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in forces.Rows.Take(90))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.Label)} | {row.SegmentLengthM:0.####} | {row.LocalSpeedMS:0.####} | {row.NormalSpeedMS:0.####} | {row.AngleFromVerticalDeg:0.####} | {row.OriginalForceN:0.####} | {row.ShapeForceN:0.####} | {row.DifferenceN:0.####} | {row.Ratio:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(forces.MethodNote);
        sb.AppendLine();
    }

    private static void AppendChecks(StringBuilder sb, CalculationResult result)
    {
        sb.AppendLine("## Проверки");
        foreach (var check in result.Checks)
        {
            sb.AppendLine($"- {check}");
        }
        sb.AppendLine();
    }

    private static string DisplayBuoyState(BuoyShapeState state)
    {
        return state switch
        {
            BuoyShapeState.Surface => "на поверхности",
            BuoyShapeState.Submerged => "под водой",
            BuoyShapeState.Overloaded => "перегружен / отрицательная плавучесть",
            _ => "не определено"
        };
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
