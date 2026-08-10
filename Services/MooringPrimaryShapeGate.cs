using System.Collections.Generic;

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

public sealed record MooringPrimaryShapeSelectionResult(
    MooringShapeResult Shape,
    MooringPrimaryShapeGateResult Gate,
    bool UsesDiscreteLoads,
    string Source,
    string MethodNote);

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
            reasons.Add("Кандидатная форма прошла gate и может быть использована как основная.");
        }
        else
        {
            reasons.Add("Основная форма должна остаться от MooringShapeSolver; gate запрещает автоматическое переключение.");
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
            "Gate оценивает, можно ли форму с дискретными нагрузками использовать как основную. Если gate не пройден, основной формой остаётся fallback MooringShapeSolver.",
            reasons);
    }

    private static string DescribeDecision(MooringPrimaryShapeGateDecision decision)
    {
        return decision switch
        {
            MooringPrimaryShapeGateDecision.CandidateReadyForPrimary => "Кандидатная форма готова для использования как основная.",
            MooringPrimaryShapeGateDecision.CandidateRejected => "Кандидатная форма есть, но gate запрещает использовать её как основную.",
            _ => "Оставить текущую основную форму MooringShapeSolver."
        };
    }
}

public static class MooringPrimaryShapeSelector
{
    public static MooringPrimaryShapeSelectionResult Select(
        MooringShapeResult fallbackShape,
        MooringIterativeSolverResult iterativeSolver)
    {
        var gate = MooringPrimaryShapeGate.Evaluate(fallbackShape, iterativeSolver);
        if (gate.CandidateCanBecomePrimary && iterativeSolver.FinalShape is not null)
        {
            var promotedShape = iterativeSolver.FinalShape with
            {
                MethodNote = "Эта форма выбрана как основная через MooringPrimaryShapeSelector, потому что она включает дискретные нагрузки и прошла MooringPrimaryShapeGate. " + iterativeSolver.FinalShape.MethodNote,
                ConvergenceCriterion = "primary shape: gate=CandidateReadyForPrimary; " + iterativeSolver.ConvergenceCriterion
            };

            return new MooringPrimaryShapeSelectionResult(
                promotedShape,
                gate,
                true,
                "MooringIterativeSolver.FinalShape",
                "Основная форма выбрана из итерационного solver с дискретными нагрузками. MooringShapeSolver сохраняется как fallback.");
        }

        return new MooringPrimaryShapeSelectionResult(
            fallbackShape,
            gate,
            false,
            "MooringShapeSolver fallback",
            "Кандидатная форма с дискретными нагрузками не стала основной, поэтому используется fallback-форма MooringShapeSolver.");
    }
}
