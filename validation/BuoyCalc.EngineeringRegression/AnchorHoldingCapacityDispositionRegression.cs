using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class AnchorHoldingCapacityDispositionRegression
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
        ValidateDeadweightReferenceFixtures();
        ValidateCoefficientContractGap();

        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var accepted = 0;
        var unavailable = 0;

        Console.WriteLine("F2C_ANCHOR_CAPACITY_DISPOSITION_BEGIN");

        foreach (var definition in definitions)
        {
            var name = Property<string>(definition, "Name");
            var environment = Property<EnvironmentInput>(definition, "Environment");
            var buoy = Property<BuoyInput>(definition, "Buoy");
            var assembly = Property<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = Property<AnchorInput>(definition, "Anchor");
            var safetyFactor = Property<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
            var result = run.Result;
            var candidate = run.Snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"F2-C {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore;
            var envelope = MooringSelectedDesignEnvelopeStateProjector.Project(result, selectedCore, candidate);
            var reaction = MooringSelectedAnchorReactionStateProjector.Project(result, envelope);

            var legacyCapacityKg = result.AnchorWeightWaterKg *
                result.AnchorBaseHoldingCoefficient *
                result.AnchorTypeMultiplier *
                result.SeabedHoldingMultiplier;
            var legacyDemandKg = result.HorizontalForceN / GravityMS2;
            var legacyReserve = legacyDemandKg > 0.0 ? legacyCapacityKg / legacyDemandKg : 0.0;

            Near(result.AnchorHoldingKg, legacyCapacityKg, name + " legacy capacity algebra");
            Near(result.RequiredAnchorHoldingKg, legacyDemandKg, name + " legacy demand algebra");
            Near(result.AnchorReserve, legacyReserve, name + " legacy reserve algebra");

            if (!AcceptedFixtures.Contains(name))
            {
                if (reaction is not null)
                    throw new InvalidOperationException($"F2-C {name}: selected anchor reaction unexpectedly available.");

                unavailable++;
                Console.WriteLine(string.Join("|",
                    "F2C_ANCHOR_CAPACITY_DISPOSITION",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    "SelectedAnchorReactionAvailable=False",
                    $"LegacyHorizontalDemandN={F(result.HorizontalForceN)}",
                    $"LegacyCapacityN={F(result.AnchorHoldingKg * GravityMS2)}",
                    $"LegacyAnchorReserve={F(result.AnchorReserve)}",
                    "CapacityAuthority=LegacyCompatibilityOnly"));
                continue;
            }

            if (reaction is null || envelope is null)
                throw new InvalidOperationException($"F2-C {name}: Accepted selected anchor reaction is missing.");

            PositiveFinite(reaction.HorizontalDemandN, name + " selected horizontal demand");
            PositiveFinite(result.AnchorWeightWaterKg, name + " submerged anchor weight");

            var legacyFactor = result.AnchorBaseHoldingCoefficient *
                result.AnchorTypeMultiplier *
                result.SeabedHoldingMultiplier;
            var counterfactualCapacityN = reaction.CompressiveNormalReactionN * legacyFactor;
            var counterfactualReserve = reaction.HorizontalDemandN > 0.0
                ? counterfactualCapacityN / reaction.HorizontalDemandN
                : 0.0;
            var legacyDemandN = result.RequiredAnchorHoldingKg * GravityMS2;
            var legacyCapacityN = result.AnchorHoldingKg * GravityMS2;
            var demandDeltaN = reaction.HorizontalDemandN - legacyDemandN;

            if (!double.IsFinite(legacyFactor) || legacyFactor < 0.0 ||
                !double.IsFinite(counterfactualCapacityN) ||
                !double.IsFinite(counterfactualReserve) ||
                !double.IsFinite(demandDeltaN))
            {
                throw new InvalidOperationException($"F2-C {name}: capacity evidence is non-finite.");
            }

            accepted++;
            Console.WriteLine(string.Join("|",
                "F2C_ANCHOR_CAPACITY_DISPOSITION",
                name,
                "CandidateStatus=Accepted",
                "SelectedAnchorReactionAvailable=True",
                $"AnchorType={anchor.Type}",
                $"Contact={reaction.ContactClassification}",
                $"AnchorWeightWaterN={F(reaction.AnchorWeightWaterN)}",
                $"AnchorDesignVN={F(reaction.InternalAnchorEndVN)}",
                $"CompressiveNormalN={F(reaction.CompressiveNormalReactionN)}",
                $"UpliftExcessN={F(reaction.UpliftExcessN)}",
                $"LegacyHorizontalDemandN={F(legacyDemandN)}",
                $"SelectedHorizontalDemandN={F(reaction.HorizontalDemandN)}",
                $"DemandDeltaSelectedMinusLegacyN={F(demandDeltaN)}",
                $"LegacyCapacityN={F(legacyCapacityN)}",
                $"LegacyAnchorReserve={F(result.AnchorReserve)}",
                $"LegacyHoldingFactor={F(legacyFactor)}",
                $"CounterfactualNormalTimesLegacyFactorN={F(counterfactualCapacityN)}",
                $"CounterfactualReserve={F(counterfactualReserve)}",
                "CounterfactualAuthority=None",
                "LegacyFactorAsFrictionCoefficient=NotValidated",
                "ProductionMigrationAuthorized=False"));
        }

        if (definitions.Count != 5 || accepted != 2 || unavailable != 3)
        {
            throw new InvalidOperationException(
                $"F2-C canonical coverage mismatch: scenarios={definitions.Count}, accepted={accepted}, unavailable={unavailable}.");
        }

        Console.WriteLine(
            "F2C_ANCHOR_CAPACITY_DISPOSITION_ROLLUP|CanonicalScenarios=5|AcceptedSelectedReaction=2|UnavailableSelectedReaction=3|DeadweightReference=H<=mu*(Wsub-Vup)|ExplicitMuInputAvailable=False|LegacyCapacityTransfer=NotAuthorized|DeadweightSelectedCapacity=RequiresExplicitFrictionCoefficient|EmbeddedAnchorCapacity=RequiresAdditionalSoilEmbedmentModel|LegacyAnchorReserve=CompatibilityOnly|ProductionMigrationAuthorized=False");
        Console.WriteLine("F2C_ANCHOR_CAPACITY_DISPOSITION_END");
    }

    private static void ValidateDeadweightReferenceFixtures()
    {
        Near(DeadweightHorizontalCapacityN(1000.0, 0.0, 0.5), 500.0, "deadweight zero vertical pull");
        Near(DeadweightHorizontalCapacityN(1000.0, 200.0, 0.5), 400.0, "deadweight positive vertical pull");
        Near(DeadweightHorizontalCapacityN(1000.0, 1000.0, 0.5), 0.0, "deadweight zero-normal limit");
        Near(DeadweightHorizontalCapacityN(1000.0, 1200.0, 0.5), 0.0, "deadweight uplift separation has no friction capacity");
        Near(DeadweightRequiredSubmergedWeightN(400.0, 200.0, 0.5), 1000.0, "deadweight required submerged weight");

        var withoutPull = DeadweightHorizontalCapacityN(1000.0, 0.0, 0.5);
        var withPull = DeadweightHorizontalCapacityN(1000.0, 200.0, 0.5);
        if (!(withPull < withoutPull))
            throw new InvalidOperationException("F2-C deadweight reference: upward line pull must reduce horizontal friction capacity.");
    }

    private static void ValidateCoefficientContractGap()
    {
        if (typeof(AnchorInput).GetProperty("FrictionCoefficient", BindingFlags.Instance | BindingFlags.Public) is not null ||
            typeof(SeabedPreset).GetProperty("FrictionCoefficient", BindingFlags.Instance | BindingFlags.Public) is not null)
        {
            throw new InvalidOperationException(
                "F2-C coefficient-contract assumption changed: an explicit friction coefficient now exists and requires re-validation.");
        }

        if (AnchorCatalog.Presets.Count == 0 ||
            AnchorCatalog.Presets.Any(x => !x.Note.Contains("Учеб", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "F2-C anchor preset provenance changed: expected current holding coefficients to remain explicitly educational presets.");
        }

        if (SeabedCatalog.Presets.Count == 0 ||
            SeabedCatalog.Presets.All(x => string.IsNullOrWhiteSpace(x.Note)))
        {
            throw new InvalidOperationException("F2-C seabed holding-multiplier provenance is unavailable.");
        }
    }

    private static double DeadweightHorizontalCapacityN(double submergedWeightN, double upwardPullN, double frictionCoefficient)
    {
        if (!double.IsFinite(submergedWeightN) ||
            !double.IsFinite(upwardPullN) ||
            !double.IsFinite(frictionCoefficient) ||
            submergedWeightN < 0.0 ||
            upwardPullN < 0.0 ||
            frictionCoefficient <= 0.0)
        {
            throw new InvalidOperationException("F2-C deadweight reference requires finite non-negative W/V and positive mu.");
        }

        return frictionCoefficient * Math.Max(0.0, submergedWeightN - upwardPullN);
    }

    private static double DeadweightRequiredSubmergedWeightN(double horizontalDemandN, double upwardPullN, double frictionCoefficient)
    {
        if (!double.IsFinite(horizontalDemandN) ||
            !double.IsFinite(upwardPullN) ||
            !double.IsFinite(frictionCoefficient) ||
            horizontalDemandN < 0.0 ||
            upwardPullN < 0.0 ||
            frictionCoefficient <= 0.0)
        {
            throw new InvalidOperationException("F2-C required-weight reference requires finite non-negative H/V and positive mu.");
        }

        return upwardPullN + horizontalDemandN / frictionCoefficient;
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F2-C: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F2-C: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F2-C: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F2-C: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Near(double actual, double expected, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > Tolerance)
            throw new InvalidOperationException($"F2-C {label}: expected {expected:R}, got {actual:R}.");
    }

    private static void PositiveFinite(double value, string label)
    {
        if (!double.IsFinite(value) || value <= 0.0)
            throw new InvalidOperationException($"F2-C {label}: expected finite positive value, got {value:R}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
