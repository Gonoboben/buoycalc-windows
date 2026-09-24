using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

internal static class CompletedRunOutcomeSupportsPreflightPhysicalRejectionWithoutFakeCalculationResultRegression
{
    public static void CompletedRunOutcome_SupportsPreflightPhysicalRejectionWithoutFakeCalculationResult()
    {
        Console.WriteLine("BC_AUD_009_PREREQUISITE_RUN_OUTCOME_BEGIN");

        var fixture = CreateFixture();
        var calculated = ApplicationCalculationRunner.RunCalculatedOutcome(
            fixture.Environment,
            fixture.Buoy,
            fixture.Assembly,
            fixture.Anchor,
            fixture.SafetyFactor);

        if (calculated.Kind != ApplicationRunOutcomeKind.Calculated ||
            calculated.Calculation.Result is null ||
            calculated.Calculation.Snapshot is null ||
            !ReferenceEquals(calculated.Provenance, calculated.Calculation.Snapshot.Provenance))
        {
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: calculated outcome lost the existing non-null run/snapshot/provenance contract.");
        }

        var rejection = PreflightPhysicalRejectionState.CreateLineShorterThanDepth(
            "Preflight_LineShorterThanDepth",
            depthM: 85.0,
            availableActiveLineLengthM: 60.0,
            minimumRequiredActiveLineLengthM: 85.0,
            deficitM: 25.0);
        var first = CreatePreflight(fixture, rejection);
        var second = CreatePreflight(fixture, rejection);

        AssertPreflightAuthority(first);
        AssertPreflightAuthority(second);
        AssertComplete(first.Provenance, "first preflight run");
        AssertComplete(second.Provenance, "second preflight run");

        Equal(calculated.Provenance.InputHash, first.Provenance.InputHash, "calculated/preflight InputHash");
        Equal(first.Provenance.InputHash, second.Provenance.InputHash, "repeated preflight InputHash");
        Equal(first.Provenance.ResultHash, second.Provenance.ResultHash, "repeated preflight ResultHash");
        NotEqual(first.Provenance.RunId, second.Provenance.RunId, "explicit preflight RunId");
        NotEqual(calculated.Provenance.ResultHash, first.Provenance.ResultHash, "calculated/preflight outcome identity");

        var changedEvidence = PreflightPhysicalRejectionState.CreateLineShorterThanDepth(
            "Preflight_LineShorterThanDepth",
            depthM: 86.0,
            availableActiveLineLengthM: 60.0,
            minimumRequiredActiveLineLengthM: 86.0,
            deficitM: 26.0);
        var changed = CreatePreflight(fixture, changedEvidence);
        Equal(first.Provenance.InputHash, changed.Provenance.InputHash, "evidence-only InputHash stability");
        NotEqual(first.Provenance.ResultHash, changed.Provenance.ResultHash, "rejection evidence ResultHash");

        AssertImpossibleStateGuards();

        if (CalculationRunFingerprint.InputSchema != "buoycalc-engineering-input/v1" ||
            CalculationRunFingerprint.ResultSchema != "buoycalc-engineering-result/v3")
        {
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: fingerprint schema identifiers are not the approved input v1/result v3 pair.");
        }

        Console.WriteLine(
            $"BC_AUD_009_PREREQUISITE_RUN_OUTCOME|CalculatedKind={calculated.Kind}|PreflightKind={first.Kind}|InputHash={first.Provenance.InputHash}|ResultHash={first.Provenance.ResultHash}|RepeatedResultHashDeterministic=True|RunIdsDistinct=True|CalculationResultAbsent=True|CalculationSnapshotAbsent=True|SelectedAuthorityAbsent=True|Source={first.Provenance.SourceIdentity}");
        Console.WriteLine("BC_AUD_009_PREREQUISITE_RUN_OUTCOME_END");
    }

    private static PreflightPhysicalRejectedApplicationRunOutcome CreatePreflight(
        Fixture fixture,
        PreflightPhysicalRejectionState rejection)
    {
        // This factory intentionally does not call ApplicationCalculationRunner.Run or
        // BuoyCalculator.Calculate. It creates completed authority from input identity
        // and typed preflight rejection evidence only.
        return ApplicationRunOutcomeFactory.CreatePreflightPhysicalRejected(
            fixture.Environment,
            fixture.Buoy,
            fixture.Assembly,
            fixture.Anchor,
            fixture.SafetyFactor,
            rejection);
    }

