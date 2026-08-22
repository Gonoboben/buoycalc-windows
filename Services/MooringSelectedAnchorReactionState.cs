using System;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum MooringAnchorContactClassification
{
    CompressiveContact,
    ZeroNormalLimit,
    UpliftSeparation
}

/// <summary>
/// Selected quasi-static anchor-boundary reaction/contact state for an Accepted
/// SignedBoundaryFeedback design envelope. This state does not own horizontal
/// holding capacity or legacy anchor reserve.
/// </summary>
public sealed record MooringSelectedAnchorReactionState(
    MooringShapeSourceIdentity SourceIdentity,
    double InternalAnchorEndHN,
    double InternalAnchorEndVN,
    double InternalAnchorEndTensionN,
    double LineOnAnchorHN,
    double LineOnAnchorVDepthPositiveN,
    double HorizontalDemandN,
    double UpwardLinePullN,
    double DownwardLinePushN,
    double AnchorWeightWaterKg,
    double AnchorWeightWaterN,
    double SignedNormalReactionN,
    double CompressiveNormalReactionN,
    double UpliftExcessN,
    MooringAnchorContactClassification ContactClassification,
    string MethodNote);

public static class MooringSelectedAnchorReactionStateProjector
{
    private const double GravityMS2 = 9.80665;
    private const double ForceConsistencyToleranceN = 1e-7;

    public static MooringSelectedAnchorReactionState? Project(
        CalculationResult result,
        MooringSelectedDesignEnvelopeState? envelope)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (envelope is null)
            return null;

        if (envelope.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback)
        {
            throw new InvalidOperationException(
                "Selected anchor-reaction state requires SignedBoundaryFeedback source identity.");
        }

        if (!Finite(envelope.AnchorDesignHN) ||
            !Finite(envelope.AnchorDesignVN) ||
            !Finite(envelope.AnchorDesignTensionN) ||
            envelope.AnchorDesignTensionN <= 0.0)
        {
            throw new InvalidOperationException(
                "Selected anchor-reaction state requires one finite positive anchor-end design resultant.");
        }

        var expectedTensionN = Math.Sqrt(
            envelope.AnchorDesignHN * envelope.AnchorDesignHN +
            envelope.AnchorDesignVN * envelope.AnchorDesignVN);
        if (!Finite(expectedTensionN) ||
            Math.Abs(expectedTensionN - envelope.AnchorDesignTensionN) > ForceConsistencyToleranceN)
        {
            throw new InvalidOperationException(
                "Selected anchor-reaction state requires internally consistent anchor-end H/V/resultant identity.");
        }

        if (!Finite(result.AnchorWeightWaterKg))
        {
            throw new InvalidOperationException(
                "Selected anchor-reaction state requires finite anchor submerged weight.");
        }

        // A non-positive submerged anchor weight cannot provide a compressive
        // seabed-contact state. Preserve the legacy result separately and do not
        // fabricate a selected physical contact authority.
        if (result.AnchorWeightWaterKg <= 0.0)
            return null;

        var anchorWeightWaterN = result.AnchorWeightWaterKg * GravityMS2;
        var lineOnAnchorHN = -envelope.AnchorDesignHN;
        var lineOnAnchorVDepthPositiveN = -envelope.AnchorDesignVN;
        var signedNormalReactionN = anchorWeightWaterN - envelope.AnchorDesignVN;
        var compressiveNormalReactionN = Math.Max(0.0, signedNormalReactionN);
        var upliftExcessN = Math.Max(0.0, -signedNormalReactionN);
        var contactClassification = signedNormalReactionN > 0.0
            ? MooringAnchorContactClassification.CompressiveContact
            : signedNormalReactionN < 0.0
                ? MooringAnchorContactClassification.UpliftSeparation
                : MooringAnchorContactClassification.ZeroNormalLimit;

        return new MooringSelectedAnchorReactionState(
            envelope.SourceIdentity,
            envelope.AnchorDesignHN,
            envelope.AnchorDesignVN,
            envelope.AnchorDesignTensionN,
            lineOnAnchorHN,
            lineOnAnchorVDepthPositiveN,
            Math.Abs(envelope.AnchorDesignHN),
            Math.Max(0.0, envelope.AnchorDesignVN),
            Math.Max(0.0, -envelope.AnchorDesignVN),
            result.AnchorWeightWaterKg,
            anchorWeightWaterN,
            signedNormalReactionN,
            compressiveNormalReactionN,
            upliftExcessN,
            contactClassification,
            "Selected v1 quasi-static anchor-boundary reaction: +Z downward; line-on-anchor is the opposite selected internal anchor-end design vector; seabed normal balance is Wsubmerged - AnchorDesignV. This state does not define soil/anchor horizontal holding capacity or replace legacy AnchorReserve.");
    }

    private static bool Finite(double value) => double.IsFinite(value);
}
