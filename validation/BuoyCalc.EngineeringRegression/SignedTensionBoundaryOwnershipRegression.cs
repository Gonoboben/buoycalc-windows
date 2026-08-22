using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedTensionBoundaryOwnershipRegression
{
    private const double G = 9.80665;
    private const double ForceToleranceN = 1e-8;
    private const double PositionToleranceM = 1e-9;

    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    public static void Validate()
    {
        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var acceptedChecked = 0;
        var acceptedWithoutInternalPoints = 0;
        var acceptedWithInternalPoints = 0;

        Console.WriteLine("E1A_BOUNDARY_OWNERSHIP_BEGIN");

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
                ?? throw new InvalidOperationException($"E1-A {name}: signed candidate is missing.");
            if (candidate.Status != MooringSignedCandidateStatus.Accepted)
                throw new InvalidOperationException($"E1-A {name}: expected Accepted candidate, got {candidate.Status}.");

            var selectedState = MooringSelectedSignedBoundaryStateProjector.Project(
                run.Snapshot.ShadowSelectedCore,
                candidate)
                ?? throw new InvalidOperationException($"E1-A {name}: selected signed boundary state is unavailable.");

            var sequence = MooringSequencePositioner.Build(run.Result);
            var orderedRows = sequence.Rows.OrderBy(x => x.Number).ToList();
            ValidateBoundaryRowOwnership(name, sequence, orderedRows, run.Result.SegmentRows);

            var topNumber = orderedRows[0].Number;
            var bottomNumber = orderedRows[^1].Number;
            var internalPoints = orderedRows
                .Where(x => x.IsDiscrete && x.Number != topNumber && x.Number != bottomNumber)
                .OrderBy(x => x.PositionAlongLineM)
                .ThenBy(x => x.Number)
                .ToList();
            var baseSegments = run.Result.SegmentRows.OrderBy(x => x.Number).ToList();
            var baseSegmentCurrentN = baseSegments.Sum(x => x.CurrentForceN);
            var pointCurrentN = internalPoints.Sum(x => x.CurrentForceN);

            // The snapshot's technical-report boundary is the pre-feedback boundary built from
            // this exact CalculationResult. It is the correct place to prove the base H balance.
            var baseBoundary = run.Snapshot.TechnicalReportData.SurfaceBoundaryInfo;
            if (!baseBoundary.Solved ||
                baseBoundary.SolutionState is null ||
                !baseBoundary.BuoySteadyDragN.HasValue ||
                !baseBoundary.Q0N.HasValue)
            {
                throw new InvalidOperationException($"E1-A {name}: base production boundary is not uniquely solved.");
            }

            var expectedBaseSurfaceHN = run.Result.CurrentForceN - baseSegmentCurrentN - pointCurrentN;
            Near(
                baseBoundary.BuoySteadyDragN.Value,
                expectedBaseSurfaceHN,
                ForceToleranceN,
                name + " base surface H ownership");
            Near(
                baseBoundary.SolutionState.EndHN,
                run.Result.CurrentForceN,
                ForceToleranceN,
                name + " base steady-current end H");
            var expectedBaseEndVN = IntegrateExpectedEndV(baseBoundary.Q0N.Value, baseSegments, internalPoints);
            Near(
                baseBoundary.SolutionState.EndVN,
                expectedBaseEndVN,
                ForceToleranceN,
                name + " base end V signed-weight balance");

            // Accepted signed state is the result of the feedback-updated current-force path.
            // MooringSignedCandidateEvaluator changes SegmentRows.CurrentForceN/CurrentForceN
            // between iterations while preserving signed submerged weights. Therefore its EndH
            // must not be compared to the original run.Result.CurrentForceN.
            var candidateBoundary = candidate.Boundary
                ?? throw new InvalidOperationException($"E1-A {name}: Accepted candidate boundary is missing.");
            if (!ReferenceEquals(candidateBoundary, candidate.Boundary) ||
                !candidateBoundary.Solved ||
                candidateBoundary.SolutionState is null ||
                !candidateBoundary.BuoySteadyDragN.HasValue ||
                !candidateBoundary.Q0N.HasValue)
            {
                throw new InvalidOperationException($"E1-A {name}: Accepted candidate boundary is incomplete.");
            }

            Near(selectedState.BuoySteadyDragN, candidateBoundary.BuoySteadyDragN.Value, 0.0, name + " selected start H identity");
            Near(selectedState.Q0N, candidateBoundary.Q0N.Value, 0.0, name + " selected start V identity");
            Near(selectedState.EndHN, candidateBoundary.SolutionState.EndHN, 0.0, name + " selected end H identity");
            Near(selectedState.EndVN, candidateBoundary.SolutionState.EndVN, 0.0, name + " selected end V identity");

            // Feedback changes only current-force rows; signed submerged weights and discrete
            // point weights remain those of the base CalculationResult/sequence.
            var expectedSelectedEndVN = IntegrateExpectedEndV(selectedState.Q0N, baseSegments, internalPoints);
            Near(
                selectedState.EndVN,
                expectedSelectedEndVN,
                ForceToleranceN,
                name + " selected end V signed-weight balance");

            if (selectedState.PointLoadCrossings != internalPoints.Count ||
                selectedState.ContainsDiscreteLoads != (internalPoints.Count > 0))
            {
                throw new InvalidOperationException(
                    $"E1-A {name}: selected signed point-load identity differs from internal sequence ownership.");
            }

            if (!baseBoundary.MethodNote.Contains("wave excluded", StringComparison.Ordinal) ||
                !candidateBoundary.MethodNote.Contains("wave excluded", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"E1-A {name}: signed boundary method no longer states wave exclusion.");
            }
            if (run.Result.WaveForceN <= ForceToleranceN)
                throw new InvalidOperationException($"E1-A {name}: canonical fixture no longer supplies a non-zero legacy wave force.");
            Near(
                run.Result.HorizontalForceN,
                run.Result.CurrentForceN + run.Result.WaveForceN,
                ForceToleranceN,
                name + " legacy horizontal-force composition");

            var impliedSelectedLineCurrentN = selectedState.EndHN - selectedState.BuoySteadyDragN - pointCurrentN;
            if (!double.IsFinite(impliedSelectedLineCurrentN) || impliedSelectedLineCurrentN < -ForceToleranceN)
            {
                throw new InvalidOperationException(
                    $"E1-A {name}: selected feedback-updated implied line current force is invalid: {impliedSelectedLineCurrentN:R} N.");
            }

            acceptedChecked++;
            if (internalPoints.Count == 0)
                acceptedWithoutInternalPoints++;
            else
                acceptedWithInternalPoints++;

            Console.WriteLine(string.Join("|",
                "E1A_BOUNDARY_OWNERSHIP",
                name,
                "Direction=BuoyToAnchor",
                "SelectedStartHNSource=BuoySteadyDragN",
                "SelectedStartVNSource=Q0N",
                $"BaseCurrentForceN={F(run.Result.CurrentForceN)}",
                $"BaseBoundaryEndHN={F(baseBoundary.SolutionState.EndHN)}",
                $"SelectedStartHN={F(selectedState.BuoySteadyDragN)}",
                $"SelectedStartVN={F(selectedState.Q0N)}",
                $"SelectedEndHN={F(selectedState.EndHN)}",
                $"SelectedEndVN={F(selectedState.EndVN)}",
                $"SelectedImpliedLineCurrentN={F(impliedSelectedLineCurrentN)}",
                $"LegacyWaveForceN={F(run.Result.WaveForceN)}",
                $"LegacyHorizontalForceN={F(run.Result.HorizontalForceN)}",
                $"InternalPointLoads={internalPoints.Count}",
                $"PointLoadCrossings={selectedState.PointLoadCrossings}",
                "SelectedHStage=FeedbackUpdatedCurrentForces",
                "WaveIncludedInSignedBoundary=False",
                "ScalarTensionAuthority=LegacyUnchanged"));
        }

        ValidateNegativeSignedWaterWeight(definitions);

        if (acceptedChecked != 2 || acceptedWithoutInternalPoints != 1 || acceptedWithInternalPoints != 1)
        {
            throw new InvalidOperationException(
                $"E1-A accepted coverage mismatch: checked={acceptedChecked}, withoutPoints={acceptedWithoutInternalPoints}, withPoints={acceptedWithInternalPoints}.");
        }

        Console.WriteLine(
            "E1A_BOUNDARY_OWNERSHIP_ROLLUP|AcceptedChecked=2|WithoutInternalPoints=1|WithInternalPoints=1|Direction=BuoyToAnchor|SelectedHStage=FeedbackUpdatedCurrentForces|WaveIncludedInSignedBoundary=False|NegativeSignedWeightConvention=Validated|ScalarTensionAuthority=LegacyUnchanged");
        Console.WriteLine("E1A_BOUNDARY_OWNERSHIP_END");
    }

    private static void ValidateBoundaryRowOwnership(
        string name,
        MooringSequencePositionResult sequence,
        IReadOnlyList<MooringSequencePositionRow> orderedRows,
        IReadOnlyList<SegmentCalculationRow> segmentRows)
    {
        if (orderedRows.Count < 2)
            throw new InvalidOperationException($"E1-A {name}: sequence has fewer than two boundary rows.");

        var top = orderedRows[0];
        var bottom = orderedRows[^1];
        if (!top.IsDiscrete ||
            !string.Equals(top.Kind, "Буй", StringComparison.OrdinalIgnoreCase) ||
            Math.Abs(top.PositionAlongLineM) > PositionToleranceM ||
            !top.SolverRole.Contains("верхний граничный узел", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"E1-A {name}: s=0 is no longer the buoy/top boundary row.");
        }

        if (!bottom.IsDiscrete ||
            !string.Equals(bottom.Kind, "Якорь", StringComparison.OrdinalIgnoreCase) ||
            Math.Abs(bottom.PositionAlongLineM - sequence.TotalLineLengthM) > PositionToleranceM ||
            !bottom.SolverRole.Contains("нижний граничный узел", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"E1-A {name}: s=L is no longer the anchor/bottom boundary row.");
        }

        var segments = segmentRows.OrderBy(x => x.Number).ToList();
        if (segments.Count == 0 ||
            Math.Abs(segments[0].StartLengthM) > PositionToleranceM ||
            Math.Abs(segments[^1].EndLengthM - sequence.TotalLineLengthM) > PositionToleranceM)
        {
            throw new InvalidOperationException($"E1-A {name}: segment chain no longer spans s=0..L.");
        }

        var previousEnd = 0.0;
        var previousDepth = double.NegativeInfinity;
        foreach (var segment in segments)
        {
            if (segment.StartLengthM + PositionToleranceM < previousEnd ||
                segment.EndLengthM + PositionToleranceM < segment.StartLengthM ||
                segment.EstimatedDepthM + PositionToleranceM < previousDepth)
            {
                throw new InvalidOperationException(
                    $"E1-A {name}: segment ordering/depth no longer increases from surface toward seabed.");
            }

            previousEnd = segment.EndLengthM;
            previousDepth = segment.EstimatedDepthM;
        }
    }

    private static double IntegrateExpectedEndV(
        double q0N,
        IReadOnlyList<SegmentCalculationRow> segments,
        IReadOnlyList<MooringSequencePositionRow> points)
    {
        var vN = q0N;
        var pointIndex = 0;
        foreach (var segment in segments)
        {
            while (pointIndex < points.Count &&
                   points[pointIndex].PositionAlongLineM <= segment.StartLengthM + PositionToleranceM)
            {
                vN -= points[pointIndex++].WeightWaterKg * G;
            }

            vN -= segment.WeightWaterKg * G;
        }

        while (pointIndex < points.Count)
            vN -= points[pointIndex++].WeightWaterKg * G;

        return vN;
    }

    private static void ValidateNegativeSignedWaterWeight(IReadOnlyList<object> definitions)
    {
        var definition = definitions.Single(x =>
            string.Equals(Property<string>(x, "Name"), "buoyant-line", StringComparison.Ordinal));
        var environment = Property<EnvironmentInput>(definition, "Environment");
        var buoy = Property<BuoyInput>(definition, "Buoy");
        var assembly = Property<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
        var anchor = Property<AnchorInput>(definition, "Anchor");
        var safetyFactor = Property<double>(definition, "SafetyFactor");

        var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
        var sequence = MooringSequencePositioner.Build(run.Result);
        var boundary = MooringSurfaceBoundaryInfoAnalyzer.Build(environment, buoy, run.Result, sequence);
        var high = boundary.CapacityBoundaryState
            ?? throw new InvalidOperationException("E1-A buoyant-line: capacity boundary state is unavailable.");
        var qCapacity = boundary.QCapacityN
            ?? throw new InvalidOperationException("E1-A buoyant-line: Q capacity is unavailable.");
        var signedLineWeightKg = run.Result.SegmentRows.Sum(x => x.WeightWaterKg);
        if (signedLineWeightKg >= 0.0)
            throw new InvalidOperationException("E1-A buoyant-line: canonical line no longer has negative signed submerged weight.");

        var orderedRows = sequence.Rows.OrderBy(x => x.Number).ToList();
        var topNumber = orderedRows[0].Number;
        var bottomNumber = orderedRows[^1].Number;
        var internalPoints = orderedRows
            .Where(x => x.IsDiscrete && x.Number != topNumber && x.Number != bottomNumber)
            .OrderBy(x => x.PositionAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();
        var expectedEndV = IntegrateExpectedEndV(
            qCapacity,
            run.Result.SegmentRows.OrderBy(x => x.Number).ToList(),
            internalPoints);
        Near(high.EndVN, expectedEndV, ForceToleranceN, "buoyant-line negative signed-weight balance");

        if (high.EndVN <= qCapacity)
        {
            throw new InvalidOperationException(
                "E1-A buoyant-line: negative signed water weight must increase V when the kernel applies V -= W_water*g.");
        }

        Console.WriteLine(string.Join("|",
            "E1A_SIGNED_WEIGHT_CONVENTION",
            "buoyant-line",
            $"SignedLineWeightKg={F(signedLineWeightKg)}",
            $"StartVNAtCapacity={F(qCapacity)}",
            $"EndVN={F(high.EndVN)}",
            "Update=VminusSignedWeightTimesG",
            "NegativeWeightEffect=IncreasesV",
            "SelectedCandidateAuthority=None"));
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "E1-A: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("E1-A: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"E1-A: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"E1-A: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Near(double actual, double expected, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"E1-A {label}: expected {expected:R}, got {actual:R}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
