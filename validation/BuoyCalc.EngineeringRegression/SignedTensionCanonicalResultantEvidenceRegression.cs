using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedTensionCanonicalResultantEvidenceRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    public static void Validate()
    {
        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var checkedAccepted = 0;
        var withoutInternalPoints = 0;
        var withInternalPoints = 0;

        Console.WriteLine("E1B2_CANONICAL_RESULTANTS_BEGIN");

        foreach (var definition in definitions)
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
                ?? throw new InvalidOperationException($"E1-B2 {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore
                ?? throw new InvalidOperationException($"E1-B2 {name}: selected core is missing.");

            if (candidate.Status != MooringSignedCandidateStatus.Accepted ||
                candidate.Shape is null ||
                candidate.Boundary is null ||
                selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                !ReferenceEquals(selectedCore.Shape, candidate.Shape))
            {
                throw new InvalidOperationException($"E1-B2 {name}: canonical Accepted signed source identity is not intact.");
            }

            var signedState = MooringSelectedSignedBoundaryStateProjector.Project(selectedCore, candidate)
                ?? throw new InvalidOperationException($"E1-B2 {name}: selected signed boundary state is unavailable.");

            if (!candidate.Boundary.MethodNote.Contains("wave excluded", StringComparison.Ordinal))
                throw new InvalidOperationException($"E1-B2 {name}: signed boundary no longer declares wave exclusion.");

            var tensionNodes = candidate.Shape.Nodes
                .Where(x => x.SegmentNumber > 0 && x.SegmentLengthM > 0.0)
                .OrderBy(x => x.SegmentNumber)
                .ToList();

            if (candidate.Shape.Nodes.Count != run.Result.SegmentRows.Count + 1 ||
                tensionNodes.Count != run.Result.SegmentRows.Count)
            {
                throw new InvalidOperationException(
                    $"E1-B2 {name}: Accepted shape must retain exactly one stored midpoint tension per production segment.");
            }

            if (tensionNodes.Any(x => !double.IsFinite(x.SegmentTensionKn) || x.SegmentTensionKn <= 0.0))
                throw new InvalidOperationException($"E1-B2 {name}: stored Accepted midpoint tension is non-finite/non-positive.");

            var maxMidNode = tensionNodes
                .OrderByDescending(x => x.SegmentTensionKn)
                .ThenBy(x => x.SegmentNumber)
                .First();

            var surfaceResultantN = Magnitude(signedState.BuoySteadyDragN, signedState.Q0N);
            var anchorEndResultantN = Magnitude(signedState.EndHN, signedState.EndVN);
            var maxMidResultantN = maxMidNode.SegmentTensionKn * 1000.0;
            var maxMidPhysicalSM = maxMidNode.AlongLineM - 0.5 * maxMidNode.SegmentLengthM;

            PositiveFinite(surfaceResultantN, name + " surface resultant");
            PositiveFinite(anchorEndResultantN, name + " anchor-end resultant");
            PositiveFinite(maxMidResultantN, name + " max-mid resultant");
            if (!double.IsFinite(maxMidPhysicalSM) || maxMidPhysicalSM < 0.0 || maxMidPhysicalSM > run.Result.LineLengthM)
                throw new InvalidOperationException($"E1-B2 {name}: max-mid physical s is outside the line.");

            var sequence = MooringSequencePositioner.Build(run.Result);
            var orderedRows = sequence.Rows.OrderBy(x => x.Number).ToList();
            var topNumber = orderedRows[0].Number;
            var bottomNumber = orderedRows[^1].Number;
            var internalPoints = orderedRows
                .Where(x => x.IsDiscrete && x.Number != topNumber && x.Number != bottomNumber)
                .ToList();

            if (signedState.PointLoadCrossings != internalPoints.Count ||
                signedState.ContainsDiscreteLoads != (internalPoints.Count > 0) ||
                candidate.ContainsDiscreteLoads != signedState.ContainsDiscreteLoads)
            {
                throw new InvalidOperationException($"E1-B2 {name}: discrete-load identity changed between sequence, candidate and selected signed state.");
            }

            if (internalPoints.Count == 0)
                withoutInternalPoints++;
            else
                withInternalPoints++;
            checkedAccepted++;

            Console.WriteLine(string.Join("|",
                "E1B2_CANONICAL_RESULTANTS",
                name,
                "CandidateStatus=Accepted",
                "SelectedSource=SignedBoundaryFeedback",
                $"SurfaceResultantN={F(surfaceResultantN)}",
                $"AnchorEndResultantN={F(anchorEndResultantN)}",
                $"MaxMidResultantN={F(maxMidResultantN)}",
                $"MaxMidSegment={maxMidNode.SegmentNumber}",
                $"MaxMidPhysicalSM={F(maxMidPhysicalSM)}",
                $"StoredMidpointSamples={tensionNodes.Count}",
                $"PointLoadCrossings={signedState.PointLoadCrossings}",
                $"ContainsDiscreteLoads={signedState.ContainsDiscreteLoads}",
                $"LegacyTensionKn={F(run.Result.TensionKn)}",
                $"LegacyWaveForceN={F(run.Result.WaveForceN)}",
                "SignedLoadSet=SteadyCurrentWaveExcluded",
                "MidpointSource=AcceptedShapeStoredFinalTraceValue",
                "DesignDemandSelected=False",
                "ProductionTensionKn=LegacyUnchanged"));
        }

        if (checkedAccepted != 2 || withoutInternalPoints != 1 || withInternalPoints != 1)
        {
            throw new InvalidOperationException(
                $"E1-B2 coverage mismatch: accepted={checkedAccepted}, withoutPoints={withoutInternalPoints}, withPoints={withInternalPoints}.");
        }

        Console.WriteLine(
            "E1B2_CANONICAL_RESULTANTS_ROLLUP|AcceptedChecked=2|WithoutInternalPoints=1|WithInternalPoints=1|SurfaceResultant=Available|AnchorEndResultant=Available|MaxMidResultant=Available|WaveIncluded=False|DesignDemandSelected=False|ProductionTensionKn=LegacyUnchanged");
        Console.WriteLine("E1B2_CANONICAL_RESULTANTS_END");
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("E1-B2: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("E1-B2: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"E1-B2: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"E1-B2: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static double Magnitude(double h, double v) => Math.Sqrt(h * h + v * v);

    private static void PositiveFinite(double value, string label)
    {
        if (!double.IsFinite(value) || value <= 0.0)
            throw new InvalidOperationException($"E1-B2 {label}: expected finite positive value, got {value:R}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
