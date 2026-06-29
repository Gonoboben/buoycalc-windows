using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Services;

public enum MooringPrimaryShapeGateDecision
{
    KeepCurrentMainShape,
    CandidateReadyForPrimary,
    CandidateRejected
}

public sealed record MooringPrimaryShapeGateResult(
    MooringPrimaryShapeGateDecision Decision,
    bool CandidateAvailable,
    bool CandidateCanBecomePrimary,
    bool SolverConverged,
    bool SolverDiverged,
    bool HasFinalShape,
    bool HasEnoughNodes,
    bool StopReasonAllowsPrimary,
    double OffsetDifferenceM,
    double MaxNodeDeltaM,
    double FinalGeometryResidualM,
    string DecisionText,
    string MethodNote,
    IReadOnlyList<string> Reasons);

public static class MooringPrimaryShapeGate
{
    public static MooringPrimaryShapeGateResult Evaluate(
        MooringShapeResult currentMainShape,
        MooringIterativeSolverResult iterativeSolver)
    {
        var reasons = new List<string>();
        var finalShape = iterativeSolver.FinalShape;
        var hasFinalShape = finalShape is not null;
        var hasEnoughNodes = hasFinalShape && finalShape!.Nodes.Count >= 2;
        var candidateAvailable = hasFinalShape && hasEnoughNodes;
        var stopReasonAllowsPrimary = iterativeSolver.StopReason == MooringIterativeSolverStopReason.Converged;
        var offsetDifferenceM = hasFinalShape ? finalShape!.HorizontalOffsetM - currentMainShape.HorizontalOffsetM : 0;
        var maxNodeDeltaM = iterativeSolver.FinalMaxNodeDeltaM;
        var finalGeometryResidualM = iterativeSolver.FinalGeometryResidualM;

        if (!hasFinalShape)
        {
            reasons.Add("Нет финальной кандидатной формы итерационного solver.");
        }

        if (!hasEnoughNodes)
        {
            reasons.Add("Кандидатная форма содержит меньше двух узлов.");
        }

        if (!iterativeSolver.Converged)
        {
            reasons.Add($"Итерационный solver не сошёлся: {iterativeSolver.StopReasonText}");
        }

        if (iterativeSolver.Diverged)
        {
            reasons.Add("Сработала защита от расходимости.");
        }

        if (!stopReasonAllowsPrimary)
        {
            reasons.Add($"Причина остановки не допускает автоматическое использование формы как основной: {iterativeSolver.StopReason}.");
        }

        var candidateCanBecomePrimary = candidateAvailable &&
            iterativeSolver.Converged &&
            !iterativeSolver.Diverged &&
            stopReasonAllowsPrimary;

        var decision = candidateCanBecomePrimary
            ? MooringPrimaryShapeGateDecision.CandidateReadyForPrimary
            : candidateAvailable
                ? MooringPrimaryShapeGateDecision.CandidateRejected
                : MooringPrimaryShapeGateDecision.KeepCurrentMainShape;

        if (candidateCanBecomePrimary)
        {
            reasons.Add("Кандидатная форма прошла gate v0.39.3 и может быть использована в v0.40 как основная только при явном подключении новой ветки solver.");
        }
        else
        {
            reasons.Add("Основная форма должна остаться от MooringShapeSolver; v0.39.3 ничего не переключает автоматически.");
        }

        return new MooringPrimaryShapeGateResult(
            decision,
            candidateAvailable,
            candidateCanBecomePrimary,
            iterativeSolver.Converged,
            iterativeSolver.Diverged,
            hasFinalShape,
            hasEnoughNodes,
            stopReasonAllowsPrimary,
            offsetDifferenceM,
            maxNodeDeltaM,
            finalGeometryResidualM,
            DescribeDecision(decision),
            "v0.39.3: добавлен gate перед v0.40. Он оценивает, можно ли кандидатную форму итерационного solver вообще рассматривать как основную, но сам не меняет основной MooringShapeSolver и не переключает 2D/PDF.",
            reasons);
    }

    private static string DescribeDecision(MooringPrimaryShapeGateDecision decision)
    {
        return decision switch
        {
            MooringPrimaryShapeGateDecision.CandidateReadyForPrimary => "Кандидатная форма готова для безопасного подключения в основной solver на этапе v0.40.",
            MooringPrimaryShapeGateDecision.CandidateRejected => "Кандидатная форма есть, но gate запрещает использовать её как основную.",
            _ => "Оставить текущую основную форму MooringShapeSolver."
        };
    }
}
