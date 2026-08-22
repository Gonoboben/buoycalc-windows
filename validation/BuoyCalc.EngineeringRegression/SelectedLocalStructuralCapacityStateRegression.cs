using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SelectedLocalStructuralCapacityStateRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    public static void Validate()
    {
        if (MooringSelectedLocalStructuralCapacityStateProjector.Project(
                HistoricalResultForNullCheck(),
                null) is not null)
        {
            throw new InvalidOperationException("F3-C: null local-demand state must not fabricate structural capacity authority.");
        }

        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var available = 0;
        var unavailable = 0;
        var payloadNotRated = 0;
        CalculationResult? syntheticBaseResult = null;
        MooringSelectedLocalElementDemandState? syntheticBaseLocal = null;

        Console.WriteLine("F3C_SELECTED_LOCAL_STRUCTURAL_CAPACITY_BEGIN");

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
                ?? throw new InvalidOperationException($"F3-C {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore;
            var sequence = run.Snapshot.TechnicalReportData.SequencePositions;

            var legacyTensionKn = run.Result.TensionKn;
            var legacyWeakLinkKn = run.Result.WeakLinkBreakingLoadKn;
            var legacyWeakLinkName = run.Result.WeakLinkName;
            var legacyWorkingLoadKn = run.Result.WorkingLoadKn;
            var legacyTensionReserve = run.Result.TensionReserve;
            var legacyAnchorReserve = run.Result.AnchorReserve;
            var legacyVerdict = run.Result.Verdict;
            var legacyMainRisk = run.Result.MainRisk;
            var legacyChecks = run.Result.Checks.ToArray();
            var legacyElementRows = run.Result.ElementRows.ToArray();
            var selectedShapeBefore = selectedCore?.Shape;

            var local = MooringSelectedLocalElementDemandStateProjector.Project(
                run.Result,
                sequence,
                selectedCore,
                candidate);
            var capacity = MooringSelectedLocalStructuralCapacityStateProjector.Project(
                run.Result,
                local);

            if (!AcceptedFixtures.Contains(name))
            {
                if (local is not null || capacity is not null)
                    throw new InvalidOperationException($"F3-C {name}: non-Accepted selection exposed selected capacity authority.");

                unavailable++;
                Console.WriteLine(string.Join("|",
                    "F3C_SELECTED_LOCAL_STRUCTURAL_CAPACITY",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    "Available=False",
                    "LegacyWeakLinkAuthority=Unchanged"));
                continue;
            }

            if (local is null || capacity is null || selectedCore is null ||
                candidate.Status != MooringSignedCandidateStatus.Accepted ||
                selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback)
            {
                throw new InvalidOperationException($"F3-C {name}: Accepted selected capacity prerequisites are incomplete.");
            }

            ValidateCanonical(name, run.Result, local, capacity);

            payloadNotRated += capacity.Rows.Count(x =>
                x.Status == MooringLocalStructuralCapacityStatus.NotRatedByCurrentModel);

            Exact(run.Result.TensionKn, legacyTensionKn, name + " legacy tension unchanged");
            Exact(run.Result.WeakLinkBreakingLoadKn, legacyWeakLinkKn, name + " legacy weak-link MBL unchanged");
            if (run.Result.WeakLinkName != legacyWeakLinkName)
                throw new InvalidOperationException($"F3-C {name}: legacy weak-link name changed.");
            Exact(run.Result.WorkingLoadKn, legacyWorkingLoadKn, name + " legacy global WLL unchanged");
            Exact(run.Result.TensionReserve, legacyTensionReserve, name + " legacy tension reserve unchanged");
            Exact(run.Result.AnchorReserve, legacyAnchorReserve, name + " legacy anchor reserve unchanged");
            if (run.Result.Verdict != legacyVerdict || run.Result.MainRisk != legacyMainRisk)
                throw new InvalidOperationException($"F3-C {name}: legacy verdict/main-risk changed.");
            if (!run.Result.Checks.SequenceEqual(legacyChecks))
                throw new InvalidOperationException($"F3-C {name}: legacy checks changed.");
            if (!run.Result.ElementRows.SequenceEqual(legacyElementRows))
                throw new InvalidOperationException($"F3-C {name}: legacy per-element reserve/status rows changed.");
            if (!ReferenceEquals(selectedCore.Shape, selectedShapeBefore))
                throw new InvalidOperationException($"F3-C {name}: selected X/Z identity changed.");

            if (syntheticBaseResult is null)
            {
                syntheticBaseResult = run.Result;
                syntheticBaseLocal = local;
            }

            available++;
            Console.WriteLine(string.Join("|",
                "F3C_SELECTED_LOCAL_STRUCTURAL_CAPACITY",
                name,
                "CandidateStatus=Accepted",
                "SelectedSource=SignedBoundaryFeedback",
                "Available=True",
                $"ExpectedStructural={capacity.ExpectedStructuralElementCount}",
                $"RatedStructural={capacity.RatedStructuralElementCount}",
                $"IncompleteStructural={capacity.IncompleteStructuralElementCount}",
                $"Insufficient={capacity.InsufficientElementCount}",
                $"CoverageComplete={capacity.StructuralCapacityCoverageComplete}",
                $"GoverningElement={capacity.GoverningElementNumber?.ToString(CultureInfo.InvariantCulture) ?? "None"}",
                $"GoverningReserve={F(capacity.GoverningReserve)}",
                "LegacyWeakLinkAuthority=Unchanged"));
        }

        if (definitions.Count != 5 || available != 2 || unavailable != 3 || payloadNotRated < 1)
        {
            throw new InvalidOperationException(
                $"F3-C canonical coverage mismatch: scenarios={definitions.Count}, available={available}, unavailable={unavailable}, payloadNotRated={payloadNotRated}.");
        }

        if (syntheticBaseResult is null || syntheticBaseLocal is null)
            throw new InvalidOperationException("F3-C: synthetic base Accepted state is unavailable.");

        ValidateSyntheticCapacitySemantics(syntheticBaseResult, syntheticBaseLocal);

        Console.WriteLine(
            "F3C_SELECTED_LOCAL_STRUCTURAL_CAPACITY_ROLLUP|CanonicalScenarios=5|Available=2|Unavailable=3|WLL=MBL/SafetyFactor|Reserve=WLL/LocalDemand|Governing=MinReserveThenSequence|PayloadCapacity=NotRatedByCurrentModel|ConnectorCountScaling=False|LegacyWeakLinkMigration=False|ChecksVerdictMigration=False|SelectedGeometryChanged=False");
        Console.WriteLine("F3C_SELECTED_LOCAL_STRUCTURAL_CAPACITY_END");
    }

    private static void ValidateCanonical(
        string scenario,
        CalculationResult result,
        MooringSelectedLocalElementDemandState local,
        MooringSelectedLocalStructuralCapacityState capacity)
    {
        if (capacity.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
            capacity.WaveHorizontalIncrementN != local.WaveHorizontalIncrementN ||
            capacity.Rows.Count != local.Rows.Count)
        {
            throw new InvalidOperationException($"F3-C {scenario}: selected local-capacity source/count identity changed.");
        }

        var elements = result.ElementRows.ToDictionary(x => x.Number);
        var capacityByNumber = capacity.Rows.ToDictionary(x => x.ElementNumber);
        var expectedCandidates = new List<(int Number, double Reserve)>();
        var expectedStructuralCount = 0;
        var expectedIncomplete = 0;
        var expectedInsufficient = 0;
        var expectedRated = 0;

        foreach (var demand in local.Rows)
        {
            if (!elements.TryGetValue(demand.ElementNumber, out var element) ||
                !capacityByNumber.TryGetValue(demand.ElementNumber, out var row))
            {
                throw new InvalidOperationException($"F3-C {scenario}: element join missing at {demand.ElementNumber}.");
            }

            if (row.Kind != demand.Kind || row.Title != demand.Title || row.PresetName != demand.PresetName ||
                row.Kind != element.Kind || row.Title != element.Title || row.PresetName != element.PresetName ||
                row.Count != element.Count ||
                row.StartAlongLineM != demand.StartAlongLineM ||
                row.EndAlongLineM != demand.EndAlongLineM ||
                row.PositionAlongLineM != demand.PositionAlongLineM ||
                row.DemandLocation != demand.GoverningLocation ||
                row.DemandSegmentNumber != demand.GoverningSegmentNumber ||
                row.DemandAlongLineM != demand.GoverningAlongLineM)
            {
                throw new InvalidOperationException($"F3-C {scenario}: exact sequence/demand provenance changed at {demand.ElementNumber}.");
            }

            var isConnector = string.Equals(element.Kind, "Соединитель", StringComparison.OrdinalIgnoreCase);
            var expectedStructural = demand.IsDistributed || isConnector;
            if (row.IsExpectedStructuralElement != expectedStructural)
                throw new InvalidOperationException($"F3-C {scenario}: structural classification changed at {demand.ElementNumber}.");

            if (!expectedStructural)
            {
                if (row.Status != MooringLocalStructuralCapacityStatus.NotRatedByCurrentModel || row.IsCapacityCandidate)
                    throw new InvalidOperationException($"F3-C {scenario}: payload/non-structural row was capacity-rated at {demand.ElementNumber}.");
                continue;
            }

            expectedStructuralCount++;

            if (!demand.Available || !demand.DesignDemandN.HasValue || demand.DesignDemandN.Value < 0.0)
            {
                ExpectStatus(row, MooringLocalStructuralCapacityStatus.DemandUnavailable, scenario);
                expectedIncomplete++;
                continue;
            }

            if (isConnector && element.Count != 1)
            {
                ExpectStatus(row, MooringLocalStructuralCapacityStatus.UnsupportedConnectorCount, scenario);
                expectedIncomplete++;
                continue;
            }

            if (element.BreakingLoadKn <= 0.0)
            {
                ExpectStatus(row, MooringLocalStructuralCapacityStatus.CapacityUnavailable, scenario);
                expectedIncomplete++;
                continue;
            }

            if (result.SafetyFactor <= 0.0)
            {
                ExpectStatus(row, MooringLocalStructuralCapacityStatus.SafetyFactorUnavailable, scenario);
                expectedIncomplete++;
                continue;
            }

            var expectedWllKn = element.BreakingLoadKn / result.SafetyFactor;
            Exact(row.BreakingLoadKn!.Value, element.BreakingLoadKn, scenario + $" element {element.Number} MBL");
            Exact(row.WorkingLoadKn!.Value, expectedWllKn, scenario + $" element {element.Number} WLL");
            expectedRated++;

            if (demand.DesignDemandN.Value == 0.0)
            {
                ExpectStatus(row, MooringLocalStructuralCapacityStatus.NoPositiveDemand, scenario);
                if (row.IsCapacityCandidate || row.LocalReserve.HasValue)
                    throw new InvalidOperationException($"F3-C {scenario}: zero-demand row fabricated finite reserve at {element.Number}.");
                continue;
            }

            var expectedReserve = expectedWllKn * 1000.0 / demand.DesignDemandN.Value;
            Exact(row.LocalDesignDemandN!.Value, demand.DesignDemandN.Value, scenario + $" element {element.Number} local demand N");
            Exact(row.LocalDesignDemandKn!.Value, demand.DesignDemandN.Value / 1000.0, scenario + $" element {element.Number} local demand kN");
            Exact(row.LocalReserve!.Value, expectedReserve, scenario + $" element {element.Number} local reserve");

            var expectedStatus = expectedReserve >= 1.0
                ? MooringLocalStructuralCapacityStatus.Ok
                : MooringLocalStructuralCapacityStatus.Insufficient;
            ExpectStatus(row, expectedStatus, scenario);
            if (!row.IsCapacityCandidate)
                throw new InvalidOperationException($"F3-C {scenario}: valid capacity row is not a governing candidate at {element.Number}.");

            if (expectedStatus == MooringLocalStructuralCapacityStatus.Insufficient)
                expectedInsufficient++;
            expectedCandidates.Add((element.Number, expectedReserve));
        }

        if (capacity.ExpectedStructuralElementCount != expectedStructuralCount ||
            capacity.RatedStructuralElementCount != expectedRated ||
            capacity.IncompleteStructuralElementCount != expectedIncomplete ||
            capacity.InsufficientElementCount != expectedInsufficient ||
            capacity.StructuralCapacityCoverageComplete != (expectedStructuralCount > 0 && expectedIncomplete == 0))
        {
            throw new InvalidOperationException($"F3-C {scenario}: structural-capacity rollup counts changed.");
        }

        var expectedGoverning = expectedCandidates
            .OrderBy(x => x.Reserve)
            .ThenBy(x => x.Number)
            .Cast<(int Number, double Reserve)?>()
            .FirstOrDefault();

        if (expectedGoverning.HasValue)
        {
            if (capacity.GoverningElementNumber != expectedGoverning.Value.Number)
                throw new InvalidOperationException($"F3-C {scenario}: governing minimum-reserve element changed.");
            Exact(capacity.GoverningReserve!.Value, expectedGoverning.Value.Reserve, scenario + " governing reserve");
        }
        else if (capacity.GoverningElementNumber.HasValue || capacity.GoverningReserve.HasValue)
        {
            throw new InvalidOperationException($"F3-C {scenario}: governing row fabricated without a valid candidate.");
        }
    }

    private static void ValidateSyntheticCapacitySemantics(
        CalculationResult sourceResult,
        MooringSelectedLocalElementDemandState sourceLocal)
    {
        var demandTemplate = sourceLocal.Rows.First(x => x.IsDistributed && x.Available && x.DesignDemandN.HasValue);
        var elementTemplate = sourceResult.ElementRows.Single(x => x.Number == demandTemplate.ElementNumber);
        var orderedSystem = sourceResult.ElementRows.OrderBy(x => x.Number).ToList();
        var top = orderedSystem[0] with { Number = 1 };
        var bottom = orderedSystem[^1] with { Number = 5 };
        const double safetyFactor = 2.0;

        var line = elementTemplate with
        {
            Number = 2,
            Kind = "Линия",
            Title = "F3C synthetic line",
            PresetName = "F3C line MBL",
            Count = 1,
            BreakingLoadKn = 10.0,
            WorkingLoadKn = 5.0
        };
        var connector = elementTemplate with
        {
            Number = 3,
            Kind = "Соединитель",
            Title = "F3C synthetic connector",
            PresetName = "F3C connector MBL",
            Count = 1,
            BreakingLoadKn = 8.0,
            WorkingLoadKn = 4.0
        };
        var payload = elementTemplate with
        {
            Number = 4,
            Kind = "Прибор",
            Title = "F3C synthetic payload",
            PresetName = "F3C payload",
            Count = 1,
            BreakingLoadKn = 0.0,
            WorkingLoadKn = 0.0
        };

        var result = sourceResult with
        {
            SafetyFactor = safetyFactor,
            ElementRows = new[] { top, line, connector, payload, bottom }
        };

        var lineDemand = demandTemplate with
        {
            ElementNumber = 2,
            Kind = line.Kind,
            Title = line.Title,
            PresetName = line.PresetName,
            IsDistributed = true,
            IsDiscrete = false,
            Available = true,
            DesignDemandN = 2500.0
        };
        var connectorDemand = demandTemplate with
        {
            ElementNumber = 3,
            Kind = connector.Kind,
            Title = connector.Title,
            PresetName = connector.PresetName,
            IsDistributed = false,
            IsDiscrete = true,
            Available = true,
            DesignDemandN = 2000.0
        };
        var payloadDemand = demandTemplate with
        {
            ElementNumber = 4,
            Kind = payload.Kind,
            Title = payload.Title,
            PresetName = payload.PresetName,
            IsDistributed = false,
            IsDiscrete = true,
            Available = true,
            DesignDemandN = 1000.0
        };
        var local = sourceLocal with
        {
            Rows = new[] { lineDemand, connectorDemand, payloadDemand },
            DistributedElementCount = 1,
            DiscreteElementCount = 2
        };

        var tie = RequireState(result, local, "tie");
        if (!tie.StructuralCapacityCoverageComplete || tie.GoverningElementNumber != 2)
            throw new InvalidOperationException("F3-C synthetic tie: lower sequence number must govern equal reserve.");
        Exact(tie.GoverningReserve!.Value, 2.0, "synthetic tie reserve");
        ExpectStatus(tie.Rows.Single(x => x.ElementNumber == 4), MooringLocalStructuralCapacityStatus.NotRatedByCurrentModel, "synthetic payload");

        var insufficientLocal = local with
        {
            Rows = new[]
            {
                lineDemand,
                connectorDemand with { DesignDemandN = 5000.0 },
                payloadDemand
            }
        };
        var insufficient = RequireState(result, insufficientLocal, "insufficient");
        if (insufficient.GoverningElementNumber != 3 || insufficient.InsufficientElementCount != 1)
            throw new InvalidOperationException("F3-C synthetic insufficient: connector must govern with one insufficient row.");
        ExpectStatus(insufficient.Rows.Single(x => x.ElementNumber == 3), MooringLocalStructuralCapacityStatus.Insufficient, "synthetic insufficient");
        Exact(insufficient.GoverningReserve!.Value, 0.8, "synthetic insufficient reserve");

        var unsupportedResult = result with
        {
            ElementRows = result.ElementRows
                .Select(x => x.Number == 3 ? x with { Count = 2 } : x)
                .ToArray()
        };
        var unsupported = RequireState(unsupportedResult, local, "unsupported connector count");
        ExpectStatus(unsupported.Rows.Single(x => x.ElementNumber == 3), MooringLocalStructuralCapacityStatus.UnsupportedConnectorCount, "synthetic connector count");
        if (unsupported.StructuralCapacityCoverageComplete || unsupported.IncompleteStructuralElementCount != 1)
            throw new InvalidOperationException("F3-C synthetic connector count: unsupported Count must make structural coverage incomplete.");

        var missingMblResult = result with
        {
            ElementRows = result.ElementRows
                .Select(x => x.Number == 2 ? x with { BreakingLoadKn = 0.0, WorkingLoadKn = 0.0 } : x)
                .ToArray()
        };
        var missingMbl = RequireState(missingMblResult, local, "missing MBL");
        ExpectStatus(missingMbl.Rows.Single(x => x.ElementNumber == 2), MooringLocalStructuralCapacityStatus.CapacityUnavailable, "synthetic missing MBL");
        if (missingMbl.StructuralCapacityCoverageComplete)
            throw new InvalidOperationException("F3-C synthetic missing MBL: missing structural MBL must not claim complete coverage.");

        var unavailableDemandLocal = local with
        {
            Rows = new[]
            {
                lineDemand with { Available = false, DesignDemandN = null },
                connectorDemand,
                payloadDemand
            }
        };
        var unavailableDemand = RequireState(result, unavailableDemandLocal, "unavailable demand");
        ExpectStatus(unavailableDemand.Rows.Single(x => x.ElementNumber == 2), MooringLocalStructuralCapacityStatus.DemandUnavailable, "synthetic unavailable demand");
        if (unavailableDemand.StructuralCapacityCoverageComplete)
            throw new InvalidOperationException("F3-C synthetic unavailable demand: missing demand must not claim complete coverage.");

        var zeroDemandLocal = local with
        {
            Rows = new[]
            {
                lineDemand with { DesignDemandN = 0.0 },
                connectorDemand,
                payloadDemand
            }
        };
        var zeroDemand = RequireState(result, zeroDemandLocal, "zero demand");
        ExpectStatus(zeroDemand.Rows.Single(x => x.ElementNumber == 2), MooringLocalStructuralCapacityStatus.NoPositiveDemand, "synthetic zero demand");
        if (!zeroDemand.StructuralCapacityCoverageComplete)
            throw new InvalidOperationException("F3-C synthetic zero demand: known zero demand is not incomplete capacity coverage.");
    }

    private static MooringSelectedLocalStructuralCapacityState RequireState(
        CalculationResult result,
        MooringSelectedLocalElementDemandState local,
        string label)
    {
        return MooringSelectedLocalStructuralCapacityStateProjector.Project(result, local)
            ?? throw new InvalidOperationException($"F3-C synthetic {label}: capacity state unavailable.");
    }

    private static void ExpectStatus(
        MooringSelectedLocalStructuralCapacityRow row,
        MooringLocalStructuralCapacityStatus expected,
        string scenario)
    {
        if (row.Status != expected)
            throw new InvalidOperationException($"F3-C {scenario}: expected status {expected}, got {row.Status} at element {row.ElementNumber}.");
    }

    private static CalculationResult HistoricalResultForNullCheck()
    {
        var definition = HistoricalDefinitions().Cast<object>().First();
        var environment = Property<EnvironmentInput>(definition, "Environment");
        var buoy = Property<BuoyInput>(definition, "Buoy");
        var assembly = Property<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
        var anchor = Property<AnchorInput>(definition, "Anchor");
        var safetyFactor = Property<double>(definition, "SafetyFactor");
        return ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor).Result;
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F3-C: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F3-C: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F3-C: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F3-C: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F3-C {label}: expected exact {expected:R}, got {actual:R}.");
    }

    private static string F(double? value) =>
        value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : "None";
}
