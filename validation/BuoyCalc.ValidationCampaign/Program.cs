using System.Globalization;
using System.Text;
using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal enum ExpectedGeometry
{
    ImpossibleLineShorterThanDepth,
    VerticalTautZeroHorizontal,
    ImpossibleTautWithHorizontalLoad,
    GeometricallyAdmissibleSlack
}

internal sealed record CampaignScenario(
    string Id,
    string Description,
    double DepthM,
    double LineLengthM,
    IReadOnlyList<CurrentProfilePointInput> CurrentProfile,
    ExpectedGeometry ExpectedGeometry,
    bool SplitLineWithPayload = false,
    double PayloadWeightAirKg = 0.0,
    double PayloadVolumeM3 = 0.0,
    double PayloadProjectedAreaM2 = 0.0,
    double PayloadDragCoefficient = 1.0);

internal sealed record CampaignFinding(
    string Severity,
    string Category,
    string Code,
    string Detail);

internal sealed record CampaignResult(
    string Id,
    string Description,
    string ExpectedGeometry,
    double DepthM,
    double LineLengthM,
    double LineToDepthRatio,
    double MaxProfileHorizontalSpeedMS,
    double CurrentForceN,
    double WaveForceN,
    double? FallbackEndpointXM,
    double? FallbackEndpointZM,
    bool? FallbackConverged,
    double? IterativeEndpointXM,
    double? IterativeEndpointZM,
    bool? IterativeConverged,
    string? IterativeStopReason,
    string? SelectedShapeSource,
    double? SelectedEndpointXM,
    double? SelectedEndpointZM,
    bool? SelectedShapeConverged,
    double? SelectedEndpointChordM,
    bool? SelectedEndpointChordWithinLine,
    string SignedStatus,
    string? BoundaryClassification,
    string SignedDiagnosticCode,
    string SignedDiagnosticText,
    int SignedFeedbackIterations,
    bool SignedExactFixedPointReached,
    bool F1Available,
    double? F1DemandN,
    bool F2Available,
    double? F2HorizontalDemandN,
    double? F2SignedNormalReactionN,
    bool F3Available,
    bool? F3CoverageComplete,
    double? F3GoverningReserve,
    bool F4Available,
    string? F4Verdict,
    double MaxProductionSegmentLengthM,
    double ProductionSegmentLengthSumM,
    string? ExceptionType,
    string? ExceptionMessage,
    IReadOnlyList<CampaignFinding> Findings);

internal static class Program
{
    private const double GeometryToleranceM = 1e-6;
    private const double ForceToleranceN = 1e-9;
    private const double SegmentToleranceM = 1e-12;
    private const double ProductionSegmentLengthM = 0.20;

    public static int Main(string[] args)
    {
        var outputDirectory = ResolveOutputDirectory(args);
        Directory.CreateDirectory(outputDirectory);

        var scenarios = BuildScenarios();
        var results = scenarios.Select(RunScenario).ToList();

        WriteJson(outputDirectory, results);
        WriteCsv(outputDirectory, results);
        WriteMarkdown(outputDirectory, results);

        var findingCount = results.Sum(x => x.Findings.Count);
        var criticalCount = results.Sum(x => x.Findings.Count(f => f.Severity == "CRITICAL"));
        var highCount = results.Sum(x => x.Findings.Count(f => f.Severity == "HIGH"));

        Console.WriteLine($"SERIES_A_COMPLETE scenarios={results.Count} findings={findingCount} critical={criticalCount} high={highCount}");
        Console.WriteLine($"SERIES_A_OUTPUT {Path.GetFullPath(outputDirectory)}");

        // Evidence-gathering campaign: discrepancies are data, not a CI-green target.
        // The runner fails only when it cannot execute/write evidence at all.
        return 0;
    }

    private static string ResolveOutputDirectory(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--output", StringComparison.Ordinal))
                return args[i + 1];
        }

