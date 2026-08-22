using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class DesignEnvelopeReferenceEvidenceRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    private const double ToleranceN = 1e-7;

    public static void Validate()
    {
        ValidateKnownVectorReferences();

        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var acceptedChecked = 0;
        var unavailableChecked = 0;

        Console.WriteLine("F1C_DESIGN_ENVELOPE_EVIDENCE_BEGIN");

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
                ?? throw new InvalidOperationException($"F1-C {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore;
            var design = MooringSelectedDesignEnvelopeStateProjector.Project(run.Result, selectedCore, candidate);

            if (!AcceptedFixtures.Contains(name))
            {
                if (design is not null)
                    throw new InvalidOperationException($"F1-C {name}: design envelope unexpectedly available.");
                unavailableChecked++;
                Console.WriteLine(string.Join("|",
                    "F1C_DESIGN_ENVELOPE_EVIDENCE",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    $"SelectedSource={selectedCore?.SourceIdentity.ToString() ?? "None"}",
                    "DesignEnvelopeAvailable=False",
                    $"LegacyTensionN={F(run.Result.TensionKn * 1000.0)}",
                    "AuthorityEvidence=LegacyOnlyForThisScenario"));
                continue;
            }

            if (design is null || selectedCore is null || candidate.Shape is null)
                throw new InvalidOperationException($"F1-C {name}: Accepted design envelope is missing.");

            var signedState = MooringSelectedSignedBoundaryStateProjector.Project(selectedCore, candidate)
                ?? throw new InvalidOperationException($"F1-C {name}: selected signed boundary state is missing.");

            var waveN = run.Result.WaveForceN;
            var referenceSurfaceN = IndependentEnvelopeMagnitude(
                signedState.BuoySteadyDragN,
                signedState.Q0N,
                waveN);
            var referenceAnchorN = IndependentEnvelopeMagnitude(
                signedState.EndHN,
                signedState.EndVN,
                waveN);

            Near(design.SurfaceDesignTensionN, referenceSurfaceN, name + " independent surface resultant");
            Near(design.AnchorDesignTensionN, referenceAnchorN, name + " independent anchor resultant");

            var nodes = candidate.Shape.Nodes.OrderBy(x => x.Number).ToList();
            if (nodes.Count != design.MidpointRows.Count + 1)
                throw new InvalidOperationException($"F1-C {name}: selected node/design row count mismatch.");

            var independentMidRows = new List<(int Segment, double TensionN)>();
            for (var i = 1; i < nodes.Count; i++)
            {
                var previous = nodes[i - 1];
                var node = nodes[i];
                var dx = node.XOffsetM - previous.XOffsetM;
                var dz = node.ZDepthM - previous.ZDepthM;
                var length = Math.Sqrt(dx * dx + dz * dz);
                var steadyTensionN = node.SegmentTensionKn * 1000.0;
                if (!double.IsFinite(length) || length <= 0.0 ||
                    !double.IsFinite(steadyTensionN) || steadyTensionN <= 0.0)
                {
                    throw new InvalidOperationException($"F1-C {name}: invalid selected midpoint reference at segment {node.SegmentNumber}.");
                }

                var steadyH = steadyTensionN * dx / length;
                var steadyV = steadyTensionN * dz / length;
                var referenceTensionN = IndependentEnvelopeMagnitude(steadyH, steadyV, waveN);
                independentMidRows.Add((node.SegmentNumber, referenceTensionN));

                var projected = design.MidpointRows[i - 1];
                if (projected.SegmentNumber != node.SegmentNumber)
                    throw new InvalidOperationException($"F1-C {name}: midpoint segment identity changed.");
                Near(projected.DesignMidTensionN, referenceTensionN, name + $" segment {node.SegmentNumber} independent resultant");
            }

            var referenceMaxMid = independentMidRows
                .OrderByDescending(x => x.TensionN)
                .ThenBy(x => x.Segment)
                .First();
            Near(design.MaxDesignMidpointTensionN, referenceMaxMid.TensionN, name + " independent max-mid resultant");
            if (design.MaxDesignMidpointSegmentNumber != referenceMaxMid.Segment)
                throw new InvalidOperationException($"F1-C {name}: independent max-mid segment differs.");

            var governing = Governing(
                design.SurfaceDesignTensionN,
                design.AnchorDesignTensionN,
                design.MaxDesignMidpointTensionN,
                design.MaxDesignMidpointSegmentNumber);
            var legacyN = run.Result.TensionKn * 1000.0;
            PositiveFinite(legacyN, name + " legacy tension");
            PositiveFinite(governing.TensionN, name + " governing design tension");

            var deltaN = governing.TensionN - legacyN;
            var ratio = governing.TensionN / legacyN;
            if (!double.IsFinite(deltaN) || !double.IsFinite(ratio))
                throw new InvalidOperationException($"F1-C {name}: old/new evidence is non-finite.");

            acceptedChecked++;
            Console.WriteLine(string.Join("|",
                "F1C_DESIGN_ENVELOPE_EVIDENCE",
                name,
                "CandidateStatus=Accepted",
                "SelectedSource=SignedBoundaryFeedback",
                "DesignEnvelopeAvailable=True",
                $"WaveForceN={F(waveN)}",
                $"LegacyTensionN={F(legacyN)}",
                $"SurfaceDesignN={F(design.SurfaceDesignTensionN)}",
                $"AnchorDesignN={F(design.AnchorDesignTensionN)}",
                $"MaxMidDesignN={F(design.MaxDesignMidpointTensionN)}",
                $"MaxMidSegment={design.MaxDesignMidpointSegmentNumber}",
                $"GoverningDesignN={F(governing.TensionN)}",
                $"GoverningLocation={governing.Location}",
                $"DeltaGoverningMinusLegacyN={F(deltaN)}",
                $"RatioGoverningToLegacy={F(ratio)}",
                "ComparisonDisposition=MeasuredNotEqualityGate",
                "AuthorityDecision=DeferredToF1D"));
        }

        if (definitions.Count != 5 || acceptedChecked != 2 || unavailableChecked != 3)
        {
            throw new InvalidOperationException(
                $"F1-C canonical coverage mismatch: scenarios={definitions.Count}, accepted={acceptedChecked}, unavailable={unavailableChecked}.");
        }

        Console.WriteLine(
            "F1C_DESIGN_ENVELOPE_EVIDENCE_ROLLUP|KnownVectorReference=True|CanonicalScenarios=5|AcceptedEvidence=2|UnavailableEvidence=3|GoverningLocations=Surface,AnchorEnd,MaxMidpoint|LegacyEqualityRequired=False|NoToleranceTuning=True|AuthorityDecision=DeferredToF1D");
        Console.WriteLine("F1C_DESIGN_ENVELOPE_EVIDENCE_END");
    }

    private static void ValidateKnownVectorReferences()
    {
        Near(IndependentEnvelopeMagnitude(0.0, 4.0, 3.0), 5.0, "3-4-5 boundary vector");
        Near(IndependentEnvelopeMagnitude(3.0, 4.0, 0.0), 5.0, "zero-wave 3-4-5 identity");

        var positiveV = IndependentEnvelopeMagnitude(3.0, 4.0, 5.0);
        var negativeV = IndependentEnvelopeMagnitude(3.0, -4.0, 5.0);
        Near(positiveV, Math.Sqrt(80.0), "known combined vector");
        Near(negativeV, positiveV, "signed-V magnitude invariance");
    }

    private static (double TensionN, string Location) Governing(
        double surfaceN,
        double anchorN,
        double maxMidN,
        int maxMidSegment)
    {
        var candidates = new[]
        {
            (TensionN: surfaceN, Location: "Surface"),
            (TensionN: anchorN, Location: "AnchorEnd"),
            (TensionN: maxMidN, Location: $"MaxMidpointSegment{maxMidSegment}")
        };
        return candidates
            .OrderByDescending(x => x.TensionN)
            .ThenBy(x => x.Location, StringComparer.Ordinal)
            .First();
    }

    private static double IndependentEnvelopeMagnitude(double steadyHN, double steadyVN, double waveHN)
    {
        var designH = steadyHN + waveHN;
        return Math.Sqrt(designH * designH + steadyVN * steadyVN);
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F1-C: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F1-C: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F1-C: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F1-C: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Near(double actual, double expected, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > ToleranceN)
            throw new InvalidOperationException($"F1-C {label}: expected {expected:R}, got {actual:R}.");
    }

    private static void PositiveFinite(double value, string label)
    {
        if (!double.IsFinite(value) || value <= 0.0)
            throw new InvalidOperationException($"F1-C {label}: expected finite positive value, got {value:R}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
