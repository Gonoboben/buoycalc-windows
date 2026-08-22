using System;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Physical location that governs the selected quasi-static design-tension demand.
/// </summary>
public enum MooringDesignTensionLocationKind
{
    Surface,
    AnchorEnd,
    Midpoint
}

/// <summary>
/// Typed selected design-tension authority for an Accepted signed-selected design envelope.
/// This state is intentionally separate from the legacy CalculationResult.TensionKn compatibility scalar.
/// </summary>
public sealed record MooringSelectedDesignTensionDemandState(
    MooringShapeSourceIdentity SourceIdentity,
    double DemandN,
    double DemandKn,
    MooringDesignTensionLocationKind LocationKind,
    int? SegmentNumber,
    string? SourceElement,
    double AlongLineM,
    double WaveHorizontalIncrementN,
    string MethodNote);

public static class MooringSelectedDesignTensionDemandProjector
{
    private const double ForceEpsilonN = 1e-9;

    public static MooringSelectedDesignTensionDemandState? Project(
        MooringSelectedDesignEnvelopeState? envelope)
    {
        if (envelope is null)
            return null;

        if (envelope.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback)
        {
            throw new InvalidOperationException(
                "Selected design-tension authority requires SignedBoundaryFeedback source identity.");
        }

        if (!double.IsFinite(envelope.WaveHorizontalIncrementN) || envelope.WaveHorizontalIncrementN < 0.0)
        {
            throw new InvalidOperationException(
                "Selected design-tension authority requires a finite non-negative wave increment.");
        }

        if (envelope.MidpointRows.Count == 0)
        {
            throw new InvalidOperationException(
                "Selected design-tension authority requires at least one validated design-envelope midpoint row.");
        }

        var maxMidpoint = envelope.MidpointRows
            .OrderByDescending(x => x.DesignMidTensionN)
            .ThenBy(x => x.SegmentNumber)
            .First();

        if (maxMidpoint.SegmentNumber != envelope.MaxDesignMidpointSegmentNumber ||
            maxMidpoint.DesignMidTensionN != envelope.MaxDesignMidpointTensionN)
        {
            throw new InvalidOperationException(
                "Selected design-tension authority requires internally consistent max-midpoint envelope identity.");
        }

        var anchorAlongLineM = envelope.MidpointRows
            .OrderBy(x => x.EndLengthM)
            .Last()
            .EndLengthM;

        var candidates = new[]
        {
            new Candidate(
                MooringDesignTensionLocationKind.Surface,
                envelope.SurfaceDesignTensionN,
                null,
                null,
                0.0),
            new Candidate(
                MooringDesignTensionLocationKind.AnchorEnd,
                envelope.AnchorDesignTensionN,
                null,
                null,
                anchorAlongLineM),
            new Candidate(
                MooringDesignTensionLocationKind.Midpoint,
                maxMidpoint.DesignMidTensionN,
                maxMidpoint.SegmentNumber,
                maxMidpoint.SourceElement,
                maxMidpoint.MidLengthM)
        };

        foreach (var candidate in candidates)
        {
            if (!double.IsFinite(candidate.TensionN) || candidate.TensionN <= ForceEpsilonN ||
                !double.IsFinite(candidate.AlongLineM) || candidate.AlongLineM < 0.0)
            {
                throw new InvalidOperationException(
                    $"Selected design-tension authority has invalid {candidate.LocationKind} demand metadata.");
            }
        }

        var governing = candidates
            .OrderByDescending(x => x.TensionN)
            .ThenBy(x => TieOrder(x.LocationKind))
            .First();

        return new MooringSelectedDesignTensionDemandState(
            envelope.SourceIdentity,
            governing.TensionN,
            governing.TensionN / 1000.0,
            governing.LocationKind,
            governing.SegmentNumber,
            governing.SourceElement,
            governing.AlongLineM,
            envelope.WaveHorizontalIncrementN,
            "Selected v1 quasi-static wave-aware design-tension authority: maximum of validated surface, anchor-end and local midpoint design-envelope resultants. Legacy CalculationResult.TensionKn remains a separate compatibility scalar; no dynamic-wave claim and no geometry feedback.");
    }

    // F1-C used ordinal location labels for deterministic equal-resultant evidence.
    // Preserve the equivalent order here: AnchorEnd, Midpoint, Surface.
    private static int TieOrder(MooringDesignTensionLocationKind kind) => kind switch
    {
        MooringDesignTensionLocationKind.AnchorEnd => 0,
        MooringDesignTensionLocationKind.Midpoint => 1,
        MooringDesignTensionLocationKind.Surface => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private sealed record Candidate(
        MooringDesignTensionLocationKind LocationKind,
        double TensionN,
        int? SegmentNumber,
        string? SourceElement,
        double AlongLineM);
}
