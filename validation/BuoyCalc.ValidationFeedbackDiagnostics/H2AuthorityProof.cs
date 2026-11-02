using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Services;

// Validation-only: all acceptance and F1-F4 decisions use the production implementation.
internal static class H2AuthorityProof
{
    internal static int Run(string[] args)
    {
        var outputIndex = Array.IndexOf(args, "--output");
        var output = outputIndex >= 0 ? args[outputIndex + 1] : "artifacts/h2-authority-proof";
        Directory.CreateDirectory(output);
        var summaries = new List<object>();
        foreach (var scenario in AuthorityProofFixtures.Build())
        {
            var environment = scenario.Environment;
            var buoy = scenario.Buoy;
            var run = ApplicationCalculationRunner.Run(environment, buoy, scenario.Assembly, scenario.Anchor, scenario.SafetyFactor);
            foreach (var mode in new[] { "B0", "H1", "H2", "H1H2" })
            {
                var candidate = MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(
                    environment, buoy, run.Result, run.Snapshot.TechnicalReportData.SequencePositions,
                    mode is "H2" or "H1H2", mode is "H1" or "H1H2");
                var replay = MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(
                    environment, buoy, run.Result, run.Snapshot.TechnicalReportData.SequencePositions,
                    mode is "H2" or "H1H2", mode is "H1" or "H1H2");
                if (ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(replay))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: candidate replay differs.");
                if (mode == "B0" && ObserverControlAudit.Canonical(candidate) !=
                    ObserverControlAudit.Canonical(run.Snapshot.SignedCandidate))
                    throw new InvalidOperationException($"{scenario.Id}: adapter changed baseline.");

                // Actual snapshot orchestration: same fallback, arbitration, selection and F1-F4.
                var snapshot = CalculationSnapshotBuilder.BuildWithDiagnosticCandidate(environment, buoy, run.Result, candidate);
                var repeatedSnapshot = CalculationSnapshotBuilder.BuildWithDiagnosticCandidate(environment, buoy, run.Result, replay);
                var f1 = snapshot.SelectedDesignTensionDemand;
                var f2 = snapshot.SelectedAnchorReaction;
                var f3 = snapshot.SelectedLocalStructuralCapacity;
                var f4 = snapshot.SelectedEngineeringAssessment;
                var authority = ObserverControlAudit.Canonical(Authority(snapshot));
                var originalAuthority = ObserverControlAudit.Canonical(Authority(run.Snapshot));
                if (authority != ObserverControlAudit.Canonical(Authority(repeatedSnapshot)))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: actual F1-F4 replay differs.");
                if (mode == "B0" && authority != originalAuthority)
                    throw new InvalidOperationException($"{scenario.Id}: actual B0 F1-F4 differ.");
                if (run.Snapshot.SignedCandidate!.Status == MooringSignedCandidateStatus.RejectedPhysical &&
                    (candidate.Status != MooringSignedCandidateStatus.RejectedPhysical || snapshot.SelectedShape is not null))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: physical rejection changed.");
                if (candidate.Status == MooringSignedCandidateStatus.Accepted &&
                    (f1 is null || f2 is null || f3 is null || f4 is null ||
                     snapshot.ShadowSelectedCore?.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: actual Accepted authority incomplete.");
                if (candidate.Status != MooringSignedCandidateStatus.Accepted &&
                    (f1 is not null || f2 is not null || f3 is not null || f4 is not null))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: non-Accepted candidate gained F1-F4.");
                if (scenario.Id == "A05" && (candidate.Status != MooringSignedCandidateStatus.Accepted || authority != originalAuthority))
                    throw new InvalidOperationException($"A05/{mode}: Accepted/F1-F4 control changed.");
                if ((scenario.Id is "A06" or "A11" or "A15") && mode == "H2" && candidate.Status != MooringSignedCandidateStatus.Accepted)
                    throw new InvalidOperationException($"{scenario.Id}: H2 failed actual acceptance.");
                var metrics = candidate.Status == MooringSignedCandidateStatus.Accepted
                    ? AuthorityProofInspection.Inspect(run.Result, run.Snapshot.TechnicalReportData.SequencePositions, candidate, mode)
                    : null;
                var evidence = new
                {
                    scenario.Id, Mode = mode, ProductionStatus = run.Snapshot.SignedCandidate!.Status.ToString(),
                    Status = candidate.Status.ToString(), candidate.FeedbackIterations,
                    candidate.DiagnosticCode, candidate.DiagnosticText, ReplayBitIdentical = true,
                    Candidate = candidate, Selected = snapshot.ShadowSelectedCore, F1 = f1, F2 = f2, F3 = f3, F4 = f4,
                    BaselineComparison = run.Snapshot.SignedCandidate.Status == MooringSignedCandidateStatus.Accepted
                        ? authority == originalAuthority
                            ? "BitIdenticalF1F4" : "DifferentF1F4"
                        : "NoProductionSelectedF1F4Reference"
                };
                if (scenario.Id is "A03" or "A05" or "A06" or "A11" or "A15")
                    File.WriteAllText(Path.Combine(output, $"{scenario.Id}-{mode}.json"),
                        JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
                summaries.Add(new { scenario.Id, Mode = mode, evidence.ProductionStatus, evidence.Status,
                    candidate.FeedbackIterations, candidate.DiagnosticCode, evidence.BaselineComparison,
                    X = candidate.Shape?.AnchorPoint?.XOffsetM, Z = candidate.Shape?.AnchorPoint?.ZDepthM,
                    F1N = f1?.DemandN, DeltaF1N = Delta(f1?.DemandN, run.Snapshot.SelectedDesignTensionDemand?.DemandN),
                    DeltaX = Delta(candidate.Shape?.AnchorPoint?.XOffsetM, run.Snapshot.SignedCandidate.Shape?.AnchorPoint?.XOffsetM),
                    F2HorizontalN = f2?.HorizontalDemandN, F2NormalN = f2?.SignedNormalReactionN,
                    F4Verdict = f4?.Verdict, Metrics = metrics });
                File.WriteAllText(Path.Combine(output, "summary.json"),
                    JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"H2_AUTHORITY|{scenario.Id}|{mode}|{candidate.Status}|{candidate.FeedbackIterations}|{evidence.BaselineComparison}");
            }
        }
        File.WriteAllText(Path.Combine(output, "summary.json"),
            JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static double? Delta(double? actual, double? original) => actual.HasValue && original.HasValue ? actual.Value - original.Value : null;
    private static object?[] Authority(CalculationSnapshot s) => new object?[]
    { s.SelectedDesignTensionDemand, s.SelectedAnchorReaction, s.SelectedLocalStructuralCapacity, s.SelectedEngineeringAssessment };
}
