using System.Globalization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class AcceptedSignedInvalidAnchorPrerequisiteIsTerminalFailureRegression
{
    public static void AcceptedSigned_InvalidAnchorPrerequisite_IsTerminalFailure()
    {
        Console.WriteLine("BC_AUD_010_ACCEPTED_SIGNED_INVALID_ANCHOR_BEGIN");

        ValidateTerminalFailure(500.0, -730.0, "500 kg / 1.2 m3");
        ValidateTerminalFailure(1000.0, -230.0, "1000 kg / 1.2 m3");
        ValidateTerminalFailure(1230.0, 0.0, "exact-zero submerged weight");
        ValidatePositiveAnchorControl();

        Console.WriteLine(
            "BC_AUD_010_ACCEPTED_SIGNED_INVALID_ANCHOR_ROLLUP|NegativeCases=2|ExactZero=True|SignedAccepted=True|SelectedGeometry=True|F2Fabricated=False|TerminalHardFailure=True|RiskCode=NonPositiveAnchorSubmergedWeight|UserReport=True|FullTxt=True|PdfGenerated=True|PositiveAnchorControl=True");
        Console.WriteLine("BC_AUD_010_ACCEPTED_SIGNED_INVALID_ANCHOR_END");
    }

    private static void ValidateTerminalFailure(
        double anchorWeightAirKg,
        double expectedWeightWaterKg,
        string label)
    {
        var fixture = CreateFixture(anchorWeightAirKg);
        var run = Run(fixture);
        var snapshot = run.Snapshot;

        if (snapshot.SignedCandidate?.Status != MooringSignedCandidateStatus.Accepted ||
            snapshot.SelectedShape is null)
        {
            throw new InvalidOperationException(
                $"BC-AUD-010 {label}: fixture must retain Accepted signed selected geometry.");
        }

        Exact(run.Result.AnchorWeightWaterKg, expectedWeightWaterKg, label + " derived AnchorWeightWaterKg");
        if (snapshot.SelectedAnchorReaction is not null)
            throw new InvalidOperationException($"BC-AUD-010 {label}: F2 contact authority was fabricated.");

        if (snapshot.Provenance is null)
            throw new InvalidOperationException($"BC-AUD-010 {label}: completed calculation lost BC-AUD-005 provenance.");

        var assessment = snapshot.SelectedEngineeringAssessment
            ?? throw new InvalidOperationException(
                $"BC-AUD-010 {label}: Accepted signed geometry with AnchorWeightWaterKg={F(run.Result.AnchorWeightWaterKg)} has no terminal selected assessment.");

        if (assessment.Verdict != "Не подходит" ||
            !assessment.HasHardFailure ||
            assessment.MainRiskCode != "NonPositiveAnchorSubmergedWeight")
        {
            throw new InvalidOperationException(
                $"BC-AUD-010 {label}: expected terminal Не подходит/HardFailure/NonPositiveAnchorSubmergedWeight, got {assessment.Verdict}/{assessment.HasHardFailure}/{assessment.MainRiskCode}.");
        }

        var anchorCheck = assessment.Checks.SingleOrDefault(
            x => x.Kind == MooringEngineeringAssessmentCheckKind.AnchorSubmergedWeight);
        if (anchorCheck is null ||
            anchorCheck.Status != MooringEngineeringAssessmentCheckStatus.HardFailure ||
            anchorCheck.Code != "NonPositiveAnchorSubmergedWeight" ||
            !anchorCheck.Detail.Contains($"AnchorWeightWaterKg={F(expectedWeightWaterKg)}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"BC-AUD-010 {label}: terminal direct anchor evidence is missing.");
        }

        var report = UserEngineeringReportReadModelProjector.Project(
            "BC-AUD-010 " + label,
            fixture.Environment,
            fixture.Buoy,
            fixture.Anchor,
            snapshot);
        if (report.Assessment?.Verdict != "Не подходит" ||
            report.Assessment.MainRiskCode != "NonPositiveAnchorSubmergedWeight" ||
            report.AnchorReaction is not null)
        {
            throw new InvalidOperationException($"BC-AUD-010 {label}: typed user/PDF read model lost terminal authority or fabricated F2.");
        }

        var fullText = TechnicalReportBuilder.Build(
            "BC-AUD-010 " + label,
            fixture.Environment,
            fixture.Buoy,
            fixture.Anchor,
            snapshot);
        foreach (var required in new[]
        {
            "Вердикт: Не подходит",
            "NonPositiveAnchorSubmergedWeight",
            "AnchorWeightWaterKg=" + F(expectedWeightWaterKg),
            "Контакт якоря F2: недоступен"
        })
        {
            if (!fullText.Contains(required, StringComparison.Ordinal))
                throw new InvalidOperationException($"BC-AUD-010 {label}: Full TXT is missing '{required}'.");
        }

        ValidatePdfBuild(report, label);

        Console.WriteLine(string.Join(
            "|",
            "BC_AUD_010_ACCEPTED_SIGNED_INVALID_ANCHOR",
            $"Case={label}",
            $"AnchorWeightAirKg={F(anchorWeightAirKg)}",
            $"AnchorVolumeM3={F(fixture.Anchor.VolumeM3)}",
            $"AnchorWeightWaterKg={F(run.Result.AnchorWeightWaterKg)}",
            $"SignedStatus={snapshot.SignedCandidate.Status}",
            "SelectedShape=True",
            "F2=None",
            $"Verdict={assessment.Verdict}",
            $"RiskCode={assessment.MainRiskCode}",
            "Provenance=True"));
    }

    private static void ValidatePositiveAnchorControl()
    {
        var fixture = CreateFixture(3000.0);
        var run = Run(fixture);
        var snapshot = run.Snapshot;
        var reaction = snapshot.SelectedAnchorReaction
            ?? throw new InvalidOperationException("BC-AUD-010 positive control: existing F2 reaction disappeared.");
        var assessment = snapshot.SelectedEngineeringAssessment
            ?? throw new InvalidOperationException("BC-AUD-010 positive control: existing F4 assessment disappeared.");

        Exact(run.Result.AnchorWeightWaterKg, 1770.0, "positive control derived AnchorWeightWaterKg");
        if (snapshot.SignedCandidate?.Status != MooringSignedCandidateStatus.Accepted ||
            reaction.ContactClassification != MooringAnchorContactClassification.CompressiveContact ||
            assessment.Verdict != "Требуется проверка" ||
            assessment.HasHardFailure ||
            assessment.MainRiskCode != "AnchorHorizontalCapacityRequiresAdditionalPhysicalModel" ||
            assessment.AnchorHorizontalCapacityDisposition != MooringAnchorHorizontalCapacityDisposition.RequiresAdditionalPhysicalModel)
        {
            throw new InvalidOperationException("BC-AUD-010 positive control: established F2/F4 semantics changed.");
        }

        Console.WriteLine(
            $"BC_AUD_010_POSITIVE_ANCHOR_CONTROL|AnchorWeightWaterKg={F(run.Result.AnchorWeightWaterKg)}|SignedStatus={snapshot.SignedCandidate.Status}|F2={reaction.ContactClassification}|Verdict={assessment.Verdict}|RiskCode={assessment.MainRiskCode}");
    }

    private static Fixture CreateFixture(double anchorWeightAirKg)
    {
        var environment = new EnvironmentInput(
            1025,
            20,
            0.2,
            0.2,
            6,
            SeabedCatalog.ById("sand"),
            true,
            new[]
            {
                new CurrentProfilePointInput(0, 0.2, 0, 0, 1025),
                new CurrentProfilePointInput(20, 0.2, 0, 0, 1025)
            });
        var buoy = new BuoyInput("Audit buoy", 1.0, 100, 0.10, 0.8);
        var anchor = new AnchorInput("Audit block", "Deadweight", "Concrete", anchorWeightAirKg, 1.2, 1.0);
        var rope = new RopePreset("audit:rope", "Audit rope", "Synthetic", 14, 70, 0.15, 1.0, "BC-AUD-010");
        var assembly = new[]
        {
            new AssemblyItemInput(AssemblyItemKind.Line, "Audit line", true, rope, null, 20.2, 1, 0, 0, 0, 0)
        };
        return new Fixture(environment, buoy, assembly, anchor, 3);
    }

    private static ApplicationCalculationRun Run(Fixture fixture) =>
        ApplicationCalculationRunner.Run(
            fixture.Environment,
            fixture.Buoy,
            fixture.Assembly,
            fixture.Anchor,
            fixture.SafetyFactor);

    private static void ValidatePdfBuild(UserEngineeringReportReadModel report, string label)
    {
        var path = Path.Combine(Path.GetTempPath(), "BuoyCalc-BC-AUD-010-" + Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            PdfReportBuilder.Build(path, report);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidOperationException($"BC-AUD-010 {label}: PDF was not generated from terminal typed authority.");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"BC-AUD-010 {label}: expected exact {F(expected)}, got {F(actual)}.");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record Fixture(
        EnvironmentInput Environment,
        BuoyInput Buoy,
        IReadOnlyList<AssemblyItemInput> Assembly,
        AnchorInput Anchor,
        double SafetyFactor);
}
