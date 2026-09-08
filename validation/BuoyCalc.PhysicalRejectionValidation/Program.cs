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

internal sealed record TargetPhysicalDisposition(
    bool HasPhysicalHardFailure,
    string? Verdict,
    string? DiagnosticCode,
    string? DiagnosticText,
    bool BlocksEngineeringGeometryAuthority);

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
    bool F1Available,
    bool F2Available,
    bool F3Available,
    bool F4Available,
    string? CurrentF4Verdict,
    string? CurrentSelectedShapeSource,
    bool? CurrentSelectedShapeConverged,
    double? CurrentSelectedEndpointXM,
    double? CurrentSelectedEndpointZM,
    bool CurrentRejectedPhysicalStillExposesSelectedGeometry,
    bool TargetHasPhysicalHardFailure,
    string? TargetVerdict,
    string? TargetDiagnosticCode,
    bool TargetBlocksEngineeringGeometryAuthority);

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

        var physicalHardFailures = results.Count(x => x.TargetHasPhysicalHardFailure);
        var currentExposureDefects = results.Count(x => x.CurrentRejectedPhysicalStillExposesSelectedGeometry);
        var acceptedControls = results.Count(x => x.CandidateStatus == MooringSignedCandidateStatus.Accepted.ToString());
        var budgetControls = results.Count(x => x.CandidateStatus == MooringSignedCandidateStatus.BudgetExhausted.ToString());

        Console.WriteLine(string.Join("|",
            "VALIDATION_601_COMPLETE",
            $"Scenarios={results.Count}",
            $"TargetPhysicalHardFailures={physicalHardFailures}",
            $"CurrentRejectedPhysicalGeometryExposure={currentExposureDefects}",
            $"AcceptedControls={acceptedControls}",
            $"BudgetExhaustedControls={budgetControls}",
            "SolverChanged=False",
            "EpsilonIntroduced=False",
            "F1F2F3Fabricated=False"));

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

        var target = ProjectValidationOnlyPhysicalDisposition(candidate);
        var expectedTargetHardFailure = candidate.Status == MooringSignedCandidateStatus.RejectedPhysical;
        if (target.HasPhysicalHardFailure != expectedTargetHardFailure ||
            target.BlocksEngineeringGeometryAuthority != expectedTargetHardFailure)
        {
            throw new InvalidOperationException(
                $"{scenario.Id}: target disposition did not isolate RejectedPhysical exactly.");
        }

        if (expectedTargetHardFailure)
        {
            if (target.Verdict != "Не подходит" ||
                target.DiagnosticCode != candidate.DiagnosticCode ||
                target.DiagnosticText != candidate.DiagnosticText)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: target hard failure lost verdict or signed diagnostic provenance.");
            }

            if (snapshot.SelectedDesignTensionDemand is not null ||
                snapshot.SelectedAnchorReaction is not null ||
                snapshot.SelectedLocalStructuralCapacity is not null)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: physical rejection fabricated downstream F1/F2/F3 authority.");
            }
        }
        else if (target.Verdict is not null || target.DiagnosticCode is not null || target.DiagnosticText is not null)
        {
            throw new InvalidOperationException(
                $"{scenario.Id}: non-physical candidate received a physical hard-failure payload.");
        }

        if (scenario.ExpectAcceptedAuthorityChain)
        {
            if (candidate.Status != MooringSignedCandidateStatus.Accepted ||
                snapshot.ShadowSelectedCore?.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                snapshot.SelectedDesignTensionDemand is null ||
                snapshot.SelectedAnchorReaction is null ||
                snapshot.SelectedLocalStructuralCapacity is null ||
                snapshot.SelectedEngineeringAssessment is null)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: Accepted control does not retain the complete selected F1/F2/F3/F4 chain.");
            }
        }

        if (candidate.Status is MooringSignedCandidateStatus.Indeterminate or MooringSignedCandidateStatus.BudgetExhausted)
        {
            if (target.HasPhysicalHardFailure)
            {
                throw new InvalidOperationException(
                    $"{scenario.Id}: {candidate.Status} was incorrectly promoted to a physical hard failure.");
            }
        }

        var selected = snapshot.SelectedShape;
        var selectedAnchor = selected?.Shape.AnchorPoint;
        var currentExposure = candidate.Status == MooringSignedCandidateStatus.RejectedPhysical && selected is not null;

        Console.WriteLine(string.Join("|",
            "VALIDATION_601_SCENARIO",
            scenario.Id,
            $"Candidate={candidate.Status}",
            $"Boundary={boundary.Classification}",
            $"AnalyticalImpossible={analyticalImpossible}",
            $"CurrentSelectedSource={selected?.Source ?? "none"}",
            $"CurrentSelectedConverged={selected?.Shape.Converged.ToString() ?? "n/a"}",
            $"CurrentF4={(snapshot.SelectedEngineeringAssessment?.Verdict ?? "none")}",
            $"TargetHardFailure={target.HasPhysicalHardFailure}",
            $"TargetVerdict={target.Verdict ?? "none"}",
            $"TargetBlocksEngineeringGeometry={target.BlocksEngineeringGeometryAuthority}",
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
            snapshot.SelectedDesignTensionDemand is not null,
            snapshot.SelectedAnchorReaction is not null,
            snapshot.SelectedLocalStructuralCapacity is not null,
            snapshot.SelectedEngineeringAssessment is not null,
            snapshot.SelectedEngineeringAssessment?.Verdict,
            selected?.Source,
            selected?.Shape.Converged,
            selectedAnchor?.XOffsetM,
            selectedAnchor?.ZDepthM,
            currentExposure,
            target.HasPhysicalHardFailure,
            target.Verdict,
            target.DiagnosticCode,
            target.BlocksEngineeringGeometryAuthority);
    }

    private static TargetPhysicalDisposition ProjectValidationOnlyPhysicalDisposition(
        MooringSignedCandidateResult candidate)
    {
        if (candidate.Status != MooringSignedCandidateStatus.RejectedPhysical)
        {
            return new TargetPhysicalDisposition(
                false,
                null,
                null,
                null,
                false);
        }

        if (string.IsNullOrWhiteSpace(candidate.DiagnosticCode) ||
            string.IsNullOrWhiteSpace(candidate.DiagnosticText))
        {
            throw new InvalidOperationException(
                "RejectedPhysical candidate requires signed diagnostic provenance.");
        }

        return new TargetPhysicalDisposition(
            true,
            "Не подходит",
            candidate.DiagnosticCode,
            candidate.DiagnosticText,
            true);
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
        sb.AppendLine("# Validation #601 — physical rejection disposition evidence");
        sb.AppendLine();
        sb.AppendLine("This report is generated by a validation-only projector over production candidate truth. It does not change production solver/physics or current presentation behavior.");
        sb.AppendLine();
        sb.AppendLine("| ID | D | L | U | Candidate | Boundary | Current selected | Conv | Current F4 | Target disposition | Geometry authority | Diagnostic |");
        sb.AppendLine("|---|---:|---:|---:|---|---|---|---|---|---|---|---|");

        foreach (var result in results)
        {
            sb.AppendLine($"| {result.Id} | {F(result.DepthM)} | {F(result.LineLengthM)} | {F(result.CurrentMS)} | {result.CandidateStatus} | {result.BoundaryClassification} | {result.CurrentSelectedShapeSource ?? "none"} | {result.CurrentSelectedShapeConverged?.ToString() ?? "n/a"} | {result.CurrentF4Verdict ?? "none"} | {(result.TargetHasPhysicalHardFailure ? result.TargetVerdict : "none")} | {(result.TargetBlocksEngineeringGeometryAuthority ? "BLOCK" : "unchanged")} | `{result.CandidateDiagnosticCode}` |");
        }

        sb.AppendLine();
        sb.AppendLine("## Contract evidence");
        sb.AppendLine();
        sb.AppendLine("- Only `RejectedPhysical` receives the validation target `HardFailure / Не подходит` disposition.");
        sb.AppendLine("- Exact signed diagnostic code/text are preserved as provenance.");
        sb.AppendLine("- F1/F2/F3 are not fabricated for physical rejection.");
        sb.AppendLine("- `Indeterminate` and `BudgetExhausted` controls are not classified as physical hard failures.");
        sb.AppendLine("- The Accepted control retains SignedBoundaryFeedback and the complete existing F1/F2/F3/F4 chain.");
        sb.AppendLine("- Any current selected legacy geometry exposed for `RejectedPhysical` is recorded as the defect to be blocked by the later production PR.");
        sb.AppendLine("- No convergence epsilon is used by this package.");

        File.WriteAllText(
            Path.Combine(outputDirectory, "validation-601-report.md"),
            sb.ToString(),
            new UTF8Encoding(false));
    }

    private static string F(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);
}
