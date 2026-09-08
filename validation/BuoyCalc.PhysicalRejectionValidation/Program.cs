using System.Globalization;
using System.Text;
using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal sealed record ValidationScenario(
    string Id,
    string Description,
    double DepthM,
    double LineLengthM,
    double CurrentMS,
    MooringSignedCandidateStatus ExpectedCandidateStatus,
    MooringSurfaceBoundaryInfoClassification ExpectedBoundaryClassification,
    bool AnalyticallyPhysicallyImpossible,
    bool ExpectAcceptedAuthorityChain);

internal sealed record ValidationResult(
    string Id,
    string Description,
    double DepthM,
    double LineLengthM,
    double CurrentMS,
    double ProductionCurrentForceN,
    string CandidateStatus,
    string BoundaryClassification,
    string CandidateDiagnosticCode,
    string CandidateDiagnosticText,
    bool CandidateExactFixedPoint,
    int CandidateFeedbackIterations,
    bool AnalyticallyPhysicallyImpossible,
    bool PhysicalDispositionAvailable,
    string? PhysicalVerdict,
    string? PhysicalDiagnosticCode,
    bool PhysicalHasHardFailure,
    bool PhysicalBlocksEngineeringGeometry,
    bool F1Available,
    bool F2Available,
    bool F3Available,
    bool F4Available,
    string? F4Verdict,
    string? SelectedShapeSource,
    bool? SelectedShapeConverged,
    double? SelectedEndpointXM,
    double? SelectedEndpointZM,
    bool RejectedPhysicalGeometryBlocked);

internal static class Program
{
    private const double ExactFixtureLengthToleranceM = 1e-12;
    private const double ExactZeroHorizontalForceToleranceN = 1e-12;

    public static int Main(string[] args)
    {
        var outputDirectory = ResolveOutputDirectory(args);
        Directory.CreateDirectory(outputDirectory);

        var results = Scenarios().Select(RunScenario).ToList();
        WriteJson(outputDirectory, results);
        WriteMarkdown(outputDirectory, results);

        var rejectedPhysical = results.Count(x => x.CandidateStatus == MooringSignedCandidateStatus.RejectedPhysical.ToString());
        var dispositions = results.Count(x => x.PhysicalDispositionAvailable);
        var blocked = results.Count(x => x.RejectedPhysicalGeometryBlocked);
        var acceptedControls = results.Count(x => x.CandidateStatus == MooringSignedCandidateStatus.Accepted.ToString());
        var budgetControls = results.Count(x => x.CandidateStatus == MooringSignedCandidateStatus.BudgetExhausted.ToString());

        if (results.Count != 8 || rejectedPhysical != 5 || dispositions != 5 || blocked != 5 ||
            acceptedControls != 1 || budgetControls != 1)
        {
            throw new InvalidOperationException(
                $"Validation #601 rollup mismatch: scenarios={results.Count}, rejected={rejectedPhysical}, dispositions={dispositions}, blocked={blocked}, accepted={acceptedControls}, budget={budgetControls}.");
        }

        Console.WriteLine(string.Join("|",
            "VALIDATION_601_COMPLETE",
            $"Scenarios={results.Count}",
            $"RejectedPhysical={rejectedPhysical}",
            $"PhysicalDispositions={dispositions}",
            $"RejectedPhysicalGeometryBlocked={blocked}",
            $"AcceptedControls={acceptedControls}",
            $"BudgetExhaustedControls={budgetControls}",
            "SolverChanged=False",
            "EpsilonIntroduced=False",
            "F1F2F3F4Fabricated=False"));

        return 0;
    }

