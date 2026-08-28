using System.Globalization;
using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class HistoricalGoldenImpactRegression
{
    private const int FeedbackBudget = 64;
    private const double GeometryToleranceM = 1e-9;
    private const double ForceToleranceN = 1e-6;
    private const string BaselinePath = "validation/BuoyCalc.EngineeringRegression/baselines/engineering-baseline.json";

    private static readonly string[] ProvablyUnchangedFields =
    {
        "Name",
        "DepthM",
        "BuoyancyKg",
        "TotalWeightWaterKg",
        "NetBuoyancyKg",
        "WaveForceN",
        "AnchorHoldingKg",
        "LineLengthM",
        "ElementCount",
        "SegmentCount",
        "SegmentLengthSumM",
        "MaxSegmentLengthM",
        "SegmentWeightWaterSumKg",
        "MinLocalSpeedMS",
        "MaxLocalSpeedMS",
        "LineElementWeightWaterKg",
        "DiscreteElementCount"
    };

    private static readonly string[] MeasuredCandidateFields =
    {
        "CurrentForceN",
        "HorizontalForceN",
        "SegmentCurrentForceSumN",
        "SelectedNodeCount",
        "SelectedHorizontalOffsetM",
        "SelectedAnchorDepthM",
        "SelectedVerticalResidualM",
        "SelectedXSumM",
        "SelectedZSumM",
        "SelectedXSquaredSumM2",
        "SelectedSamples"
    };

    private static readonly string[] ProductionIntegrationRequiredFields =
    {
        "TensionKn",
        "AnchorReserve",
        "EstimatedOffsetM",
        "SelectedUsesDiscreteLoads",
        "SelectedConverged",
        "SelectedTensionSumKn",
        "SelectedAngleSumDeg",
        "IterativeConverged",
        "IterativeStopReason",
        "DiagnosticsSeverity"
    };

    private static readonly string[] FutureSourceIdentityFields =
    {
        "SelectedSource"
    };

    public static void Validate()
    {
        using var baselineDocument = JsonDocument.Parse(File.ReadAllText(BaselinePath));
        var historicalByName = baselineDocument.RootElement
            .GetProperty("Scenarios")
            .EnumerateArray()
            .ToDictionary(
                x => x.GetProperty("Name").GetString()
                    ?? throw new InvalidOperationException("Historical golden audit: scenario name is null."),
                x => x,
                StringComparer.Ordinal);

        var definitions = BuildHistoricalScenarios();
        if (historicalByName.Count != definitions.Count)
        {
            throw new InvalidOperationException(
                $"Historical golden audit: baseline has {historicalByName.Count} scenarios but fixture set has {definitions.Count}.");
        }

        var availableCount = 0;
        var blocked = new List<string>();

        Console.WriteLine("HISTORICAL_GOLDEN_IMPACT_BEGIN");

        foreach (var definition in definitions)
        {
            if (!historicalByName.TryGetValue(definition.Name, out var historical))
                throw new InvalidOperationException($"Historical golden audit: baseline scenario '{definition.Name}' is missing.");

            ValidateFieldCoverage(definition.Name, historical);
            ValidateHistoricalFixtureIdentity(definition, historical);
            WriteFieldContract(definition.Name);

            var candidate = RunCandidate(definition);
            if (!candidate.Available)
            {
                blocked.Add(definition.Name);
                Console.WriteLine(string.Join("|",
                    "HISTORICAL_GOLDEN_IMPACT_SCENARIO",
                    definition.Name,
                    "CandidateAvailable=False",
                    $"InitialClass={candidate.InitialClassification}",
                    $"Stop={candidate.StopReason}",
                    $"HistoricalX={Format(GetDouble(historical, "SelectedHorizontalOffsetM"))}",
                    $"HistoricalZ={Format(GetDouble(historical, "SelectedAnchorDepthM"))}",
                    $"HistoricalCurrentForceN={Format(GetDouble(historical, "CurrentForceN"))}",
                    "MeasuredCandidateFields=BlockedByUnavailableBoundary",
                    "ProductionSwitchBlocker=True"));
                continue;
            }

            availableCount++;
            var nodes = candidate.Nodes;
            var historicalNodeCount = GetInt(historical, "SelectedNodeCount");
            if (nodes.Count != historicalNodeCount)
            {
                throw new InvalidOperationException(
                    $"Historical golden audit {definition.Name}: candidate node count {nodes.Count} != historical {historicalNodeCount}.");
            }

            var candidateXSum = nodes.Sum(x => x.XM);
            var candidateZSum = nodes.Sum(x => x.ZM);
            var candidateXSquaredSum = nodes.Sum(x => x.XM * x.XM);
            var candidateVerticalResidual = candidate.EndpointZM - definition.Environment.DepthM;
            var candidateSegmentCurrentForce = candidate.Result.SegmentRows.Sum(x => x.CurrentForceN);

            var historicalSamples = historical.GetProperty("SelectedSamples").EnumerateArray().ToList();
            var maxSampleXDelta = 0.0;
            var maxSampleZDelta = 0.0;
            foreach (var sample in historicalSamples)
            {
                var index = sample.GetProperty("Index").GetInt32();
                if (index < 0 || index >= nodes.Count)
                    throw new InvalidOperationException($"Historical golden audit {definition.Name}: sample index {index} is outside candidate nodes.");

                maxSampleXDelta = Math.Max(maxSampleXDelta, Math.Abs(nodes[index].XM - sample.GetProperty("XOffsetM").GetDouble()));
                maxSampleZDelta = Math.Max(maxSampleZDelta, Math.Abs(nodes[index].ZM - sample.GetProperty("ZDepthM").GetDouble()));
            }

            Console.WriteLine(string.Join("|",
                "HISTORICAL_GOLDEN_IMPACT_SCENARIO",
                definition.Name,
                "CandidateAvailable=True",
                $"InitialClass={candidate.InitialClassification}",
                $"Budget={FeedbackBudget}",
                $"Iterations={candidate.Iterations}",
                $"Stop={candidate.StopReason}",
                $"Q0N={Format(candidate.Q0N)}",
                Pair("CurrentForceN", GetDouble(historical, "CurrentForceN"), candidate.Result.CurrentForceN),
                Pair("HorizontalForceN", GetDouble(historical, "HorizontalForceN"), candidate.Result.HorizontalForceN),
                Pair("SegmentCurrentForceSumN", GetDouble(historical, "SegmentCurrentForceSumN"), candidateSegmentCurrentForce),
                Pair("SelectedHorizontalOffsetM", GetDouble(historical, "SelectedHorizontalOffsetM"), candidate.EndpointXM),
                Pair("SelectedAnchorDepthM", GetDouble(historical, "SelectedAnchorDepthM"), candidate.EndpointZM),
                Pair("SelectedVerticalResidualM", GetDouble(historical, "SelectedVerticalResidualM"), candidateVerticalResidual),
                Pair("SelectedXSumM", GetDouble(historical, "SelectedXSumM"), candidateXSum),
                Pair("SelectedZSumM", GetDouble(historical, "SelectedZSumM"), candidateZSum),
                Pair("SelectedXSquaredSumM2", GetDouble(historical, "SelectedXSquaredSumM2"), candidateXSquaredSum),
                $"SelectedNodeCountHistorical={historicalNodeCount}",
                $"SelectedNodeCountCandidate={nodes.Count}",
                $"MaxSelectedSampleXDeltaM={Format(maxSampleXDelta)}",
                $"MaxSelectedSampleZDeltaM={Format(maxSampleZDelta)}",
                $"NegativeDz={candidate.NegativeDzSegmentCount}",
                $"PointLoads={candidate.PointLoadCrossings}",
                "ProductionSwitchBlocker=False"));
        }

        Console.WriteLine(string.Join("|",
            "HISTORICAL_GOLDEN_IMPACT_ROLLUP",
            $"Scenarios={definitions.Count}",
            $"CandidateAvailable={availableCount}",
            $"CandidateBlocked={blocked.Count}",
            $"BlockedNames={(blocked.Count == 0 ? "none" : string.Join(",", blocked))}",
            "GoldenBaselineModified=False",
            "ToleranceIntroduced=False"));
        Console.WriteLine("HISTORICAL_GOLDEN_IMPACT_END");
    }

    private static CandidateState RunCandidate(HistoricalScenario definition)
    {
        var run = ApplicationCalculationRunner.Run(
            definition.Environment,
            definition.Buoy,
            definition.Assembly,
            definition.Anchor,
            definition.SafetyFactor);

        var sequence = run.Snapshot.TechnicalReportData.SequencePositions;
        var initialBoundary = run.Snapshot.TechnicalReportData.SurfaceBoundaryInfo;
        var initialTrace = run.Snapshot.TechnicalReportData.SurfaceBoundaryTensionTrace;

        if (!initialBoundary.Solved ||
            initialBoundary.SolutionState is null ||
            !initialBoundary.Q0N.HasValue ||
            !initialBoundary.BuoySteadyDragN.HasValue ||
            !initialTrace.Available)
        {
            return CandidateState.Unavailable(initialBoundary.Classification.ToString(), "InitialBoundary:" + initialBoundary.Classification, run.Result);
        }

        var currentResult = run.Result;
        var currentBoundary = initialBoundary;
        var currentTrace = initialTrace;
        var currentGeometry = BuildGeometry(currentTrace, definition.Name + " initial");
        var buoySteadyDragN = initialBoundary.BuoySteadyDragN.Value;
        var stopReason = "BudgetReached";
        var iterations = 0;

        for (var iteration = 1; iteration <= FeedbackBudget; iteration++)
        {
            var projection = BuildProjection(currentTrace, currentGeometry, definition.Name + $" iteration {iteration}");
            var shapeForces = MooringShapeForceAnalyzer.Build(currentResult, projection);
            if (shapeForces.Rows.Count != currentResult.SegmentRows.Count)
            {
                throw new InvalidOperationException(
                    $"Historical golden audit {definition.Name}: shape-force row count {shapeForces.Rows.Count} != segment count {currentResult.SegmentRows.Count}.");
            }

            var forceBySegment = shapeForces.Rows.ToDictionary(x => x.SegmentNumber);
            var updatedSegments = new List<SegmentCalculationRow>(currentResult.SegmentRows.Count);
            foreach (var segment in currentResult.SegmentRows.OrderBy(x => x.Number))
            {
                if (!forceBySegment.TryGetValue(segment.Number, out var force))
                    throw new InvalidOperationException($"Historical golden audit {definition.Name}: missing shape force for segment {segment.Number}.");
                if (!double.IsFinite(force.ShapeForceN) || force.ShapeForceN < -ForceToleranceN)
                    throw new InvalidOperationException($"Historical golden audit {definition.Name}: invalid ShapeForceN={force.ShapeForceN:R}.");

                updatedSegments.Add(segment with { CurrentForceN = force.ShapeForceN });
            }

            var updatedLineForceN = updatedSegments.Sum(x => x.CurrentForceN);
            var updatedTotalCurrentForceN = buoySteadyDragN + updatedLineForceN + sequence.DiscreteCurrentForceN;
            var nextResult = currentResult with
            {
                SegmentRows = updatedSegments,
                CurrentForceN = updatedTotalCurrentForceN,
                HorizontalForceN = updatedTotalCurrentForceN + run.Result.WaveForceN
            };

            var nextBoundary = MooringSurfaceBoundaryInfoAnalyzer.Build(
                definition.Environment,
                definition.Buoy,
                nextResult,
                sequence);
            iterations = iteration;

            if (!nextBoundary.Solved || nextBoundary.SolutionState is null || !nextBoundary.Q0N.HasValue)
            {
                stopReason = "Boundary:" + nextBoundary.Classification;
                return CandidateState.Unavailable(initialBoundary.Classification.ToString(), stopReason, nextResult, iterations);
            }

            var nextTrace = MooringSurfaceBoundaryTensionTraceBuilder.Build(nextResult, sequence, nextBoundary);
            if (!nextTrace.Available)
            {
                stopReason = "TraceUnavailable:" + nextTrace.UnavailableReason;
                return CandidateState.Unavailable(initialBoundary.Classification.ToString(), stopReason, nextResult, iterations);
            }

            currentResult = nextResult;
            currentBoundary = nextBoundary;
            currentTrace = nextTrace;
            currentGeometry = BuildGeometry(currentTrace, definition.Name + $" iteration {iteration}");
        }

        return new CandidateState(
            true,
            initialBoundary.Classification.ToString(),
            stopReason,
            iterations,
            currentBoundary.Q0N,
            currentResult,
            currentGeometry.Nodes,
            currentGeometry.EndpointXM,
            currentGeometry.EndpointZM,
            currentGeometry.NegativeDzSegmentCount,
            currentTrace.PointLoadCrossings);
    }

    private static Geometry BuildGeometry(MooringSurfaceBoundaryTensionTraceResult trace, string label)
    {
        var nodes = new List<GeometryNode>(trace.Rows.Count + 1) { new(0.0, 0.0) };
        var x = 0.0;
        var z = 0.0;
        var negativeDz = 0;

        foreach (var row in trace.Rows)
        {
            var ds = row.EndLengthM - row.StartLengthM;
            var tx = row.TangentX ?? throw new InvalidOperationException($"Historical golden audit {label}: tangent X missing at segment {row.SegmentNumber}.");
            var tz = row.TangentZ ?? throw new InvalidOperationException($"Historical golden audit {label}: tangent Z missing at segment {row.SegmentNumber}.");
            if (!double.IsFinite(ds) || ds < -GeometryToleranceM || !double.IsFinite(tx) || !double.IsFinite(tz))
                throw new InvalidOperationException($"Historical golden audit {label}: invalid geometry state at segment {row.SegmentNumber}.");

            var dx = ds * tx;
            var dz = ds * tz;
            x += dx;
            z += dz;
            if (dz < -GeometryToleranceM)
                negativeDz++;
            nodes.Add(new GeometryNode(x, z));
        }

        return new Geometry(nodes, x, z, negativeDz);
    }

    private static MooringShapeProjectionResult BuildProjection(
        MooringSurfaceBoundaryTensionTraceResult trace,
        Geometry geometry,
        string label)
    {
        if (geometry.Nodes.Count != trace.Rows.Count + 1)
            throw new InvalidOperationException($"Historical golden audit {label}: geometry/trace count mismatch.");

        var rows = new List<MooringShapeProjectionRow>(trace.Rows.Count);
        for (var i = 0; i < trace.Rows.Count; i++)
        {
            var traceRow = trace.Rows[i];
            var start = geometry.Nodes[i];
            var end = geometry.Nodes[i + 1];
            var ds = traceRow.EndLengthM - traceRow.StartLengthM;
            var dx = end.XM - start.XM;
            var dz = end.ZM - start.ZM;
            var projectedLength = Math.Sqrt(dx * dx + dz * dz);
            var lengthResidual = Math.Abs(projectedLength - ds);
            if (lengthResidual > GeometryToleranceM)
                throw new InvalidOperationException($"Historical golden audit {label}: projection length residual {lengthResidual:R} m.");

            var displayAngleDeg = Math.Atan2(Math.Abs(dx), Math.Max(1e-12, Math.Abs(dz))) * 180.0 / Math.PI;
            rows.Add(new MooringShapeProjectionRow(
                i + 1,
                traceRow.SegmentNumber,
                traceRow.SourceElement,
                ds,
                dx,
                dz,
                projectedLength,
                lengthResidual,
                displayAngleDeg,
                traceRow.MidTensionN / 1000.0,
                "INFO: validation-only historical golden impact projection"));
        }

        var totalSegmentLength = rows.Sum(x => x.SegmentLengthM);
        var totalProjectedLength = rows.Sum(x => x.ProjectedLengthM);
        var totalLengthResidual = Math.Abs(totalProjectedLength - totalSegmentLength);
        var maxAngle = rows.Count > 0 ? rows.Max(x => x.AngleFromVerticalDeg) : 0.0;
        var averageAngle = rows.Count > 0 ? rows.Average(x => x.AngleFromVerticalDeg) : 0.0;

        return new MooringShapeProjectionResult(
            rows,
            geometry.EndpointXM,
            geometry.EndpointZM,
            totalSegmentLength,
            totalProjectedLength,
            totalLengthResidual,
            geometry.EndpointXM,
            geometry.EndpointZM,
            0.0,
            0.0,
            maxAngle,
            averageAngle,
            totalLengthResidual <= GeometryToleranceM,
            "Validation-only historical golden impact projection. Signed dx/dz are authoritative; scalar angle is display-only.");
    }

    private static void ValidateFieldCoverage(string scenarioName, JsonElement historical)
    {
        var classified = ProvablyUnchangedFields
            .Concat(MeasuredCandidateFields)
            .Concat(ProductionIntegrationRequiredFields)
            .Concat(FutureSourceIdentityFields)
            .ToHashSet(StringComparer.Ordinal);
        var actual = historical.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);

        if (!actual.SetEquals(classified))
        {
            var missing = actual.Except(classified).OrderBy(x => x).ToArray();
            var extra = classified.Except(actual).OrderBy(x => x).ToArray();
            throw new InvalidOperationException(
                $"Historical golden audit {scenarioName}: field classification mismatch; unclassified=[{string.Join(",", missing)}], stale=[{string.Join(",", extra)}].");
        }

        foreach (var sample in historical.GetProperty("SelectedSamples").EnumerateArray())
        {
            var sampleFields = sample.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
            var expected = new HashSet<string>(new[] { "Index", "XOffsetM", "ZDepthM", "TensionKn", "AngleFromVerticalDeg" }, StringComparer.Ordinal);
            if (!sampleFields.SetEquals(expected))
                throw new InvalidOperationException($"Historical golden audit {scenarioName}: SelectedSamples field set changed.");
        }
    }

    private static void WriteFieldContract(string scenarioName)
    {
        Console.WriteLine(string.Join("|",
            "HISTORICAL_GOLDEN_IMPACT_FIELDS",
            scenarioName,
            $"ProvablyUnchanged={string.Join(",", ProvablyUnchangedFields)}",
            $"MeasuredCandidate={string.Join(",", MeasuredCandidateFields)}",
            $"ProductionIntegrationRequired={string.Join(",", ProductionIntegrationRequiredFields)}",
            $"FutureSourceIdentity={string.Join(",", FutureSourceIdentityFields)}",
            "SelectedSamples.Index=ProvablyUnchanged",
            "SelectedSamples.XOffsetM=MeasuredCandidate",
            "SelectedSamples.ZDepthM=MeasuredCandidate",
            "SelectedSamples.TensionKn=ProductionIntegrationRequired",
            "SelectedSamples.AngleFromVerticalDeg=ProductionIntegrationRequired"));
    }

    private static void ValidateHistoricalFixtureIdentity(HistoricalScenario definition, JsonElement historical)
    {
        Near(definition.Environment.DepthM, GetDouble(historical, "DepthM"), 1e-12, definition.Name + " depth");
        var expectedLineLength = definition.Assembly.Where(x => x.Kind == AssemblyItemKind.Line).Sum(x => x.LengthM);
        Near(expectedLineLength, GetDouble(historical, "LineLengthM"), 1e-12, definition.Name + " line length");
    }

    private static IReadOnlyList<HistoricalScenario> BuildHistoricalScenarios()
    {
        var seabed = new SeabedPreset("reg:sand", "Regression sand", 1.2, "Deterministic regression seabed preset.");
        var buoy = new BuoyInput("Regression buoy", 1.0, 100.0, 0.8, 0.8);
        var anchor = new AnchorInput("Regression concrete anchor", "Concrete block", "Concrete", 1000.0, 0.4, 1.0);
        var heavyLine = new RopePreset("reg:heavy-line", "Regression heavy line", "Polyester", 20.0, 100.0, 0.1, 1.2, "Deterministic heavy-line regression preset.");
        var buoyantLine = new RopePreset("reg:buoyant-line", "Regression buoyant line", "Synthetic buoyant", 20.0, 100.0, -0.05, 1.2, "Negative signed water weight is intentional and must be preserved.");
        var connector = new ConnectorPreset("reg:connector", "Regression connector", "Shackle", 5.0, 0.0007, 60.0, 0.01, 1.0, "Deterministic connector regression preset.");

        return new[]
        {
            new HistoricalScenario(
                "vertical-zero-current",
                Environment(50, 0, 0, 0, seabed),
                buoy,
                new[] { Line("Vertical line", heavyLine, 50) },
                anchor,
                3.0),
            new HistoricalScenario(
                "uniform-current-slack-line",
                Environment(50, 0.5, 1.0, 6.0, seabed),
                buoy,
                new[] { Line("Slack line", heavyLine, 55) },
                anchor,
                3.0),
            new HistoricalScenario(
                "buoyant-line",
                Environment(30, 0.3, 0, 0, seabed),
                buoy,
                new[] { Line("Buoyant line", buoyantLine, 30) },
                anchor,
                3.0),
            new HistoricalScenario(
                "discrete-payload",
                Environment(50, 0.5, 0.5, 5.0, seabed),
                buoy,
                new AssemblyItemInput[]
                {
                    Line("Upper line", heavyLine, 30),
                    Connector("Shackle", connector),
                    Payload("Payload", 40.0, 0.005, 0.05, 1.0),
                    Line("Lower line", heavyLine, 25)
                },
                anchor,
                3.0),
            new HistoricalScenario(
                "depth-varying-current-profile",
                new EnvironmentInput(
                    1025.0,
                    50.0,
                    0.2,
                    0,
                    0,
                    seabed,
                    true,
                    new[]
                    {
                        new CurrentProfilePointInput(0, 0.6, 0, 0, 1025),
                        new CurrentProfilePointInput(25, 0.3, 0, 0, 1025),
                        new CurrentProfilePointInput(50, 0.1, 0, 0, 1025)
                    }),
                buoy,
                new[] { Line("Profile line", heavyLine, 50) },
                anchor,
                3.0)
        };
    }

    private static EnvironmentInput Environment(double depthM, double currentSpeedMS, double waveHeightM, double wavePeriodS, SeabedPreset seabed) =>
        new(
            1025.0,
            depthM,
            0.0,
            waveHeightM,
            wavePeriodS,
            seabed,
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, currentSpeedMS, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(depthM, currentSpeedMS, 0.0, 0.0, 1025.0)
            });

    private static AssemblyItemInput Line(string title, RopePreset preset, double lengthM) =>
        new(AssemblyItemKind.Line, title, true, preset, null, lengthM, 1, 0, 0, 0, 0);

    private static AssemblyItemInput Connector(string title, ConnectorPreset preset) =>
        new(AssemblyItemKind.Connector, title, true, null, preset, 0, 1, 0, 0, 0, 0);

    private static AssemblyItemInput Payload(string title, double weightAirKg, double volumeM3, double projectedAreaM2, double dragCoefficient) =>
        new(AssemblyItemKind.Payload, title, true, null, null, 0, 1, weightAirKg, volumeM3, projectedAreaM2, dragCoefficient);

    private static double GetDouble(JsonElement scenario, string property) => scenario.GetProperty(property).GetDouble();
    private static int GetInt(JsonElement scenario, string property) => scenario.GetProperty(property).GetInt32();

    private static string Pair(string name, double historical, double candidate) =>
        $"{name}Historical={Format(historical)},{name}Candidate={Format(candidate)},{name}Delta={Format(candidate - historical)}";

    private static string Format(double? value) => value.HasValue ? Format(value.Value) : "n/a";
    private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static void Near(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Historical golden audit {label}: expected {expected:R}, got {actual:R}.");
    }

    private sealed record HistoricalScenario(
        string Name,
        EnvironmentInput Environment,
        BuoyInput Buoy,
        IReadOnlyList<AssemblyItemInput> Assembly,
        AnchorInput Anchor,
        double SafetyFactor);

    private sealed record GeometryNode(double XM, double ZM);

    private sealed record Geometry(
        IReadOnlyList<GeometryNode> Nodes,
        double EndpointXM,
        double EndpointZM,
        int NegativeDzSegmentCount);

    private sealed record CandidateState(
        bool Available,
        string InitialClassification,
        string StopReason,
        int Iterations,
        double? Q0N,
        CalculationResult Result,
        IReadOnlyList<GeometryNode> Nodes,
        double EndpointXM,
        double EndpointZM,
        int NegativeDzSegmentCount,
        int PointLoadCrossings)
    {
        public static CandidateState Unavailable(
            string initialClassification,
            string stopReason,
            CalculationResult result,
            int iterations = 0) =>
            new(false, initialClassification, stopReason, iterations, null, result, Array.Empty<GeometryNode>(), double.NaN, double.NaN, 0, 0);
    }
}
