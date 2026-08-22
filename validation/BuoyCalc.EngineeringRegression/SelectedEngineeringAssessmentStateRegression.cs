using System.Collections;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SelectedEngineeringAssessmentStateRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    public static void Validate()
    {
        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var available = 0;
        var unavailable = 0;
        ApplicationCalculationRun? syntheticBaseRun = null;
        EnvironmentInput? syntheticBaseEnvironment = null;

        Console.WriteLine("F4A_SELECTED_ENGINEERING_ASSESSMENT_BEGIN");

        foreach (var definition in definitions)
        {
            var name = Property<string>(definition, "Name");
            var environment = Property<EnvironmentInput>(definition, "Environment");
            var buoy = Property<BuoyInput>(definition, "Buoy");
            var assembly = Property<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = Property<AnchorInput>(definition, "Anchor");
            var safetyFactor = Property<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
            var snapshot = run.Snapshot;
            var candidate = snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"F4-A {name}: signed candidate is missing.");

            var legacyVerdict = run.Result.Verdict;
            var legacyMainRisk = run.Result.MainRisk;
            var legacyChecks = run.Result.Checks.ToArray();
            var legacyWeakLinkKn = run.Result.WeakLinkBreakingLoadKn;
            var legacyWeakLinkName = run.Result.WeakLinkName;
            var legacyWorkingLoadKn = run.Result.WorkingLoadKn;
            var legacyTensionReserve = run.Result.TensionReserve;
            var legacyAnchorHoldingKg = run.Result.AnchorHoldingKg;
            var legacyRequiredAnchorHoldingKg = run.Result.RequiredAnchorHoldingKg;
            var legacyAnchorReserve = run.Result.AnchorReserve;
            var legacyElementRows = run.Result.ElementRows.ToArray();
            var selectedShapeBefore = snapshot.ShadowSelectedCore?.Shape;
            var legacyUserReport = UserReportBuilder.Build(environment, run.Result);
            var selectedUserReport = UserReportBuilder.Build(environment, snapshot);
            var boundaryUserReport = ReportBuildBoundary.Build(
                "F4-A regression",
                environment,
                buoy,
                anchor,
                snapshot).UserResultText;

            if (!AcceptedFixtures.Contains(name))
            {
                if (snapshot.SelectedDesignEnvelope is not null ||
                    snapshot.SelectedDesignTensionDemand is not null ||
                    snapshot.SelectedAnchorReaction is not null ||
                    snapshot.SelectedLocalElementDemand is not null ||
                    snapshot.SelectedLocalStructuralCapacity is not null ||
                    snapshot.SelectedEngineeringAssessment is not null)
                {
                    throw new InvalidOperationException($"F4-A {name}: non-Accepted selection exposed selected F1-F4 authority state.");
                }

                if (selectedUserReport != legacyUserReport || boundaryUserReport != legacyUserReport)
                    throw new InvalidOperationException($"F4-B1 {name}: non-Accepted user summary did not preserve exact legacy fallback.");

                unavailable++;
                Console.WriteLine(string.Join("|",
                    "F4A_SELECTED_ENGINEERING_ASSESSMENT",
                    name,
                    $"CandidateStatus={candidate.Status}",
                    "Available=False",
                    "PresentationMigration=False",
                    "LegacyPresentationFallback=True"));
            }
            else
            {
                ValidateAccepted(name, environment, run);
                available++;
                syntheticBaseRun ??= run;
                syntheticBaseEnvironment ??= environment;

                if (boundaryUserReport != selectedUserReport)
                    throw new InvalidOperationException($"F4-B1 {name}: Accepted report boundary did not use selected user summary.");

                var assessment = snapshot.SelectedEngineeringAssessment!;
                Console.WriteLine(string.Join("|",
                    "F4A_SELECTED_ENGINEERING_ASSESSMENT",
                    name,
                    "CandidateStatus=Accepted",
                    "SelectedSource=SignedBoundaryFeedback",
                    "Available=True",
                    $"Verdict={assessment.Verdict}",
                    $"MainRiskCode={assessment.MainRiskCode}",
                    $"AnchorContact={assessment.AnchorContactClassification}",
                    $"StructuralCoverageComplete={snapshot.SelectedLocalStructuralCapacity!.StructuralCapacityCoverageComplete}",
                    $"StructuralInsufficient={snapshot.SelectedLocalStructuralCapacity.InsufficientElementCount}",
                    $"AnchorHorizontalCapacity={assessment.AnchorHorizontalCapacityDisposition}",
                    "LegacyAnchorReserveAuthority=False",
                    "PresentationMigration=True"));
            }

            if (run.Result.Verdict != legacyVerdict || run.Result.MainRisk != legacyMainRisk)
                throw new InvalidOperationException($"F4-A {name}: legacy Result Verdict/MainRisk changed.");
            if (!run.Result.Checks.SequenceEqual(legacyChecks))
                throw new InvalidOperationException($"F4-A {name}: legacy Result.Checks changed.");
            Exact(run.Result.WeakLinkBreakingLoadKn, legacyWeakLinkKn, name + " legacy weak-link MBL");
            if (run.Result.WeakLinkName != legacyWeakLinkName)
                throw new InvalidOperationException($"F4-A {name}: legacy weak-link name changed.");
            Exact(run.Result.WorkingLoadKn, legacyWorkingLoadKn, name + " legacy WLL");
            Exact(run.Result.TensionReserve, legacyTensionReserve, name + " legacy tension reserve");
            Exact(run.Result.AnchorHoldingKg, legacyAnchorHoldingKg, name + " legacy anchor holding");
            Exact(run.Result.RequiredAnchorHoldingKg, legacyRequiredAnchorHoldingKg, name + " legacy required anchor holding");
            Exact(run.Result.AnchorReserve, legacyAnchorReserve, name + " legacy anchor reserve");
            if (!run.Result.ElementRows.SequenceEqual(legacyElementRows))
                throw new InvalidOperationException($"F4-A {name}: legacy element reserve/status rows changed.");
            if (!ReferenceEquals(snapshot.ShadowSelectedCore?.Shape, selectedShapeBefore))
                throw new InvalidOperationException($"F4-A {name}: selected X/Z identity changed.");
        }

        if (definitions.Count != 5 || available != 2 || unavailable != 3)
        {
            throw new InvalidOperationException(
                $"F4-A canonical coverage mismatch: scenarios={definitions.Count}, available={available}, unavailable={unavailable}.");
        }

        if (syntheticBaseRun is null || syntheticBaseEnvironment is null)
            throw new InvalidOperationException("F4-A: Accepted synthetic policy base is unavailable.");

        ValidateAssessmentPolicy(syntheticBaseEnvironment, syntheticBaseRun);

        Console.WriteLine(
            "F4A_SELECTED_ENGINEERING_ASSESSMENT_ROLLUP|CanonicalScenarios=5|Available=2|Unavailable=3|HardPreconditions=DirectInputs|AnchorContact=F2|LocalStructuralCapacity=F3|AnchorHorizontalCapacity=RequiresAdditionalPhysicalModel|LegacyAnchorReserveAuthorizesPass=False|LegacyChecksVerdictChanged=False|PresentationMigration=AcceptedOnly|LegacyPresentationFallback=NonAccepted|SelectedGeometryChanged=False");
        Console.WriteLine("F4A_SELECTED_ENGINEERING_ASSESSMENT_END");
    }

    private static void ValidateAccepted(
        string scenario,
        EnvironmentInput environment,
        ApplicationCalculationRun run)
    {
        var snapshot = run.Snapshot;
        var selectedCore = snapshot.ShadowSelectedCore;
        var candidate = snapshot.SignedCandidate;
        var envelope = snapshot.SelectedDesignEnvelope;
        var tension = snapshot.SelectedDesignTensionDemand;
        var anchor = snapshot.SelectedAnchorReaction;
        var local = snapshot.SelectedLocalElementDemand;
        var capacity = snapshot.SelectedLocalStructuralCapacity;
        var assessment = snapshot.SelectedEngineeringAssessment;

        if (selectedCore is null || candidate is null || envelope is null || tension is null ||
            anchor is null || local is null || capacity is null || assessment is null)
        {
            throw new InvalidOperationException($"F4-A {scenario}: Accepted selected authority chain is incomplete.");
        }

        if (candidate.Status != MooringSignedCandidateStatus.Accepted ||
            selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
            envelope.SourceIdentity != selectedCore.SourceIdentity ||
            tension.SourceIdentity != selectedCore.SourceIdentity ||
            anchor.SourceIdentity != selectedCore.SourceIdentity ||
            local.SourceIdentity != selectedCore.SourceIdentity ||
            capacity.SourceIdentity != selectedCore.SourceIdentity ||
            assessment.SourceIdentity != selectedCore.SourceIdentity)
        {
            throw new InvalidOperationException($"F4-A {scenario}: selected source identity chain changed.");
        }

        if (!ReferenceEquals(selectedCore.Shape, candidate.Shape))
            throw new InvalidOperationException($"F4-A {scenario}: selected signed shape is not the Accepted candidate shape.");
        if (tension.WaveHorizontalIncrementN != envelope.WaveHorizontalIncrementN ||
            capacity.WaveHorizontalIncrementN != envelope.WaveHorizontalIncrementN ||
            local.WaveHorizontalIncrementN != envelope.WaveHorizontalIncrementN)
        {
            throw new InvalidOperationException($"F4-A {scenario}: F1/F3 wave increment identity changed.");
        }

        Exact(assessment.DesignTensionDemandN, tension.DemandN, scenario + " selected design demand N");
        Exact(assessment.DesignTensionDemandKn, tension.DemandKn, scenario + " selected design demand kN");
        Exact(assessment.AnchorHorizontalDemandN, anchor.HorizontalDemandN, scenario + " selected anchor horizontal demand");
        Exact(assessment.AnchorSignedNormalReactionN, anchor.SignedNormalReactionN, scenario + " selected anchor normal reaction");
        if (assessment.AnchorContactClassification != anchor.ContactClassification)
            throw new InvalidOperationException($"F4-A {scenario}: anchor contact classification changed.");
        if (assessment.AnchorHorizontalCapacityDisposition != MooringAnchorHorizontalCapacityDisposition.RequiresAdditionalPhysicalModel)
            throw new InvalidOperationException($"F4-A {scenario}: selected horizontal anchor capacity was fabricated.");

        if (assessment.GoverningWeakLinkElementNumber != capacity.GoverningElementNumber ||
            assessment.GoverningWeakLinkTitle != capacity.GoverningTitle ||
            assessment.GoverningWeakLinkPresetName != capacity.GoverningPresetName ||
            assessment.GoverningWeakLinkReserve != capacity.GoverningReserve)
        {
            throw new InvalidOperationException($"F4-A {scenario}: F3 governing weak-link provenance changed.");
        }

        var expectedHard = run.Result.NetBuoyancyKg <= 0.0 ||
                           (environment.DepthM > 0.0 && run.Result.LineLengthM < environment.DepthM) ||
                           run.Result.AnchorWeightWaterKg <= 0.0;
        if (assessment.HasHardFailure != expectedHard)
            throw new InvalidOperationException($"F4-A {scenario}: hard-precondition classification changed.");

        var contactReview = anchor.ContactClassification != MooringAnchorContactClassification.CompressiveContact;
        var structuralReview = !capacity.StructuralCapacityCoverageComplete || capacity.InsufficientElementCount > 0;
        var expectedReview = contactReview || structuralReview || true; // F2-C: horizontal capacity model is unavailable.
        if (!assessment.RequiresReview || !expectedReview)
            throw new InvalidOperationException($"F4-A {scenario}: selected review requirement changed.");

        var expectedVerdict = expectedHard ? "Не подходит" : "Требуется проверка";
        if (assessment.Verdict != expectedVerdict)
            throw new InvalidOperationException($"F4-A {scenario}: expected selected verdict {expectedVerdict}, got {assessment.Verdict}.");

        var checks = assessment.Checks.ToDictionary(x => x.Kind);
        RequireStatus(checks, MooringEngineeringAssessmentCheckKind.PositiveBuoyancy,
            run.Result.NetBuoyancyKg > 0.0 ? MooringEngineeringAssessmentCheckStatus.Ok : MooringEngineeringAssessmentCheckStatus.HardFailure,
            scenario);
        RequireStatus(checks, MooringEngineeringAssessmentCheckKind.LineLength,
            environment.DepthM > 0.0 && run.Result.LineLengthM < environment.DepthM
                ? MooringEngineeringAssessmentCheckStatus.HardFailure
                : MooringEngineeringAssessmentCheckStatus.Ok,
            scenario);
        RequireStatus(checks, MooringEngineeringAssessmentCheckKind.AnchorSubmergedWeight,
            run.Result.AnchorWeightWaterKg > 0.0 ? MooringEngineeringAssessmentCheckStatus.Ok : MooringEngineeringAssessmentCheckStatus.HardFailure,
            scenario);
        RequireStatus(checks, MooringEngineeringAssessmentCheckKind.AnchorContact,
            contactReview ? MooringEngineeringAssessmentCheckStatus.RequiresReview : MooringEngineeringAssessmentCheckStatus.Ok,
            scenario);
        RequireStatus(checks, MooringEngineeringAssessmentCheckKind.LocalStructuralCapacity,
            structuralReview ? MooringEngineeringAssessmentCheckStatus.RequiresReview : MooringEngineeringAssessmentCheckStatus.Ok,
            scenario);
        RequireStatus(checks, MooringEngineeringAssessmentCheckKind.AnchorHorizontalCapacity,
            MooringEngineeringAssessmentCheckStatus.RequiresReview,
            scenario);
    }

    private static void ValidateAssessmentPolicy(
        EnvironmentInput environment,
        ApplicationCalculationRun run)
    {
        var snapshot = run.Snapshot;
        var tension = snapshot.SelectedDesignTensionDemand!;
        var anchor = snapshot.SelectedAnchorReaction!;
        var capacity = snapshot.SelectedLocalStructuralCapacity!;

        var baseline = RequireAssessment(environment, run.Result, tension, anchor, capacity, "baseline");
        if (baseline.Verdict != "Требуется проверка")
            throw new InvalidOperationException("F4-A synthetic baseline: missing selected anchor-capacity model must prevent selected pass.");

        var nonPositiveBuoyancy = RequireAssessment(
            environment,
            run.Result with { NetBuoyancyKg = 0.0 },
            tension,
            anchor,
            capacity,
            "non-positive buoyancy");
        ExpectRisk(nonPositiveBuoyancy, "NonPositiveNetBuoyancy", "Не подходит", "non-positive buoyancy");

        var shortLineEnvironment = environment with
        {
            DepthM = Math.Max(environment.DepthM, run.Result.LineLengthM) + 1.0
        };
        var shortLine = RequireAssessment(
            shortLineEnvironment,
            run.Result,
            tension,
            anchor,
            capacity,
            "short line");
        ExpectRisk(shortLine, "LineShorterThanDepth", "Не подходит", "short line");

        var negativeAnchorResult = run.Result with { AnchorWeightWaterKg = -1.0 };
        var negativeAnchorState = anchor with
        {
            AnchorWeightWaterKg = -1.0,
            AnchorWeightWaterN = -9.80665
        };
        var negativeAnchor = RequireAssessment(
            environment,
            negativeAnchorResult,
            tension,
            negativeAnchorState,
            capacity,
            "non-positive anchor submerged weight");
        ExpectRisk(negativeAnchor, "NonPositiveAnchorSubmergedWeight", "Не подходит", "non-positive anchor submerged weight");

        var zeroNormalAnchor = anchor with
        {
            ContactClassification = MooringAnchorContactClassification.ZeroNormalLimit,
            SignedNormalReactionN = 0.0,
            CompressiveNormalReactionN = 0.0,
            UpliftExcessN = 0.0
        };
        var zeroNormal = RequireAssessment(environment, run.Result, tension, zeroNormalAnchor, capacity, "zero normal");
        ExpectRisk(zeroNormal, "AnchorZeroNormalLimit", "Требуется проверка", "zero normal");

        var upliftAnchor = anchor with
        {
            ContactClassification = MooringAnchorContactClassification.UpliftSeparation,
            SignedNormalReactionN = -10.0,
            CompressiveNormalReactionN = 0.0,
            UpliftExcessN = 10.0
        };
        var uplift = RequireAssessment(environment, run.Result, tension, upliftAnchor, capacity, "uplift");
        ExpectRisk(uplift, "AnchorUpliftSeparation", "Требуется проверка", "uplift");

        var compressiveAnchor = anchor with
        {
            ContactClassification = MooringAnchorContactClassification.CompressiveContact,
            SignedNormalReactionN = Math.Max(1.0, anchor.SignedNormalReactionN),
            CompressiveNormalReactionN = Math.Max(1.0, anchor.SignedNormalReactionN),
            UpliftExcessN = 0.0
        };
        var incompleteCapacity = capacity with
        {
            StructuralCapacityCoverageComplete = false,
            IncompleteStructuralElementCount = Math.Max(1, capacity.IncompleteStructuralElementCount),
            InsufficientElementCount = 0
        };
        var incomplete = RequireAssessment(environment, run.Result, tension, compressiveAnchor, incompleteCapacity, "incomplete structural capacity");
        ExpectRisk(incomplete, "LocalStructuralCapacityIncomplete", "Требуется проверка", "incomplete structural capacity");

        var insufficientCapacity = capacity with
        {
            StructuralCapacityCoverageComplete = true,
            IncompleteStructuralElementCount = 0,
            InsufficientElementCount = 1
        };
        var insufficient = RequireAssessment(environment, run.Result, tension, compressiveAnchor, insufficientCapacity, "insufficient structural capacity");
        ExpectRisk(insufficient, "LocalStructuralCapacityInsufficient", "Требуется проверка", "insufficient structural capacity");

        var completeCapacity = capacity with
        {
            StructuralCapacityCoverageComplete = true,
            IncompleteStructuralElementCount = 0,
            InsufficientElementCount = 0
        };
        var onlyAnchorModel = RequireAssessment(environment, run.Result, tension, compressiveAnchor, completeCapacity, "anchor model disposition");
        ExpectRisk(
            onlyAnchorModel,
            "AnchorHorizontalCapacityRequiresAdditionalPhysicalModel",
            "Требуется проверка",
            "anchor model disposition");

        var hardPriorityEnvironment = shortLineEnvironment;
        var hardPriorityResult = run.Result with { NetBuoyancyKg = -1.0 };
        var hardPriority = RequireAssessment(
            hardPriorityEnvironment,
            hardPriorityResult,
            tension,
            upliftAnchor,
            insufficientCapacity,
            "risk priority");
        ExpectRisk(hardPriority, "NonPositiveNetBuoyancy", "Не подходит", "risk priority");
    }

    private static MooringSelectedEngineeringAssessmentState RequireAssessment(
        EnvironmentInput environment,
        CalculationResult result,
        MooringSelectedDesignTensionDemandState tension,
        MooringSelectedAnchorReactionState anchor,
        MooringSelectedLocalStructuralCapacityState capacity,
        string label)
    {
        return MooringSelectedEngineeringAssessmentStateProjector.Project(
                   environment,
                   result,
                   tension,
                   anchor,
                   capacity)
               ?? throw new InvalidOperationException($"F4-A synthetic {label}: assessment unavailable.");
    }

    private static void ExpectRisk(
        MooringSelectedEngineeringAssessmentState assessment,
        string expectedRiskCode,
        string expectedVerdict,
        string label)
    {
        if (assessment.MainRiskCode != expectedRiskCode || assessment.Verdict != expectedVerdict)
        {
            throw new InvalidOperationException(
                $"F4-A synthetic {label}: expected {expectedVerdict}/{expectedRiskCode}, got {assessment.Verdict}/{assessment.MainRiskCode}.");
        }
    }

    private static void RequireStatus(
        IReadOnlyDictionary<MooringEngineeringAssessmentCheckKind, MooringSelectedEngineeringAssessmentCheck> checks,
        MooringEngineeringAssessmentCheckKind kind,
        MooringEngineeringAssessmentCheckStatus expected,
        string scenario)
    {
        if (!checks.TryGetValue(kind, out var check) || check.Status != expected)
        {
            throw new InvalidOperationException(
                $"F4-A {scenario}: expected {kind} status {expected}, got {check?.Status.ToString() ?? "missing"}.");
        }
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F4-A: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F4-A: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F4-A: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F4-A: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F4-A {label}: expected exact {expected:R}, got {actual:R}.");
    }
}