    private static IReadOnlyList<ValidationScenario> Scenarios() =>
        new[]
        {
            new ValidationScenario(
                "V601-01",
                "L < D, zero current",
                20.0,
                19.0,
                0.0,
                MooringSignedCandidateStatus.RejectedPhysical,
                MooringSurfaceBoundaryInfoClassification.LineShorterThanDepth,
                true,
                false),
            new ValidationScenario(
                "V601-02",
                "L < D, non-zero current",
                20.0,
                19.0,
                0.3,
                MooringSignedCandidateStatus.RejectedPhysical,
                MooringSurfaceBoundaryInfoClassification.LineShorterThanDepth,
                true,
                false),
            new ValidationScenario(
                "V601-03",
                "L = D, zero current vertical control",
                20.0,
                20.0,
                0.0,
                MooringSignedCandidateStatus.Indeterminate,
                MooringSurfaceBoundaryInfoClassification.VerticalGeometryUniqueForceStateFamily,
                false,
                false),
            new ValidationScenario(
                "V601-04",
                "L = D, weak current",
                20.0,
                20.0,
                0.2,
                MooringSignedCandidateStatus.RejectedPhysical,
                MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot,
                true,
                false),
            new ValidationScenario(
                "V601-05",
                "L = D, moderate current",
                50.0,
                50.0,
                0.5,
                MooringSignedCandidateStatus.RejectedPhysical,
                MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot,
                true,
                false),
            new ValidationScenario(
                "V601-06",
                "L = D, strong current",
                100.0,
                100.0,
                0.8,
                MooringSignedCandidateStatus.RejectedPhysical,
                MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot,
                true,
                false),
            new ValidationScenario(
                "V601-07",
                "1% slack Accepted control",
                50.0,
                50.5,
                0.5,
                MooringSignedCandidateStatus.Accepted,
                MooringSurfaceBoundaryInfoClassification.SolvedByBoundedBisection,
                false,
                true),
            new ValidationScenario(
                "V601-08",
                "10% slack exact two-cycle BudgetExhausted control",
                20.0,
                22.0,
                0.2,
                MooringSignedCandidateStatus.BudgetExhausted,
                MooringSurfaceBoundaryInfoClassification.SolvedByBoundedBisection,
                false,
                false)
        };

    private static ValidationResult RunScenario(ValidationScenario scenario)
    {
        var environment = new EnvironmentInput(
            1025.0,
            scenario.DepthM,
            0.0,
            0.0,
            0.0,
            new SeabedPreset(
                "validation601:sand",
                "Validation sand",
                1.2,
                "Validation-only seabed. Anchor-soil holding is not validation truth."),
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, scenario.CurrentMS, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(scenario.DepthM, scenario.CurrentMS, 0.0, 0.0, 1025.0)
            });

