using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

internal static class CompletedRunOutcomeSupportsPreflightPhysicalRejectionWithoutFakeCalculationResultRegression
{
    public static void CompletedRunOutcome_SupportsPreflightPhysicalRejectionWithoutFakeCalculationResult()
    {
        Console.WriteLine("BC_AUD_009_PREREQUISITE_RUN_OUTCOME_BEGIN");

        var fixtureA = CreateFixture(depthM: 85.0, activeLineLengthM: 60.0);
        var calculated = ApplicationCalculationRunner.RunCalculatedOutcome(
            fixtureA.Environment,
            fixtureA.Buoy,
            fixtureA.Assembly,
            fixtureA.Anchor,
            fixtureA.SafetyFactor);

        if (calculated.Kind != ApplicationRunOutcomeKind.Calculated ||
            calculated.Calculation.Result is null ||
            calculated.Calculation.Snapshot is null ||
            !ReferenceEquals(calculated.Provenance, calculated.Calculation.Snapshot.Provenance))
        {
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: calculated outcome lost the existing non-null run/snapshot/provenance contract.");
        }

        var first = CreatePreflight(fixtureA);
        var second = CreatePreflight(fixtureA);

        AssertPreflightAuthority(first, expectedDepthM: 85.0, expectedActiveLineLengthM: 60.0, expectedDeficitM: 25.0);
        AssertPreflightAuthority(second, expectedDepthM: 85.0, expectedActiveLineLengthM: 60.0, expectedDeficitM: 25.0);
        AssertComplete(first.Provenance, "first preflight run");
        AssertComplete(second.Provenance, "second preflight run");

        Equal(ComputeInputHash(fixtureA), first.Provenance.InputHash, "Fixture A canonical InputHash binding");
        Equal(calculated.Provenance.InputHash, first.Provenance.InputHash, "calculated/preflight InputHash");
        Equal(first.Provenance.InputHash, second.Provenance.InputHash, "repeated preflight InputHash");
        Equal(first.Provenance.ResultHash, second.Provenance.ResultHash, "repeated preflight ResultHash");
        NotEqual(first.Provenance.RunId, second.Provenance.RunId, "explicit preflight RunId");
        NotEqual(calculated.Provenance.ResultHash, first.Provenance.ResultHash, "calculated/preflight outcome identity");

        var fixtureB = CreateFixture(depthM: 86.0, activeLineLengthM: 60.0);
        var changed = CreatePreflight(fixtureB);
        AssertPreflightAuthority(changed, expectedDepthM: 86.0, expectedActiveLineLengthM: 60.0, expectedDeficitM: 26.0);
        Equal(ComputeInputHash(fixtureB), changed.Provenance.InputHash, "Fixture B canonical InputHash binding");
        NotEqual(first.Provenance.InputHash, changed.Provenance.InputHash, "changed-input InputHash");
        NotEqual(first.Provenance.ResultHash, changed.Provenance.ResultHash, "changed-input rejection ResultHash");

        AssertImpossibleStateGuards();
        AssertNonShortLineRejected();
        AssertBcAud004ValidationReused(fixtureA);
        AssertBcAud007CurrentProfileRequirementReused(fixtureA);

        if (CalculationRunFingerprint.InputSchema != "buoycalc-engineering-input/v1" ||
            CalculationRunFingerprint.ResultSchema != "buoycalc-engineering-result/v3")
        {
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: fingerprint schema identifiers are not the approved input v1/result v3 pair.");
        }

        Console.WriteLine(
            $"BC_AUD_009_PREREQUISITE_RUN_OUTCOME|CalculatedKind={calculated.Kind}|PreflightKind={first.Kind}|InputHashA={first.Provenance.InputHash}|ResultHashA={first.Provenance.ResultHash}|InputHashB={changed.Provenance.InputHash}|ResultHashB={changed.Provenance.ResultHash}|RepeatedResultHashDeterministic=True|ChangedInputsChangeBothHashes=True|RunIdsDistinct=True|EvidenceDerivedFromHashedInputs=True|DisabledLinesExcluded=True|NonLineItemsExcluded=True|BC_AUD_004_Gate=True|BC_AUD_007_Gate=True|MismatchConstructionBlocked=True|CalculationResultAbsent=True|CalculationSnapshotAbsent=True|SelectedAuthorityAbsent=True|Source={first.Provenance.SourceIdentity}");
        Console.WriteLine("BC_AUD_009_PREREQUISITE_RUN_OUTCOME_END");
    }

