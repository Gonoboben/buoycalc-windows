using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SelectedDesignTensionDemandAuthorityRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    public static void Validate()
    {
        if (MooringSelectedDesignTensionDemandProjector.Project(null) is not null)
            throw new InvalidOperationException("F1-D: null envelope must not fabricate a selected design-tension authority.");

        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var available = 0;
        var unavailable = 0;

        Console.WriteLine("F1D_SELECTED_DESIGN_TENSION_AUTHORITY_BEGIN");

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
                ?? throw new InvalidOperationException($"F1-D {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore;
            var selectedShapeBefore = selectedCore?.Shape;
            var legacyTensionKnBefore = run.Result.TensionKn;

            var envelope = MooringSelectedDesignEnvelopeStateProjector.Project(
                run.Result,
                selectedCore,
                candidate);
            var demand = MooringSelectedDesignTensionDemandProjector.Project(envelope);

            Exact(run.Result.TensionKn, legacyTensionKnBefore, name + " legacy TensionKn unchanged");
            if (!ReferenceEquals(selectedCore?.Shape, selectedShapeBefore))
                throw new InvalidOperationException($"F1-D {name}: selected geometry reference changed during demand projection.");

            if (!AcceptedFixtures.Contains(name))
            {
                if (envelope is not null || demand is not null)
                    throw new InvalidOperationException($"F1-D {name}: non-Accepted scenario exposed signed selected demand authority.");

                unavailable++;
                Console.WriteLine(string.Join("|",
                    "F1D_SELECTED_DESIGN_TENSION_AUTHORITY",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    $"SelectedSource={selectedCore?.SourceIdentity.ToString() ?? "None"}",
                    "AuthorityAvailable=False",
                    $"LegacyTensionN={F(run.Result.TensionKn * 1000.0)}",
                    "LegacyCompatibilityScalar=Unchanged"));
                continue;
            }

            if (envelope is null || demand is null || selectedCore is null || candidate.Shape is null)
                throw new InvalidOperationException($"F1-D {name}: Accepted signed-selected demand authority is unavailable.");
            if (candidate.Status != MooringSignedCandidateStatus.Accepted ||
                selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                demand.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                !ReferenceEquals(selectedCore.Shape, candidate.Shape))
            {
                throw new InvalidOperationException($"F1-D {name}: selected signed source identity changed.");
            }

            var expected = IndependentGoverning(envelope);
            Exact(demand.DemandN, expected.TensionN, name + " governing N");
            Exact(demand.DemandKn, expected.TensionN / 1000.0, name + " governing kN");
            if (demand.LocationKind != expected.LocationKind)
                throw new InvalidOperationException($"F1-D {name}: expected location {expected.LocationKind}, got {demand.LocationKind}.");
            if (demand.SegmentNumber != expected.SegmentNumber)
                throw new InvalidOperationException($"F1-D {name}: governing segment identity changed.");
            if (!string.Equals(demand.SourceElement, expected.SourceElement, StringComparison.Ordinal))
                throw new InvalidOperationException($"F1-D {name}: governing source-element identity changed.");
            Exact(demand.AlongLineM, expected.AlongLineM, name + " governing along-line coordinate");
            Exact(demand.WaveHorizontalIncrementN, envelope.WaveHorizontalIncrementN, name + " wave provenance");

            if (string.IsNullOrWhiteSpace(demand.MethodNote) ||
                !demand.MethodNote.Contains("quasi-static", StringComparison.OrdinalIgnoreCase) ||
                !demand.MethodNote.Contains("compatibility scalar", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"F1-D {name}: authority method note lost semantic separation.");
            }

            var deltaN = demand.DemandN - run.Result.TensionKn * 1000.0;
            if (!double.IsFinite(deltaN))
                throw new InvalidOperationException($"F1-D {name}: legacy/new evidence delta is non-finite.");

            available++;
            Console.WriteLine(string.Join("|",
                "F1D_SELECTED_DESIGN_TENSION_AUTHORITY",
                name,
                "CandidateStatus=Accepted",
                "SelectedSource=SignedBoundaryFeedback",
                "AuthorityAvailable=True",
                $"DemandN={F(demand.DemandN)}",
                $"DemandKn={F(demand.DemandKn)}",
                $"LocationKind={demand.LocationKind}",
                $"SegmentNumber={demand.SegmentNumber?.ToString(CultureInfo.InvariantCulture) ?? "None"}",
                $"AlongLineM={F(demand.AlongLineM)}",
                $"WaveHorizontalIncrementN={F(demand.WaveHorizontalIncrementN)}",
                $"LegacyTensionN={F(run.Result.TensionKn * 1000.0)}",
                $"DeltaDemandMinusLegacyN={F(deltaN)}",
                "LegacyCompatibilityScalar=Unchanged",
                "GeometryAuthority=Unchanged",
                "DownstreamMigration=None"));
        }

        if (definitions.Count != 5 || available != 2 || unavailable != 3)
        {
            throw new InvalidOperationException(
                $"F1-D canonical coverage mismatch: scenarios={definitions.Count}, available={available}, unavailable={unavailable}.");
        }

        Console.WriteLine(
            "F1D_SELECTED_DESIGN_TENSION_AUTHORITY_ROLLUP|CanonicalScenarios=5|AcceptedAuthority=2|Unavailable=3|AuthorityDefinition=MaxSurfaceAnchorEndMidpoint|LegacyTensionKn=CompatibilityUnchanged|SelectedGeometry=Unchanged|WeakLinkMigration=None|AnchorMigration=None|ChecksVerdictMigration=None|DynamicClaim=False");
        Console.WriteLine("F1D_SELECTED_DESIGN_TENSION_AUTHORITY_END");
    }

    private static ExpectedDemand IndependentGoverning(MooringSelectedDesignEnvelopeState envelope)
    {
        var maxMidpoint = envelope.MidpointRows
            .OrderByDescending(x => x.DesignMidTensionN)
            .ThenBy(x => x.SegmentNumber)
            .First();
        var anchorAlongLineM = envelope.MidpointRows.OrderBy(x => x.EndLengthM).Last().EndLengthM;

        var candidates = new[]
        {
            new ExpectedDemand(
                envelope.SurfaceDesignTensionN,
                MooringDesignTensionLocationKind.Surface,
                null,
                null,
                0.0,
                2),
            new ExpectedDemand(
                envelope.AnchorDesignTensionN,
                MooringDesignTensionLocationKind.AnchorEnd,
                null,
                null,
                anchorAlongLineM,
                0),
            new ExpectedDemand(
                maxMidpoint.DesignMidTensionN,
                MooringDesignTensionLocationKind.Midpoint,
                maxMidpoint.SegmentNumber,
                maxMidpoint.SourceElement,
                maxMidpoint.MidLengthM,
                1)
        };

        return candidates
            .OrderByDescending(x => x.TensionN)
            .ThenBy(x => x.TieOrder)
            .First();
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F1-D: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F1-D: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F1-D: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F1-D: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F1-D {label}: expected exact {expected:R}, got {actual:R}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record ExpectedDemand(
        double TensionN,
        MooringDesignTensionLocationKind LocationKind,
        int? SegmentNumber,
        string? SourceElement,
        double AlongLineM,
        int TieOrder);
}
