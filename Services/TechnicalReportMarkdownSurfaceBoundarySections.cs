using System.Text;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownSurfaceBoundarySections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        if (methodName != "AppendSurfaceBoundaryInfo")
        {
            return false;
        }

        AppendSurfaceBoundaryInfo(
            (StringBuilder)args[0],
            (MooringSurfaceBoundaryInfoResult)args[1]);
        return true;
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