        return Path.Combine("artifacts", "validation-series-a");
    }

    private static IReadOnlyList<CampaignScenario> BuildScenarios()
    {
        return new[]
        {
            Scenario("A01", "20 m: line shorter than depth, zero current", 20.0, 19.0, ConstantProfile(20.0, 0.0), ExpectedGeometry.ImpossibleLineShorterThanDepth),
            Scenario("A02", "20 m: exactly taut vertical, zero current", 20.0, 20.0, ConstantProfile(20.0, 0.0), ExpectedGeometry.VerticalTautZeroHorizontal),
            Scenario("A03", "20 m: exactly taut with weak current", 20.0, 20.0, ConstantProfile(20.0, 0.2), ExpectedGeometry.ImpossibleTautWithHorizontalLoad),
            Scenario("A04", "20 m: exactly taut with strong current", 20.0, 20.0, ConstantProfile(20.0, 0.6), ExpectedGeometry.ImpossibleTautWithHorizontalLoad),
            Scenario("A05", "20 m: 1% slack with weak current", 20.0, 20.2, ConstantProfile(20.0, 0.2), ExpectedGeometry.GeometricallyAdmissibleSlack),
            Scenario("A06", "20 m: 10% slack with weak current", 20.0, 22.0, ConstantProfile(20.0, 0.2), ExpectedGeometry.GeometricallyAdmissibleSlack),

            Scenario("A07", "50 m: line shorter than depth under current", 50.0, 49.0, ConstantProfile(50.0, 0.3), ExpectedGeometry.ImpossibleLineShorterThanDepth),
            Scenario("A08", "50 m: exactly taut vertical, zero current", 50.0, 50.0, ConstantProfile(50.0, 0.0), ExpectedGeometry.VerticalTautZeroHorizontal),
            Scenario("A09", "50 m: exactly taut with moderate current", 50.0, 50.0, ConstantProfile(50.0, 0.5), ExpectedGeometry.ImpossibleTautWithHorizontalLoad),
            Scenario("A10", "50 m: 1% slack with moderate current", 50.0, 50.5, ConstantProfile(50.0, 0.5), ExpectedGeometry.GeometricallyAdmissibleSlack),
            Scenario("A11", "50 m: 10% slack with moderate current", 50.0, 55.0, ConstantProfile(50.0, 0.5), ExpectedGeometry.GeometricallyAdmissibleSlack),

            Scenario("A12", "100 m: line shorter than depth, zero current", 100.0, 99.0, ConstantProfile(100.0, 0.0), ExpectedGeometry.ImpossibleLineShorterThanDepth),
            Scenario("A13", "100 m: exactly taut with moderate current", 100.0, 100.0, ConstantProfile(100.0, 0.3), ExpectedGeometry.ImpossibleTautWithHorizontalLoad),
            Scenario("A14", "100 m: 1% slack with moderate current", 100.0, 101.0, ConstantProfile(100.0, 0.3), ExpectedGeometry.GeometricallyAdmissibleSlack),
            Scenario("A15", "100 m: 10% slack with moderate current and one internal payload", 100.0, 110.0, ConstantProfile(100.0, 0.3), ExpectedGeometry.GeometricallyAdmissibleSlack, true, 20.0, 0.002, 0.05, 1.0),

            Scenario("A16", "350 m: manual-smoke class, line length equals depth under current", 350.0, 350.0, ConstantProfile(350.0, 0.45), ExpectedGeometry.ImpossibleTautWithHorizontalLoad),
            Scenario("A17", "350 m: 1% slack under current", 350.0, 353.5, ConstantProfile(350.0, 0.45), ExpectedGeometry.GeometricallyAdmissibleSlack),
            Scenario("A18", "350 m: 10% slack with current decreasing with depth", 350.0, 385.0, ShearedProfile(350.0, 0.60, 0.30, 0.10), ExpectedGeometry.GeometricallyAdmissibleSlack),

            Scenario("A19", "500 m: exactly taut with strong current", 500.0, 500.0, ConstantProfile(500.0, 0.60), ExpectedGeometry.ImpossibleTautWithHorizontalLoad),
            Scenario("A20", "500 m: 10% slack, veering profile and one internal payload", 500.0, 550.0, VeeringProfile(500.0), ExpectedGeometry.GeometricallyAdmissibleSlack, true, 35.0, 0.003, 0.08, 1.1)
        };
    }

    private static CampaignScenario Scenario(
        string id,
        string description,
        double depthM,
        double lineLengthM,
        IReadOnlyList<CurrentProfilePointInput> profile,
        ExpectedGeometry expectedGeometry,
        bool splitLineWithPayload = false,
        double payloadWeightAirKg = 0.0,
        double payloadVolumeM3 = 0.0,
        double payloadProjectedAreaM2 = 0.0,
        double payloadDragCoefficient = 1.0) =>
        new(
            id,
            description,
            depthM,
            lineLengthM,
            profile,
            expectedGeometry,
            splitLineWithPayload,
            payloadWeightAirKg,
            payloadVolumeM3,
            payloadProjectedAreaM2,
            payloadDragCoefficient);

    private static IReadOnlyList<CurrentProfilePointInput> ConstantProfile(double depthM, double speedMS) =>
        new[]
        {
            new CurrentProfilePointInput(0.0, speedMS, 0.0, 0.0, 1025.0),
            new CurrentProfilePointInput(depthM, speedMS, 0.0, 0.0, 1025.0)
        };

    private static IReadOnlyList<CurrentProfilePointInput> ShearedProfile(
        double depthM,
        double surfaceMS,
        double midMS,
        double bottomMS) =>
        new[]
        {
            new CurrentProfilePointInput(0.0, surfaceMS, 0.0, 0.0, 1025.0),
            new CurrentProfilePointInput(depthM / 2.0, midMS, 0.0, 0.0, 1025.0),
            new CurrentProfilePointInput(depthM, bottomMS, 0.0, 0.0, 1025.0)
        };

    private static IReadOnlyList<CurrentProfilePointInput> VeeringProfile(double depthM) =>
        new[]
        {
            new CurrentProfilePointInput(0.0, 0.50, 0.00, 0.0, 1025.0),
            new CurrentProfilePointInput(depthM / 2.0, 0.20, 0.20, 0.0, 1025.0),
            new CurrentProfilePointInput(depthM, 0.00, 0.10, 0.0, 1025.0)
        };

    private static CampaignResult RunScenario(CampaignScenario scenario)
    {
        try
        {
            var environment = new EnvironmentInput(
                1025.0,
                scenario.DepthM,
                0.0,
                0.0,
                0.0,
                new SeabedPreset("campaign:sand", "Validation sand", 1.2, "Series A deterministic placeholder; anchor holding is not validation truth."),
                true,
                scenario.CurrentProfile);

            var buoy = new BuoyInput(
                "Series A reference buoy",
                1.0,
                100.0,
                0.10,
                0.8);

            var rope = new RopePreset(
                "campaign:rope-14",
                "Series A 14 mm line",
                "Synthetic reference",
                14.0,
                70.0,
                0.15,
                1.0,
                "Deterministic campaign line; not a product recommendation.");

            var anchor = new AnchorInput(
                "Series A 3000 kg block",
                "Concrete block",
                "Concrete",
                3000.0,
                1.2,
                1.0);

            var assembly = BuildAssembly(scenario, rope);
            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, 3.0);
            var result = run.Result;
            var snapshot = run.Snapshot;
            var data = snapshot.TechnicalReportData;
            var candidate = snapshot.SignedCandidate!;

            var fallback = data.Shape;
            var iterative = data.IterativeSolver.FinalShape;
            var selectedReadModel = snapshot.SelectedShape;
            var selected = selectedReadModel?.Shape;
            var selectedAnchor = selected?.AnchorPoint;
            var selectedChordM = selectedAnchor is null
                ? (double?)null
                : Math.Sqrt(selectedAnchor.XOffsetM * selectedAnchor.XOffsetM + selectedAnchor.ZDepthM * selectedAnchor.ZDepthM);
            var selectedChordWithinLine = selectedChordM.HasValue
                ? selectedChordM.Value <= scenario.LineLengthM + GeometryToleranceM
                : (bool?)null;

            var maxSegmentLengthM = result.SegmentRows.Count == 0
                ? 0.0
                : result.SegmentRows.Max(x => x.SegmentLengthM);
            var segmentSumM = result.SegmentRows.Sum(x => x.SegmentLengthM);

            var findings = EvaluateFindings(
                scenario,
                result,
                snapshot,
                fallback,
                iterative,
                selectedReadModel,
                selectedChordM,
                selectedChordWithinLine,
                maxSegmentLengthM,
                segmentSumM);

            return new CampaignResult(
                scenario.Id,
                scenario.Description,
                scenario.ExpectedGeometry.ToString(),
                scenario.DepthM,
                scenario.LineLengthM,
                scenario.DepthM > 0.0 ? scenario.LineLengthM / scenario.DepthM : double.NaN,
                scenario.CurrentProfile.Max(x => x.HorizontalSpeedMS),
                result.CurrentForceN,
                result.WaveForceN,
                fallback.AnchorPoint?.XOffsetM,
                fallback.AnchorPoint?.ZDepthM,
                fallback.Converged,
                iterative?.AnchorPoint?.XOffsetM,
                iterative?.AnchorPoint?.ZDepthM,
                iterative?.Converged,
                data.IterativeSolver.StopReason.ToString(),
                selectedReadModel?.Source,
                selectedAnchor?.XOffsetM,
                selectedAnchor?.ZDepthM,
                selected?.Converged,
                selectedChordM,
                selectedChordWithinLine,
                candidate.Status.ToString(),
                candidate.Boundary?.Classification.ToString(),
                candidate.DiagnosticCode,
                candidate.DiagnosticText,
                candidate.FeedbackIterations,
                candidate.ExactFixedPointReached,
                snapshot.SelectedDesignTensionDemand is not null,
                snapshot.SelectedDesignTensionDemand?.DemandN,
                snapshot.SelectedAnchorReaction is not null,
                snapshot.SelectedAnchorReaction?.HorizontalDemandN,
                snapshot.SelectedAnchorReaction?.SignedNormalReactionN,
                snapshot.SelectedLocalStructuralCapacity is not null,
                snapshot.SelectedLocalStructuralCapacity?.StructuralCapacityCoverageComplete,
                snapshot.SelectedLocalStructuralCapacity?.GoverningReserve,
                snapshot.SelectedEngineeringAssessment is not null,
                snapshot.SelectedEngineeringAssessment?.Verdict,
                maxSegmentLengthM,
                segmentSumM,
                null,
                null,
                findings);
        }
        catch (Exception ex)
        {
            return new CampaignResult(
                scenario.Id,
                scenario.Description,
                scenario.ExpectedGeometry.ToString(),
                scenario.DepthM,
                scenario.LineLengthM,
                scenario.DepthM > 0.0 ? scenario.LineLengthM / scenario.DepthM : double.NaN,
                scenario.CurrentProfile.Max(x => x.HorizontalSpeedMS),
                double.NaN,
                double.NaN,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "EXCEPTION",
                null,
                "CampaignExecutionException",
                ex.Message,
                0,
                false,
                false,
                null,
                false,
                null,
                null,
                false,
                null,
                null,
                false,
                null,
                0.0,
                0.0,
                ex.GetType().FullName,
                ex.Message,
                new[]
                {
                    new CampaignFinding("CRITICAL", "Execution", "ScenarioException", $"Production pipeline threw {ex.GetType().Name}: {ex.Message}")
                });
        }
    }

    private static IReadOnlyList<AssemblyItemInput> BuildAssembly(CampaignScenario scenario, RopePreset rope)
    {
        if (!scenario.SplitLineWithPayload)
        {
            return new[]
            {
                LineItem("Main line", scenario.LineLengthM, rope)
            };
        }

        var firstLength = scenario.LineLengthM / 2.0;
        var secondLength = scenario.LineLengthM - firstLength;
        return new[]
        {
            LineItem("Upper line", firstLength, rope),
            new AssemblyItemInput(
                AssemblyItemKind.Payload,
                "Internal instrument",
                true,
                null,
                null,
                0.0,
                1,
                scenario.PayloadWeightAirKg,
                scenario.PayloadVolumeM3,
                scenario.PayloadProjectedAreaM2,
                scenario.PayloadDragCoefficient),
            LineItem("Lower line", secondLength, rope)
        };
    }

    private static AssemblyItemInput LineItem(string title, double lengthM, RopePreset rope) =>
        new(
            AssemblyItemKind.Line,
            title,
            true,
            rope,
            null,
            lengthM,
            1,
            0.0,
            0.0,
            0.0,
            0.0);

    private static IReadOnlyList<CampaignFinding> EvaluateFindings(
        CampaignScenario scenario,
        CalculationResult result,
        CalculationSnapshot snapshot,
        MooringShapeResult fallback,
        MooringShapeResult? iterative,
        SelectedShapeReadModel? selectedReadModel,
        double? selectedChordM,
        bool? selectedChordWithinLine,
        double maxSegmentLengthM,
        double segmentSumM)
    {
        var findings = new List<CampaignFinding>();
        var candidate = snapshot.SignedCandidate!;
        var profileHasHorizontalFlow = scenario.CurrentProfile.Any(x => x.HorizontalSpeedMS > 0.0);
        var selected = selectedReadModel?.Shape;

        if (maxSegmentLengthM > ProductionSegmentLengthM + SegmentToleranceM)
        {
            findings.Add(new CampaignFinding(
                "CRITICAL",
                "Segmentation",
                "ProductionSegmentExceeds020m",
                $"max segment={maxSegmentLengthM:R} m > {ProductionSegmentLengthM:R} m."));
        }

        if (Math.Abs(segmentSumM - result.LineLengthM) > GeometryToleranceM)
        {
            findings.Add(new CampaignFinding(
                "CRITICAL",
                "Segmentation",
                "SegmentLengthClosureMismatch",
                $"segment sum={segmentSumM:R} m, CalculationResult.LineLength={result.LineLengthM:R} m."));
        }

        if (!profileHasHorizontalFlow && Math.Abs(result.CurrentForceN) > ForceToleranceN)
        {
            findings.Add(new CampaignFinding(
                "CRITICAL",
                "Physics",
                "ZeroCurrentProducedDrag",
                $"explicit zero-current profile produced CurrentForceN={result.CurrentForceN:R}."));
        }

        CheckShapeChord(findings, "Fallback", fallback, scenario.LineLengthM);
        if (iterative is not null)
            CheckShapeChord(findings, "Iterative", iterative, scenario.LineLengthM);
        if (selected is not null)
            CheckShapeChord(findings, "Selected", selected, scenario.LineLengthM);

        switch (scenario.ExpectedGeometry)
        {
            case ExpectedGeometry.ImpossibleLineShorterThanDepth:
                if (candidate.Status != MooringSignedCandidateStatus.RejectedPhysical)
                {
                    findings.Add(new CampaignFinding(
                        "HIGH",
                        "Authority",
                        "LineShortNotRejectedPhysical",
                        $"independent geometry says L<D; signed status={candidate.Status}, boundary={candidate.Boundary?.Classification}."));
                }

                if (selected?.Converged == true)
                {
                    findings.Add(new CampaignFinding(
                        "CRITICAL",
                        "Presentation",
                        "ImpossibleGeometryExposedAsConverged",
                        $"L={scenario.LineLengthM:R} m < D={scenario.DepthM:R} m but selected read-model shape is Converged from {selectedReadModel?.Source}."));
                }
                break;

            case ExpectedGeometry.VerticalTautZeroHorizontal:
                if (selected?.AnchorPoint is { } tautAnchor && Math.Abs(tautAnchor.XOffsetM) > GeometryToleranceM)
                {
                    findings.Add(new CampaignFinding(
                        "CRITICAL",
                        "Geometry",
                        "ZeroLoadTautHasHorizontalOffset",
                        $"L=D with zero horizontal flow but selected X={tautAnchor.XOffsetM:R} m."));
                }
                break;

            case ExpectedGeometry.ImpossibleTautWithHorizontalLoad:
                if (candidate.Status != MooringSignedCandidateStatus.RejectedPhysical ||
                    candidate.Boundary?.Classification != MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot)
                {
                    findings.Add(new CampaignFinding(
                        "CRITICAL",
                        "Authority",
                        "TautHorizontalNotRejectedByExpectedGate",
                        $"expected RejectedPhysical/TautNonZeroHorizontalLoadNoFiniteRoot; got {candidate.Status}/{candidate.Boundary?.Classification}."));
                }

                if (selected?.Converged == true)
                {
                    findings.Add(new CampaignFinding(
                        "CRITICAL",
                        "Presentation",
                        "TautHorizontalImpossibleButSelectedConverged",
                        $"L=D with horizontal environmental load but selected read-model shape is Converged from {selectedReadModel?.Source}, X={selected.AnchorPoint?.XOffsetM:R} m."));
                }
                break;

            case ExpectedGeometry.GeometricallyAdmissibleSlack:
                if (selectedChordWithinLine == false)
                {
                    findings.Add(new CampaignFinding(
                        "CRITICAL",
                        "Geometry",
                        "SlackSelectedChordExceedsLine",
                        $"selected chord={selectedChordM:R} m > line={scenario.LineLengthM:R} m."));
                }
                break;
        }

        if (candidate.Status == MooringSignedCandidateStatus.Accepted)
        {
            if (!candidate.ExactFixedPointReached)
            {
                findings.Add(new CampaignFinding(
                    "CRITICAL",
                    "Authority",
                    "AcceptedWithoutExactFixedPoint",
                    "Accepted signed candidate does not carry exact deterministic fixed-point identity."));
            }

            if (!string.Equals(selectedReadModel?.Source, MooringShapeSourceIdentity.SignedBoundaryFeedback.ToString(), StringComparison.Ordinal))
            {
                findings.Add(new CampaignFinding(
                    "CRITICAL",
                    "Authority",
                    "AcceptedSignedCandidateNotSelected",
                    $"signed candidate Accepted but selected source={selectedReadModel?.Source ?? "null"}."));
            }

            if (snapshot.SelectedDesignTensionDemand is null ||
                snapshot.SelectedAnchorReaction is null ||
                snapshot.SelectedLocalStructuralCapacity is null ||
                snapshot.SelectedEngineeringAssessment is null)
            {
                findings.Add(new CampaignFinding(
                    "HIGH",
                    "Authority",
                    "AcceptedSignedCandidateMissingF1F4",
                    $"Accepted signed candidate has F1={snapshot.SelectedDesignTensionDemand is not null}, F2={snapshot.SelectedAnchorReaction is not null}, F3={snapshot.SelectedLocalStructuralCapacity is not null}, F4={snapshot.SelectedEngineeringAssessment is not null}."));
            }
        }

        if (candidate.Status == MooringSignedCandidateStatus.RejectedPhysical &&
            snapshot.SelectedEngineeringAssessment is null)
        {
            findings.Add(new CampaignFinding(
                "HIGH",
                "Presentation",
                "PhysicalRejectionNotPromotedToF4HardFailure",
                $"signed physical rejection {candidate.DiagnosticCode} leaves F4 undefined instead of a user-facing hard-failure assessment."));
        }

        return findings;
    }

    private static void CheckShapeChord(
        ICollection<CampaignFinding> findings,
        string source,
        MooringShapeResult shape,
        double lineLengthM)
    {
        if (shape.AnchorPoint is not { } anchor)
            return;

        var chordM = Math.Sqrt(anchor.XOffsetM * anchor.XOffsetM + anchor.ZDepthM * anchor.ZDepthM);
        if (chordM > lineLengthM + GeometryToleranceM)
        {
            findings.Add(new CampaignFinding(
                "CRITICAL",
                "Geometry",
                source + "ChordExceedsLineLength",
                $"{source} endpoint chord={chordM:R} m exceeds line length={lineLengthM:R} m; X={anchor.XOffsetM:R}, Z={anchor.ZDepthM:R}, Converged={shape.Converged}."));
        }
    }

    private static void WriteJson(string outputDirectory, IReadOnlyList<CampaignResult> results)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(
            Path.Combine(outputDirectory, "series-a-results.json"),
            JsonSerializer.Serialize(results, options),
            Encoding.UTF8);
    }

    private static void WriteCsv(string outputDirectory, IReadOnlyList<CampaignResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Expected,DepthM,LineLengthM,LDivD,MaxCurrentMS,CurrentForceN,FallbackX,FallbackConverged,IterativeX,IterativeConverged,SelectedSource,SelectedX,SelectedZ,SelectedChordM,ChordWithinLine,SignedStatus,BoundaryClass,DiagnosticCode,FeedbackIterations,ExactFixedPoint,F1Available,F1DemandN,F2Available,F2HorizontalDemandN,F2NormalN,F3Available,F3CoverageComplete,F3Reserve,F4Available,F4Verdict,MaxSegmentM,SegmentSumM,CriticalCount,HighCount,FindingCodes");

        foreach (var r in results)
        {
            var codes = string.Join(";", r.Findings.Select(x => x.Code));
            var values = new[]
            {
                r.Id,
                r.ExpectedGeometry,
                F(r.DepthM),
                F(r.LineLengthM),
                F(r.LineToDepthRatio),
                F(r.MaxProfileHorizontalSpeedMS),
                F(r.CurrentForceN),
                F(r.FallbackEndpointXM),
                B(r.FallbackConverged),
                F(r.IterativeEndpointXM),
                B(r.IterativeConverged),
                r.SelectedShapeSource ?? string.Empty,
                F(r.SelectedEndpointXM),
                F(r.SelectedEndpointZM),
                F(r.SelectedEndpointChordM),
                B(r.SelectedEndpointChordWithinLine),
                r.SignedStatus,
                r.BoundaryClassification ?? string.Empty,
                r.SignedDiagnosticCode,
                r.SignedFeedbackIterations.ToString(CultureInfo.InvariantCulture),
                r.SignedExactFixedPointReached.ToString(CultureInfo.InvariantCulture),
                r.F1Available.ToString(CultureInfo.InvariantCulture),
                F(r.F1DemandN),
                r.F2Available.ToString(CultureInfo.InvariantCulture),
                F(r.F2HorizontalDemandN),
                F(r.F2SignedNormalReactionN),
                r.F3Available.ToString(CultureInfo.InvariantCulture),
                B(r.F3CoverageComplete),
                F(r.F3GoverningReserve),
                r.F4Available.ToString(CultureInfo.InvariantCulture),
                r.F4Verdict ?? string.Empty,
                F(r.MaxProductionSegmentLengthM),
                F(r.ProductionSegmentLengthSumM),
                r.Findings.Count(x => x.Severity == "CRITICAL").ToString(CultureInfo.InvariantCulture),
                r.Findings.Count(x => x.Severity == "HIGH").ToString(CultureInfo.InvariantCulture),
                codes
            };
            sb.AppendLine(string.Join(",", values.Select(Csv)));
        }

        File.WriteAllText(Path.Combine(outputDirectory, "series-a-results.csv"), sb.ToString(), Encoding.UTF8);
    }

    private static void WriteMarkdown(string outputDirectory, IReadOnlyList<CampaignResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# BuoyCalc pre-v1 validation campaign — Series A");
        sb.AppendLine();
        sb.AppendLine("Geometry/equilibrium evidence generated by the production BuoyCalc application/core pipeline. No solver or physics changes are made by this campaign.");
        sb.AppendLine();
        sb.AppendLine("| ID | D, m | L, m | L/D | Umax, m/s | Selected X, m | Signed | Boundary | F1 | F2 | F3 | F4 | Crit | High |");
        sb.AppendLine("|---|---:|---:|---:|---:|---:|---|---|---|---|---|---|---:|---:|");
        foreach (var r in results)
        {
            sb.AppendLine($"| {r.Id} | {Md(r.DepthM)} | {Md(r.LineLengthM)} | {Md(r.LineToDepthRatio)} | {Md(r.MaxProfileHorizontalSpeedMS)} | {Md(r.SelectedEndpointXM)} | {r.SignedStatus} | {r.BoundaryClassification ?? "—"} | {(r.F1Available ? "yes" : "no")} | {(r.F2Available ? "yes" : "no")} | {(r.F3Available ? "yes" : "no")} | {(r.F4Available ? "yes" : "no")} | {r.Findings.Count(x => x.Severity == "CRITICAL")} | {r.Findings.Count(x => x.Severity == "HIGH")} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Discrepancy register");
        sb.AppendLine();
        foreach (var r in results.Where(x => x.Findings.Count > 0))
        {
            sb.AppendLine($"### {r.Id} — {r.Description}");
            sb.AppendLine();
            foreach (var finding in r.Findings)
                sb.AppendLine($"- **{finding.Severity} / {finding.Category} / {finding.Code}** — {finding.Detail}");
            sb.AppendLine();
        }

        sb.AppendLine("## Interpretation guard");
        sb.AppendLine();
        sb.AppendLine("A slack case with no campaign finding is not automatically 'validated'. Series A proves/checks only the listed independent geometry/statics invariants and authority consistency. Numerical agreement for more complex equilibrium requires later independent reference validation.");

        File.WriteAllText(Path.Combine(outputDirectory, "series-a-report.md"), sb.ToString(), Encoding.UTF8);
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string F(double? value) => value.HasValue ? F(value.Value) : string.Empty;
    private static string B(bool? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    private static string Md(double value) => double.IsFinite(value) ? value.ToString("0.####", CultureInfo.InvariantCulture) : "—";
    private static string Md(double? value) => value.HasValue ? Md(value.Value) : "—";
    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
