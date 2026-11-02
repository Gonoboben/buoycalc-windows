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
        var scenarios = FixedProgram.Scenarios().Concat(new[]
        {
            new FixedDiagnosticScenario("A03", 20, 20, 0.2),
            new FixedDiagnosticScenario("A05", 20, 20.2, 0.2)
        });
        var summaries = new List<object>();
        foreach (var scenario in scenarios)
        {
            var (environment, buoy, assembly, anchor) = FixedProgram.BuildInputs(scenario);
            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, 3.0);
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

                // Same arbitration, selected shape and engineering projectors as CalculationSnapshotBuilder.
                var selected = MooringSelectedShapeArbitrator.Arbitrate(run.Snapshot.ShadowSelectedCore, candidate);
                var envelope = MooringSelectedDesignEnvelopeStateProjector.Project(run.Result, selected, candidate);
                var f1 = MooringSelectedDesignTensionDemandProjector.Project(envelope);
                var f2 = MooringSelectedAnchorReactionStateProjector.Project(run.Result, envelope);
                var local = MooringSelectedLocalElementDemandStateProjector.Project(
                    run.Result, run.Snapshot.TechnicalReportData.SequencePositions, selected, candidate);
                var f3 = MooringSelectedLocalStructuralCapacityStateProjector.Project(run.Result, local);
                var f4 = MooringSelectedEngineeringAssessmentStateProjector.Project(
                    environment, run.Result, selected?.SourceIdentity, f1, f2, f3);
                var evidence = new
                {
                    scenario.Id, Mode = mode, ProductionStatus = run.Snapshot.SignedCandidate!.Status.ToString(),
                    Status = candidate.Status.ToString(), candidate.FeedbackIterations,
                    candidate.DiagnosticCode, candidate.DiagnosticText, ReplayBitIdentical = true,
                    Candidate = candidate, Selected = selected, F1 = f1, F2 = f2, F3 = f3, F4 = f4,
                    BaselineComparison = run.Snapshot.SignedCandidate.Status == MooringSignedCandidateStatus.Accepted
                        ? ObserverControlAudit.Canonical(new object?[] { f1, f2, f3, f4 }) ==
                            ObserverControlAudit.Canonical(new object?[] {
                                run.Snapshot.SelectedDesignTensionDemand, run.Snapshot.SelectedAnchorReaction,
                                run.Snapshot.SelectedLocalStructuralCapacity, run.Snapshot.SelectedEngineeringAssessment })
                            ? "BitIdenticalF1F4" : "DifferentF1F4"
                        : "NoProductionSelectedF1F4Reference"
                };
                File.WriteAllText(Path.Combine(output, $"{scenario.Id}-{mode}.json"),
                    JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
                summaries.Add(new { scenario.Id, Mode = mode, evidence.ProductionStatus, evidence.Status,
                    candidate.FeedbackIterations, candidate.DiagnosticCode, evidence.BaselineComparison });
                Console.WriteLine($"H2_AUTHORITY|{scenario.Id}|{mode}|{candidate.Status}|{candidate.FeedbackIterations}|{evidence.BaselineComparison}");
            }
        }
        File.WriteAllText(Path.Combine(output, "summary.json"),
            JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
