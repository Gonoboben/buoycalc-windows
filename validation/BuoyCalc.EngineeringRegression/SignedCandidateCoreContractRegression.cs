using System.Collections;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedCandidateCoreContractRegression
{
    private static readonly HashSet<string> RequiredFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload",
        "vertical-zero-current"
    };

    public static void Validate()
    {
        var scenarioBuilder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Signed core contract: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");

        var definitions = scenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("Signed core contract: historical fixture definitions are unavailable.");

        var measured = 0;
        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!RequiredFixtures.Contains(name))
                continue;

            measured++;
            var environment = RequireProperty<EnvironmentInput>(definition, "Environment");
            var buoy = RequireProperty<BuoyInput>(definition, "Buoy");
            var assembly = RequireProperty<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = RequireProperty<AnchorInput>(definition, "Anchor");
            var safetyFactor = RequireProperty<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
            var data = run.Snapshot.TechnicalReportData;

            if (name == "vertical-zero-current")
                ValidateIndeterminateContract(name, data.SurfaceBoundaryInfo);
            else
                ValidateAcceptedStructure(name, run.Result, data.SurfaceBoundaryInfo, data.SurfaceBoundaryTensionTrace);
        }

        if (measured != RequiredFixtures.Count)
        {
            throw new InvalidOperationException(
                $"Signed core contract: expected {RequiredFixtures.Count} fixtures, measured {measured}.");
        }

        ValidateFactoryRejections();

        Console.WriteLine(string.Join("|",
            "SIGNED_CANDIDATE_CORE_CONTRACT",
            $"ProductionFeedbackBudget={MooringSignedCandidateResult.ProductionFeedbackBudget}",
            "TypedSourceIdentity=True",
            "AcceptedBoundaryGeometryIdentity=True",
            "CandidateStatusDistinctFromSelectedSource=True",
            "ReadModelChanged=False",
            "ProjectJsonChanged=False",
            "SelectedAuthoritySwitch=False"));
    }

    private static void ValidateAcceptedStructure(
        string name,
        CalculationResult result,
        MooringSurfaceBoundaryInfoResult boundary,
        MooringSurfaceBoundaryTensionTraceResult trace)
    {
        if (!boundary.Solved || boundary.SolutionState is null || !boundary.Q0N.HasValue || !trace.Available)
        {
            throw new InvalidOperationException(
                $"Signed core contract {name}: structural fixture requires solved boundary/trace, got {boundary.Classification}.");
        }

        var shape = BuildContractShape(result, boundary, trace);
        var pointLoads = trace.PointLoadCrossings;
        var containsDiscreteLoads = pointLoads > 0;

        var candidate = MooringSignedCandidateResult.CreateAccepted(
            shape,
            boundary,
            feedbackIterations: 16,
            containsDiscreteLoads,
            pointLoads,
            "ContractFixture",
            "Structural contract fixture only; production acceptance remains owned by the future signed evaluator.");

        if (candidate.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
            candidate.Status != MooringSignedCandidateStatus.Accepted ||
            !candidate.ExactFixedPointReached ||
            candidate.Shape != shape ||
            candidate.Boundary != boundary ||
            candidate.ContainsDiscreteLoads != containsDiscreteLoads ||
            candidate.PointLoadCrossings != pointLoads)
        {
            throw new InvalidOperationException($"Signed core contract {name}: Accepted result identity is inconsistent.");
        }

        var selected = MooringSelectedShapeResult.Create(
            shape,
            MooringShapeSourceIdentity.SignedBoundaryFeedback,
            selectedConverged: true,
            selectedUsesDiscreteLoads: containsDiscreteLoads,
            "Structural signed selected-core contract fixture.");

        if (selected.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
            !selected.SelectedConverged ||
            selected.SelectedUsesDiscreteLoads != containsDiscreteLoads ||
            selected.Shape != shape)
        {
            throw new InvalidOperationException($"Signed core contract {name}: selected-core result identity is inconsistent.");
        }

        ExpectThrows<ArgumentException>(
            () => MooringSelectedShapeResult.Create(
                shape,
                MooringShapeSourceIdentity.SignedBoundaryFeedback,
                selectedConverged: false,
                selectedUsesDiscreteLoads: containsDiscreteLoads,
                "Invalid signed selected-core fixture."),
            name + " signed selected source cannot claim non-convergence");

        var wrongEndpointShape = shape with
        {
            HorizontalOffsetM = shape.HorizontalOffsetM + 1.0
        };
        ExpectThrows<ArgumentException>(
            () => MooringSignedCandidateResult.CreateAccepted(
                wrongEndpointShape,
                boundary,
                16,
                containsDiscreteLoads,
                pointLoads,
                "WrongEndpoint",
                "Expected rejection."),
            name + " accepted endpoint identity");

        ExpectThrows<ArgumentException>(
            () => MooringSignedCandidateResult.CreateAccepted(
                shape,
                boundary,
                16,
                !containsDiscreteLoads,
                pointLoads,
                "WrongDiscreteIdentity",
                "Expected rejection."),
            name + " accepted discrete identity");
    }

    private static void ValidateIndeterminateContract(
        string name,
        MooringSurfaceBoundaryInfoResult boundary)
    {
        if (boundary.Classification != MooringSurfaceBoundaryInfoClassification.VerticalGeometryUniqueForceStateFamily ||
            boundary.Solved ||
            boundary.Q0N.HasValue)
        {
            throw new InvalidOperationException(
                $"Signed core contract {name}: expected non-unique Q0 vertical family, got {boundary.Classification}, solved={boundary.Solved}, q0={boundary.Q0N}.");
        }

        var candidate = MooringSignedCandidateResult.CreateNonAccepted(
            MooringSignedCandidateStatus.Indeterminate,
            shape: null,
            boundary,
            feedbackIterations: 0,
            containsDiscreteLoads: false,
            pointLoadCrossings: 0,
            "VerticalForceStateNonUnique",
            "Straight vertical geometry does not define one unique signed Q0 force state.");

        if (candidate.Status != MooringSignedCandidateStatus.Indeterminate ||
            candidate.ExactFixedPointReached ||
            candidate.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback)
        {
            throw new InvalidOperationException($"Signed core contract {name}: Indeterminate state identity is inconsistent.");
        }
    }

    private static void ValidateFactoryRejections()
    {
        ExpectThrows<ArgumentException>(
            () => MooringSignedCandidateResult.CreateNonAccepted(
                MooringSignedCandidateStatus.Accepted,
                null,
                null,
                0,
                false,
                0,
                "InvalidAcceptedFactory",
                "Expected rejection."),
            "Accepted must use CreateAccepted");

        ExpectThrows<ArgumentException>(
            () => MooringSignedCandidateResult.CreateNonAccepted(
                MooringSignedCandidateStatus.BudgetExhausted,
                null,
                null,
                MooringSignedCandidateResult.ProductionFeedbackBudget - 1,
                false,
                0,
                "WrongBudget",
                "Expected rejection."),
            "BudgetExhausted requires fixed production budget");

        var budgetExhausted = MooringSignedCandidateResult.CreateNonAccepted(
            MooringSignedCandidateStatus.BudgetExhausted,
            null,
            null,
            MooringSignedCandidateResult.ProductionFeedbackBudget,
            false,
            0,
            "ExactFixedPointNotReachedWithinBudget",
            "Candidate remained non-accepted at the fixed production feedback budget.");

        if (budgetExhausted.ExactFixedPointReached ||
            budgetExhausted.FeedbackIterations != MooringSignedCandidateResult.ProductionFeedbackBudget)
        {
            throw new InvalidOperationException("Signed core contract: BudgetExhausted state is inconsistent.");
        }
    }

    private static MooringShapeResult BuildContractShape(
        CalculationResult result,
        MooringSurfaceBoundaryInfoResult boundary,
        MooringSurfaceBoundaryTensionTraceResult trace)
    {
        var nodes = new List<MooringShapePoint>();
        var segments = result.SegmentRows.ToDictionary(x => x.Number);
        var x = 0.0;
        var z = 0.0;

        nodes.Add(new MooringShapePoint(
            1,
            0,
            "Signed contract top",
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            "INFO: structural contract fixture"));

        foreach (var row in trace.Rows)
        {
            if (!segments.TryGetValue(row.SegmentNumber, out var segment))
            {
                throw new InvalidOperationException(
                    $"Signed core contract: segment {row.SegmentNumber} is missing from CalculationResult.");
            }

            var tx = row.TangentX
                ?? throw new InvalidOperationException($"Signed core contract: missing tangent X on segment {row.SegmentNumber}.");
            var tz = row.TangentZ
                ?? throw new InvalidOperationException($"Signed core contract: missing tangent Z on segment {row.SegmentNumber}.");
            if (!double.IsFinite(row.MidTensionN) || row.MidTensionN <= 0.0)
            {
                throw new InvalidOperationException(
                    $"Signed core contract: invalid mid-tension on segment {row.SegmentNumber}.");
            }

            // Match MooringSurfaceBoundaryIntegrationKernel operation order exactly:
            // x += SegmentLengthM * MidH / MidTension and likewise for z.
            // Rewriting this as SegmentLengthM * (MidH / MidTension) is
            // mathematically equivalent but not guaranteed bit-identical in floating point.
            x += segment.SegmentLengthM * row.MidHN / row.MidTensionN;
            z += segment.SegmentLengthM * row.MidVN / row.MidTensionN;
            var angle = Math.Atan2(Math.Abs(tx), Math.Max(1e-12, Math.Abs(tz))) * 180.0 / Math.PI;

            nodes.Add(new MooringShapePoint(
                nodes.Count + 1,
                row.SegmentNumber,
                row.SourceElement,
                row.EndLengthM,
                x,
                z,
                segment.SegmentLengthM,
                angle,
                row.MidTensionN / 1000.0,
                "INFO: structural contract fixture"));
        }

        if (boundary.SolutionState is null ||
            x != boundary.SolutionState.EndpointXM ||
            z != boundary.SolutionState.EndpointZM)
        {
            throw new InvalidOperationException(
                "Signed core contract: kernel-order structural shape endpoint does not exactly match boundary endpoint.");
        }

        var top = nodes[0];
        var bottom = nodes[^1];
        var targetDepth = boundary.TargetDepthM ?? z;
        var lineLength = boundary.LineLengthM ?? result.SegmentRows.Sum(segment => segment.SegmentLengthM);

        return new MooringShapeResult(
            nodes,
            top,
            bottom,
            BuoyShapeState.Surface,
            targetDepth,
            lineLength,
            x,
            Math.Abs(z - targetDepth),
            true,
            "Validation-only structural shape for signed candidate core contract.",
            16,
            0.0,
            1.0,
            "Structural contract only; no production convergence rule is executed here.");
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

        throw new InvalidOperationException($"Signed core contract: expected {typeof(TException).Name} for {label}.");
    }

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Signed core contract: property {source.GetType().Name}.{propertyName} was not found.");

        var value = property.GetValue(source);
        if (value is T typed)
            return typed;

        throw new InvalidOperationException(
            $"Signed core contract: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }
}
