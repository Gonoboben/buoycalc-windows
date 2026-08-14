using System.Linq;
using System.Text;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownSurfaceBoundarySections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        if (methodName == "AppendSurfaceBoundaryInfo")
        {
            AppendSurfaceBoundaryInfo(
                (StringBuilder)args[0],
                (MooringSurfaceBoundaryInfoResult)args[1]);
            return true;
        }

        if (methodName == "AppendSurfaceBoundaryTensionTrace")
        {
            AppendSurfaceBoundaryTensionTrace(
                (StringBuilder)args[0],
                (MooringSurfaceBoundaryTensionTraceResult)args[1]);
            return true;
        }

        return false;
    }

    private static void AppendSurfaceBoundaryInfo(
        StringBuilder sb,
        MooringSurfaceBoundaryInfoResult diagnostic)
    {
        sb.AppendLine("## Поверхностная вертикальная граница буя — frozen-load INFO");
        sb.AppendLine("INFO-only read model. Renderer только отображает уже рассчитанный результат и не выполняет shooting, интегрирование или пересчёт нагрузок.");
        sb.AppendLine("Диагностические X/Z и Q0 не участвуют в solver feedback, MooringPrimaryShapeGate, selected X/Z, verdict, anchor reserve, weak-link checks или геометрии 2D/PDF.");
        sb.AppendLine();
        sb.AppendLine($"- Классификация: {DisplayClassification(diagnostic.Classification)} ({diagnostic.Classification})");
        sb.AppendLine($"- Доступно: {(diagnostic.Available ? "да" : "нет")}");
        sb.AppendLine($"- Решение Q0 найдено: {(diagnostic.Solved ? "да" : "нет")}");
        sb.AppendLine($"- Метод: {Escape(diagnostic.MethodNote)}");

        if (!diagnostic.Available)
        {
            sb.AppendLine();
            return;
        }

        AppendValue(sb, "Проектная глубина, м", diagnostic.TargetDepthM);
        AppendValue(sb, "Длина линии, м", diagnostic.LineLengthM);
        AppendValue(sb, "Стационарная сила течения на буе D_b, Н", diagnostic.BuoySteadyDragN);
        AppendValue(sb, "Предельный Q_capacity, Н", diagnostic.QCapacityN);
        AppendValue(sb, "Q0, Н", diagnostic.Q0N);
        AppendValue(sb, "Q0 / Q_capacity", diagnostic.Q0CapacityRatio);
        AppendValue(sb, "B_actual / B_max", diagnostic.ActualBuoyancyRatio);
        AppendValue(sb, "Невязка Z при Q0=0, м", diagnostic.LowerResidualM);
        AppendValue(sb, "Невязка Z при Q0=Q_capacity, м", diagnostic.CapacityResidualM);
        AppendValue(sb, "Минимальный Q для строго нисходящей вертикальной геометрии, Н", diagnostic.MinimumQForDownwardVerticalGeometryN);
        sb.AppendLine($"- Корень заключён в [0, Q_capacity]: {(diagnostic.RootBracketed ? "да" : "нет")}");
        sb.AppendLine($"- Контроль монотонности: {(diagnostic.MonotoneSample ? "пройден" : "не пройден")}");
        sb.AppendLine($"- Итераций bounded search: {diagnostic.Iterations}");

        if (diagnostic.SolutionState is not null)
        {
            var state = diagnostic.SolutionState;
            sb.AppendLine($"- Диагностический конец X, м: {state.EndpointXM:0.####}");
            sb.AppendLine($"- Диагностический конец Z, м: {state.EndpointZM:0.####}");
            sb.AppendLine($"- H на конце, Н: {state.EndHN:0.####}");
            sb.AppendLine($"- V на конце, Н: {state.EndVN:0.####}");
            sb.AppendLine($"- Диапазон H, Н: {state.MinHN:0.####} … {state.MaxHN:0.####}");
            sb.AppendLine($"- Диапазон V, Н: {state.MinVN:0.####} … {state.MaxVN:0.####}");
            sb.AppendLine($"- V меняет знак: {(state.VSignChange ? "да" : "нет")}");
            sb.AppendLine($"- Пересечений дискретных point-load: {state.PointLoadCrossings}");
            sb.AppendLine($"- Неопределённых сегментов: {state.IndeterminateSegmentCount}");
        }

        sb.AppendLine();
    }

    private static void AppendSurfaceBoundaryTensionTrace(
        StringBuilder sb,
        MooringSurfaceBoundaryTensionTraceResult trace)
    {
        sb.AppendLine("## Boundary-conditioned trace натяжения — INFO");
        sb.AppendLine("Renderer отображает только сохранённый passive read model и не вызывает integration kernel, SurfaceBoundaryInfoAnalyzer или tension-trace builder.");
        sb.AppendLine($"- Доступно: {(trace.Available ? "да" : "нет")}");
        sb.AppendLine($"- Parent classification: {trace.ParentClassification}");
        sb.AppendLine($"- Метод: {Escape(trace.MethodNote)}");

        if (!trace.Available)
        {
            sb.AppendLine($"- Причина недоступности: {Escape(trace.UnavailableReason)}");
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"- Сегментов trace: {trace.Rows.Count}");
        sb.AppendLine($"- Пересечений внутренних point-load: {trace.PointLoadCrossings}");
        sb.AppendLine($"- Неопределённых сегментов: {trace.IndeterminateSegmentCount}");
        AppendValue(sb, "H на поверхности, Н", trace.StartHN);
        AppendValue(sb, "V на поверхности, Н", trace.StartVN);
        AppendValue(sb, "H на якорной стороне, Н", trace.EndHN);
        AppendValue(sb, "V на якорной стороне, Н", trace.EndVN);

        if (trace.Rows.Count == 0)
        {
            sb.AppendLine();
            return;
        }

        var maxTension = trace.Rows.OrderByDescending(x => x.MidTensionN).First();
        sb.AppendLine($"- Макс. midpoint tension: {maxTension.MidTensionN:0.####} Н, сегмент №{maxTension.SegmentNumber}, s≈{maxTension.MidLengthM:0.####} м");
        sb.AppendLine();
        sb.AppendLine("Реперные строки trace (0 / 25 / 50 / 75 / 100% по списку сегментов):");
        sb.AppendLine("| Сегмент | s mid, м | z source, м | H mid, Н | V mid, Н | |T| mid, Н | tx | tz | signed angle от +Z, ° | crossed point-load |");
        sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var row in RepresentativeRows(trace))
        {
            sb.AppendLine($"| {row.SegmentNumber} | {row.MidLengthM:0.####} | {row.EstimatedDepthM:0.####} | {row.MidHN:0.####} | {row.MidVN:0.####} | {row.MidTensionN:0.####} | {FormatNullable(row.TangentX)} | {FormatNullable(row.TangentZ)} | {FormatNullable(row.SignedAngleFromDownwardVerticalDeg)} | {row.PointLoadCrossingsAppliedBeforeSegment} |");
        }
        sb.AppendLine();
    }

    private static MooringSurfaceBoundaryTensionTraceRow[] RepresentativeRows(
        MooringSurfaceBoundaryTensionTraceResult trace)
    {
        var count = trace.Rows.Count;
        var indices = new[]
        {
            0,
            (int)System.Math.Round((count - 1) * 0.25),
            (int)System.Math.Round((count - 1) * 0.50),
            (int)System.Math.Round((count - 1) * 0.75),
            count - 1
        };
        return indices.Distinct().Select(index => trace.Rows[index]).ToArray();
    }

    private static string FormatNullable(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.####") : "н/д";
    }

    private static void AppendValue(StringBuilder sb, string label, double? value)
    {
        sb.AppendLine(value.HasValue
            ? $"- {label}: {value.Value:0.####}"
            : $"- {label}: н/д");
    }

    private static string DisplayClassification(MooringSurfaceBoundaryInfoClassification classification)
    {
        return classification switch
        {
            MooringSurfaceBoundaryInfoClassification.UnavailableMissingBuoyInput => "нет типизированного BuoyInput",
            MooringSurfaceBoundaryInfoClassification.UnavailableMissingBoundaryRows => "нет граничных строк последовательности",
            MooringSurfaceBoundaryInfoClassification.InvalidInput => "некорректные входные данные",
            MooringSurfaceBoundaryInfoClassification.LineShorterThanDepth => "линия короче глубины",
            MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot => "натянутая линия с горизонтальной нагрузкой: конечного точного корня нет",
            MooringSurfaceBoundaryInfoClassification.VerticalGeometryBoundaryNonUnique => "вертикальная taut-геометрия не определяет Q0 однозначно",
            MooringSurfaceBoundaryInfoClassification.VerticalGeometryCapacityInsufficient => "ёмкости плавучести недостаточно для вертикальной taut-границы",
            MooringSurfaceBoundaryInfoClassification.IndeterminateEndpointState => "неопределённое граничное состояние",
            MooringSurfaceBoundaryInfoClassification.NonMonotoneDepthResponse => "контрольная зависимость Z(Q0) не монотонна",
            MooringSurfaceBoundaryInfoClassification.SolvedAtLowerBoundary => "решение на Q0=0",
            MooringSurfaceBoundaryInfoClassification.SolvedAtCapacityBoundary => "решение на Q0=Q_capacity",
            MooringSurfaceBoundaryInfoClassification.NoRootRequiresNegativeQ0 => "для корня потребовался бы отрицательный Q0",
            MooringSurfaceBoundaryInfoClassification.InsufficientBuoyancyCapacity => "недостаточная предельная плавучесть",
            MooringSurfaceBoundaryInfoClassification.NoRootUnclassified => "корень в допустимом диапазоне не найден",
            MooringSurfaceBoundaryInfoClassification.IndeterminateDuringRootSearch => "неопределённое состояние при bounded search",
            MooringSurfaceBoundaryInfoClassification.BracketedButDepthToleranceNotReached => "корень заключён, но допуск по глубине не достигнут",
            MooringSurfaceBoundaryInfoClassification.SolvedByBoundedBisection => "решено bounded bisection",
            _ => classification.ToString()
        };
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
