using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SelectedDesignEnvelopeStateRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    private const double Tolerance = 1e-7;

    public static void Validate()
    {
        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var available = 0;
        var unavailable = 0;
        var withPoints = 0;
        var withoutPoints = 0;

        Console.WriteLine("F1B_SELECTED_DESIGN_ENVELOPE_BEGIN");

        foreach (var definition in definitions)
        {
            var name = Property<string>(definition, "Name");
            var environment = Property<EnvironmentInput>(definition, "Environment");
            var buoy = Property<BuoyInput>(definition, "Buoy");
            var assembly = Property<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = Property<AnchorInput>(definition, "Anchor");
            var safetyFactor = Property<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
            var candidate = run.Snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"F1-B {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore;
            var legacyTensionKn = run.Result.TensionKn;
            var selectedShapeBefore = selectedCore?.Shape;

            var design = MooringSelectedDesignEnvelopeStateProjector.Project(
                run.Result,
                selectedCore,
                candidate);

            if (!AcceptedFixtures.Contains(name))
            {
                if (design is not null)
                    throw new InvalidOperationException($"F1-B {name}: non-Accepted/non-signed selection exposed a design envelope.");
                unavailable++;
                Console.WriteLine(string.Join("|",
                    "F1B_SELECTED_DESIGN_ENVELOPE",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    $"SelectedSource={selectedCore?.SourceIdentity.ToString() ?? "None"}",
                    "Available=False",
                    "ProductionTensionAuthority=LegacyUnchanged"));
                continue;
            }

            if (design is null ||
                candidate.Status != MooringSignedCandidateStatus.Accepted ||
                selectedCore is null ||
                selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                !ReferenceEquals(selectedCore.Shape, candidate.Shape) ||
                !ReferenceEquals(selectedCore.Shape, selectedShapeBefore))
            {
                throw new InvalidOperationException($"F1-B {name}: Accepted selected-source identity is not intact.");
            }

            Near(design.WaveHorizontalIncrementN, run.Result.WaveForceN, name + " wave increment identity");
            Near(design.SurfaceDesignHN, design.SurfaceSteadyHN + run.Result.WaveForceN, name + " surface H increment");
            Exact(design.SurfaceDesignVN, design.SurfaceSteadyVN, name + " surface V unchanged");
            Near(design.AnchorDesignHN, design.AnchorSteadyHN + run.Result.WaveForceN, name + " anchor H increment");
            Exact(design.AnchorDesignVN, design.AnchorSteadyVN, name + " anchor V unchanged");
            Near(
                design.SurfaceDesignTensionN,
                Magnitude(design.SurfaceDesignHN, design.SurfaceDesignVN),
                name + " surface resultant");
            Near(
                design.AnchorDesignTensionN,
                Magnitude(design.AnchorDesignHN, design.AnchorDesignVN),
                name + " anchor resultant");

            if (design.MidpointRows.Count != run.Result.SegmentRows.Count || design.MidpointRows.Count == 0)
                throw new InvalidOperationException($"F1-B {name}: midpoint row count no longer matches production segments.");

            foreach (var row in design.MidpointRows)
            {
                Near(row.DesignMidHN, row.SteadyMidHN + run.Result.WaveForceN, name + $" segment {row.SegmentNumber} H increment");
                Exact(row.DesignMidVN, row.SteadyMidVN, name + $" segment {row.SegmentNumber} V unchanged");
                Near(
                    row.DesignMidTensionN,
                    Magnitude(row.DesignMidHN, row.DesignMidVN),
                    name + $" segment {row.SegmentNumber} design resultant");
                Near(
                    row.SteadyMidTensionN,
                    Magnitude(row.SteadyMidHN, row.SteadyMidVN),
                    name + $" segment {row.SegmentNumber} steady resultant");
            }

            var expectedMax = design.MidpointRows
                .OrderByDescending(x => x.DesignMidTensionN)
                .ThenBy(x => x.SegmentNumber)
                .First();
            Exact(design.MaxDesignMidpointSegmentNumber, expectedMax.SegmentNumber, name + " max-mid segment");
            Exact(design.MaxDesignMidpointTensionN, expectedMax.DesignMidTensionN, name + " max-mid tension");
            Exact(run.Result.TensionKn, legacyTensionKn, name + " legacy scalar unchanged");

            var zeroWaveRun = ApplicationCalculationRunner.Run(
                environment with { WaveHeightM = 0.0 },
                buoy,
                assembly,
                anchor,
                safetyFactor);
            var zeroCandidate = zeroWaveRun.Snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"F1-B {name}: zero-wave signed candidate is missing.");
            var zeroDesign = MooringSelectedDesignEnvelopeStateProjector.Project(
                zeroWaveRun.Result,
                zeroWaveRun.Snapshot.ShadowSelectedCore,
                zeroCandidate)
                ?? throw new InvalidOperationException($"F1-B {name}: zero-wave Accepted design envelope is unavailable.");

            Exact(zeroDesign.WaveHorizontalIncrementN, 0.0, name + " zero-wave increment");
            Exact(zeroDesign.SurfaceDesignHN, zeroDesign.SurfaceSteadyHN, name + " zero-wave surface H");
            Exact(zeroDesign.SurfaceDesignVN, zeroDesign.SurfaceSteadyVN, name + " zero-wave surface V");
            Exact(zeroDesign.SurfaceDesignTensionN, zeroDesign.SurfaceSteadyTensionN, name + " zero-wave surface tension");
            Exact(zeroDesign.AnchorDesignHN, zeroDesign.AnchorSteadyHN, name + " zero-wave anchor H");
            Exact(zeroDesign.AnchorDesignVN, zeroDesign.AnchorSteadyVN, name + " zero-wave anchor V");
            Exact(zeroDesign.AnchorDesignTensionN, zeroDesign.AnchorSteadyTensionN, name + " zero-wave anchor tension");
            foreach (var row in zeroDesign.MidpointRows)
                Exact(row.DesignMidTensionN, row.SteadyMidTensionN, name + $" zero-wave segment {row.SegmentNumber} tension");

            if (design.ContainsDiscreteLoads)
                withPoints++;
            else
                withoutPoints++;
            available++;

            Console.WriteLine(string.Join("|",
                "F1B_SELECTED_DESIGN_ENVELOPE",
                name,
                "CandidateStatus=Accepted",
                "SelectedSource=SignedBoundaryFeedback",
                "Available=True",
                $"WaveHorizontalIncrementN={F(design.WaveHorizontalIncrementN)}",
                $"SurfaceSteadyTensionN={F(design.SurfaceSteadyTensionN)}",
                $"SurfaceDesignTensionN={F(design.SurfaceDesignTensionN)}",
                $"AnchorSteadyTensionN={F(design.AnchorSteadyTensionN)}",
                $"AnchorDesignTensionN={F(design.AnchorDesignTensionN)}",
                $"MaxDesignMidpointTensionN={F(design.MaxDesignMidpointTensionN)}",
                $"MaxDesignMidpointSegment={design.MaxDesignMidpointSegmentNumber}",
                $"PointLoadCrossings={design.PointLoadCrossings}",
                $"ContainsDiscreteLoads={design.ContainsDiscreteLoads}",
                "GeometryFeedback=None",
                "WaveModel=LegacyBuoyHorizontalDragProxy",
                "DynamicClaim=False",
                "ProductionTensionAuthority=LegacyUnchanged"));
        }

        if (definitions.Count != 5 || available != 2 || unavailable != 3 || withPoints != 1 || withoutPoints != 1)
        {
            throw new InvalidOperationException(
                $"F1-B canonical coverage mismatch: scenarios={definitions.Count}, available={available}, unavailable={unavailable}, withPoints={withPoints}, withoutPoints={withoutPoints}.");
        }

        Console.WriteLine(
            "F1B_SELECTED_DESIGN_ENVELOPE_ROLLUP|Scenarios=5|Available=2|Unavailable=3|AcceptedSignedOnly=True|ZeroWaveCollapsesToSteady=True|WaveAppliedOnceHorizontally=True|VerticalComponentsUnchanged=True|GeometryFeedback=None|DynamicClaim=False|ProductionTensionAuthority=LegacyUnchanged");
        Console.WriteLine("F1B_SELECTED_DESIGN_ENVELOPE_END");
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F1-B: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F1-B: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F1-B: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F1-B: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static double Magnitude(double h, double v) => Math.Sqrt(h * h + v * v);

    private static void Near(double actual, double expected, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > Tolerance)
            throw new InvalidOperationException($"F1-B {label}: expected {expected:R}, got {actual:R}.");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F1-B {label}: expected exact {expected:R}, got {actual:R}.");
    }

    private static void Exact(int actual, int expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F1-B {label}: expected exact {expected}, got {actual}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