    private static PreflightPhysicalRejectedApplicationRunOutcome CreatePreflight(Fixture fixture)
    {
        // This trusted factory validates and derives rejection evidence from the same
        // canonical inputs it fingerprints. Callers cannot provide evidence separately.
        return ApplicationRunOutcomeFactory.CreateLineShorterThanDepthPreflightPhysicalRejected(
            fixture.Environment,
            fixture.Buoy,
            fixture.Assembly,
            fixture.Anchor,
            fixture.SafetyFactor);
    }

    private static void AssertPreflightAuthority(
        PreflightPhysicalRejectedApplicationRunOutcome outcome,
        double expectedDepthM,
        double expectedActiveLineLengthM,
        double expectedDeficitM)
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
        if (evidence.DepthM != expectedDepthM ||
            evidence.AvailableActiveLineLengthM != expectedActiveLineLengthM ||
            evidence.MinimumRequiredActiveLineLengthM != expectedDepthM ||
            evidence.DeficitM != expectedDeficitM)
        {
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: rejection evidence was not derived exactly from canonical depth/active-line inputs.");
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

        var unsafePublicFactory = typeof(ApplicationRunOutcomeFactory)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.GetParameters().Any(p => p.ParameterType == typeof(PreflightPhysicalRejectionState)))
            .Select(x => x.Name)
            .ToArray();
        var publicStateFactory = typeof(PreflightPhysicalRejectionState)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.ReturnType == typeof(PreflightPhysicalRejectionState))
            .Select(x => x.Name)
            .ToArray();
        if (unsafePublicFactory.Length != 0 || publicStateFactory.Length != 0)
        {
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: public construction still accepts independent rejection evidence: " +
                string.Join(", ", unsafePublicFactory.Concat(publicStateFactory)));
        }
    }

    private static void AssertNonShortLineRejected()
    {
        var taut = CreateFixture(depthM: 85.0, activeLineLengthM: 85.0);
        try
        {
            _ = CreatePreflight(taut);
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: line >= depth incorrectly created short-line authority.");
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("requires the active line to be shorter than depth", StringComparison.Ordinal))
        {
            // Expected: this is not a short-line preflight outcome.
        }
    }

    private static void AssertBcAud004ValidationReused(Fixture valid)
    {
        var invalid = valid with
        {
            Anchor = valid.Anchor with { WeightAirKg = -1.0 }
        };
        try
        {
            _ = CreatePreflight(invalid);
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: invalid typed physical input bypassed BC-AUD-004 validation.");
        }
        catch (EngineeringInputValidationException ex) when (
            ex.Code == "NONNEGATIVE_REQUIRED" && ex.Field == "Anchor.WeightAirKg")
        {
            // Expected: the same production validation contract blocks authority.
        }
    }

    private static void AssertBcAud007CurrentProfileRequirementReused(Fixture valid)
    {
        var duplicateProfile = new[]
        {
            new CurrentProfilePointInput(0, 0.2, 0, 0, 1025),
            new CurrentProfilePointInput(0, 0.3, 0, 0, 1025),
            new CurrentProfilePointInput(valid.Environment.DepthM, 0.2, 0, 0, 1025)
        };
        var invalid = valid with
        {
            Environment = valid.Environment with { CurrentProfile = duplicateProfile }
        };
        try
        {
            _ = CreatePreflight(invalid);
            throw new InvalidOperationException(
                "BC-AUD-009 prerequisite: duplicate current-profile depth bypassed BC-AUD-007 validation.");
        }
        catch (CurrentProfileValidationException ex) when (
            ex.Code == CurrentProfileRequirement.DuplicateDepthCode && ex.DuplicateDepthM == 0.0)
        {
            // Expected: the same production profile requirement blocks authority.
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

    private static string ComputeInputHash(Fixture fixture)
    {
        return CalculationRunFingerprint.ComputeInputHash(
            fixture.Environment,
            fixture.Buoy,
            fixture.Assembly,
            fixture.Anchor,
            fixture.SafetyFactor);
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

    private static Fixture CreateFixture(double depthM, double activeLineLengthM)
    {
        var environment = new EnvironmentInput(
            1025,
            depthM,
            0.2,
            0.5,
            6,
            SeabedCatalog.ById("sand"),
            true,
            new[]
            {
                new CurrentProfilePointInput(0, 0.2, 0, 0, 1025),
                new CurrentProfilePointInput(depthM, 0.2, 0, 0, 1025)
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
                "Active prerequisite line",
                true,
                rope,
                null,
                activeLineLengthM,
                1,
                0,
                0,
                0,
                0),
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Disabled line must not contribute",
                false,
                rope,
                null,
                1000.0,
                1,
                0,
                0,
                0,
                0),
            new AssemblyItemInput(
                AssemblyItemKind.Payload,
                "Non-line length must not contribute",
                true,
                null,
                null,
                500.0,
                1,
                10.0,
                0.01,
                0.02,
                0.8)
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
