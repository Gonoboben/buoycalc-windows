using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

internal static class SignedCandidateConvergenceTrajectoryRegression
{
    private static readonly int[] ProtocolBudgets = { 64, 128, 256, 512, 1024 };
    private static readonly int[] SampleIterations = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024 };
    private static readonly HashSet<string> TargetFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    private const double PackageFIdentityTolerance = 1e-9;

    public static void Validate()
    {
        var historicalScenarioBuilder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed candidate convergence trajectory: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");

        var runBudget = typeof(BoundaryConditionedFeedbackCouplingRegression).GetMethod(
            "RunBudget",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed candidate convergence trajectory: BoundaryConditionedFeedbackCouplingRegression.RunBudget was not found.");

        var definitions = historicalScenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException(
                "Signed candidate convergence trajectory: historical fixture definitions are unavailable.");

        var measuredFixtures = 0;
        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!TargetFixtures.Contains(name))
                continue;

            measuredFixtures++;
            var environment = RequireProperty<EnvironmentInput>(definition, "Environment");
            var buoy = RequireProperty<BuoyInput>(definition, "Buoy");
            var assembly = RequireProperty<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = RequireProperty<AnchorInput>(definition, "Anchor");
            var safetyFactor = RequireProperty<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(
                environment,
                buoy,
                assembly,
                anchor,
                safetyFactor);

            var data = run.Snapshot.TechnicalReportData;
            var initialBoundary = data.SurfaceBoundaryInfo;
            var initialTrace = data.SurfaceBoundaryTensionTrace;

            if (!initialBoundary.Solved ||
                initialBoundary.SolutionState is null ||
                !initialBoundary.Q0N.HasValue ||
                !initialTrace.Available)
            {
                throw new InvalidOperationException(
                    $"Signed candidate convergence trajectory {name}: expected the Package F measurable initial boundary, got {initialBoundary.Classification}, solved={initialBoundary.Solved}, trace={initialTrace.Available}.");
            }

            Console.WriteLine(string.Join("|",
                "SIGNED_CANDIDATE_TRAJECTORY_SCENARIO",
                name,
                $"InitialClass={initialBoundary.Classification}",
                $"InitialQ0N={Format(initialBoundary.Q0N)}",
                $"ProtocolBudgets={string.Join(",", ProtocolBudgets)}",
                $"SampleIterations={string.Join(",", SampleIterations)}",
                "ConvergenceTolerance=None",
                "ProductionAcceptance=None"));

            var measurements = new Dictionary<int, Measurement>();
            foreach (var horizon in SampleIterations)
            {
                var measurement = MeasureBudget(
                    runBudget,
                    name,
                    horizon,
                    environment,
                    buoy,
                    run.Result,
                    data.SequencePositions,
                    initialBoundary,
                    initialTrace);
                measurements[horizon] = measurement;

                ValidateIntegrity(name, measurement);
                Console.WriteLine(FormatMeasurement(name, measurement, ProtocolBudgets.Contains(horizon)));
            }

            ValidatePackageFIdentity(name, measurements[64]);

            foreach (var budget in ProtocolBudgets)
            {
                if (!measurements.ContainsKey(budget))
                    throw new InvalidOperationException($"Signed candidate convergence trajectory {name}: protocol budget {budget} was not measured.");
            }

            Console.WriteLine(string.Join("|",
                "SIGNED_CANDIDATE_TRAJECTORY_ROLLUP",
                name,
                $"MeasuredHorizons={measurements.Count}",
                $"LongestBudget={SampleIterations[^1]}",
                $"LongestStop={measurements[SampleIterations[^1]].StopReason}",
                $"LongestClass={measurements[SampleIterations[^1]].Classification}",
                $"LongestQ0N={Format(measurements[SampleIterations[^1]].Q0N)}",
                $"LongestX={Format(measurements[SampleIterations[^1]].EndpointXM)}",
                $"LongestZ={Format(measurements[SampleIterations[^1]].EndpointZM)}",
                $"LongestDepthResidualM={Format(measurements[SampleIterations[^1]].DepthResidualM)}",
                $"LongestDeltaX={Format(measurements[SampleIterations[^1]].LastDeltaXM)}",
                $"LongestDeltaZ={Format(measurements[SampleIterations[^1]].LastDeltaZM)}",
                $"LongestDeltaQ0N={Format(measurements[SampleIterations[^1]].LastDeltaQ0N)}",
                $"LongestMaxNodeDeltaM={Format(measurements[SampleIterations[^1]].LastMaxNodeDeltaM)}",
                $"LongestDeltaLineForceN={Format(measurements[SampleIterations[^1]].LastDeltaLineForceN)}",
                $"LongestMaxSegmentForceDeltaN={Format(measurements[SampleIterations[^1]].LastMaxSegmentForceDeltaN)}",
                "ProductionDecision=None"));
        }

        if (measuredFixtures != TargetFixtures.Count)
        {
            throw new InvalidOperationException(
                $"Signed candidate convergence trajectory: expected {TargetFixtures.Count} target fixtures, measured {measuredFixtures}.");
        }
    }

    private static Measurement MeasureBudget(
        MethodInfo runBudget,
        string name,
        int budget,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult baseResult,
        object sequence,
        object initialBoundary,
        object initialTrace)
    {
        var originalOut = Console.Out;
        using var capture = new StringWriter(CultureInfo.InvariantCulture);
        object outcome;
        try
        {
            Console.SetOut(capture);
            outcome = runBudget.Invoke(
                null,
                new[]
                {
                    (object)name,
                    budget,
                    environment,
                    buoy,
                    baseResult,
                    sequence,
                    initialBoundary,
                    initialTrace,
                    true
                }) ?? throw new InvalidOperationException(
                    $"Signed candidate convergence trajectory {name} budget {budget}: RunBudget returned null.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Signed candidate convergence trajectory {name} budget {budget}: existing feedback regression failed.",
                ex.InnerException);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var iterations = RequireProperty<int>(outcome, "Iterations");
        var stopReason = RequireProperty<string>(outcome, "StopReason");
        var classification = ExtractFinalClassification(capture.ToString(), name, budget, stopReason);

        return new Measurement(
            budget,
            iterations,
            stopReason,
            classification,
            RequireProperty<double>(outcome, "EndpointXM"),
            RequireProperty<double>(outcome, "EndpointZM"),
            OptionalDoubleProperty(outcome, "Q0N"),
            OptionalDoubleProperty(outcome, "LastDeltaXM"),
            OptionalDoubleProperty(outcome, "LastDeltaZM"),
            OptionalDoubleProperty(outcome, "LastDeltaQ0N"),
            OptionalDoubleProperty(outcome, "LastMaxNodeDeltaM"),
            RequireProperty<double>(outcome, "LineForceN"),
            OptionalDoubleProperty(outcome, "LastDeltaLineForceN"),
            OptionalDoubleProperty(outcome, "LastMaxSegmentForceDeltaN"),
            OptionalDoubleProperty(outcome, "DepthResidualM"),
            RequireProperty<int>(outcome, "NegativeDzSegmentCount"),
            RequireProperty<int>(outcome, "PointLoadCrossings"),
            RequireProperty<double>(outcome, "MaxPointJumpResidualN"));
    }

    private static string ExtractFinalClassification(
        string capturedOutput,
        string name,
        int budget,
        string stopReason)
    {
        var prefix = $"BOUNDARY_FEEDBACK_ITER|{name}|Budget={budget}|";
        var lines = capturedOutput.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        var last = lines.LastOrDefault(x => x.StartsWith(prefix, StringComparison.Ordinal));
        if (last is not null)
        {
            foreach (var field in last.Split('|'))
            {
                if (field.StartsWith("Class=", StringComparison.Ordinal))
                    return field["Class=".Length..];
            }
        }

        if (stopReason.StartsWith("Boundary:", StringComparison.Ordinal))
            return stopReason["Boundary:".Length..];

        return "NotEmitted:" + stopReason;
    }

    private static void ValidateIntegrity(string name, Measurement value)
    {
        if (value.Iterations <= 0 || value.Iterations > value.Budget)
        {
            throw new InvalidOperationException(
                $"Signed candidate convergence trajectory {name}: invalid iteration count {value.Iterations} for budget {value.Budget}.");
        }

        if (value.StopReason == "BudgetReached" && value.Iterations != value.Budget)
        {
            throw new InvalidOperationException(
                $"Signed candidate convergence trajectory {name}: BudgetReached at {value.Iterations} != requested {value.Budget}.");
        }

        RequireFinite(value.EndpointXM, name, value.Budget, "EndpointXM");
        RequireFinite(value.EndpointZM, name, value.Budget, "EndpointZM");
        RequireFinite(value.LineForceN, name, value.Budget, "LineForceN");
        RequireFinite(value.MaxPointJumpResidualN, name, value.Budget, "MaxPointJumpResidualN");
        RequireFiniteNullable(value.Q0N, name, value.Budget, "Q0N");
        RequireFiniteNullable(value.LastDeltaXM, name, value.Budget, "LastDeltaXM");
        RequireFiniteNullable(value.LastDeltaZM, name, value.Budget, "LastDeltaZM");
        RequireFiniteNullable(value.LastDeltaQ0N, name, value.Budget, "LastDeltaQ0N");
        RequireFiniteNullable(value.LastMaxNodeDeltaM, name, value.Budget, "LastMaxNodeDeltaM");
        RequireFiniteNullable(value.LastDeltaLineForceN, name, value.Budget, "LastDeltaLineForceN");
        RequireFiniteNullable(value.LastMaxSegmentForceDeltaN, name, value.Budget, "LastMaxSegmentForceDeltaN");
        RequireFiniteNullable(value.DepthResidualM, name, value.Budget, "DepthResidualM");

        if (value.NegativeDzSegmentCount != 0)
        {
            throw new InvalidOperationException(
                $"Signed candidate convergence trajectory {name}: negative-dz segment count {value.NegativeDzSegmentCount} at budget {value.Budget}.");
        }

        var expectedPointLoads = name == "discrete-payload" ? 2 : 0;
        if (value.PointLoadCrossings != expectedPointLoads)
        {
            throw new InvalidOperationException(
                $"Signed candidate convergence trajectory {name}: point-load crossings {value.PointLoadCrossings} != expected historical fixture identity {expectedPointLoads} at budget {value.Budget}.");
        }
    }

    private static void ValidatePackageFIdentity(string name, Measurement at64)
    {
        var expected = name switch
        {
            "uniform-current-slack-line" => new PackageFIdentity(
                379.810165863037,
                22.073605655669077,
                50.0007670051935),
            "discrete-payload" => new PackageFIdentity(
                720.8641923522947,
                19.583341922076137,
                49.99914589480685),
            _ => throw new InvalidOperationException($"Signed candidate convergence trajectory: unexpected fixture {name}.")
        };

        Near(expected.Q0N, at64.Q0N, PackageFIdentityTolerance, name + " Package F Q0 continuity");
        Near(expected.EndpointXM, at64.EndpointXM, PackageFIdentityTolerance, name + " Package F endpoint X continuity");
        Near(expected.EndpointZM, at64.EndpointZM, PackageFIdentityTolerance, name + " Package F endpoint Z continuity");

        if (at64.StopReason != "BudgetReached")
        {
            throw new InvalidOperationException(
                $"Signed candidate convergence trajectory {name}: Package F continuity expected BudgetReached at 64, got {at64.StopReason}.");
        }
    }

    private static string FormatMeasurement(string name, Measurement value, bool protocolBudget) =>
        string.Join("|",
            "SIGNED_CANDIDATE_TRAJECTORY",
            name,
            $"Budget={value.Budget}",
            $"ProtocolBudget={protocolBudget}",
            $"Iterations={value.Iterations}",
            $"Stop={value.StopReason}",
            $"Class={value.Classification}",
            $"Q0N={Format(value.Q0N)}",
            $"X={Format(value.EndpointXM)}",
            $"Z={Format(value.EndpointZM)}",
            $"DepthResidualM={Format(value.DepthResidualM)}",
            $"LineForceN={Format(value.LineForceN)}",
            $"DeltaLineForceN={Format(value.LastDeltaLineForceN)}",
            $"MaxSegmentForceDeltaN={Format(value.LastMaxSegmentForceDeltaN)}",
            $"DeltaX={Format(value.LastDeltaXM)}",
            $"DeltaZ={Format(value.LastDeltaZM)}",
            $"DeltaQ0N={Format(value.LastDeltaQ0N)}",
            $"MaxNodeDeltaM={Format(value.LastMaxNodeDeltaM)}",
            $"NegativeDz={value.NegativeDzSegmentCount}",
            $"PointLoads={value.PointLoadCrossings}",
            $"MaxPointJumpResidualN={Format(value.MaxPointJumpResidualN)}",
            "Acceptance=None");

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Signed candidate convergence trajectory: property {source.GetType().Name}.{propertyName} was not found.");
        var value = property.GetValue(source);
        if (value is T typed)
            return typed;

        throw new InvalidOperationException(
            $"Signed candidate convergence trajectory: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }

    private static double? OptionalDoubleProperty(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Signed candidate convergence trajectory: property {source.GetType().Name}.{propertyName} was not found.");
        var value = property.GetValue(source);
        return value is null ? null : Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static void RequireFinite(double value, string name, int budget, string field)
    {
        if (!double.IsFinite(value))
            throw new InvalidOperationException($"Signed candidate convergence trajectory {name}: {field} is non-finite at budget {budget}.");
    }

    private static void RequireFiniteNullable(double? value, string name, int budget, string field)
    {
        if (value.HasValue && !double.IsFinite(value.Value))
            throw new InvalidOperationException($"Signed candidate convergence trajectory {name}: {field} is non-finite at budget {budget}.");
    }

    private static void Near(double expected, double? actual, double tolerance, string label)
    {
        if (!actual.HasValue || !double.IsFinite(actual.Value) || Math.Abs(expected - actual.Value) > tolerance)
        {
            throw new InvalidOperationException(
                $"Signed candidate convergence trajectory {label}: expected {expected:R}, got {Format(actual)}; tolerance={tolerance:R}. This tolerance checks Package F evidence identity only and is not a convergence criterion.");
        }
    }

    private static void Near(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Signed candidate convergence trajectory {label}: expected {expected:R}, got {actual:R}; tolerance={tolerance:R}. This tolerance checks Package F evidence identity only and is not a convergence criterion.");
        }
    }

    private static string Format(double? value) =>
        value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : "n/a";

    private static string Format(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record PackageFIdentity(double Q0N, double EndpointXM, double EndpointZM);

    private sealed record Measurement(
        int Budget,
        int Iterations,
        string StopReason,
        string Classification,
        double EndpointXM,
        double EndpointZM,
        double? Q0N,
        double? LastDeltaXM,
        double? LastDeltaZM,
        double? LastDeltaQ0N,
        double? LastMaxNodeDeltaM,
        double LineForceN,
        double? LastDeltaLineForceN,
        double? LastMaxSegmentForceDeltaN,
        double? DepthResidualM,
        int NegativeDzSegmentCount,
        int PointLoadCrossings,
        double MaxPointJumpResidualN);
}