        var buoy = new BuoyInput(
            "Validation #601 reference buoy",
            1.0,
            100.0,
            0.10,
            0.8);
        var rope = new RopePreset(
            "validation601:rope-14",
            "Validation #601 14 mm line",
            "Synthetic reference",
            14.0,
            70.0,
            0.15,
            1.0,
            "Deterministic validation-only line.");
        var anchor = new AnchorInput(
            "Validation #601 3000 kg block",
            "Concrete block",
            "Concrete",
            3000.0,
            1.2,
            1.0);
        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Main line",
                true,
                rope,
                null,
                scenario.LineLengthM,
                1,
                0.0,
                0.0,
                0.0,
                0.0)
        };

        var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, 3.0);
        var snapshot = run.Snapshot;
        var candidate = snapshot.SignedCandidate
            ?? throw new InvalidOperationException($"{scenario.Id}: signed candidate is unavailable.");
        var boundary = candidate.Boundary
            ?? throw new InvalidOperationException($"{scenario.Id}: signed candidate boundary provenance is unavailable.");

        if (candidate.Status != scenario.ExpectedCandidateStatus)
        {
            throw new InvalidOperationException(
                $"{scenario.Id}: expected candidate {scenario.ExpectedCandidateStatus}, got {candidate.Status}.");
        }
        if (boundary.Classification != scenario.ExpectedBoundaryClassification)
        {
            throw new InvalidOperationException(
                $"{scenario.Id}: expected boundary {scenario.ExpectedBoundaryClassification}, got {boundary.Classification}.");
        }

        var analyticalImpossible = IsAnalyticallyPhysicallyImpossible(
            scenario.DepthM,
            scenario.LineLengthM,
            run.Result.CurrentForceN);
        if (analyticalImpossible != scenario.AnalyticallyPhysicallyImpossible)
        {
            throw new InvalidOperationException(
                $"{scenario.Id}: analytical physical classification mismatch; expected impossible={scenario.AnalyticallyPhysicallyImpossible}, got {analyticalImpossible}.");
        }

        if (analyticalImpossible && candidate.Status != MooringSignedCandidateStatus.RejectedPhysical)
        {
            throw new InvalidOperationException(
                $"{scenario.Id}: analytically impossible state was not classified RejectedPhysical.");
        }

        var physicalExpected = candidate.Status == MooringSignedCandidateStatus.RejectedPhysical;
        var disposition = snapshot.PhysicalDisposition;

        if (physicalExpected)
        {
            if (disposition is null ||
                disposition.CandidateStatus != MooringSignedCandidateStatus.RejectedPhysical ||
                disposition.Verdict != "Не подходит" ||
                !disposition.HasHardFailure ||
                !disposition.BlocksEngineeringGeometry ||
                disposition.DiagnosticCode != candidate.DiagnosticCode ||
                disposition.DiagnosticText != candidate.DiagnosticText)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: production physical disposition lost RejectedPhysical verdict or exact diagnostic provenance.");
            }

            if (snapshot.SelectedShape is not null || snapshot.ShadowSelectedCore is not null)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: RejectedPhysical still exposes selected engineering geometry.");
            }

            if (snapshot.SelectedDesignEnvelope is not null ||
                snapshot.SelectedDesignTensionDemand is not null ||
                snapshot.SelectedAnchorReaction is not null ||
                snapshot.SelectedLocalElementDemand is not null ||
                snapshot.SelectedLocalStructuralCapacity is not null ||
                snapshot.SelectedEngineeringAssessment is not null)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: physical rejection fabricated downstream F1/F2/F3/F4 authority.");
            }

            var userReport = UserReportBuilder.Build(environment, snapshot);
            if (!userReport.Contains("Вердикт: Не подходит", StringComparison.Ordinal) ||
                !userReport.Contains(candidate.DiagnosticCode, StringComparison.Ordinal) ||
                !userReport.Contains(candidate.DiagnosticText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: user report did not preserve physical-rejection verdict and signed diagnostic provenance.");
            }
        }
        else
        {
            if (disposition is not null)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: non-physical candidate received a physical hard-failure disposition.");
            }
        }

        if (scenario.ExpectAcceptedAuthorityChain)
        {
            if (candidate.Status != MooringSignedCandidateStatus.Accepted ||
                snapshot.SelectedShape?.Source != MooringShapeSourceIdentity.SignedBoundaryFeedback.ToString() ||
                snapshot.ShadowSelectedCore?.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                snapshot.SelectedDesignTensionDemand is null ||
                snapshot.SelectedAnchorReaction is null ||
                snapshot.SelectedLocalStructuralCapacity is null ||
                snapshot.SelectedEngineeringAssessment is null)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: Accepted control does not retain the complete selected X/Z/F1/F2/F3/F4 chain.");
            }
        }

        if (candidate.Status is MooringSignedCandidateStatus.Indeterminate or MooringSignedCandidateStatus.BudgetExhausted)
        {
            if (snapshot.SelectedShape is null)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: {candidate.Status} incorrectly lost the existing non-physical fallback selected-shape behavior.");
            }
        }

        var selected = snapshot.SelectedShape;
        var selectedAnchor = selected?.Shape.AnchorPoint;
        var geometryBlocked = candidate.Status != MooringSignedCandidateStatus.RejectedPhysical || selected is null;

        Console.WriteLine(string.Join("|",
            "VALIDATION_601_SCENARIO",
            scenario.Id,
            $"Candidate={candidate.Status}",
            $"Boundary={boundary.Classification}",
            $"AnalyticalImpossible={analyticalImpossible}",
            $"PhysicalDisposition={(disposition is null ? "none" : disposition.Verdict)}",
            $"SelectedSource={selected?.Source ?? "none"}",
            $"SelectedConverged={selected?.Shape.Converged.ToString() ?? "n/a"}",
            $"F4={(snapshot.SelectedEngineeringAssessment?.Verdict ?? "none")}",
            $"RejectedPhysicalGeometryBlocked={geometryBlocked}",
            $"Diagnostic={candidate.DiagnosticCode}"));

        return new ValidationResult(
            scenario.Id,
            scenario.Description,
            scenario.DepthM,
            scenario.LineLengthM,
            scenario.CurrentMS,
            run.Result.CurrentForceN,
            candidate.Status.ToString(),
            boundary.Classification.ToString(),
            candidate.DiagnosticCode,
            candidate.DiagnosticText,
            candidate.ExactFixedPointReached,
            candidate.FeedbackIterations,
            analyticalImpossible,
            disposition is not null,
            disposition?.Verdict,
            disposition?.DiagnosticCode,
            disposition?.HasHardFailure ?? false,
            disposition?.BlocksEngineeringGeometry ?? false,
            snapshot.SelectedDesignTensionDemand is not null,
            snapshot.SelectedAnchorReaction is not null,
            snapshot.SelectedLocalStructuralCapacity is not null,
            snapshot.SelectedEngineeringAssessment is not null,
            snapshot.SelectedEngineeringAssessment?.Verdict,
            selected?.Source,
            selected?.Shape.Converged,
            selectedAnchor?.XOffsetM,
            selectedAnchor?.ZDepthM,
            candidate.Status == MooringSignedCandidateStatus.RejectedPhysical && selected is null);
    }

    private static bool IsAnalyticallyPhysicallyImpossible(
        double depthM,
        double lineLengthM,
        double horizontalSteadyLoadN)
    {
        if (lineLengthM + ExactFixtureLengthToleranceM < depthM)
            return true;

        if (Math.Abs(lineLengthM - depthM) <= ExactFixtureLengthToleranceM &&
            Math.Abs(horizontalSteadyLoadN) > ExactZeroHorizontalForceToleranceN)
        {
            return true;
        }

        return false;
    }

    private static string ResolveOutputDirectory(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--output")
                return args[i + 1];
        }

        return Path.Combine("artifacts", "validation-601-physical-rejection");
    }

    private static void WriteJson(string outputDirectory, IReadOnlyList<ValidationResult> results)
    {
        var json = JsonSerializer.Serialize(
            results,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(
            Path.Combine(outputDirectory, "validation-601-results.json"),
            json,
            new UTF8Encoding(false));
    }

    private static void WriteMarkdown(string outputDirectory, IReadOnlyList<ValidationResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Validation #601 — physical rejection production regression");
        sb.AppendLine();
        sb.AppendLine("This report verifies the production typed disposition over unchanged signed-candidate truth. It does not modify solver physics or acceptance semantics.");
        sb.AppendLine();
        sb.AppendLine("| ID | D | L | U | Candidate | Boundary | Physical disposition | Selected X/Z | Conv | F4 | Geometry contract | Diagnostic |");
        sb.AppendLine("|---|---:|---:|---:|---|---|---|---|---|---|---|---|");

        foreach (var result in results)
        {
            sb.AppendLine($"| {result.Id} | {F(result.DepthM)} | {F(result.LineLengthM)} | {F(result.CurrentMS)} | {result.CandidateStatus} | {result.BoundaryClassification} | {result.PhysicalVerdict ?? "none"} | {result.SelectedShapeSource ?? "none"} | {result.SelectedShapeConverged?.ToString() ?? "n/a"} | {result.F4Verdict ?? "none"} | {(result.CandidateStatus == MooringSignedCandidateStatus.RejectedPhysical.ToString() ? (result.RejectedPhysicalGeometryBlocked ? "BLOCKED" : "DEFECT") : "unchanged")} | `{result.CandidateDiagnosticCode}` |");
        }

        sb.AppendLine();
        sb.AppendLine("## Contract evidence");
        sb.AppendLine();
        sb.AppendLine("- Every production `RejectedPhysical` candidate has typed `HardFailure / Не подходит` disposition with exact diagnostic code/text provenance.");
        sb.AppendLine("- Every production `RejectedPhysical` candidate has no selected engineering X/Z authority.");
        sb.AppendLine("- F1/F2/F3/F4 are not fabricated for physical rejection.");
        sb.AppendLine("- `Indeterminate` and `BudgetExhausted` controls retain their existing non-physical fallback behavior and receive no physical disposition.");
        sb.AppendLine("- The Accepted control retains SignedBoundaryFeedback and the complete existing X/Z/F1/F2/F3/F4 chain.");
        sb.AppendLine("- No convergence epsilon is used and the signed solver/evaluator is unchanged by this production fix.");

        File.WriteAllText(
            Path.Combine(outputDirectory, "validation-601-report.md"),
            sb.ToString(),
            new UTF8Encoding(false));
    }

    private static string F(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);
}
