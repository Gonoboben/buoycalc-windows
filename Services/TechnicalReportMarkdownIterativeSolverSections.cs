using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

internal static class TechnicalReportMarkdownIterativeSolverSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        switch (methodName)
        {
            case "AppendIterativeSolverRows":
                AppendIterativeSolverRows((StringBuilder)args[0], (MooringIterativeSolverResult)args[1]);
                return true;
            default:
                return false;
        }
    }

    private static void AppendIterativeSolverRows(StringBuilder sb, MooringIterativeSolverResult solver)
    {
        sb.AppendLine("## Итерационный solver — итерации и кандидатная форма");
        sb.AppendLine("Раздел показывает feedback-цикл: форма → силы по форме → натяжения → дискретные нагрузки → новая форма → проверка сходимости. Финальная форма является кандидатом: при выполнении критериев и прохождении MooringPrimaryShapeGate она может стать основной; иначе сохраняется fallback MooringShapeSolver.");
        sb.AppendLine($"Критерий: {solver.ConvergenceCriterion}");
        sb.AppendLine($"Итог: {(solver.Converged ? "сошёлся" : "не сошёлся")}; {solver.StopReasonText} Итераций={solver.IterationCount}; финальная ΔX={solver.FinalOffsetChangeM:0.####} м; финальный max Δузла={solver.FinalMaxNodeDeltaM:0.####} м; финальная невязка Z={solver.FinalGeometryResidualM:0.####} м; divergence={(solver.Diverged ? "YES" : "NO")}.");
        sb.AppendLine();

        if (solver.Rows.Count == 0)
        {
            sb.AppendLine(solver.MethodNote);
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Итерация | X вход, м | F shape, Н | T shape top, кН | T дискр. top, кН | X новая, м | ΔX, м | max Δузла, м | невязка Z, м | Геометрия | Статус |");
        sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");
        foreach (var row in solver.Rows)
        {
            sb.AppendLine($"| {row.IterationNumber} | {row.InputOffsetM:0.####} | {row.ShapeLineForceN:0.####} | {row.TopShapeTensionKn:0.####} | {row.TopDiscreteTensionKn:0.####} | {row.OutputOffsetM:0.####} | {row.OffsetChangeM:0.####} | {row.MaxNodeDeltaM:0.####} | {row.GeometryResidualM:0.####} | {(row.GeometryConverged ? "OK" : "WARNING")} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(solver.MethodNote);
        sb.AppendLine();
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
