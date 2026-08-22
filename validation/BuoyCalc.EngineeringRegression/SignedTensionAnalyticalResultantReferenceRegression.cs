using System.Globalization;
using System.Reflection;

internal static class SignedTensionAnalyticalResultantReferenceRegression
{
    private const double IdentityToleranceN = 1e-9;

    public static void Validate()
    {
        var reference = InvokePrivate("SolveAnalyticalReference");
        var candidate = InvokePrivate("RunCandidate");

        var referenceQ0N = Property<double>(reference, "Q0N");
        var referenceEndHN = Property<double>(reference, "EndHN");
        var referenceLineForceN = Property<double>(reference, "LineForceN");
        var candidateQ0N = Property<double>(candidate, "Q0N");
        var candidateEndHN = Property<double>(candidate, "EndHN");
        var candidateLineForceN = Property<double>(candidate, "LineForceN");
        var candidateIterations = Property<int>(candidate, "Iterations");
        var candidateStopReason = Property<string>(candidate, "StopReason");
        var candidatePointLoads = Property<int>(candidate, "PointLoadCrossings");
        var candidateNegativeDz = Property<int>(candidate, "NegativeDzSegmentCount");

        PositiveFinite(referenceQ0N, "reference Q0");
        PositiveFinite(referenceEndHN, "reference end H");
        PositiveFinite(candidateQ0N, "candidate Q0");
        PositiveFinite(candidateEndHN, "candidate end H");

        Near(referenceEndHN, referenceLineForceN, IdentityToleranceN, "reference end-H/line-force identity");
        Near(candidateEndHN, candidateLineForceN, IdentityToleranceN, "candidate end-H/line-force identity");

        if (candidateIterations != 64 || !string.Equals(candidateStopReason, "BudgetReached", StringComparison.Ordinal))
            throw new InvalidOperationException("E1-B1: independent-reference candidate no longer completes the frozen 64-iteration validation path.");
        if (candidatePointLoads != 0 || candidateNegativeDz != 0)
            throw new InvalidOperationException("E1-B1: neutral analytical fixture acquired point loads or negative-dz geometry.");

        // Existing independent fixture is deliberately neutral in submerged weight and has
        // zero buoy drag / zero internal point loads. Therefore V is constant along the line:
        // Surface H = 0, Surface V = Q0, Anchor-end H = EndH, Anchor-end V = Q0.
        var referenceSurfaceResultantN = Math.Abs(referenceQ0N);
        var referenceAnchorEndResultantN = Magnitude(referenceEndHN, referenceQ0N);
        var candidateSurfaceResultantN = Math.Abs(candidateQ0N);
        var candidateAnchorEndResultantN = Magnitude(candidateEndHN, candidateQ0N);

        PositiveFinite(referenceSurfaceResultantN, "reference surface resultant");
        PositiveFinite(referenceAnchorEndResultantN, "reference anchor-end resultant");
        PositiveFinite(candidateSurfaceResultantN, "candidate surface resultant");
        PositiveFinite(candidateAnchorEndResultantN, "candidate anchor-end resultant");

        if (referenceAnchorEndResultantN < referenceSurfaceResultantN ||
            candidateAnchorEndResultantN < candidateSurfaceResultantN)
        {
            throw new InvalidOperationException("E1-B1: anchor-end resultant must not be below the surface resultant for the neutral horizontal-drag fixture.");
        }

        var surfaceDeltaN = candidateSurfaceResultantN - referenceSurfaceResultantN;
        var anchorEndDeltaN = candidateAnchorEndResultantN - referenceAnchorEndResultantN;

        Console.WriteLine("E1B1_ANALYTICAL_RESULTANTS_BEGIN");
        Console.WriteLine(string.Join("|",
            "E1B1_ANALYTICAL_RESULTANTS",
            "Fixture=neutral-line-independent-reference",
            $"ReferenceSurfaceResultantN={F(referenceSurfaceResultantN)}",
            $"CandidateSurfaceResultantN={F(candidateSurfaceResultantN)}",
            $"SurfaceDeltaN={F(surfaceDeltaN)}",
            $"SurfaceRelativeDelta={F(Relative(surfaceDeltaN, referenceSurfaceResultantN))}",
            $"ReferenceAnchorEndResultantN={F(referenceAnchorEndResultantN)}",
            $"CandidateAnchorEndResultantN={F(candidateAnchorEndResultantN)}",
            $"AnchorEndDeltaN={F(anchorEndDeltaN)}",
            $"AnchorEndRelativeDelta={F(Relative(anchorEndDeltaN, referenceAnchorEndResultantN))}",
            $"CandidateIterations={candidateIterations}",
            $"CandidateStop={candidateStopReason}",
            "SurfaceLocation=s0-buoy",
            "AnchorEndLocation=sL-anchor",
            "LoadSet=SteadyCurrentWaveExcluded",
            "AcceptanceToleranceIntroduced=False",
            "Authority=EvidenceOnly",
            "ProductionTensionKn=LegacyUnchanged"));
        Console.WriteLine("E1B1_ANALYTICAL_RESULTANTS_END");
    }

    private static object InvokePrivate(string methodName)
    {
        var method = typeof(BoundaryFeedbackIndependentReferenceRegression).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"E1-B1: independent-reference method {methodName} was not found.");
        return method.Invoke(null, null)
            ?? throw new InvalidOperationException($"E1-B1: independent-reference method {methodName} returned null.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"E1-B1: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"E1-B1: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void PositiveFinite(double value, string label)
    {
        if (!double.IsFinite(value) || value <= 0.0)
            throw new InvalidOperationException($"E1-B1 {label}: expected finite positive value, got {value:R}.");
    }

    private static void Near(double actual, double expected, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"E1-B1 {label}: expected {expected:R}, got {actual:R}.");
    }

    private static double Magnitude(double h, double v) => Math.Sqrt(h * h + v * v);

    private static double Relative(double delta, double reference) =>
        Math.Abs(reference) > 0.0 ? delta / reference : 0.0;

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
