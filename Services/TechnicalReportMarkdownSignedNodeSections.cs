using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownSignedNodeSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        if (methodName == "AppendSignedNodeEquilibriumRows")
        {
            AppendSignedNodeEquilibriumRows(
                (StringBuilder)args[0],
                (MooringSignedNodeEquilibriumResult)args[1],
                "## Signed-равновесие внутренних дискретных узлов",
                "Candidate B — информационная signed X/Z free-body диагностика внутренних сгруппированных дискретных узлов pre-iterative candidate-формы с дискретными нагрузками. Раздел только отображает уже рассчитанный MooringSignedNodeEquilibriumResult.",
                "Это не selected-shape equilibrium: верхний узел буя и нижний узел якоря не считаются решёнными free body без соответствующих граничных реакций. Инженерный pass/fail threshold не утверждён; диагностика не влияет на solver convergence, MooringPrimaryShapeGate, selected X/Z, CalculationResult verdict, anchor reserve или weak-link checks.");
            return true;
        }

        if (methodName == "AppendFinalIterationSignedNodeEquilibriumUnavailable")
        {
            AppendFinalIterationSignedNodeEquilibriumUnavailable((StringBuilder)args[0]);
            return true;
        }

        if (methodName == "AppendFinalIterationSignedNodeEquilibriumRows")
        {
            AppendFinalIterationSignedNodeEquilibriumRows(
                (StringBuilder)args[0],
                (MooringSignedNodeEquilibriumResult)args[1]);
            return true;
        }

        return false;
    }

    private static void AppendFinalIterationSignedNodeEquilibriumUnavailable(StringBuilder sb)
    {
        sb.AppendLine("## Signed-равновесие внутренних узлов — финальная итерационная кандидатная форма");
        sb.AppendLine("Final-iteration Candidate B недоступен: итерационный solver не опубликовал retained same-iteration discrete tension/shape state. Нулевой residual не синтезируется.");
        sb.AppendLine("Это не влияет на solver convergence, stop reason, MooringPrimaryShapeGate, selected X/Z, CalculationResult verdict, anchor reserve или weak-link checks.");
        sb.AppendLine();
    }

    private static void AppendFinalIterationSignedNodeEquilibriumRows(
        StringBuilder sb,
        MooringSignedNodeEquilibriumResult equilibrium)
    {
        AppendSignedNodeEquilibriumRows(
            sb,
            equilibrium,
            "## Signed-равновесие внутренних узлов — финальная итерационная кандидатная форма",
            "Final-iteration Candidate B — информационная signed X/Z free-body диагностика внутренних сгруппированных дискретных узлов, рассчитанная ранее из retained same-iteration FinalDiscreteLoadTensions + FinalDiscreteLoadShape. Markdown не перестраивает force/shape state и не пересчитывает residual.",
            "Это диагностика финальной iterative candidate, а не автоматически selected-shape equilibrium: MooringPrimaryShapeGate может принять или отклонить candidate. Инженерный pass/fail threshold не утверждён; результат не влияет на solver convergence, stop reason, gate, selected X/Z, CalculationResult verdict, anchor reserve или weak-link checks.");
    }

    private static void AppendSignedNodeEquilibriumRows(
        StringBuilder sb,
        MooringSignedNodeEquilibriumResult equilibrium,
        string heading,
        string stateDescription,
        string boundaryDescription)
    {
        sb.AppendLine(heading);
        sb.AppendLine(stateDescription);
        sb.AppendLine(boundaryDescription);
        sb.AppendLine();
        sb.AppendLine($"- Сгруппированных внутренних узлов: {equilibrium.NodeCount}");
        sb.AppendLine($"- Доступных residual: {equilibrium.AvailableNodeCount}");
        sb.AppendLine($"- Неопределённых residual: {equilibrium.IndeterminateNodeCount}");
        sb.AppendLine($"- Max R: {Format(equilibrium.MaxResidualN)} Н");
        sb.AppendLine($"- Max R_rel: {Format(equilibrium.MaxRelativeResidual)}");
        sb.AppendLine($"- Худший узел по R_rel: {Format(equilibrium.WorstNodeNumber)}");
        sb.AppendLine();

        if (equilibrium.Rows.Count == 0)
        {
            sb.AppendLine("Внутренние сгруппированные дискретные узлы для этого Candidate B state отсутствуют.");
            sb.AppendLine();
            sb.AppendLine(equilibrium.MethodNote);
            sb.AppendLine();
            return;
        }

        sb.AppendLine($"Показаны первые 40 и последние 40 строк из {equilibrium.Rows.Count}; residual в renderer не пересчитывается.");
        sb.AppendLine();
        sb.AppendLine("| № | s, м | Источники | n | Fx узла, Н | Fz узла, Н | seg↑ | seg↓ | t↑x | t↑z | t↓x | t↓z | H incl, Н | V incl, Н | H below, Н | V below, Н | T↑, Н | T↓, Н | R_x, Н | R_z, Н | R, Н | R_rel | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(equilibrium.Rows, 40, 40))
        {
            sb.AppendLine(
                $"| {row.Number} | {row.PositionAlongLineM:0.####} | {Escape(row.SourceElements)} | {row.SourceElementCount} | {row.NodeForceXN:0.####} | {row.NodeForceZN:0.####} | {Format(row.UpperSegmentNumber)} | {Format(row.LowerSegmentNumber)} | {Format(row.UpperTangentX)} | {Format(row.UpperTangentZ)} | {Format(row.LowerTangentX)} | {Format(row.LowerTangentZ)} | {Format(row.InclusiveHorizontalForceN)} | {Format(row.InclusiveVerticalForceN)} | {Format(row.BelowHorizontalForceN)} | {Format(row.BelowVerticalForceN)} | {Format(row.UpperTensionN)} | {Format(row.LowerTensionN)} | {Format(row.ResidualXN)} | {Format(row.ResidualZN)} | {Format(row.ResidualN)} | {Format(row.RelativeResidual)} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(equilibrium.MethodNote);
        sb.AppendLine();
    }

    private static IReadOnlyList<T> SampleRows<T>(IReadOnlyList<T> rows, int firstCount, int lastCount)
    {
        if (rows.Count <= firstCount + lastCount)
        {
            return rows;
        }

        return rows.Take(firstCount).Concat(rows.Skip(rows.Count - lastCount)).ToList();
    }

    private static string Format(double? value)
    {
        return value.HasValue && double.IsFinite(value.Value)
            ? value.Value.ToString("0.####")
            : "—";
    }

    private static string Format(int? value)
    {
        return value?.ToString() ?? "—";
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
