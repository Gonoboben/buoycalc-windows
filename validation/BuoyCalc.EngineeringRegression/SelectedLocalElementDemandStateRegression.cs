using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SelectedLocalElementDemandStateRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    private const double GravityMS2 = 9.80665;
    private const double LengthToleranceM = 1e-9;
    private const double ForceToleranceN = 1e-7;

    private sealed record ReferenceLineCandidate(
        double TensionN,
        MooringLocalElementDemandLocationKind Location,
        int SegmentNumber,
        double AlongLineM,
        double SteadyHN,
        double SteadyVN);

    public static void Validate()
    {
        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var available = 0;
        var unavailable = 0;
        var withPoints = 0;
        var withoutPoints = 0;

        Console.WriteLine("F3B_SELECTED_LOCAL_ELEMENT_DEMAND_BEGIN");

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
                ?? throw new InvalidOperationException($"F3-B {name}: signed candidate is missing.");
            var selectedCore = run.Snapshot.ShadowSelectedCore;
            var sequence = run.Snapshot.TechnicalReportData.SequencePositions;

            var legacyTensionKn = run.Result.TensionKn;
            var legacyWeakLinkKn = run.Result.WeakLinkBreakingLoadKn;
            var legacyWeakLinkName = run.Result.WeakLinkName;
            var legacyWorkingLoadKn = run.Result.WorkingLoadKn;
            var legacyTensionReserve = run.Result.TensionReserve;
            var legacyAnchorReserve = run.Result.AnchorReserve;
            var legacyChecks = run.Result.Checks.ToArray();
            var selectedShapeBefore = selectedCore?.Shape;

            var local = MooringSelectedLocalElementDemandStateProjector.Project(
                run.Result,
                sequence,
                selectedCore,
                candidate);

            if (!AcceptedFixtures.Contains(name))
            {
                if (local is not null)
                    throw new InvalidOperationException($"F3-B {name}: non-Accepted selection exposed local element demand.");
                unavailable++;
                Console.WriteLine(string.Join("|",
                    "F3B_SELECTED_LOCAL_ELEMENT_DEMAND",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    "Available=False",
                    "WeakLinkAuthority=LegacyUnchanged"));
                continue;
            }

            if (local is null ||
                candidate.Status != MooringSignedCandidateStatus.Accepted ||
                candidate.FinalTensionTrace is null ||
                selectedCore is null ||
                selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                !ReferenceEquals(selectedCore.Shape, candidate.Shape))
            {
                throw new InvalidOperationException($"F3-B {name}: Accepted local-demand prerequisites are incomplete.");
            }

            var trace = candidate.FinalTensionTrace;
            var internalSequence = sequence.Rows
                .OrderBy(x => x.Number)
                .Skip(1)
                .SkipLast(1)
                .ToList();
            var localByNumber = local.Rows.ToDictionary(x => x.ElementNumber);

            if (local.Rows.Count != internalSequence.Count ||
                local.MappedTraceSegmentCount != trace.Rows.Count ||
                local.ResolvedPointLoadCount != candidate.PointLoadCrossings ||
                local.DistributedElementCount != internalSequence.Count(x => x.IsDistributed) ||
                local.DiscreteElementCount != internalSequence.Count(x => x.IsDiscrete))
            {
                throw new InvalidOperationException($"F3-B {name}: local-demand element/segment/point counts changed.");
            }

            Near(local.WaveHorizontalIncrementN, run.Result.WaveForceN, name + " wave identity");

            foreach (var element in internalSequence)
            {
                if (!localByNumber.TryGetValue(element.Number, out var row))
                    throw new InvalidOperationException($"F3-B {name}: local row missing for element {element.Number}.");

                if (row.Kind != element.Kind ||
                    row.Title != element.Title ||
                    row.PresetName != element.PresetName ||
                    row.StartAlongLineM != element.StartAlongLineM ||
                    row.EndAlongLineM != element.EndAlongLineM ||
                    row.PositionAlongLineM != element.PositionAlongLineM ||
                    row.IsDistributed != element.IsDistributed ||
                    row.IsDiscrete != element.IsDiscrete)
                {
                    throw new InvalidOperationException($"F3-B {name}: sequence identity changed for element {element.Number}.");
                }

                if (element.IsDistributed)
                    ValidateLine(name, element, row, trace, run.Result.WaveForceN);
                else if (element.IsDiscrete)
                    ValidatePoint(name, element, row, run.Result.WaveForceN);
                else
                    throw new InvalidOperationException($"F3-B {name}: unsupported internal sequence role at {element.Number}.");
            }

            Exact(run.Result.TensionKn, legacyTensionKn, name + " legacy tension unchanged");
            Exact(run.Result.WeakLinkBreakingLoadKn, legacyWeakLinkKn, name + " legacy weak-link MBL unchanged");
            if (run.Result.WeakLinkName != legacyWeakLinkName)
                throw new InvalidOperationException($"F3-B {name}: legacy weak-link name changed.");
            Exact(run.Result.WorkingLoadKn, legacyWorkingLoadKn, name + " legacy WLL unchanged");
            Exact(run.Result.TensionReserve, legacyTensionReserve, name + " legacy tension reserve unchanged");
            Exact(run.Result.AnchorReserve, legacyAnchorReserve, name + " legacy anchor reserve unchanged");
            if (!run.Result.Checks.SequenceEqual(legacyChecks))
                throw new InvalidOperationException($"F3-B {name}: legacy checks changed.");
            if (!ReferenceEquals(selectedCore.Shape, selectedShapeBefore))
                throw new InvalidOperationException($"F3-B {name}: selected X/Z identity changed.");

            if (candidate.ContainsDiscreteLoads)
                withPoints++;
            else
                withoutPoints++;
            available++;

            var maxLocal = local.Rows
                .Where(x => x.Available && x.DesignDemandN.HasValue)
                .OrderByDescending(x => x.DesignDemandN!.Value)
                .ThenBy(x => x.ElementNumber)
                .FirstOrDefault();

            Console.WriteLine(string.Join("|",
                "F3B_SELECTED_LOCAL_ELEMENT_DEMAND",
                name,
                "CandidateStatus=Accepted",
                "SelectedSource=SignedBoundaryFeedback",
                "Available=True",
                $"InternalElements={local.Rows.Count}",
                $"DistributedElements={local.DistributedElementCount}",
                $"DiscreteElements={local.DiscreteElementCount}",
                $"TraceSegments={local.MappedTraceSegmentCount}",
                $"PointLoads={local.ResolvedPointLoadCount}",
                $"WaveForceN={F(local.WaveHorizontalIncrementN)}",
                $"MaxLocalDemandN={F(maxLocal?.DesignDemandN ?? 0.0)}",
                $"MaxLocalElement={maxLocal?.ElementNumber.ToString(CultureInfo.InvariantCulture) ?? "None"}",
                "LineDemand=MaxStartMidEnd",
                "PointDemand=MaxBeforeAfterOwnJump",
                "WeakLinkAuthority=LegacyUnchanged"));
        }

        if (definitions.Count != 5 || available != 2 || unavailable != 3 || withPoints != 1 || withoutPoints != 1)
        {
            throw new InvalidOperationException(
                $"F3-B canonical coverage mismatch: scenarios={definitions.Count}, available={available}, unavailable={unavailable}, withPoints={withPoints}, withoutPoints={withoutPoints}.");
        }

        Console.WriteLine(
            "F3B_SELECTED_LOCAL_ELEMENT_DEMAND_ROLLUP|CanonicalScenarios=5|Available=2|Unavailable=3|WithPointLoads=1|WithoutPointLoads=1|LineDemand=MaxStartMidEnd|PointDemand=MaxBeforeAfterOwnJump|WaveAppliedOnceToH=True|PointJumpClosure=True|SelectedGeometryChanged=False|WeakLinkMigration=False");
        Console.WriteLine("F3B_SELECTED_LOCAL_ELEMENT_DEMAND_END");
    }

    private static void ValidateLine(
        string scenario,
        MooringSequencePositionRow element,
        MooringSelectedLocalElementDemandRow row,
        MooringSurfaceBoundaryTensionTraceResult trace,
        double waveN)
    {
        var segments = trace.Rows
            .Where(x =>
                x.StartLengthM + LengthToleranceM >= element.StartAlongLineM &&
                x.EndLengthM <= element.EndAlongLineM + LengthToleranceM &&
                x.EndLengthM > x.StartLengthM)
            .OrderBy(x => x.SegmentNumber)
            .ToList();

        if (segments.Count == 0)
        {
            if (row.Available || row.DesignDemandN.HasValue || row.AvailabilityReason != "NoProductionSegmentsInLineRange")
                throw new InvalidOperationException($"F3-B {scenario}: zero-segment line availability semantics changed at {element.Number}.");
            return;
        }

        var candidates = new List<ReferenceLineCandidate>(segments.Count * 3);
        foreach (var segment in segments)
        {
            candidates.Add(Reference(segment.StartHN, segment.StartVN, MooringLocalElementDemandLocationKind.LineStart, segment.SegmentNumber, segment.StartLengthM));
            candidates.Add(Reference(segment.MidHN, segment.MidVN, MooringLocalElementDemandLocationKind.LineMidpoint, segment.SegmentNumber, segment.MidLengthM));
            candidates.Add(Reference(segment.EndHN, segment.EndVN, MooringLocalElementDemandLocationKind.LineEnd, segment.SegmentNumber, segment.EndLengthM));
        }

        var expected = candidates
            .OrderByDescending(x => x.TensionN)
            .ThenBy(x => x.AlongLineM)
            .ThenBy(x => Rank(x.Location))
            .ThenBy(x => x.SegmentNumber)
            .First();

        if (!row.Available || !row.DesignDemandN.HasValue || !row.GoverningLocation.HasValue ||
            !row.GoverningSegmentNumber.HasValue || !row.GoverningAlongLineM.HasValue ||
            !row.GoverningSteadyHN.HasValue || !row.GoverningSteadyVN.HasValue ||
            !row.GoverningDesignHN.HasValue || !row.GoverningDesignVN.HasValue)
        {
            throw new InvalidOperationException($"F3-B {scenario}: line demand state incomplete at element {element.Number}.");
        }

        Near(row.DesignDemandN.Value, expected.TensionN, scenario + $" line {element.Number} demand");
        if (row.GoverningLocation.Value != expected.Location || row.GoverningSegmentNumber.Value != expected.SegmentNumber)
            throw new InvalidOperationException($"F3-B {scenario}: line governing location changed at element {element.Number}.");
        Exact(row.GoverningAlongLineM.Value, expected.AlongLineM, scenario + $" line {element.Number} governing s");
        Exact(row.GoverningSteadyHN.Value, expected.SteadyHN, scenario + $" line {element.Number} steady H");
        Exact(row.GoverningSteadyVN.Value, expected.SteadyVN, scenario + $" line {element.Number} steady V");
        Exact(row.GoverningDesignHN.Value, expected.SteadyHN + waveN, scenario + $" line {element.Number} design H");
        Exact(row.GoverningDesignVN.Value, expected.SteadyVN, scenario + $" line {element.Number} design V");

        ReferenceLineCandidate Reference(
            double h,
            double v,
            MooringLocalElementDemandLocationKind location,
            int segmentNumber,
            double alongLineM)
        {
            var designH = h + waveN;
            return new ReferenceLineCandidate(
                Math.Sqrt(designH * designH + v * v),
                location,
                segmentNumber,
                alongLineM,
                h,
                v);
        }
    }

    private static void ValidatePoint(
        string scenario,
        MooringSequencePositionRow element,
        MooringSelectedLocalElementDemandRow row,
        double waveN)
    {
        if (!row.Available ||
            !row.DesignDemandN.HasValue ||
            !row.GoverningLocation.HasValue ||
            !row.GoverningSteadyHN.HasValue ||
            !row.GoverningSteadyVN.HasValue ||
            !row.GoverningDesignHN.HasValue ||
            !row.GoverningDesignVN.HasValue ||
            !row.PointBeforeSteadyHN.HasValue ||
            !row.PointBeforeSteadyVN.HasValue ||
            !row.PointBeforeDesignTensionN.HasValue ||
            !row.PointAfterSteadyHN.HasValue ||
            !row.PointAfterSteadyVN.HasValue ||
            !row.PointAfterDesignTensionN.HasValue)
        {
            throw new InvalidOperationException($"F3-B {scenario}: point demand state incomplete at element {element.Number}.");
        }

        Near(
            row.PointAfterSteadyHN.Value,
            row.PointBeforeSteadyHN.Value + element.CurrentForceN,
            scenario + $" point {element.Number} H jump");
        Near(
            row.PointAfterSteadyVN.Value,
            row.PointBeforeSteadyVN.Value - element.WeightWaterKg * GravityMS2,
            scenario + $" point {element.Number} V jump");

        var beforeDesignH = row.PointBeforeSteadyHN.Value + waveN;
        var beforeExpected = Math.Sqrt(
            beforeDesignH * beforeDesignH +
            row.PointBeforeSteadyVN.Value * row.PointBeforeSteadyVN.Value);
        var afterDesignH = row.PointAfterSteadyHN.Value + waveN;
        var afterExpected = Math.Sqrt(
            afterDesignH * afterDesignH +
            row.PointAfterSteadyVN.Value * row.PointAfterSteadyVN.Value);

        Near(row.PointBeforeDesignTensionN.Value, beforeExpected, scenario + $" point {element.Number} before resultant");
        Near(row.PointAfterDesignTensionN.Value, afterExpected, scenario + $" point {element.Number} after resultant");

        var afterGoverns = afterExpected > beforeExpected;
        var expectedDemand = afterGoverns ? afterExpected : beforeExpected;
        var expectedLocation = afterGoverns
            ? MooringLocalElementDemandLocationKind.PointAfter
            : MooringLocalElementDemandLocationKind.PointBefore;
        Near(row.DesignDemandN.Value, expectedDemand, scenario + $" point {element.Number} governing demand");
        if (row.GoverningLocation.Value != expectedLocation)
            throw new InvalidOperationException($"F3-B {scenario}: point governing side changed at element {element.Number}.");

        var expectedH = afterGoverns ? row.PointAfterSteadyHN.Value : row.PointBeforeSteadyHN.Value;
        var expectedV = afterGoverns ? row.PointAfterSteadyVN.Value : row.PointBeforeSteadyVN.Value;
        Exact(row.GoverningSteadyHN.Value, expectedH, scenario + $" point {element.Number} governing steady H");
        Exact(row.GoverningSteadyVN.Value, expectedV, scenario + $" point {element.Number} governing steady V");
        Exact(row.GoverningDesignHN.Value, expectedH + waveN, scenario + $" point {element.Number} governing design H");
        Exact(row.GoverningDesignVN.Value, expectedV, scenario + $" point {element.Number} governing design V");
    }

    private static int Rank(MooringLocalElementDemandLocationKind location) => location switch
    {
        MooringLocalElementDemandLocationKind.LineStart => 0,
        MooringLocalElementDemandLocationKind.LineMidpoint => 1,
        MooringLocalElementDemandLocationKind.LineEnd => 2,
        _ => 3
    };

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F3-B: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F3-B: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F3-B: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F3-B: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Near(double actual, double expected, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > ForceToleranceN)
            throw new InvalidOperationException($"F3-B {label}: expected {expected:R}, got {actual:R}.");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F3-B {label}: expected exact {expected:R}, got {actual:R}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
