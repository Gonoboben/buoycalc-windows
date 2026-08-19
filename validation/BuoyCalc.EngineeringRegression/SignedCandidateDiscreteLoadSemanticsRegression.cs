using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedCandidateDiscreteLoadSemanticsRegression
{
    private const int FeedbackBudget = 64;

    private static readonly HashSet<string> TargetFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    public static void Validate()
    {
        var historicalScenarioBuilder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed discrete-load semantics: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");

        var runBudget = typeof(BoundaryConditionedFeedbackCouplingRegression).GetMethod(
            "RunBudget",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed discrete-load semantics: BoundaryConditionedFeedbackCouplingRegression.RunBudget was not found.");

        var definitions = historicalScenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException(
                "Signed discrete-load semantics: historical fixture definitions are unavailable.");

        var measured = 0;
        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!TargetFixtures.Contains(name))
                continue;

            measured++;
            var environment = RequireProperty<EnvironmentInput>(definition, "Environment");
            var buoy = RequireProperty<BuoyInput>(definition, "Buoy");
            var assembly = RequireProperty<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = RequireProperty<AnchorInput>(definition, "Anchor");
            var safetyFactor = RequireProperty<double>(definition, "SafetyFactor");

            ValidateFixtureAssemblyIdentity(name, assembly);

            var run = ApplicationCalculationRunner.Run(
                environment,
                buoy,
                assembly,
                anchor,
                safetyFactor);

            var data = run.Snapshot.TechnicalReportData;
            var sequence = data.SequencePositions;
            var boundary = data.SurfaceBoundaryInfo;
            var trace = data.SurfaceBoundaryTensionTrace;
            var internalPoints = InternalPoints(sequence);

            if (!boundary.Solved || boundary.SolutionState is null || !boundary.Q0N.HasValue)
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: expected solved unique boundary state, got {boundary.Classification}.");
            }
            if (!trace.Available)
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: initial signed trace is unavailable: {trace.UnavailableReason}");
            }

            var expectedInternalPoints = name == "discrete-payload" ? 2 : 0;
            if (internalPoints.Count != expectedInternalPoints)
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: internal point count {internalPoints.Count} != expected {expectedInternalPoints}.");
            }
            if (sequence.DiscreteElementCount != expectedInternalPoints)
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: sequence discrete element count {sequence.DiscreteElementCount} != expected {expectedInternalPoints}.");
            }
            if (boundary.SolutionState.PointLoadCrossings != expectedInternalPoints)
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: boundary crossing count {boundary.SolutionState.PointLoadCrossings} != expected {expectedInternalPoints}.");
            }
            if (trace.PointLoadCrossings != expectedInternalPoints)
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: trace crossing count {trace.PointLoadCrossings} != expected {expectedInternalPoints}.");
            }

            if (name == "discrete-payload")
                ValidateDiscretePayloadPointIdentity(internalPoints);

            var outcome = InvokeBudget(
                runBudget,
                name,
                environment,
                buoy,
                run.Result,
                sequence,
                boundary,
                trace);

            var iterations = RequireProperty<int>(outcome, "Iterations");
            var stopReason = RequireProperty<string>(outcome, "StopReason");
            var feedbackCrossings = RequireProperty<int>(outcome, "PointLoadCrossings");
            var maxPointJumpResidualN = RequireProperty<double>(outcome, "MaxPointJumpResidualN");

            if (iterations != FeedbackBudget || stopReason != "BudgetReached")
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: expected historical 64-step measurement continuity, got iterations={iterations}, stop={stopReason}.");
            }
            if (feedbackCrossings != expectedInternalPoints)
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: final feedback crossings {feedbackCrossings} != expected {expectedInternalPoints}.");
            }
            if (!double.IsFinite(maxPointJumpResidualN) || maxPointJumpResidualN != 0.0)
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: expected exact emitted point-jump closure identity, got {maxPointJumpResidualN:R} N.");
            }

            var containsDiscreteLoads = internalPoints.Count > 0;
            if (containsDiscreteLoads != (name == "discrete-payload"))
            {
                throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: candidate ContainsDiscreteLoads truth is inconsistent with internal point ownership.");
            }

            Console.WriteLine(string.Join("|",
                "SIGNED_CANDIDATE_DISCRETE_LOAD_SEMANTICS",
                name,
                $"InternalPoints={internalPoints.Count}",
                $"SequenceDiscreteElementCount={sequence.DiscreteElementCount}",
                $"BoundaryPointLoadCrossings={boundary.SolutionState.PointLoadCrossings}",
                $"InitialTracePointLoadCrossings={trace.PointLoadCrossings}",
                $"FeedbackBudget={FeedbackBudget}",
                $"FeedbackPointLoadCrossings={feedbackCrossings}",
                $"MaxPointJumpResidualN={Format(maxPointJumpResidualN)}",
                $"CandidateContainsDiscreteLoads={containsDiscreteLoads}",
                "SelectedUsesDiscreteLoadsRuntime=None"));
        }

        if (measured != TargetFixtures.Count)
        {
            throw new InvalidOperationException(
                $"Signed discrete-load semantics: expected {TargetFixtures.Count} target fixtures, measured {measured}.");
        }
    }

    private static object InvokeBudget(
        MethodInfo runBudget,
        string name,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult result,
        MooringSequencePositionResult sequence,
        MooringSurfaceBoundaryInfoResult boundary,
        MooringSurfaceBoundaryTensionTraceResult trace)
    {
        try
        {
            return runBudget.Invoke(
                null,
                new object[]
                {
                    name,
                    FeedbackBudget,
                    environment,
                    buoy,
                    result,
                    sequence,
                    boundary,
                    trace,
                    false
                }) ?? throw new InvalidOperationException(
                    $"Signed discrete-load semantics {name}: RunBudget returned null.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Signed discrete-load semantics {name}: existing feedback validation failed.",
                ex.InnerException);
        }
    }

    private static void ValidateFixtureAssemblyIdentity(
        string name,
        IReadOnlyList<AssemblyItemInput> assembly)
    {
        var connectorCount = assembly.Count(x => x.Kind == AssemblyItemKind.Connector);
        var payloadCount = assembly.Count(x => x.Kind == AssemblyItemKind.Payload);

        if (name == "uniform-current-slack-line")
        {
            if (connectorCount != 0 || payloadCount != 0)
            {
                throw new InvalidOperationException(
                    "Signed discrete-load semantics uniform-current-slack-line: fixture unexpectedly contains connector/payload point loads.");
            }
            return;
        }

        if (connectorCount != 1 || payloadCount != 1)
        {
            throw new InvalidOperationException(
                $"Signed discrete-load semantics discrete-payload: expected one connector and one payload, got connectors={connectorCount}, payloads={payloadCount}.");
        }
    }

    private static void ValidateDiscretePayloadPointIdentity(
        IReadOnlyList<MooringSequencePositionRow> internalPoints)
    {
        if (internalPoints.Count != 2)
            throw new InvalidOperationException("Signed discrete-load semantics discrete-payload: expected two internal points.");

        if (!string.Equals(internalPoints[0].Title, "Shackle", StringComparison.Ordinal) ||
            !string.Equals(internalPoints[1].Title, "Payload", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Signed discrete-load semantics discrete-payload: expected Shackle/Payload point order, got {string.Join(",", internalPoints.Select(x => x.Title))}.");
        }

        if (internalPoints[0].PositionAlongLineM != 30.0 || internalPoints[1].PositionAlongLineM != 30.0)
        {
            throw new InvalidOperationException(
                $"Signed discrete-load semantics discrete-payload: expected both point loads at s=30 m, got {Format(internalPoints[0].PositionAlongLineM)}, {Format(internalPoints[1].PositionAlongLineM)}.");
        }

        if (!internalPoints.All(x => x.IsDiscrete && !x.IsDistributed))
        {
            throw new InvalidOperationException(
                "Signed discrete-load semantics discrete-payload: internal connector/payload rows are not represented as discrete sequence points.");
        }
    }

    private static IReadOnlyList<MooringSequencePositionRow> InternalPoints(
        MooringSequencePositionResult sequence)
    {
        var ordered = sequence.Rows.OrderBy(x => x.Number).ToList();
        if (ordered.Count < 2)
            return Array.Empty<MooringSequencePositionRow>();

        var topNumber = ordered[0].Number;
        var bottomNumber = ordered[^1].Number;
        return ordered
            .Where(x => x.IsDiscrete && x.Number != topNumber && x.Number != bottomNumber)
            .OrderBy(x => x.PositionAlongLineM)
            .ThenBy(x => x.Number)
            .ToList();
    }

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Signed discrete-load semantics: property {source.GetType().Name}.{propertyName} was not found.");

        var value = property.GetValue(source);
        if (value is T typed)
            return typed;

        throw new InvalidOperationException(
            $"Signed discrete-load semantics: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }

    private static string Format(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);
}