    private static void AssertPreflightAuthority(
        PreflightPhysicalRejectedApplicationRunOutcome outcome)
    {
        if (outcome.Kind != ApplicationRunOutcomeKind.PreflightPhysicalRejected ||
            outcome.Rejection.Classification != PreflightPhysicalRejectionKind.LineShorterThanDepth ||
            outcome.Rejection.DiagnosticCode != "Preflight_LineShorterThanDepth" ||
            outcome.Rejection.VerdictKind != PreflightPhysicalRejectionVerdict.NotSuitable ||
            outcome.Rejection.Verdict != "Не подходит" ||
            !outcome.Rejection.HasHardFailure ||
            !outcome.Rejection.BlocksEngineeringGeometry)
        {
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: preflight physical-rejection terminal authority is incomplete.");
        }

        var evidence = outcome.Rejection.Evidence;
        if (evidence.DepthM != 85.0 ||
            evidence.AvailableActiveLineLengthM != 60.0 ||
            evidence.MinimumRequiredActiveLineLengthM != 85.0 ||
            evidence.DeficitM != 25.0)
        {
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: typed preflight engineering evidence changed.");
        }
    }

    private static void AssertImpossibleStateGuards()
    {
        var forbiddenNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Result",
            "CalculationResult",
            "Snapshot",
            "CalculationSnapshot",
            "SelectedShape",
            "SelectedDesignEnvelope",
            "SelectedDesignTensionDemand",
            "SelectedAnchorReaction",
            "SelectedLocalElementDemand",
            "SelectedLocalStructuralCapacity",
            "SelectedEngineeringAssessment",
            "CurrentForceN",
            "WaveForceN",
            "HorizontalForceN",
            "MaxTensionN"
        };
        var exposedForbidden = typeof(PreflightPhysicalRejectedApplicationRunOutcome)
            .GetProperties()
            .Select(x => x.Name)
            .Concat(typeof(PreflightPhysicalRejectionState).GetProperties().Select(x => x.Name))
            .Where(forbiddenNames.Contains)
            .ToArray();
        if (exposedForbidden.Length != 0)
        {
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: preflight outcome exposes fabricated calculated/selected authority: " +
                string.Join(", ", exposedForbidden));
        }

        try
        {
            _ = PreflightPhysicalRejectionState.CreateLineShorterThanDepth(
                "Preflight_LineShorterThanDepth",
                85.0,
                60.0,
                85.0,
                24.0);
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: inconsistent rejection evidence bypassed constructor invariants.");
        }
        catch (ArgumentException)
        {
            // Expected: the canonical evidence must be internally exact, with no epsilon.
        }
    }

    private static void AssertComplete(CalculationRunProvenance provenance, string context)
    {
        if (!Guid.TryParseExact(provenance.RunId, "D", out _) ||
            provenance.CalculationTimestampUtc.Offset != TimeSpan.Zero ||
            provenance.InputHash.Length != 64 ||
            provenance.ResultHash.Length != 64 ||
            string.IsNullOrWhiteSpace(provenance.SourceIdentity))
        {
            throw new InvalidOperationException(
                $"BC-AUD-009 prerequisite: {context} provenance is incomplete.");
        }
    }

    private static void Equal(string expected, string actual, string context)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException($"BC-AUD-009 prerequisite: {context} is not deterministic.");
    }

    private static void NotEqual(string first, string second, string context)
    {
        if (string.Equals(first, second, StringComparison.Ordinal))
            throw new InvalidOperationException($"BC-AUD-009 prerequisite: {context} did not change.");
    }

    private static Fixture CreateFixture()
    {
        var environment = new EnvironmentInput(
            1025,
            85,
            0.2,
            0.5,
            6,
            SeabedCatalog.ById("sand"),
            true,
            new[]
            {
                new CurrentProfilePointInput(0, 0.2, 0, 0, 1025),
                new CurrentProfilePointInput(85, 0.2, 0, 0, 1025)
            });
        var buoy = new BuoyInput("Prerequisite buoy", 1.0, 100, 0.10, 0.8);
        var anchor = new AnchorInput("Prerequisite block", "Deadweight", "Concrete", 3000, 1.2, 1.0);
        var rope = new RopePreset(
            "audit:prerequisite-rope",
            "Prerequisite rope",
            "Synthetic",
            14,
            70,
            0.15,
            1.0,
            "BC-AUD-009 prerequisite");
        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Prerequisite line",
                true,
                rope,
                null,
                60.0,
                1,
                0,
                0,
                0,
                0)
        };

        return new Fixture(environment, buoy, assembly, anchor, 3.0);
    }

    private sealed record Fixture(
        EnvironmentInput Environment,
        BuoyInput Buoy,
        IReadOnlyList<AssemblyItemInput> Assembly,
        AnchorInput Anchor,
        double SafetyFactor);
}
