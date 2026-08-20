namespace BuoyCalc.Windows.Services;

/// <summary>
/// Typed calculation-core arbitration between the current selected geometry and the
/// signed boundary-feedback candidate. This service selects geometry/source identity only;
/// downstream scalar tension, anchor, verdict and other force authority remain unchanged.
/// </summary>
public static class MooringSelectedShapeArbitrator
{
    public static MooringSelectedShapeResult? Arbitrate(
        MooringSelectedShapeResult? currentSelected,
        MooringSignedCandidateResult signedCandidate)
    {
        ArgumentNullException.ThrowIfNull(signedCandidate);

        if (signedCandidate.Status != MooringSignedCandidateStatus.Accepted)
            return currentSelected;

        var signedShape = signedCandidate.Shape
            ?? throw new InvalidOperationException(
                "Accepted signed candidate must carry a shape before selected-core arbitration.");

        return MooringSelectedShapeResult.Create(
            signedShape,
            signedCandidate.SourceIdentity,
            selectedConverged: signedCandidate.ExactFixedPointReached,
            selectedUsesDiscreteLoads: signedCandidate.ContainsDiscreteLoads,
            "Typed geometry/source arbitration selected Accepted SignedBoundaryFeedback; downstream scalar-force authority remains unchanged.");
    }
}
