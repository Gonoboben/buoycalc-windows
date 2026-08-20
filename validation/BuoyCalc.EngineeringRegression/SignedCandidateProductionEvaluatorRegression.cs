using System.Collections;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedCandidateProductionEvaluatorRegression
{
    private const int EvidenceHorizon = 64;
    private const int EvidenceFixedPointByIteration = 16;
    private const double ExistingGeometryIdentityToleranceM = 1e-9;

    private static readonly Dictionary<string, MooringSignedCandidateStatus> ExpectedStatus =
        new(StringComparer.Ordinal)
        {
            ["uniform-current-slack-line"] = MooringSignedCandidateStatus.Accepted,
            ["discrete-payload"] = MooringSignedCandidateStatus.Accepted,
            ["vertical-zero-current"] = MooringSignedCandidateStatus.Indeterminate,
            ["buoyant-line"] = MooringSignedCandidateStatus.RejectedPhysical,
            ["depth-varying-current-profile"] = MooringSignedCandidateStatus.RejectedPhysical
        };

    public static void Validate()
    {
        var scenarioBuilder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed production evaluator: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");

        var runBudget = typeof(BoundaryConditionedFeedbackCouplingRegression).GetMethod(
            "RunBudget",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed production evaluator: BoundaryConditionedFeedbackCouplingRegression.RunBudget was not found.");

        var definitions = scenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException(
                "Signed production evaluator: historical fixture definitions are unavailable.");

        var total = 0;
        var accepted = 0;
        var rejectedPhysical = 0;
        var indeterminate = 0;

        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!ExpectedStatus.TryGetValue(name, out var expectedStatus))
            {
                throw new InvalidOperationException(
                    $"Signed production evaluator: unexpected historical fixture '{name}'.");
            }

            total++;
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
            var productionBefore = CaptureSelected(run.Snapshot.SelectedShape, name);
            var data = run.Snapshot.TechnicalReportData;

            var candidate = MooringSignedCandidateEvaluator.Build(
                environment,
                buoy,
                run.Result,
                data.SequencePositions);

            if (candidate.Status != expectedStatus)
            {
                throw new InvalidOperationException(
                    $"Signed production evaluator {name}: expected {expectedStatus}, got {candidate.Status}; code={candidate.DiagnosticCode}; text={candidate.DiagnosticText}");
            }
            if (candidate.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback)
            {
                throw new InvalidOperationException(
                    $"Signed production evaluator {name}: candidate source identity changed.");
            }

            switch (candidate.Status)
            {
                case MooringSignedCandidateStatus.Accepted:
                    accepted++;
                    ValidateAcceptedParity(
                        runBudget,
                        name,
                        environment,
                        buoy,
                        run.Result,
                        data.SequencePositions,
                        data.SurfaceBoundaryInfo,
                        data.SurfaceBoundaryTensionTrace,
                        candidate);
                    break;

                case MooringSignedCandidateStatus.RejectedPhysical:
                    rejectedPhysical++;
                    ValidatePhysicalBlocker(name, candidate);
                    break;

                case MooringSignedCandidateStatus.Indeterminate:
                    indeterminate++;
                    ValidateIndeterminate(name, candidate);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Signed production evaluator {name}: unexpected canonical status {candidate.Status}.");
            }

            var productionAfter = CaptureSelected(run.Snapshot.SelectedShape, name);
            if (productionBefore != productionAfter)
            {
                throw new InvalidOperationException(
                    $"Signed production evaluator {name}: candidate evaluation mutated current production selected shape.");
            }

            Console.WriteLine(string.Join("|",
                "SIGNED_CANDIDATE_PRODUCTION_EVALUATOR",
                name,
                $"Status={candidate.Status}",
                $"BoundaryClass={candidate.Boundary?.Classification.ToString() ?? "none"}",
                $"ExactFixedPoint={candidate.ExactFixedPointReached}",
                $"Iterations={candidate.FeedbackIterations}",
                $"PointLoads={candidate.PointLoadCrossings}",
                $"UsesDiscreteLoads={candidate.ContainsDiscreteLoads}",
                $"X={Format(candidate.Shape?.HorizontalOffsetM)}",
                $"Z={Format(candidate.Shape?.AnchorPoint?.ZDepthM)}",
                "SelectedAuthoritySwitch=False"));
        }

        if (total != 5 || accepted != 2 || rejectedPhysical != 2 || indeterminate != 1)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator truth table mismatch: total={total}, accepted={accepted}, rejectedPhysical={rejectedPhysical}, indeterminate={indeterminate}.");
        }

        var unavailable = MooringSignedCandidateEvaluator.Build(null, null, null, null);
        if (unavailable.Status != MooringSignedCandidateStatus.Unavailable ||
            unavailable.FeedbackIterations != 0 ||
            unavailable.ExactFixedPointReached)
        {
            throw new InvalidOperationException(
                "Signed production evaluator: missing input must map deterministically to Unavailable.");
        }

        Console.WriteLine(string.Join("|",
            "SIGNED_CANDIDATE_PRODUCTION_EVALUATOR_ROLLUP",
            $"Scenarios={total}",
            $"Accepted={accepted}",
            $"RejectedPhysical={rejectedPhysical}",
            $"Indeterminate={indeterminate}",
            $"ProductionFeedbackBudget={MooringSignedCandidateResult.ProductionFeedbackBudget}",
            "Existing64StepEvidenceParity=True",
            "ProductionSelectedShapeMutated=False",
            "GoldenBaselineModified=False",
            "AuthoritySwitch=False"));
    }

    private static void ValidateAcceptedParity(
        MethodInfo runBudget,
        string name,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult result,
        MooringSequencePositionResult sequence,
        MooringSurfaceBoundaryInfoResult initialBoundary,
        MooringSurfaceBoundaryTensionTraceResult initialTrace,
        MooringSignedCandidateResult candidate)
    {
        if (!candidate.ExactFixedPointReached ||
            candidate.Shape is null ||
            candidate.Boundary is null ||
            !candidate.Shape.Converged)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: Accepted candidate lacks converged shape/boundary truth.");
        }
        if (candidate.FeedbackIterations < 1 || candidate.FeedbackIterations > EvidenceFixedPointByIteration)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: exact fixed point was expected by iteration {EvidenceFixedPointByIteration}, got {candidate.FeedbackIterations}.");
        }

        var expectedPointLoads = name == "discrete-payload" ? 2 : 0;
        if (candidate.PointLoadCrossings != expectedPointLoads ||
            candidate.ContainsDiscreteLoads != (expectedPointLoads > 0))
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: discrete-load identity mismatch.");
        }
        if (candidate.Shape.Nodes.Count != result.SegmentRows.Count + 1)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: shape node count {candidate.Shape.Nodes.Count} != segment count + 1.");
        }
        if (candidate.Shape.AnchorPoint is null || candidate.Boundary.SolutionState is null ||
            candidate.Shape.HorizontalOffsetM != candidate.Shape.AnchorPoint.XOffsetM ||
            candidate.Shape.AnchorPoint.XOffsetM != candidate.Boundary.SolutionState.EndpointXM ||
            candidate.Shape.AnchorPoint.ZDepthM != candidate.Boundary.SolutionState.EndpointZM)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: accepted shape/boundary endpoint identity is not exact.");
        }

        object evidenceOutcome;
        try
        {
            evidenceOutcome = runBudget.Invoke(
                null,
                new object[]
                {
                    name,
                    EvidenceHorizon,
                    environment,
                    buoy,
                    result,
                    sequence,
                    initialBoundary,
                    initialTrace,
                    false
                }) ?? throw new InvalidOperationException("64-step evidence outcome is null.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: historical 64-step evidence path failed.",
                ex.InnerException);
        }

        if (RequireProperty<int>(evidenceOutcome, "Iterations") != EvidenceHorizon ||
            RequireProperty<string>(evidenceOutcome, "StopReason") != "BudgetReached")
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: historical evidence horizon changed.");
        }

        RequireExactZero(evidenceOutcome, "LastDeltaXM", name);
        RequireExactZero(evidenceOutcome, "LastDeltaZM", name);
        RequireExactZero(evidenceOutcome, "LastDeltaQ0N", name);
        RequireExactZero(evidenceOutcome, "LastMaxNodeDeltaM", name);
        RequireExactZero(evidenceOutcome, "LastDeltaLineForceN", name);
        RequireExactZero(evidenceOutcome, "LastMaxSegmentForceDeltaN", name);

        var evidenceQ0 = RequireNullableDouble(evidenceOutcome, "Q0N", name);
        if (!candidate.Boundary.Q0N.HasValue || candidate.Boundary.Q0N.Value != evidenceQ0)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: production Q0 does not exactly match merged 64-step evidence.");
        }

        Near(
            RequireProperty<double>(evidenceOutcome, "EndpointXM"),
            candidate.Shape.HorizontalOffsetM,
            ExistingGeometryIdentityToleranceM,
            name + " endpoint X parity");
        Near(
            RequireProperty<double>(evidenceOutcome, "EndpointZM"),
            candidate.Shape.AnchorPoint.ZDepthM,
            ExistingGeometryIdentityToleranceM,
            name + " endpoint Z parity");

        if (RequireProperty<int>(evidenceOutcome, "PointLoadCrossings") != candidate.PointLoadCrossings)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: point-load crossing count differs from merged 64-step evidence.");
        }
    }

    private static void ValidatePhysicalBlocker(
        string name,
        MooringSignedCandidateResult candidate)
    {
        if (name is not ("buoyant-line" or "depth-varying-current-profile") ||
            candidate.Boundary?.Classification != MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot ||
            candidate.Shape is not null || candidate.ExactFixedPointReached || candidate.FeedbackIterations != 0)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: RejectedPhysical blocker identity changed.");
        }
    }

    private static void ValidateIndeterminate(
        string name,
        MooringSignedCandidateResult candidate)
    {
        if (name != "vertical-zero-current" ||
            candidate.Boundary?.Classification != MooringSurfaceBoundaryInfoClassification.VerticalGeometryUniqueForceStateFamily ||
            candidate.Boundary.Q0N.HasValue ||
            candidate.Shape is not null || candidate.ExactFixedPointReached || candidate.FeedbackIterations != 0)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: Indeterminate force-family identity changed.");
        }
    }

    private static SelectedSnapshot CaptureSelected(SelectedShapeReadModel? selected, string name)
    {
        if (selected is null || selected.Shape.AnchorPoint is null)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: current production selected shape is unavailable.");
        }

        return new SelectedSnapshot(
            selected.Source,
            selected.Shape.Converged,
            selected.UsesDiscreteLoads,
            selected.Shape.HorizontalOffsetM,
            selected.Shape.AnchorPoint.ZDepthM,
            selected.Shape.Nodes.Count);
    }

    private static void RequireExactZero(object source, string propertyName, string name)
    {
        var value = RequireNullableDouble(source, propertyName, name);
        if (value != 0.0)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {name}: evidence {propertyName} expected exact zero, got {value:R}.");
        }
    }

    private static double RequireNullableDouble(object source, string propertyName, string name)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Signed production evaluator {name}: property {propertyName} was not found.");
        var raw = property.GetValue(source);
        if (raw is double value)
            return value;
        throw new InvalidOperationException(
            $"Signed production evaluator {name}: property {propertyName} is null/non-double.");
    }

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Signed production evaluator: property {source.GetType().Name}.{propertyName} was not found.");
        var raw = property.GetValue(source);
        if (raw is T typed)
            return typed;
        throw new InvalidOperationException(
            $"Signed production evaluator: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }

    private static void Near(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Signed production evaluator {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private static string Format(double? value) =>
        value.HasValue ? value.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "n/a";

    private sealed record SelectedSnapshot(
        string Source,
        bool Converged,
        bool UsesDiscreteLoads,
        double HorizontalOffsetM,
        double AnchorDepthM,
        int NodeCount);
}
