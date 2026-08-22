using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class AcceptedFinalTensionTraceRetentionRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    public static void Validate()
    {
        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var retained = 0;
        var unavailable = 0;
        var withPoints = 0;
        var withoutPoints = 0;
        var rejectionChecked = false;

        Console.WriteLine("F3A_ACCEPTED_FINAL_TRACE_BEGIN");

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
                ?? throw new InvalidOperationException($"F3-A {name}: signed candidate is missing.");

            if (!AcceptedFixtures.Contains(name))
            {
                if (candidate.FinalTensionTrace is not null)
                    throw new InvalidOperationException($"F3-A {name}: non-Accepted candidate retained an Accepted final trace.");
                unavailable++;
                Console.WriteLine(string.Join("|",
                    "F3A_ACCEPTED_FINAL_TRACE",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    "FinalTraceRetained=False",
                    "LocalElementDemandAuthority=None"));
                continue;
            }

            if (candidate.Status != MooringSignedCandidateStatus.Accepted ||
                !candidate.ExactFixedPointReached ||
                candidate.Shape is null ||
                candidate.Boundary is null ||
                candidate.Boundary.SolutionState is null ||
                candidate.FinalTensionTrace is null)
            {
                throw new InvalidOperationException($"F3-A {name}: Accepted candidate final trace contract is incomplete.");
            }

            var trace = candidate.FinalTensionTrace;
            var shape = candidate.Shape;
            var boundary = candidate.Boundary;
            var solution = boundary.SolutionState;

            if (!trace.Available ||
                trace.ParentClassification != boundary.Classification ||
                trace.Rows.Count != run.Result.SegmentRows.Count ||
                trace.Rows.Count != shape.Nodes.Count - 1 ||
                trace.PointLoadCrossings != candidate.PointLoadCrossings ||
                candidate.ContainsDiscreteLoads != (trace.PointLoadCrossings > 0))
            {
                throw new InvalidOperationException($"F3-A {name}: retained trace count/classification/discrete identity mismatch.");
            }

            Exact(trace.StartHN, boundary.BuoySteadyDragN, name + " trace/boundary start H");
            Exact(trace.StartVN, boundary.Q0N, name + " trace/boundary start V");
            Exact(trace.EndHN, solution.EndHN, name + " trace/boundary end H");
            Exact(trace.EndVN, solution.EndVN, name + " trace/boundary end V");

            for (var i = 0; i < trace.Rows.Count; i++)
            {
                var row = trace.Rows[i];
                var node = shape.Nodes[i + 1];
                if (row.SegmentNumber != node.SegmentNumber)
                    throw new InvalidOperationException($"F3-A {name}: segment identity differs at retained row {i}.");
                Exact(row.EndLengthM, node.AlongLineM, name + $" segment {row.SegmentNumber} end s");
                Exact(row.MidTensionN / 1000.0, node.SegmentTensionKn, name + $" segment {row.SegmentNumber} midpoint tension");
            }

            var selectedCore = run.Snapshot.ShadowSelectedCore;
            if (selectedCore is null ||
                selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                !ReferenceEquals(selectedCore.Shape, shape))
            {
                throw new InvalidOperationException($"F3-A {name}: selected Accepted shape identity changed.");
            }

            if (!rejectionChecked)
            {
                var wrongEndH = trace with { EndHN = trace.EndHN!.Value + 1.0 };
                ExpectThrows<ArgumentException>(
                    () => MooringSignedCandidateResult.CreateAccepted(
                        shape,
                        boundary,
                        candidate.FeedbackIterations,
                        candidate.ContainsDiscreteLoads,
                        candidate.PointLoadCrossings,
                        "F3AWrongTrace",
                        "Expected trace-identity rejection.",
                        wrongEndH),
                    name + " changed terminal H");

                var compatibilityFactory = MooringSignedCandidateResult.CreateAccepted(
                    shape,
                    boundary,
                    candidate.FeedbackIterations,
                    candidate.ContainsDiscreteLoads,
                    candidate.PointLoadCrossings,
                    "F3ACompatibilityFactory",
                    "Validation-only legacy factory compatibility without retained trace.");
                if (compatibilityFactory.FinalTensionTrace is not null)
                    throw new InvalidOperationException("F3-A compatibility factory unexpectedly fabricated a final trace.");

                rejectionChecked = true;
            }

            if (candidate.ContainsDiscreteLoads)
                withPoints++;
            else
                withoutPoints++;
            retained++;

            Console.WriteLine(string.Join("|",
                "F3A_ACCEPTED_FINAL_TRACE",
                name,
                "CandidateStatus=Accepted",
                "SelectedSource=SignedBoundaryFeedback",
                "FinalTraceRetained=True",
                $"Rows={trace.Rows.Count}",
                $"PointLoadCrossings={trace.PointLoadCrossings}",
                $"StartH={F(trace.StartHN!.Value)}",
                $"StartV={F(trace.StartVN!.Value)}",
                $"EndH={F(trace.EndHN!.Value)}",
                $"EndV={F(trace.EndVN!.Value)}",
                "TraceSource=ExactFixedPointNextTrace",
                "Reconstruction=None",
                "ScalarAuthorityChanged=False"));
        }

        if (definitions.Count != 5 || retained != 2 || unavailable != 3 ||
            withPoints != 1 || withoutPoints != 1 || !rejectionChecked)
        {
            throw new InvalidOperationException(
                $"F3-A canonical coverage mismatch: scenarios={definitions.Count}, retained={retained}, unavailable={unavailable}, withPoints={withPoints}, withoutPoints={withoutPoints}, rejection={rejectionChecked}.");
        }

        Console.WriteLine(
            "F3A_ACCEPTED_FINAL_TRACE_ROLLUP|CanonicalScenarios=5|AcceptedRetained=2|NonAcceptedRetained=0|WithPointLoads=1|WithoutPointLoads=1|TraceBoundaryShapeIdentity=Exact|TraceSource=ExactFixedPointNextTrace|HiddenTraceReconstruction=None|ProductionScalarAuthorityChanged=False|WeakLinkMigration=False");
        Console.WriteLine("F3A_ACCEPTED_FINAL_TRACE_END");
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F3-A: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F3-A: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F3-A: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F3-A: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Exact(double? actual, double? expected, string label)
    {
        if (!actual.HasValue || !expected.HasValue || actual.Value != expected.Value)
            throw new InvalidOperationException($"F3-A {label}: expected exact {expected}, got {actual}.");
    }

    private static void Exact(double? actual, double expected, string label)
    {
        if (!actual.HasValue || actual.Value != expected)
            throw new InvalidOperationException($"F3-A {label}: expected exact {expected:R}, got {actual}.");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F3-A {label}: expected exact {expected:R}, got {actual:R}.");
    }

    private static void ExpectThrows<TException>(Action action, string label)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"F3-A expected {typeof(TException).Name} for {label}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
