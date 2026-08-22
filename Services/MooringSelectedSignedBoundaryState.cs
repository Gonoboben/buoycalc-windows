using System;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Read-only direct boundary state for a SignedBoundaryFeedback shape that has
/// actually been selected. This contract copies only values already carried by
/// the accepted signed candidate; it does not reconstruct a tension trace and
/// does not replace CalculationResult scalar authority.
/// </summary>
public sealed record MooringSelectedSignedBoundaryState(
    MooringShapeSourceIdentity SourceIdentity,
    MooringSurfaceBoundaryInfoClassification BoundaryClassification,
    double Q0N,
    double BuoySteadyDragN,
    double EndpointXM,
    double EndpointZM,
    double EndHN,
    double EndVN,
    double MinHN,
    double MaxHN,
    double MinVN,
    double MaxVN,
    bool VSignChange,
    int PointLoadCrossings,
    int FeedbackIterations,
    bool ContainsDiscreteLoads,
    string BoundaryMethodNote,
    string CandidateDiagnosticCode,
    string CandidateDiagnosticText);

public static class MooringSelectedSignedBoundaryStateProjector
{
    public static MooringSelectedSignedBoundaryState? Project(
        MooringSelectedShapeResult? selectedCore,
        MooringSignedCandidateResult signedCandidate)
    {
        ArgumentNullException.ThrowIfNull(signedCandidate);

        if (selectedCore is null ||
            selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback)
        {
            return null;
        }

        if (signedCandidate.Status != MooringSignedCandidateStatus.Accepted ||
            signedCandidate.Shape is null ||
            signedCandidate.Boundary is null)
        {
            throw new InvalidOperationException(
                "Selected SignedBoundaryFeedback requires the same Accepted signed candidate and its boundary state.");
        }

        if (!ReferenceEquals(selectedCore.Shape, signedCandidate.Shape))
        {
            throw new InvalidOperationException(
                "Selected SignedBoundaryFeedback shape must be the exact Accepted signed-candidate shape.");
        }

        if (!selectedCore.SelectedConverged ||
            !signedCandidate.ExactFixedPointReached ||
            selectedCore.SelectedUsesDiscreteLoads != signedCandidate.ContainsDiscreteLoads)
        {
            throw new InvalidOperationException(
                "Selected SignedBoundaryFeedback convergence/discrete-load identity differs from the Accepted candidate.");
        }

        var boundary = signedCandidate.Boundary;
        var solution = boundary.SolutionState;
        if (!boundary.Solved ||
            solution is null ||
            !Finite(boundary.Q0N) ||
            !Finite(boundary.BuoySteadyDragN) ||
            !Finite(solution.EndpointXM) ||
            !Finite(solution.EndpointZM) ||
            !Finite(solution.EndHN) ||
            !Finite(solution.EndVN) ||
            !Finite(solution.MinHN) ||
            !Finite(solution.MaxHN) ||
            !Finite(solution.MinVN) ||
            !Finite(solution.MaxVN))
        {
            throw new InvalidOperationException(
                "Selected SignedBoundaryFeedback requires one complete finite direct solved-boundary force state.");
        }

        if (solution.PointLoadCrossings != signedCandidate.PointLoadCrossings ||
            signedCandidate.ContainsDiscreteLoads != (solution.PointLoadCrossings > 0))
        {
            throw new InvalidOperationException(
                "Selected signed boundary point-load identity differs from the Accepted candidate.");
        }

        var anchor = selectedCore.Shape.AnchorPoint
            ?? throw new InvalidOperationException(
                "Selected SignedBoundaryFeedback shape requires an anchor/end point.");

        if (anchor.XOffsetM != solution.EndpointXM ||
            anchor.ZDepthM != solution.EndpointZM ||
            selectedCore.Shape.HorizontalOffsetM != solution.EndpointXM)
        {
            throw new InvalidOperationException(
                "Selected signed boundary endpoint differs from the selected shape endpoint.");
        }

        return new MooringSelectedSignedBoundaryState(
            selectedCore.SourceIdentity,
            boundary.Classification,
            boundary.Q0N!.Value,
            boundary.BuoySteadyDragN!.Value,
            solution.EndpointXM,
            solution.EndpointZM,
            solution.EndHN,
            solution.EndVN,
            solution.MinHN,
            solution.MaxHN,
            solution.MinVN,
            solution.MaxVN,
            solution.VSignChange,
            solution.PointLoadCrossings,
            signedCandidate.FeedbackIterations,
            signedCandidate.ContainsDiscreteLoads,
            boundary.MethodNote,
            signedCandidate.DiagnosticCode,
            signedCandidate.DiagnosticText);
    }

    private static bool Finite(double? value) =>
        value.HasValue && double.IsFinite(value.Value);

    private static bool Finite(double value) => double.IsFinite(value);
}
