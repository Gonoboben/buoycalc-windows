using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class AnchorEndReactionOwnershipRegression
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
        ValidateKnownReactionFixtures();

        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var acceptedChecked = 0;
        var unavailableChecked = 0;

        Console.WriteLine("F2A_ANCHOR_END_REACTION_OWNERSHIP_BEGIN");

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
                ?? throw new InvalidOperationException($"F2-A {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore;
            var envelope = MooringSelectedDesignEnvelopeStateProjector.Project(run.Result, selectedCore, candidate);
            var legacyRequiredHoldingKg = run.Result.RequiredAnchorHoldingKg;
            var legacyAnchorHoldingKg = run.Result.AnchorHoldingKg;
            var legacyAnchorReserve = run.Result.AnchorReserve;

            ValidateSequenceDirection(run.Result, name);

            var independentAnchorWeightWaterKg =
                anchor.WeightAirKg - environment.EffectiveWaterDensityKgM3 * anchor.VolumeM3;
            Near(
                run.Result.AnchorWeightWaterKg,
                independentAnchorWeightWaterKg,
                name + " independent anchor submerged weight");

            if (!AcceptedFixtures.Contains(name))
            {
                if (envelope is not null)
                    throw new InvalidOperationException($"F2-A {name}: non-Accepted scenario exposed selected anchor-end design evidence.");

                Exact(run.Result.RequiredAnchorHoldingKg, legacyRequiredHoldingKg, name + " legacy required holding unchanged");
                Exact(run.Result.AnchorHoldingKg, legacyAnchorHoldingKg, name + " legacy holding unchanged");
                Exact(run.Result.AnchorReserve, legacyAnchorReserve, name + " legacy anchor reserve unchanged");

                unavailableChecked++;
                Console.WriteLine(string.Join("|",
                    "F2A_ANCHOR_END_REACTION_OWNERSHIP",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    $"SelectedSource={selectedCore?.SourceIdentity.ToString() ?? "None"}",
                    "SelectedAnchorReactionEvidence=False",
                    $"AnchorWeightWaterKg={F(independentAnchorWeightWaterKg)}",
                    "LegacyAnchorAuthority=Unchanged"));
                continue;
            }

            if (envelope is null || selectedCore is null || candidate.Shape is null)
                throw new InvalidOperationException($"F2-A {name}: Accepted selected design envelope is unavailable.");

            var signed = MooringSelectedSignedBoundaryStateProjector.Project(selectedCore, candidate)
                ?? throw new InvalidOperationException($"F2-A {name}: selected signed boundary state is unavailable.");

            if (candidate.Status != MooringSignedCandidateStatus.Accepted ||
                selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                !ReferenceEquals(selectedCore.Shape, candidate.Shape))
            {
                throw new InvalidOperationException($"F2-A {name}: selected signed source identity changed.");
            }

            Near(envelope.AnchorDesignHN, signed.EndHN + run.Result.WaveForceN, name + " wave-aware anchor-end H");
            Exact(envelope.AnchorDesignVN, signed.EndVN, name + " anchor-end V wave invariance");
            Near(
                envelope.AnchorDesignTensionN,
                Magnitude(envelope.AnchorDesignHN, envelope.AnchorDesignVN),
                name + " anchor-end resultant");

            // +Z is downward and the signed line state follows surface -> anchor.
            // Newton's third law therefore gives line-on-anchor = - internal lower-end vector.
            var lineOnAnchorHN = -envelope.AnchorDesignHN;
            var lineOnAnchorVDepthPositiveN = -envelope.AnchorDesignVN;
            Exact(lineOnAnchorHN, -envelope.AnchorDesignHN, name + " line-on-anchor H action/reaction");
            Exact(lineOnAnchorVDepthPositiveN, -envelope.AnchorDesignVN, name + " line-on-anchor V action/reaction");

            var anchorWeightN = independentAnchorWeightWaterKg * GravityMS2;
            if (!double.IsFinite(anchorWeightN) || anchorWeightN <= 0.0)
                throw new InvalidOperationException($"F2-A {name}: canonical anchor submerged weight must be finite and positive.");

            var reaction = Reaction(anchorWeightN, envelope.AnchorDesignVN);
            var upwardLinePullN = Math.Max(0.0, envelope.AnchorDesignVN);
            var downwardLinePushN = Math.Max(0.0, -envelope.AnchorDesignVN);
            Near(
                reaction.NormalReactionN,
                anchorWeightN + lineOnAnchorVDepthPositiveN,
                name + " independent depth-positive equilibrium closure");
            Near(reaction.UpliftExcessN, Math.Max(0.0, envelope.AnchorDesignVN - anchorWeightN), name + " uplift excess");

            Exact(run.Result.RequiredAnchorHoldingKg, legacyRequiredHoldingKg, name + " legacy required holding unchanged");
            Exact(run.Result.AnchorHoldingKg, legacyAnchorHoldingKg, name + " legacy holding unchanged");
            Exact(run.Result.AnchorReserve, legacyAnchorReserve, name + " legacy anchor reserve unchanged");

            acceptedChecked++;
            Console.WriteLine(string.Join("|",
                "F2A_ANCHOR_END_REACTION_OWNERSHIP",
                name,
                "CandidateStatus=Accepted",
                "SelectedSource=SignedBoundaryFeedback",
                "SelectedAnchorReactionEvidence=True",
                "CoordinateZ=PositiveDown",
                "InternalLineDirection=SurfaceToAnchor",
                $"AnchorDesignHN={F(envelope.AnchorDesignHN)}",
                $"AnchorDesignVN={F(envelope.AnchorDesignVN)}",
                $"LineOnAnchorHN={F(lineOnAnchorHN)}",
                $"LineOnAnchorVDepthPositiveN={F(lineOnAnchorVDepthPositiveN)}",
                $"HorizontalDemandN={F(Math.Abs(envelope.AnchorDesignHN))}",
                $"UpwardLinePullN={F(upwardLinePullN)}",
                $"DownwardLinePushN={F(downwardLinePushN)}",
                $"AnchorWeightWaterN={F(anchorWeightN)}",
                $"NormalReactionN={F(reaction.NormalReactionN)}",
                $"UpliftExcessN={F(reaction.UpliftExcessN)}",
                $"ContactState={reaction.ContactState}",
                "HorizontalCapacityMigration=None",
                "LegacyAnchorAuthority=Unchanged"));
        }

        if (definitions.Count != 5 || acceptedChecked != 2 || unavailableChecked != 3)
        {
            throw new InvalidOperationException(
                $"F2-A canonical coverage mismatch: scenarios={definitions.Count}, accepted={acceptedChecked}, unavailable={unavailableChecked}.");
        }

        Console.WriteLine(
            "F2A_ANCHOR_END_REACTION_OWNERSHIP_ROLLUP|KnownReactionFixtures=True|CanonicalScenarios=5|AcceptedEvidence=2|UnavailableEvidence=3|CoordinateZ=PositiveDown|InternalDirection=SurfaceToAnchor|LineOnAnchor=OppositeEndVector|NormalReactionEquation=Wsubmerged-EndV|HorizontalCapacityMigration=None|LegacyAnchorAuthority=Unchanged");
        Console.WriteLine("F2A_ANCHOR_END_REACTION_OWNERSHIP_END");
    }

    private static void ValidateKnownReactionFixtures()
    {
        AssertReaction(100.0, 40.0, 60.0, 0.0, "CompressiveContact", "positive normal");
        AssertReaction(100.0, 100.0, 0.0, 0.0, "ZeroNormalLimit", "zero-normal limit");
        AssertReaction(100.0, 140.0, -40.0, 40.0, "UpliftSeparation", "uplift excess");
        AssertReaction(100.0, -20.0, 120.0, 0.0, "CompressiveContact", "downward line push");
    }

    private static void AssertReaction(
        double anchorWeightN,
        double endVN,
        double expectedNormalN,
        double expectedUpliftN,
        string expectedState,
        string label)
    {
        var actual = Reaction(anchorWeightN, endVN);
        Exact(actual.NormalReactionN, expectedNormalN, label + " normal");
        Exact(actual.UpliftExcessN, expectedUpliftN, label + " uplift");
        if (!string.Equals(actual.ContactState, expectedState, StringComparison.Ordinal))
            throw new InvalidOperationException($"F2-A {label}: expected state {expectedState}, got {actual.ContactState}.");
    }

    private static ReactionEvidence Reaction(double anchorWeightN, double endVN)
    {
        var normalN = anchorWeightN - endVN;
        var upliftN = Math.Max(0.0, -normalN);
        var state = normalN > 0.0
            ? "CompressiveContact"
            : normalN < 0.0
                ? "UpliftSeparation"
                : "ZeroNormalLimit";
        return new ReactionEvidence(normalN, upliftN, state);
    }

    private static void ValidateSequenceDirection(CalculationResult result, string name)
    {
        var sequence = MooringSequencePositioner.Build(result);
        var buoy = sequence.Rows.FirstOrDefault(x => string.Equals(x.Kind, "Буй", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"F2-A {name}: buoy boundary row is missing.");
        var anchor = sequence.Rows.FirstOrDefault(x => string.Equals(x.Kind, "Якорь", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"F2-A {name}: anchor boundary row is missing.");

        Exact(buoy.PositionAlongLineM, 0.0, name + " buoy s=0");
        Exact(anchor.PositionAlongLineM, sequence.TotalLineLengthM, name + " anchor s=L");
        if (!string.Equals(buoy.SolverRole, "верхний граничный узел", StringComparison.Ordinal) ||
            !string.Equals(anchor.SolverRole, "нижний граничный узел", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"F2-A {name}: boundary role identity changed.");
        }
    }

    private static double Magnitude(double hN, double vN) => Math.Sqrt(hN * hN + vN * vN);

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F2-A: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F2-A: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F2-A: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F2-A: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Near(double actual, double expected, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > Tolerance)
            throw new InvalidOperationException($"F2-A {label}: expected {expected:R}, got {actual:R}.");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F2-A {label}: expected exact {expected:R}, got {actual:R}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record ReactionEvidence(double NormalReactionN, double UpliftExcessN, string ContactState);
}
