using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedCandidateShadowArbitrationRegression
{
    private const int FeedbackBudget = 64;
    private const double ExistingPointJumpClosureToleranceN = 1e-6;

    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    private static readonly Dictionary<string, ShadowCandidateStatus> BlockedFixtureStatus = new(StringComparer.Ordinal)
    {
        ["vertical-zero-current"] = ShadowCandidateStatus.Indeterminate,
        ["buoyant-line"] = ShadowCandidateStatus.RejectedPhysical,
        ["depth-varying-current-profile"] = ShadowCandidateStatus.RejectedPhysical
    };

    public static void Validate()
    {
        var historicalScenarioBuilder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed shadow arbitration: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");

        var historicalRunCandidate = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "RunCandidate",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed shadow arbitration: HistoricalGoldenImpactRegression.RunCandidate was not found.");

        var runBudget = typeof(BoundaryConditionedFeedbackCouplingRegression).GetMethod(
            "RunBudget",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed shadow arbitration: BoundaryConditionedFeedbackCouplingRegression.RunBudget was not found.");

        var definitions = historicalScenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException(
                "Signed shadow arbitration: historical fixture definitions are unavailable.");

        var acceptedCount = 0;
        var rejectedPhysicalCount = 0;
        var indeterminateCount = 0;
        var totalCount = 0;

        Console.WriteLine("SIGNED_CANDIDATE_SHADOW_ARBITRATION_BEGIN");

        foreach (var definition in definitions.Cast<object>())
        {
            totalCount++;
            var name = RequireProperty<string>(definition, "Name");
            if (!AcceptedFixtures.Contains(name) && !BlockedFixtureStatus.ContainsKey(name))
            {
                throw new InvalidOperationException(
                    $"Signed shadow arbitration: unexpected historical fixture '{name}'.");
            }

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

            var productionSelected = run.Snapshot.SelectedShape
                ?? throw new InvalidOperationException(
                    $"Signed shadow arbitration {name}: production selected shape is unavailable.");
            var productionBefore = CaptureSelected(productionSelected);
            var data = run.Snapshot.TechnicalReportData;
            var boundary = data.SurfaceBoundaryInfo;
            var trace = data.SurfaceBoundaryTensionTrace;
            var sequence = data.SequencePositions;

            var historicalCandidate = InvokeHistoricalCandidate(historicalRunCandidate, definition, name);
            ShadowCandidate candidate;

            if (AcceptedFixtures.Contains(name))
            {
                candidate = BuildAcceptedCandidate(
                    runBudget,
                    name,
                    environment,
                    buoy,
                    run.Result,
                    sequence,
                    boundary,
                    trace,
                    historicalCandidate);
                acceptedCount++;
            }
            else
            {
                candidate = BuildBlockedCandidate(name, boundary, historicalCandidate);
                if (candidate.Status == ShadowCandidateStatus.RejectedPhysical)
                    rejectedPhysicalCount++;
                else if (candidate.Status == ShadowCandidateStatus.Indeterminate)
                    indeterminateCount++;
            }

            var shadow = SelectShadow(productionBefore, candidate);
            ValidateShadowSelection(name, productionBefore, candidate, shadow);

            var productionAfter = CaptureSelected(
                run.Snapshot.SelectedShape
                ?? throw new InvalidOperationException(
                    $"Signed shadow arbitration {name}: production selected shape disappeared after shadow evaluation."));
            AssertProductionUnchanged(name, productionBefore, productionAfter);

            Console.WriteLine(string.Join("|",
                "SIGNED_CANDIDATE_SHADOW_ARBITRATION",
                name,
                $"CandidateStatus={candidate.Status}",
                $"BoundaryClass={boundary.Classification}",
                $"CandidateAvailable={candidate.Available}",
                $"CandidateExactFixedPoint={candidate.ExactFixedPointReached}",
                $"CandidateIterations={candidate.FeedbackIterations}",
                $"CandidatePointLoads={candidate.PointLoadCrossings}",
                $"CandidateUsesDiscreteLoads={candidate.ContainsDiscreteLoads}",
                $"ProductionSource={productionBefore.Source}",
                $"ProductionConverged={productionBefore.Converged}",
                $"ProductionUsesDiscreteLoads={productionBefore.UsesDiscreteLoads}",
                $"ShadowSource={shadow.SourceIdentity}",
                $"ShadowConverged={shadow.SelectedConverged}",
                $"ShadowUsesDiscreteLoads={shadow.SelectedUsesDiscreteLoads}",
                $"ShadowX={Format(shadow.HorizontalOffsetM)}",
                $"ShadowZ={Format(shadow.AnchorDepthM)}",
                $"ProductionRuntimeUnchanged=True"));
        }

        if (totalCount != 5 || acceptedCount != 2 || rejectedPhysicalCount != 2 || indeterminateCount != 1)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration: truth-table counts mismatch. total={totalCount}, accepted={acceptedCount}, rejectedPhysical={rejectedPhysicalCount}, indeterminate={indeterminateCount}.");
        }

        Console.WriteLine(string.Join("|",
            "SIGNED_CANDIDATE_SHADOW_ARBITRATION_ROLLUP",
            $"Scenarios={totalCount}",
            $"Accepted={acceptedCount}",
            $"RejectedPhysical={rejectedPhysicalCount}",
            $"Indeterminate={indeterminateCount}",
            "ProductionSelectedShapeMutated=False",
            "GoldenBaselineModified=False",
            "AuthoritySwitch=False"));
        Console.WriteLine("SIGNED_CANDIDATE_SHADOW_ARBITRATION_END");
    }

    private static ShadowCandidate BuildAcceptedCandidate(
        MethodInfo runBudget,
        string name,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult result,
        MooringSequencePositionResult sequence,
        MooringSurfaceBoundaryInfoResult boundary,
        MooringSurfaceBoundaryTensionTraceResult trace,
        object historicalCandidate)
    {
        if (!boundary.Solved || boundary.SolutionState is null || !boundary.Q0N.HasValue || !trace.Available)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: accepted fixture boundary/trace is unavailable: {boundary.Classification}.");
        }

        if (!RequireProperty<bool>(historicalCandidate, "Available"))
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: historical signed candidate is unexpectedly unavailable.");
        }

        var outcome = InvokeBudget(
            runBudget,
            name,
            environment,
            buoy,
            result,
            sequence,
            boundary,
            trace);

        var iterations = RequireProperty<int>(outcome, "Iterations");
        var stopReason = RequireProperty<string>(outcome, "StopReason");
        if (iterations != FeedbackBudget || stopReason != "BudgetReached")
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: expected 64-step validation horizon, got iterations={iterations}, stop={stopReason}.");
        }

        RequireExactZero(outcome, "LastDeltaXM", name);
        RequireExactZero(outcome, "LastDeltaZM", name);
        RequireExactZero(outcome, "LastDeltaQ0N", name);
        RequireExactZero(outcome, "LastMaxNodeDeltaM", name);
        RequireExactZero(outcome, "LastDeltaLineForceN", name);
        RequireExactZero(outcome, "LastMaxSegmentForceDeltaN", name);

        var negativeDz = RequireProperty<int>(outcome, "NegativeDzSegmentCount");
        if (negativeDz != 0)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: accepted candidate has {negativeDz} negative-dz segments.");
        }

        var pointLoads = RequireProperty<int>(outcome, "PointLoadCrossings");
        var expectedPointLoads = name == "discrete-payload" ? 2 : 0;
        if (pointLoads != expectedPointLoads)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: point-load crossings {pointLoads} != expected {expectedPointLoads}.");
        }

        var pointJumpResidualN = RequireProperty<double>(outcome, "MaxPointJumpResidualN");
        if (!double.IsFinite(pointJumpResidualN) || pointJumpResidualN > ExistingPointJumpClosureToleranceN)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: point-jump closure residual {pointJumpResidualN:R} N exceeds existing validation identity tolerance.");
        }

        var historicalIterations = RequireProperty<int>(historicalCandidate, "Iterations");
        var historicalStop = RequireProperty<string>(historicalCandidate, "StopReason");
        if (historicalIterations != FeedbackBudget || historicalStop != "BudgetReached")
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: historical candidate continuity mismatch.");
        }

        var endpointX = RequireProperty<double>(historicalCandidate, "EndpointXM");
        var endpointZ = RequireProperty<double>(historicalCandidate, "EndpointZM");
        var outcomeX = RequireProperty<double>(outcome, "EndpointXM");
        var outcomeZ = RequireProperty<double>(outcome, "EndpointZM");
        if (endpointX != outcomeX || endpointZ != outcomeZ)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: historical candidate geometry and exact-fixed-point measurement disagree.");
        }

        var nodes = RequirePropertyValue(historicalCandidate, "Nodes") as IEnumerable
            ?? throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: candidate node collection is unavailable.");
        var nodeCount = nodes.Cast<object>().Count();
        if (nodeCount != result.SegmentRows.Count + 1 || nodeCount < 2)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: candidate node count {nodeCount} is inconsistent with segment count {result.SegmentRows.Count}.");
        }

        return new ShadowCandidate(
            ShadowCandidateStatus.Accepted,
            true,
            true,
            FeedbackBudget,
            endpointX,
            endpointZ,
            nodeCount,
            expectedPointLoads > 0,
            pointLoads,
            boundary.Classification.ToString());
    }

    private static ShadowCandidate BuildBlockedCandidate(
        string name,
        MooringSurfaceBoundaryInfoResult boundary,
        object historicalCandidate)
    {
        var expectedStatus = BlockedFixtureStatus[name];
        if (RequireProperty<bool>(historicalCandidate, "Available"))
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: blocked historical candidate unexpectedly became available.");
        }

        switch (name)
        {
            case "vertical-zero-current":
                if (boundary.Classification != MooringSurfaceBoundaryInfoClassification.VerticalGeometryUniqueForceStateFamily)
                {
                    throw new InvalidOperationException(
                        $"Signed shadow arbitration {name}: expected VerticalGeometryUniqueForceStateFamily, got {boundary.Classification}.");
                }
                break;
            case "buoyant-line":
            case "depth-varying-current-profile":
                if (boundary.Classification != MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot)
                {
                    throw new InvalidOperationException(
                        $"Signed shadow arbitration {name}: expected TautNonZeroHorizontalLoadNoFiniteRoot, got {boundary.Classification}.");
                }
                break;
            default:
                throw new InvalidOperationException($"Signed shadow arbitration: unsupported blocked fixture {name}.");
        }

        return new ShadowCandidate(
            expectedStatus,
            false,
            false,
            0,
            null,
            null,
            0,
            false,
            0,
            boundary.Classification.ToString());
    }

    private static ShadowSelection SelectShadow(
        SelectedSnapshot current,
        ShadowCandidate candidate)
    {
        if (candidate.Status == ShadowCandidateStatus.Accepted)
        {
            if (!candidate.Available || !candidate.ExactFixedPointReached ||
                !candidate.HorizontalOffsetM.HasValue || !candidate.AnchorDepthM.HasValue || candidate.NodeCount < 2)
            {
                throw new InvalidOperationException(
                    "Signed shadow arbitration: Accepted candidate is internally contradictory.");
            }

            return new ShadowSelection(
                "SignedBoundaryFeedback",
                true,
                candidate.ContainsDiscreteLoads,
                candidate.HorizontalOffsetM.Value,
                candidate.AnchorDepthM.Value,
                candidate.NodeCount);
        }

        return new ShadowSelection(
            current.Source,
            current.Converged,
            current.UsesDiscreteLoads,
            current.HorizontalOffsetM,
            current.AnchorDepthM,
            current.NodeCount);
    }

    private static void ValidateShadowSelection(
        string name,
        SelectedSnapshot current,
        ShadowCandidate candidate,
        ShadowSelection shadow)
    {
        if (candidate.Status == ShadowCandidateStatus.Accepted)
        {
            if (!string.Equals(shadow.SourceIdentity, "SignedBoundaryFeedback", StringComparison.Ordinal) ||
                !shadow.SelectedConverged ||
                shadow.SelectedUsesDiscreteLoads != candidate.ContainsDiscreteLoads ||
                shadow.HorizontalOffsetM != candidate.HorizontalOffsetM ||
                shadow.AnchorDepthM != candidate.AnchorDepthM ||
                shadow.NodeCount != candidate.NodeCount)
            {
                throw new InvalidOperationException(
                    $"Signed shadow arbitration {name}: accepted signed candidate was not represented truthfully by shadow selection.");
            }
            return;
        }

        if (!string.Equals(shadow.SourceIdentity, current.Source, StringComparison.Ordinal) ||
            shadow.SelectedConverged != current.Converged ||
            shadow.SelectedUsesDiscreteLoads != current.UsesDiscreteLoads ||
            shadow.HorizontalOffsetM != current.HorizontalOffsetM ||
            shadow.AnchorDepthM != current.AnchorDepthM ||
            shadow.NodeCount != current.NodeCount)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: non-Accepted candidate changed selected-source truth in shadow mode.");
        }
    }

    private static SelectedSnapshot CaptureSelected(SelectedShapeReadModel selected)
    {
        var anchor = selected.Shape.AnchorPoint
            ?? throw new InvalidOperationException(
                $"Signed shadow arbitration: selected source {selected.Source} has no anchor point.");

        return new SelectedSnapshot(
            selected.Source,
            selected.Shape.Converged,
            selected.UsesDiscreteLoads,
            selected.Shape.HorizontalOffsetM,
            anchor.ZDepthM,
            selected.Shape.Nodes.Count);
    }

    private static void AssertProductionUnchanged(
        string name,
        SelectedSnapshot before,
        SelectedSnapshot after)
    {
        if (!string.Equals(before.Source, after.Source, StringComparison.Ordinal) ||
            before.Converged != after.Converged ||
            before.UsesDiscreteLoads != after.UsesDiscreteLoads ||
            before.HorizontalOffsetM != after.HorizontalOffsetM ||
            before.AnchorDepthM != after.AnchorDepthM ||
            before.NodeCount != after.NodeCount)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: validation-only arbitration mutated production selected-shape state.");
        }
    }

    private static object InvokeHistoricalCandidate(MethodInfo method, object definition, string name)
    {
        try
        {
            return method.Invoke(null, new[] { definition })
                ?? throw new InvalidOperationException(
                    $"Signed shadow arbitration {name}: HistoricalGoldenImpactRegression.RunCandidate returned null.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: historical candidate measurement failed.",
                ex.InnerException);
        }
    }

    private static object InvokeBudget(
        MethodInfo method,
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
            return method.Invoke(
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
                    $"Signed shadow arbitration {name}: RunBudget returned null.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: existing feedback validation failed.",
                ex.InnerException);
        }
    }

    private static void RequireExactZero(object source, string propertyName, string name)
    {
        var value = RequireProperty<double>(source, propertyName);
        if (value != 0.0)
        {
            throw new InvalidOperationException(
                $"Signed shadow arbitration {name}: exact fixed-point field {propertyName}={value:R}, expected 0.");
        }
    }

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var value = RequirePropertyValue(source, propertyName);
        if (value is T typed)
            return typed;

        throw new InvalidOperationException(
            $"Signed shadow arbitration: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }

    private static object? RequirePropertyValue(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Signed shadow arbitration: property {source.GetType().Name}.{propertyName} was not found.");
        return property.GetValue(source);
    }

    private static string Format(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private enum ShadowCandidateStatus
    {
        Accepted,
        RejectedPhysical,
        RejectedNumerical,
        BudgetExhausted,
        Indeterminate,
        Unavailable
    }

    private sealed record ShadowCandidate(
        ShadowCandidateStatus Status,
        bool Available,
        bool ExactFixedPointReached,
        int FeedbackIterations,
        double? HorizontalOffsetM,
        double? AnchorDepthM,
        int NodeCount,
        bool ContainsDiscreteLoads,
        int PointLoadCrossings,
        string DiagnosticCode);

    private sealed record SelectedSnapshot(
        string Source,
        bool Converged,
        bool UsesDiscreteLoads,
        double HorizontalOffsetM,
        double AnchorDepthM,
        int NodeCount);

    private sealed record ShadowSelection(
        string SourceIdentity,
        bool SelectedConverged,
        bool SelectedUsesDiscreteLoads,
        double HorizontalOffsetM,
        double AnchorDepthM,
        int NodeCount);
}
