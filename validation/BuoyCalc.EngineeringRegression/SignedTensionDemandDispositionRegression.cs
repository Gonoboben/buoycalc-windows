using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedTensionDemandDispositionRegression
{
    private const double G = 9.80665;
    private const double IdentityToleranceKn = 1e-9;

    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    public static void Validate()
    {
        var checkedAccepted = 0;
        Console.WriteLine("E1C_TENSION_DEMAND_DISPOSITION_BEGIN");

        foreach (var definition in HistoricalDefinitions().Cast<object>())
        {
            var name = Property<string>(definition, "Name");
            if (!AcceptedFixtures.Contains(name))
                continue;

            var environment = Property<EnvironmentInput>(definition, "Environment");
            var buoy = Property<BuoyInput>(definition, "Buoy");
            var assembly = Property<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = Property<AnchorInput>(definition, "Anchor");
            var safetyFactor = Property<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
            var candidate = run.Snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"E1-C {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore
                ?? throw new InvalidOperationException($"E1-C {name}: selected core is missing.");

            if (candidate.Status != MooringSignedCandidateStatus.Accepted ||
                candidate.Shape is null ||
                candidate.Boundary is null ||
                selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                !ReferenceEquals(selectedCore.Shape, candidate.Shape))
            {
                throw new InvalidOperationException($"E1-C {name}: Accepted signed selected-source contract changed.");
            }

            if (!candidate.Boundary.MethodNote.Contains("wave excluded", StringComparison.Ordinal))
                throw new InvalidOperationException($"E1-C {name}: signed boundary no longer explicitly excludes wave loading.");
            if (!double.IsFinite(run.Result.WaveForceN) || run.Result.WaveForceN <= 0.0)
                throw new InvalidOperationException($"E1-C {name}: canonical fixture must retain a positive legacy wave force.");

            var expectedLegacyHorizontalN = run.Result.CurrentForceN + run.Result.WaveForceN;
            if (run.Result.HorizontalForceN != expectedLegacyHorizontalN)
                throw new InvalidOperationException($"E1-C {name}: legacy HorizontalForceN composition changed.");

            var legacyVerticalN = Math.Max(0.0, run.Result.NetBuoyancyKg) * G;
            var expectedLegacyTensionKn = Math.Sqrt(
                run.Result.HorizontalForceN * run.Result.HorizontalForceN +
                legacyVerticalN * legacyVerticalN) / 1000.0;
            Near(run.Result.TensionKn, expectedLegacyTensionKn, IdentityToleranceKn, name + " legacy TensionKn formula");

            var signedState = MooringSelectedSignedBoundaryStateProjector.Project(selectedCore, candidate)
                ?? throw new InvalidOperationException($"E1-C {name}: selected signed boundary state is unavailable.");
            var surfaceResultantN = Magnitude(signedState.BuoySteadyDragN, signedState.Q0N);
            var anchorEndResultantN = Magnitude(signedState.EndHN, signedState.EndVN);
            var maxMidResultantN = candidate.Shape.Nodes
                .Where(x => x.SegmentNumber > 0 && x.SegmentLengthM > 0.0)
                .Max(x => x.SegmentTensionKn) * 1000.0;

            PositiveFinite(surfaceResultantN, name + " signed surface resultant");
            PositiveFinite(anchorEndResultantN, name + " signed anchor-end resultant");
            PositiveFinite(maxMidResultantN, name + " signed max-mid resultant");

            Console.WriteLine(string.Join("|",
                "E1C_TENSION_DEMAND_DISPOSITION",
                name,
                "SelectedGeometrySource=SignedBoundaryFeedback",
                $"LegacyCurrentForceN={F(run.Result.CurrentForceN)}",
                $"LegacyWaveForceN={F(run.Result.WaveForceN)}",
                $"LegacyHorizontalForceN={F(run.Result.HorizontalForceN)}",
                $"LegacyTensionKn={F(run.Result.TensionKn)}",
                $"SignedSurfaceResultantN={F(surfaceResultantN)}",
                $"SignedAnchorEndResultantN={F(anchorEndResultantN)}",
                $"SignedMaxMidResultantN={F(maxMidResultantN)}",
                "SignedLoadSet=SteadyCurrentWaveExcluded",
                "LegacyGlobalLoadSet=CurrentPlusWave",
                "SignedSurfaceDisposition=EvidenceOnly",
                "SignedAnchorEndDisposition=EvidenceOnlyFutureAnchorInput",
                "SignedMaxMidDisposition=EvidenceOnlyFutureLocalDemand",
                "GlobalProductionTensionAuthority=LegacyRetained",
                "WaveAwareLocalDemandModelRequired=True",
                "ProductionMigrationAuthorized=False"));

            checkedAccepted++;
        }

        if (checkedAccepted != 2)
            throw new InvalidOperationException($"E1-C canonical Accepted coverage mismatch: {checkedAccepted}.");

        Console.WriteLine(
            "E1C_TENSION_DEMAND_DISPOSITION_ROLLUP|AcceptedChecked=2|SelectedGeometryAuthority=SignedWhereAccepted|GlobalProductionTensionAuthority=LegacyRetained|SignedSurface=EvidenceOnly|SignedAnchorEnd=EvidenceOnlyFutureAnchorInput|SignedMaxMid=EvidenceOnlyFutureLocalDemand|WaveModelGap=True|WeakLinkLocalDemandGap=True|ProductionMigrationAuthorized=False|E1ValidationComplete=True");
        Console.WriteLine("E1C_TENSION_DEMAND_DISPOSITION_END");
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("E1-C: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("E1-C: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"E1-C: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"E1-C: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static double Magnitude(double h, double v) => Math.Sqrt(h * h + v * v);

    private static void PositiveFinite(double value, string label)
    {
        if (!double.IsFinite(value) || value <= 0.0)
            throw new InvalidOperationException($"E1-C {label}: expected finite positive value, got {value:R}.");
    }

    private static void Near(double actual, double expected, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"E1-C {label}: expected {expected:R}, got {actual:R}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
