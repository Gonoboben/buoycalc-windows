using System.Text.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
        if (args.Contains("--cycle-survey", StringComparer.Ordinal))
            return RawCycleSurvey.Run(output);
        // Numerical detector controls only, not physical test evidence.
        long B(double value) => BitConverter.DoubleToInt64Bits(value);
        bool Cycle(long[]? a, long[] b, long[] c) => MooringSignedCandidateEvaluator.IsExactTwoCycle(a, b, c);
        if (Cycle(null, new[] { B(1) }, new[] { B(1) }) ||
            Cycle(new[] { B(1) }, new[] { B(1) }, new[] { B(1) }) ||
            Cycle(new[] { B(1) }, new[] { B(2) }, new[] { B(3) }) ||
            Cycle(new[] { B(1) }, new[] { B(2) }, new[] { B(Math.BitIncrement(1)) }) ||
            Cycle(new[] { B(1), B(2) }, new[] { B(3) }, new[] { B(1) }) ||
            !Cycle(new[] { B(1) }, new[] { B(2) }, new[] { B(1) }) ||
            !Cycle(new[] { B(0.0) }, new[] { B(-0.0) }, new[] { B(0.0) }))
            throw new InvalidOperationException("Exact two-cycle detector failed missing/drifting/fixed/one-ULP/schema/signed-zero controls.");
        bool Cycle3(long[]? a, long[]? b, long[] c, long[] d) => MooringSignedCandidateEvaluator.IsExactThreeCycle(a, b, c, d);
        if (Cycle(new[] { B(1) }, new[] { B(2), B(3) }, new[] { B(1) }) ||
            !Cycle3(new[] { B(1) }, new[] { B(2) }, new[] { B(3) }, new[] { B(1) }) ||
            Cycle3(null, new[] { B(2) }, new[] { B(3) }, new[] { B(1) }) ||
            Cycle3(new[] { B(1) }, null, new[] { B(3) }, new[] { B(1) }) ||
            Cycle3(new[] { B(1) }, new[] { B(2) }, new[] { B(3) }, new[] { B(Math.BitIncrement(1)) }) ||
            Cycle3(new[] { B(1) }, new[] { B(2) }, new[] { B(1) }, new[] { B(1) }) ||
            Cycle3(new[] { B(1) }, new[] { B(2), B(3) }, new[] { B(3) }, new[] { B(1) }) ||
            !Cycle3(new[] { B(0.0) }, new[] { B(-0.0) }, new[] { B(1) }, new[] { B(0.0) }))
            throw new InvalidOperationException("Exact three-cycle detector failed raw/schema/drift/fixed/period-2/signed-zero controls.");
        var summaries = new List<object>();
        var fixtures = AuthorityProofFixtures.Build();
        if (args.Contains("--residual-only", StringComparer.Ordinal))
            fixtures = fixtures.Where(f => f.Id is "A03" or "A05" or "A06" or "A11" or "A15" or
                "grid-20-1.05-0.6" or "grid-20-1.2-0.6" or "grid-380-1.1-0.45" or "coeff-0.5-14-1030" or
                "neighborhood-20-1.19-0.65" or "neighborhood-20-1.2-0.65" or "neighborhood-380-1.19-0.65" or
                AuthorityProofFixtures.AlternateControl or AuthorityProofFixtures.BudgetControl);
        foreach (var scenario in fixtures)
        {
            var environment = scenario.Environment;
            var buoy = scenario.Buoy;
            var run = ApplicationCalculationRunner.Run(environment, buoy, scenario.Assembly, scenario.Anchor, scenario.SafetyFactor);
            var legacyCandidate = MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(
                environment, buoy, run.Result, run.Snapshot.TechnicalReportData.SequencePositions, false, false);
            var legacySnapshot = CalculationSnapshotBuilder.BuildWithDiagnosticCandidate(environment, buoy, run.Result, legacyCandidate);
            MooringSignedCandidateResult? previousRecovery = null;
            string? previousRecoveryAuthority = null;
            MooringSignedCandidateResult? previousSquaredRecovery = null;
            string? previousSquaredAuthority = null;
            long[]? referenceH1Bits = null;
            string? referenceH1Engineering = null;
            MooringSignedCandidateResult? previousRatio = null;
            string? previousRatioAuthority = null;
            foreach (var mode in new[] { "B0", "H1", "H2", "H1H2", "H2R", "H2R3", "H2R4", "H2RF", "H2RF3", "H2RF3P", "H2RF3Q", "H2RF3QK", "H2RF3QPK" })
            {
                var fixedPointRawFieldCount = 0;
                MooringShapeProjectionResult? finalFeedbackProjection = null;
                MooringShapeForceResult? finalFeedbackForces = null;
                void ObserveFixedPointForces(MooringShapeProjectionResult projection, MooringShapeForceResult forces)
                { finalFeedbackProjection = projection; finalFeedbackForces = forces; }
                var transitions = new List<string>();
                void ObserveRecovery(int iteration, string stage) => transitions.Add($"{iteration}:{stage}");
                long[]? fixedBefore = null;
                long[]? fixedAfter = null;
                void VerifyFixedPoint(IReadOnlyList<long> before, IReadOnlyList<long> after)
                {
                    if (!before.SequenceEqual(after))
                        throw new InvalidOperationException($"{scenario.Id}/{mode}: Accepted fixed-point raw states differ.");
                    if (fixedBefore is not null && (!before.SequenceEqual(fixedBefore) || !after.SequenceEqual(fixedAfter!)))
                        throw new InvalidOperationException($"{scenario.Id}/{mode}: fixed-point raw-state replay differs.");
                    fixedBefore = before.ToArray(); fixedAfter = after.ToArray();
                    fixedPointRawFieldCount = before.Count;
                }
                var productionRecovery = mode is "H2RF3" or "H2RF3P" or "H2RF3Q" or "H2RF3QK" or "H2RF3QPK";
                var perpendicularRecovery = mode is "H2RF3P" or "H2RF3Q" or "H2RF3QK" or "H2RF3QPK";
                var canonicalRecovery = mode is "H2RF3QK" or "H2RF3QPK";
                MooringSignedCandidateResult Evaluate() => MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(
                    environment, buoy, run.Result, run.Snapshot.TechnicalReportData.SequencePositions,
                    mode is "H2" or "H1H2", mode is "H1" or "H1H2",
                    recoverExactTwoCycles: productionRecovery || mode is "H2R" or "H2R3" or "H2R4" or "H2RF",
                    fixedPointObserver: VerifyFixedPoint,
                    recoverSquaredNormalCycles: productionRecovery || mode is "H2R3" or "H2R4" or "H2RF",
                    recoverGroupedNormalCycles: mode == "H2R4", recoveryObserver: ObserveRecovery,
                    recoverKnownBuoyCycles: productionRecovery || mode == "H2RF",
                    recoverExactThreeCycles: productionRecovery, recoverPerpendicularNormalCycles: perpendicularRecovery,
                    useNormalForceRatio: canonicalRecovery || mode == "H2RF3Q",
                    recoverCanonicalRestartCycles: canonicalRecovery, fixedPointForceObserver: ObserveFixedPointForces,
                    recoverAlternatePerpendicularCycles: mode == "H2RF3QPK");
                var candidate = Evaluate();
                var firstTransitions = transitions.ToArray();
                transitions.Clear();
                var replay = Evaluate();
                if (!transitions.SequenceEqual(firstTransitions))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: arithmetic transition replay differs.");
                if (ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(replay))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: candidate replay differs.");
                if (mode == "B0" && ObserverControlAudit.Canonical(candidate) !=
                    ObserverControlAudit.Canonical(legacyCandidate))
                    throw new InvalidOperationException($"{scenario.Id}: adapter changed baseline.");

                // Actual snapshot orchestration: same fallback, arbitration, selection and F1-F4.
                var snapshot = CalculationSnapshotBuilder.BuildWithDiagnosticCandidate(environment, buoy, run.Result, candidate);
                var repeatedSnapshot = CalculationSnapshotBuilder.BuildWithDiagnosticCandidate(environment, buoy, run.Result, replay);
                var f1 = snapshot.SelectedDesignTensionDemand;
                var f2 = snapshot.SelectedAnchorReaction;
                var f3 = snapshot.SelectedLocalStructuralCapacity;
                var f4 = snapshot.SelectedEngineeringAssessment;
                var authority = ObserverControlAudit.Canonical(Authority(snapshot));
                var originalAuthority = ObserverControlAudit.Canonical(Authority(legacySnapshot));
                var engineering = ObserverControlAudit.Canonical(new { F1 = f1, F2 = f2, F3 = f3, F4 = f4 });
                if (mode == "H1" && candidate.Status == MooringSignedCandidateStatus.Accepted)
                { referenceH1Bits = fixedAfter; referenceH1Engineering = engineering; }
                if (mode == "H2RF3Q") { previousRatio = candidate; previousRatioAuthority = authority; }
                if (canonicalRecovery && previousRatio?.Status == MooringSignedCandidateStatus.Accepted &&
                    (ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(previousRatio) || authority != previousRatioAuthority))
                    throw new InvalidOperationException($"{scenario.Id}: restart changed an already Accepted ratio result.");
                if (canonicalRecovery && candidate.Status == MooringSignedCandidateStatus.Accepted &&
                    candidate.DiagnosticText.Contains("Canonical H1 restart", StringComparison.Ordinal) &&
                    (referenceH1Bits is null || fixedAfter is null || !referenceH1Bits.SequenceEqual(fixedAfter) || engineering != referenceH1Engineering))
                    throw new InvalidOperationException($"{scenario.Id}: canonical restart differs from the actual H1 terminal physical state.");
                if (mode == "H2R") { previousRecovery = candidate; previousRecoveryAuthority = authority; }
                if ((productionRecovery || mode is "H2R3" or "H2R4" or "H2RF") && previousRecovery?.Status == MooringSignedCandidateStatus.Accepted &&
                    (ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(previousRecovery) || authority != previousRecoveryAuthority))
                    throw new InvalidOperationException($"{scenario.Id}: second-stage arithmetic changed an original H2R Accepted result.");
                if (mode == "H2R3") { previousSquaredRecovery = candidate; previousSquaredAuthority = authority; }
                if (productionRecovery && previousSquaredRecovery?.Status == MooringSignedCandidateStatus.Accepted &&
                    (ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(previousSquaredRecovery) || authority != previousSquaredAuthority))
                    throw new InvalidOperationException($"{scenario.Id}: recovery changed an original H2R3 Accepted result.");
                if (productionRecovery && run.Snapshot.SignedCandidate!.Status == MooringSignedCandidateStatus.Accepted &&
                    (ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(run.Snapshot.SignedCandidate) ||
                     authority != ObserverControlAudit.Canonical(Authority(run.Snapshot))))
                    throw new InvalidOperationException($"{scenario.Id}: second-stage recovery changed an existing production Accepted result.");
                if (mode == "H2RF3" && (ObserverControlAudit.Canonical(candidate) !=
                    ObserverControlAudit.Canonical(run.Snapshot.SignedCandidate) ||
                    authority != ObserverControlAudit.Canonical(Authority(run.Snapshot))))
                    throw new InvalidOperationException($"{scenario.Id}: production differs from the proven second-stage candidate/F1-F4.");
                if (authority != ObserverControlAudit.Canonical(Authority(repeatedSnapshot)))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: actual F1-F4 replay differs.");
                if (mode == "B0" && authority != originalAuthority)
                    throw new InvalidOperationException($"{scenario.Id}: actual B0 F1-F4 differ.");
                if (mode == "H2R" && legacyCandidate!.Status == MooringSignedCandidateStatus.Accepted &&
                    (authority != originalAuthority || ObserverControlAudit.Canonical(candidate) !=
                     ObserverControlAudit.Canonical(legacyCandidate)))
                    throw new InvalidOperationException($"{scenario.Id}: recovery changed a previously Accepted candidate/F1-F4.");
                if (legacyCandidate!.Status == MooringSignedCandidateStatus.RejectedPhysical &&
                    (candidate.Status != MooringSignedCandidateStatus.RejectedPhysical || snapshot.SelectedShape is not null))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: physical rejection changed.");
                if (candidate.Status == MooringSignedCandidateStatus.Accepted &&
                    (f1 is null || f2 is null || f3 is null || f4 is null ||
                     snapshot.ShadowSelectedCore?.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: actual Accepted authority incomplete.");
                if (candidate.Status != MooringSignedCandidateStatus.Accepted &&
                    (f1 is not null || f2 is not null || f3 is not null || f4 is not null))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: non-Accepted candidate gained F1-F4.");
                if (mode == "H2RF3QPK" && scenario.Id == AuthorityProofFixtures.AlternateControl &&
                    (candidate.Status != MooringSignedCandidateStatus.Accepted ||
                     !transitions.Any(t => t.EndsWith(":TangentPerpendicularAlternate", StringComparison.Ordinal))))
                    throw new InvalidOperationException($"{scenario.Id}: alternate recovery did not produce actual Accepted.");
                if (mode == "H2RF3QPK" && scenario.Id == AuthorityProofFixtures.BudgetControl &&
                    (candidate.Status != MooringSignedCandidateStatus.BudgetExhausted ||
                     candidate.FeedbackIterations != MooringSignedCandidateResult.ProductionFeedbackBudget))
                    throw new InvalidOperationException($"{scenario.Id}: real budget control changed.");
                if (scenario.Id == "A05" && (candidate.Status != MooringSignedCandidateStatus.Accepted || authority != originalAuthority))
                    throw new InvalidOperationException($"A05/{mode}: Accepted/F1-F4 control changed.");
                if ((scenario.Id is "A06" or "A11" or "A15") && (mode is "H2" or "H2R") && candidate.Status != MooringSignedCandidateStatus.Accepted)
                    throw new InvalidOperationException($"{scenario.Id}: H2 failed actual acceptance.");
                var observedStages = new List<MooringSignedFeedbackObservedStep>();
                if (mode == "H2RF3")
                {
                    var observed = MooringSignedCandidateEvaluator.BuildWithDiagnosticObserver(environment, buoy, run.Result,
                        run.Snapshot.TechnicalReportData.SequencePositions, observedStages.Add);
                    if (ObserverControlAudit.Canonical(observed) != ObserverControlAudit.Canonical(candidate))
                        throw new InvalidOperationException($"{scenario.Id}: public observer changed candidate/recovery behavior.");
                }
                var metrics = candidate.Status == MooringSignedCandidateStatus.Accepted
                    ? AuthorityProofInspection.Inspect(run.Result, run.Snapshot.TechnicalReportData.SequencePositions, candidate, mode, transitions,
                        finalFeedbackProjection ?? throw new InvalidOperationException("Missing actual fixed-point projection."),
                        finalFeedbackForces ?? throw new InvalidOperationException("Missing actual fixed-point forces."))
                    : null;
                var evidence = new
                {
                    scenario.Id, Mode = mode, LegacyStatus = legacyCandidate.Status.ToString(),
                    ProductionStatus = run.Snapshot.SignedCandidate!.Status.ToString(),
                    Status = candidate.Status.ToString(), candidate.FeedbackIterations,
                    candidate.DiagnosticCode, candidate.DiagnosticText, ReplayBitIdentical = true,
                    FinalFeedbackProjection = candidate.DiagnosticText.Contains("Canonical H1 restart", StringComparison.Ordinal) ? finalFeedbackProjection : null,
                    FinalFeedbackForces = candidate.DiagnosticText.Contains("Canonical H1 restart", StringComparison.Ordinal) ? finalFeedbackForces : null,
                    ObservedProductionStages = observedStages,
                    Candidate = candidate, Selected = snapshot.ShadowSelectedCore, F1 = f1, F2 = f2, F3 = f3, F4 = f4,
                    BaselineComparison = legacyCandidate.Status == MooringSignedCandidateStatus.Accepted
                        ? authority == originalAuthority
                            ? "BitIdenticalF1F4" : "DifferentF1F4"
                        : "NoLegacySelectedF1F4Reference"
                };
                if (scenario.Id is "A03" or "A05" or "A06" or "A11" or "A15" or
                    "grid-20-1.05-0.6" or "grid-20-1.2-0.6" or "grid-380-1.1-0.45" or "coeff-0.5-14-1030" or
                    "neighborhood-20-1.19-0.65" or "neighborhood-20-1.2-0.65" or "neighborhood-380-1.19-0.65" or
                    AuthorityProofFixtures.AlternateControl or AuthorityProofFixtures.BudgetControl)
                {
                    File.WriteAllText(Path.Combine(output, $"{scenario.Id}-{mode}.json"),
                        JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
                    if (fixedBefore is not null)
                    {
                        using var writer = new StreamWriter(Path.Combine(output, $"{scenario.Id}-{mode}-fixed-point.csv"), false, new UTF8Encoding(false));
                        writer.WriteLine("FieldIndex,PreviousRawBits,NextRawBits");
                        for (var i = 0; i < fixedBefore.Length; i++)
                            writer.WriteLine($"{i},{fixedBefore[i].ToString("X16", CultureInfo.InvariantCulture)},{fixedAfter![i].ToString("X16", CultureInfo.InvariantCulture)}");
                    }
                }
                summaries.Add(new { scenario.Id, Mode = mode, evidence.LegacyStatus, evidence.ProductionStatus, evidence.Status,
                    candidate.FeedbackIterations, candidate.DiagnosticCode, evidence.BaselineComparison,
                    InputHash = run.Snapshot.Provenance!.InputHash,
                    ObservedProductionSteps = observedStages.Count,
                    ObservedKnownBuoySteps = observedStages.Count(step => step.Fields.Any(f => f.Name == "KnownBuoyDragSourceN")),
                    PreviousSquaredRecoveryStatus = previousSquaredRecovery?.Status.ToString(),
                    PreviousSquaredCandidateSHA256 = previousSquaredRecovery is null ? null : Digest(ObserverControlAudit.Canonical(previousSquaredRecovery)),
                    PreviousSquaredAuthoritySHA256 = previousSquaredAuthority is null ? null : Digest(previousSquaredAuthority),
                    OriginalRecoveryStatus = previousRecovery?.Status.ToString(),
                    OriginalRecoveryCandidateSHA256 = previousRecovery is null ? null : Digest(ObserverControlAudit.Canonical(previousRecovery)),
                    OriginalRecoveryAuthoritySHA256 = previousRecoveryAuthority is null ? null : Digest(previousRecoveryAuthority),
                    CandidateSHA256 = Digest(ObserverControlAudit.Canonical(candidate)),
                    LegacyCandidateSHA256 = Digest(ObserverControlAudit.Canonical(legacyCandidate)),
                    ProductionCandidateSHA256 = Digest(ObserverControlAudit.Canonical(run.Snapshot.SignedCandidate)),
                    AuthoritySHA256 = Digest(authority), LegacyAuthoritySHA256 = Digest(originalAuthority),
                    ProductionAuthoritySHA256 = Digest(ObserverControlAudit.Canonical(Authority(run.Snapshot))),
                    X = candidate.Shape?.AnchorPoint?.XOffsetM, Z = candidate.Shape?.AnchorPoint?.ZDepthM,
                    F1N = f1?.DemandN, DeltaF1N = Delta(f1?.DemandN, legacySnapshot.SelectedDesignTensionDemand?.DemandN),
                    DeltaX = Delta(candidate.Shape?.AnchorPoint?.XOffsetM, legacyCandidate.Shape?.AnchorPoint?.XOffsetM),
                    F2HorizontalN = f2?.HorizontalDemandN, F2NormalN = f2?.SignedNormalReactionN,
                    RecoveryTransitions = transitions.ToArray(), F4Verdict = f4?.Verdict, FixedPointRawFieldCount = fixedPointRawFieldCount, Metrics = metrics });
                File.WriteAllText(Path.Combine(output, "summary.json"),
                    JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"H2_AUTHORITY|{scenario.Id}|{mode}|{candidate.Status}|{candidate.FeedbackIterations}|{evidence.BaselineComparison}");
            }
        }
        File.WriteAllText(Path.Combine(output, "summary.json"),
            JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static string Digest(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    private static double? Delta(double? actual, double? original) => actual.HasValue && original.HasValue ? actual.Value - original.Value : null;
    private static object?[] Authority(CalculationSnapshot s) => new object?[]
    { s.SelectedDesignTensionDemand, s.SelectedAnchorReaction, s.SelectedLocalStructuralCapacity, s.SelectedEngineeringAssessment };
}
