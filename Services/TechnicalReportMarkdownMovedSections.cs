using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Section renderers already moved out of legacy ReportBuilder.
///
/// This class is intentionally narrow while sections are migrated in small,
/// output-stable PRs.
/// </summary>
internal static class TechnicalReportMarkdownMovedSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        switch (methodName)
        {
            case "AppendVectorBalanceRows":
                AppendVectorBalanceRows((StringBuilder)args[0], (MooringVectorBalanceResult)args[1]);
                return true;
            case "AppendElementRows":
                AppendElementRows((StringBuilder)args[0], (CalculationResult)args[1]);
                return true;
            case "AppendSequencePositionRows":
                AppendSequencePositionRows((StringBuilder)args[0], (MooringSequencePositionResult)args[1]);
                return true;
            case "AppendModelCoverageRows":
                AppendModelCoverageRows((StringBuilder)args[0], (CalculationResult)args[1]);
                return true;
            case "AppendSegmentRows":
                AppendSegmentRows((StringBuilder)args[0], (CalculationResult)args[1]);
                return true;
            case "AppendTensionRows":
                AppendTensionRows((StringBuilder)args[0], (IReadOnlyList<SegmentTensionRow>)args[1]);
                return true;
            case "AppendShapeRows":
                AppendShapeRows((StringBuilder)args[0], (MooringShapeResult)args[1]);
                return true;
            case "AppendShapeProjectionRows":
                AppendShapeProjectionRows((StringBuilder)args[0], (MooringShapeProjectionResult)args[1]);
                return true;
            default:
                return false;
        }
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

    private static void AppendSequencePositionRows(StringBuilder sb, MooringSequencePositionResult positions)
    {
        sb.AppendLine("## Позиционная модель последовательности по s");
        sb.AppendLine("Координата s отсчитывается вдоль линии от верхнего конца к якорю. Линейные элементы занимают интервал s0–s1; соединители, приборы, буй и якорь имеют точечную позицию s.");
        sb.AppendLine($"Длина линии по позиционной модели: {positions.TotalLineLengthM:0.####} м; распределённых участков: {positions.DistributedElementCount}; дискретных элементов без буя/якоря: {positions.DiscreteElementCount}.");
        sb.AppendLine();
        sb.AppendLine("| № | Тип | Элемент | Пресет | s0, м | s1, м | s, м | L, м | Вес в воде, кг | Fx, Н | Роль в solver | Следующий шаг |");
        sb.AppendLine("|---:|---|---|---|---:|---:|---:|---:|---:|---:|---|---|");
        foreach (var row in positions.Rows)
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.Kind)} | {Escape(row.Title)} | {Escape(row.PresetName)} | {row.StartAlongLineM:0.####} | {row.EndAlongLineM:0.####} | {row.PositionAlongLineM:0.####} | {row.LengthM:0.####} | {row.WeightWaterKg:0.####} | {row.CurrentForceN:0.####} | {Escape(row.SolverRole)} | {Escape(row.NextStepNote)} |");
        }
        sb.AppendLine();
        sb.AppendLine(positions.MethodNote);
        sb.AppendLine();
    }

    private static void AppendModelCoverageRows(StringBuilder sb, CalculationResult result)
    {
        sb.AppendLine("## Область учёта элементов в текущей модели");
        sb.AppendLine("Эта таблица показывает, где элемент уже участвует в расчётах. В v0.39 добавлен отдельный итерационный solver-слой, но основной solver, 2D и PDF-схемы пока не заменены.");
        sb.AppendLine();
        sb.AppendLine("| № | Элемент | Тип | Ведомость элементов | Векторный баланс | Позиция s | Дискретные натяжения | Альт. форма X/Z | Дискретный X/Z-узел | Основная форма X/Z | Основные натяжения | Примечание |");
        sb.AppendLine("|---:|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var row in result.ElementRows)
        {
            var shapeScope = row.Kind switch
            {
                "Буй" => "граничный узел",
                "Якорь" => "граничный узел",
                "Линия" => "да, сегменты",
                _ => "нет, дискретный узел пока не вставлен"
            };
            var tensionScope = row.Kind == "Линия" ? "да" : "нет, только общая сила/вес";
            var discreteTensionScope = row.Kind switch
            {
                "Буй" => "нет, граничное условие",
                "Якорь" => "нет, граничное условие",
                "Линия" => "как распределённый участок",
                _ => "да, как локальная нагрузка по s"
            };
            var alternativeShapeScope = row.Kind switch
            {
                "Буй" => "граничный узел",
                "Якорь" => "граничный узел",
                "Линия" => "да, через новые углы",
                _ => "да, влияет через натяжения и имеет X/Z-точку"
            };
            var discreteNodeScope = row.Kind switch
            {
                "Буй" => "граничный узел",
                "Якорь" => "граничный узел",
                "Линия" => "не точечный элемент",
                _ => "да, отдельная X/Z-точка"
            };
            var note = row.Kind switch
            {
                "Линия" => "используется в SegmentRows, ShapeRows, shape-based силах, натяжениях и v0.39 feedback-цикле",
                "Буй" => "задаёт верхнее граничное условие и плавучесть",
                "Якорь" => "задаёт нижнее граничное условие и удержание",
                _ => "участвует как дискретная нагрузка по s в альтернативной форме и v0.39 feedback-цикле"
            };
            sb.AppendLine($"| {row.Number} | {Escape(row.Title)} | {Escape(row.Kind)} | да | да | да | {discreteTensionScope} | {alternativeShapeScope} | {discreteNodeScope} | {shapeScope} | {tensionScope} | {Escape(note)} |");
        }
        sb.AppendLine();
    }

    private static void AppendSegmentRows(StringBuilder sb, CalculationResult result)
    {
        if (result.SegmentRows.Count == 0) return;
        sb.AppendLine("## Расчётные сегменты линии");
        sb.AppendLine($"Линия разбита на {result.SegmentRows.Count} сегментов. В таблице показаны первые 40 и последние 40 сегментов, чтобы были видны верхние и нижние участки линии.");
        sb.AppendLine();
        sb.AppendLine("| № | Элемент | Пресет | s0, м | s1, м | L, м | z, м | U | V | W | |Uгор| | ρ | A, м² | Cd | Сила, Н |");
        sb.AppendLine("|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var row in SampleRows(result.SegmentRows, 40, 40))
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
        sb.AppendLine($"Расчёт ведётся снизу вверх: от якоря к бую. Показаны первые 40 и последние 40 сегментов.");
        sb.AppendLine($"Максимальное оценочное натяжение линии: {maxTension.TensionKn:0.####} кН на сегменте №{maxTension.Number}.");
        sb.AppendLine();
        sb.AppendLine("| № | Элемент | z, м | L, м | Вес в воде, кг | Fтек, Н | ΣFгор, Н | ΣFверт, Н | T, кН | Угол от вертикали, ° | Статус |");
        sb.AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(rows, 40, 40))
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
        sb.AppendLine($"Показаны первые 45 и последние 45 узлов из {shape.Nodes.Count}.");
        sb.AppendLine();
        sb.AppendLine("| Узел | Сегмент | Элемент | s, м | X, м | Z, м | Lсег, м | Угол, ° | T, кН | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(shape.Nodes, 45, 45))
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
        foreach (var row in SampleRows(projection.Rows, 45, 45))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.Label)} | {row.SegmentLengthM:0.####} | {row.DeltaXM:0.####} | {row.DeltaZM:0.####} | {row.ProjectedLengthM:0.####} | {row.LengthResidualM:0.####} | {row.AngleFromVerticalDeg:0.####} | {row.TensionKn:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(projection.MethodNote);
        sb.AppendLine();
    }

    private static IEnumerable<T> SampleRows<T>(IReadOnlyList<T> rows, int firstCount, int lastCount)
    {
        if (rows.Count <= firstCount + lastCount)
        {
            return rows;
        }

        return rows.Take(firstCount).Concat(rows.Skip(rows.Count - lastCount));
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
