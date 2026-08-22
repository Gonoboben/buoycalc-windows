using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SelectedAnchorReactionStateRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    private const double GravityMS2 = 9.80665;
    private const double Tolerance = 1e-7;

    public static void Validate()
    {
        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var accepted = 0;
        var unavailable = 0;
        MooringSelectedDesignEnvelopeState? syntheticTemplateEnvelope = null;
        CalculationResult? syntheticTemplateResult = null;

        Console.WriteLine("F2B_SELECTED_ANCHOR_REACTION_STATE_BEGIN");

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
                ?? throw new InvalidOperationException($"F2-B {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore;
            var selectedShapeBefore = selectedCore?.Shape;
            var legacyRequiredHoldingKg = run.Result.RequiredAnchorHoldingKg;
            var legacyAnchorHoldingKg = run.Result.AnchorHoldingKg;
            var legacyAnchorReserve = run.Result.AnchorReserve;
            var legacyTensionKn = run.Result.TensionKn;

            var envelope = MooringSelectedDesignEnvelopeStateProjector.Project(run.Result, selectedCore, candidate);
            var reaction = MooringSelectedAnchorReactionStateProjector.Project(run.Result, envelope);

            Exact(run.Result.RequiredAnchorHoldingKg, legacyRequiredHoldingKg, name + " legacy required holding unchanged");
            Exact(run.Result.AnchorHoldingKg, legacyAnchorHoldingKg, name + " legacy holding unchanged");
            Exact(run.Result.AnchorReserve, legacyAnchorReserve, name + " legacy anchor reserve unchanged");
            Exact(run.Result.TensionKn, legacyTensionKn, name + " legacy tension unchanged");
            if (!ReferenceEquals(selectedCore?.Shape, selectedShapeBefore))
                throw new InvalidOperationException($"F2-B {name}: selected geometry reference changed during reaction projection.");

            if (!AcceptedFixtures.Contains(name))
            {
                if (envelope is not null || reaction is not null)
                    throw new InvalidOperationException($"F2-B {name}: non-Accepted scenario exposed selected anchor reaction state.");

                unavailable++;
                Console.WriteLine(string.Join("|",
                    "F2B_SELECTED_ANCHOR_REACTION_STATE",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    $"SelectedSource={selectedCore?.SourceIdentity.ToString() ?? "None"}",
                    "Available=False",
                    "LegacyAnchorAuthority=Unchanged"));
                continue;
            }

            if (envelope is null || reaction is null || selectedCore is null || candidate.Shape is null)
                throw new InvalidOperationException($"F2-B {name}: Accepted selected anchor reaction state is unavailable.");
            if (candidate.Status != MooringSignedCandidateStatus.Accepted ||
                reaction.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                !ReferenceEquals(selectedCore.Shape, candidate.Shape))
            {
                throw new InvalidOperationException($"F2-B {name}: signed selected identity changed.");
            }

            Exact(reaction.InternalAnchorEndHN, envelope.AnchorDesignHN, name + " internal H");
            Exact(reaction.InternalAnchorEndVN, envelope.AnchorDesignVN, name + " internal V");
            Exact(reaction.InternalAnchorEndTensionN, envelope.AnchorDesignTensionN, name + " internal resultant");
            Exact(reaction.LineOnAnchorHN, -envelope.AnchorDesignHN, name + " line-on-anchor H");
            Exact(reaction.LineOnAnchorVDepthPositiveN, -envelope.AnchorDesignVN, name + " line-on-anchor V");
            Exact(reaction.HorizontalDemandN, Math.Abs(envelope.AnchorDesignHN), name + " horizontal demand");
            Exact(reaction.UpwardLinePullN, Math.Max(0.0, envelope.AnchorDesignVN), name + " upward line pull");
            Exact(reaction.DownwardLinePushN, Math.Max(0.0, -envelope.AnchorDesignVN), name + " downward line push");
            Exact(reaction.AnchorWeightWaterKg, run.Result.AnchorWeightWaterKg, name + " anchor weight kg");
            Near(reaction.AnchorWeightWaterN, run.Result.AnchorWeightWaterKg * GravityMS2, name + " anchor weight N");
            Near(reaction.SignedNormalReactionN, reaction.AnchorWeightWaterN - envelope.AnchorDesignVN, name + " signed normal");
            Near(reaction.CompressiveNormalReactionN, Math.Max(0.0, reaction.SignedNormalReactionN), name + " compressive normal");
            Near(reaction.UpliftExcessN, Math.Max(0.0, -reaction.SignedNormalReactionN), name + " uplift excess");

            var expectedClassification = reaction.SignedNormalReactionN > 0.0
                ? MooringAnchorContactClassification.CompressiveContact
                : reaction.SignedNormalReactionN < 0.0
                    ? MooringAnchorContactClassification.UpliftSeparation
                    : MooringAnchorContactClassification.ZeroNormalLimit;
            if (reaction.ContactClassification != expectedClassification)
                throw new InvalidOperationException($"F2-B {name}: contact classification mismatch.");

            if (string.IsNullOrWhiteSpace(reaction.MethodNote) ||
                !reaction.MethodNote.Contains("quasi-static", StringComparison.OrdinalIgnoreCase) ||
                !reaction.MethodNote.Contains("does not define", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"F2-B {name}: method note lost the authority boundary.");
            }

            syntheticTemplateEnvelope ??= envelope;
            syntheticTemplateResult ??= run.Result;
            accepted++;

            Console.WriteLine(string.Join("|",
                "F2B_SELECTED_ANCHOR_REACTION_STATE",
                name,
                "CandidateStatus=Accepted",
                "SelectedSource=SignedBoundaryFeedback",
                "Available=True",
                $"InternalAnchorEndHN={F(reaction.InternalAnchorEndHN)}",
                $"InternalAnchorEndVN={F(reaction.InternalAnchorEndVN)}",
                $"HorizontalDemandN={F(reaction.HorizontalDemandN)}",
                $"UpwardLinePullN={F(reaction.UpwardLinePullN)}",
                $"DownwardLinePushN={F(reaction.DownwardLinePushN)}",
                $"AnchorWeightWaterN={F(reaction.AnchorWeightWaterN)}",
                $"SignedNormalReactionN={F(reaction.SignedNormalReactionN)}",
                $"CompressiveNormalReactionN={F(reaction.CompressiveNormalReactionN)}",
                $"UpliftExcessN={F(reaction.UpliftExcessN)}",
                $"ContactClassification={reaction.ContactClassification}",
                "HorizontalCapacityAuthority=None",
                "LegacyAnchorAuthority=Unchanged"));
        }

        if (definitions.Count != 5 || accepted != 2 || unavailable != 3 ||
            syntheticTemplateEnvelope is null || syntheticTemplateResult is null)
        {
            throw new InvalidOperationException(
                $"F2-B canonical coverage mismatch: scenarios={definitions.Count}, accepted={accepted}, unavailable={unavailable}.");
        }

        ValidateKnownProjectionFixtures(syntheticTemplateResult, syntheticTemplateEnvelope);
        ValidateNonPositiveWeightUnavailable(syntheticTemplateResult, syntheticTemplateEnvelope);

        Console.WriteLine(
            "F2B_SELECTED_ANCHOR_REACTION_STATE_ROLLUP|CanonicalScenarios=5|AcceptedAvailable=2|Unavailable=3|KnownContactFixtures=True|ContactClasses=CompressiveContact,ZeroNormalLimit,UpliftSeparation|NonPositiveSubmergedWeightUnavailable=True|HorizontalCapacityAuthority=None|LegacyAnchorAuthority=Unchanged|SelectedGeometry=Unchanged");
        Console.WriteLine("F2B_SELECTED_ANCHOR_REACTION_STATE_END");
    }

    private static void ValidateKnownProjectionFixtures(
        CalculationResult templateResult,
        MooringSelectedDesignEnvelopeState templateEnvelope)
    {
        const double syntheticWeightKg = 10.0;
        var weightN = syntheticWeightKg * GravityMS2;
        AssertKnown(templateResult, templateEnvelope, syntheticWeightKg, weightN - 60.0, 60.0, 0.0, MooringAnchorContactClassification.CompressiveContact, "compressive");
        AssertKnown(templateResult, templateEnvelope, syntheticWeightKg, weightN, 0.0, 0.0, MooringAnchorContactClassification.ZeroNormalLimit, "zero-normal");
        AssertKnown(templateResult, templateEnvelope, syntheticWeightKg, weightN + 40.0, -40.0, 40.0, MooringAnchorContactClassification.UpliftSeparation, "uplift");
        AssertKnown(templateResult, templateEnvelope, syntheticWeightKg, -20.0, weightN + 20.0, 0.0, MooringAnchorContactClassification.CompressiveContact, "downward-push");
    }

    private static void AssertKnown(
        CalculationResult templateResult,
        MooringSelectedDesignEnvelopeState templateEnvelope,
        double weightKg,
        double endVN,
        double expectedSignedNormalN,
        double expectedUpliftN,
        MooringAnchorContactClassification expectedClass,
        string label)
    {
        const double hN = 30.0;
        var envelope = templateEnvelope with
        {
            AnchorDesignHN = hN,
            AnchorDesignVN = endVN,
            AnchorDesignTensionN = Math.Sqrt(hN * hN + endVN * endVN)
        };
        var result = templateResult with { AnchorWeightWaterKg = weightKg };
        var reaction = MooringSelectedAnchorReactionStateProjector.Project(result, envelope)
            ?? throw new InvalidOperationException($"F2-B known fixture {label}: state unexpectedly unavailable.");

        Near(reaction.SignedNormalReactionN, expectedSignedNormalN, "known " + label + " signed normal");
        Near(reaction.CompressiveNormalReactionN, Math.Max(0.0, expectedSignedNormalN), "known " + label + " compressive normal");
        Near(reaction.UpliftExcessN, expectedUpliftN, "known " + label + " uplift");
        if (reaction.ContactClassification != expectedClass)
            throw new InvalidOperationException($"F2-B known fixture {label}: expected {expectedClass}, got {reaction.ContactClassification}.");
    }

    private static void ValidateNonPositiveWeightUnavailable(
        CalculationResult templateResult,
        MooringSelectedDesignEnvelopeState templateEnvelope)
    {
        if (MooringSelectedAnchorReactionStateProjector.Project(
                templateResult with { AnchorWeightWaterKg = 0.0 },
                templateEnvelope) is not null)
        {
            throw new InvalidOperationException("F2-B: zero submerged anchor weight must not expose selected contact authority.");
        }
        if (MooringSelectedAnchorReactionStateProjector.Project(
                templateResult with { AnchorWeightWaterKg = -1.0 },
                templateEnvelope) is not null)
        {
            throw new InvalidOperationException("F2-B: negative submerged anchor weight must not expose selected contact authority.");
        }
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F2-B: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F2-B: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F2-B: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F2-B: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Near(double actual, double expected, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > Tolerance)
            throw new InvalidOperationException($"F2-B {label}: expected {expected:R}, got {actual:R}.");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F2-B {label}: expected exact {expected:R}, got {actual:R}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
