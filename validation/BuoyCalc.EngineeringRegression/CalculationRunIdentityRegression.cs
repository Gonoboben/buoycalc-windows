using System.Globalization;
using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;

internal static class CalculationRunIdentityRegression
{
    public static void CalculationRunIdentity_PropagatesExactlyAcrossArtifacts()
    {
        Console.WriteLine("BC_AUD_005_CALCULATION_RUN_IDENTITY_BEGIN");

        var fixtureA = CreateFixture(20.2);
        var beforeRun = DateTimeOffset.UtcNow;
        var runA = Run(fixtureA);
        var provenanceA = RequireProvenance(runA, "Run A");
        RequireComplete(provenanceA, beforeRun, "Run A");

        var typedA = UserEngineeringReportReadModelProjector.Project(
            "BC-AUD-005 Run A",
            fixtureA.Environment,
            fixtureA.Buoy,
            fixtureA.Anchor,
            runA.Snapshot);
        Same(provenanceA, typedA.Provenance, "typed report");

        var exportTimeA = DateTimeOffset.Parse(
            "2026-09-23T10:00:00Z",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);
        var exportTimeB = exportTimeA.AddMinutes(5);
        var pdfA = PdfReportProvenanceReadModelProjector.Project(typedA.Provenance, exportTimeA);
        var pdfB = PdfReportProvenanceReadModelProjector.Project(typedA.Provenance, exportTimeB);
        Same(provenanceA, pdfA, "PDF provenance input A");
        Same(provenanceA, pdfB, "PDF provenance input B");
        if (pdfA.ExportTimestampUtc == pdfB.ExportTimestampUtc ||
            pdfA.CalculationTimestampUtc != pdfB.CalculationTimestampUtc)
        {
            throw new InvalidOperationException(
                "BC-AUD-005: PDF export time is not separate from retained calculation time.");
        }
        ValidateRepeatedPdfExport(typedA, provenanceA);

        var fullTextA = TechnicalReportBuilder.Build(
            "BC-AUD-005 Run A",
            fixtureA.Environment,
            fixtureA.Buoy,
            fixtureA.Anchor,
            runA.Snapshot);
        AssertFullText(provenanceA, fullTextA);
        var fullTextARepeat = TechnicalReportBuilder.Build(
            "BC-AUD-005 Run A",
            fixtureA.Environment,
            fixtureA.Buoy,
            fixtureA.Anchor,
            runA.Snapshot);
        AssertFullText(provenanceA, fullTextARepeat);
        Same(provenanceA, RequireProvenance(runA, "Run A after repeated export"), "repeated Full TXT export");

        var runARepeat = Run(fixtureA);
        var provenanceARepeat = RequireProvenance(runARepeat, "Run A repeated Calculate");
        if (provenanceA.RunId == provenanceARepeat.RunId)
            throw new InvalidOperationException("BC-AUD-005: separate Calculate calls reused a RunId.");
        Equal(provenanceA.InputHash, provenanceARepeat.InputHash, "deterministic InputHash");
        Equal(provenanceA.ResultHash, provenanceARepeat.ResultHash, "deterministic ResultHash");

        var fixtureB = CreateFixture(20.3);
        var runB = Run(fixtureB);
        var provenanceB = RequireProvenance(runB, "Run B");
        if (provenanceA.RunId == provenanceB.RunId)
            throw new InvalidOperationException("BC-AUD-005: Run B reused Run A identity.");
        NotEqual(provenanceA.InputHash, provenanceB.InputHash, "calculation-relevant input mutation");
        NotEqual(provenanceA.ResultHash, provenanceB.ResultHash, "authoritative result mutation");

        ValidateBcAud002Lifecycle();
        ValidateProjectNameContract();
        ValidateInputOnlyPersistenceContract();

        Console.WriteLine(
            $"BC_AUD_005_CALCULATION_RUN_IDENTITY|RunA={provenanceA.RunId}|RunB={provenanceB.RunId}|InputHashChanged=True|ResultHashChanged=True|RepeatedExportStable=True|SameInputHashDeterministic=True|SameResultHashDeterministic=True|Source={provenanceA.SourceIdentity}");
        Console.WriteLine("BC_AUD_005_CALCULATION_RUN_IDENTITY_END");
    }

    private static void ValidateBcAud002Lifecycle()
    {
        var viewModel = CreateCalculatedViewModel();
        var runA = viewModel.UserEngineeringReport?.Provenance
            ?? throw new InvalidOperationException("BC-AUD-005/002: Run A provenance missing in UI authority.");

        var line = viewModel.AssemblyItems.First(x => x.IsLine);
        var originalLength = double.Parse(line.LengthM, CultureInfo.InvariantCulture);
        line.LengthM = (originalLength + 1).ToString("R", CultureInfo.InvariantCulture);

        if (viewModel.IsCalculationCurrent ||
            viewModel.UserEngineeringReport is not null ||
            viewModel.CanExportPdf ||
            viewModel.CanExportFullReport)
        {
            throw new InvalidOperationException(
                "BC-AUD-005/002: input mutation retained Run A or an export path.");
        }

        viewModel.CalculateCommand.Execute(null);
        var runB = viewModel.UserEngineeringReport?.Provenance
            ?? throw new InvalidOperationException("BC-AUD-005/002: recalculation did not publish Run B provenance.");
        NotEqual(runA.RunId, runB.RunId, "BC-AUD-002 recalculation RunId");
        NotEqual(runA.InputHash, runB.InputHash, "BC-AUD-002 recalculation InputHash");
    }

    private static void ValidateRepeatedPdfExport(
        UserEngineeringReportReadModel report,
        CalculationRunProvenance provenance)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "BuoyCalc-BC-AUD-005-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var pathA = Path.Combine(directory, "destination-a.pdf");
            var pathB = Path.Combine(directory, "presentation-only-destination-b.pdf");
            PdfReportBuilder.Build(pathA, report);
            PdfReportBuilder.Build(pathB, report);

            if (!File.Exists(pathA) || new FileInfo(pathA).Length == 0 ||
                !File.Exists(pathB) || new FileInfo(pathB).Length == 0)
            {
                throw new InvalidOperationException("BC-AUD-005: repeated PDF export did not create both artifacts.");
            }

            Same(provenance, report.Provenance, "repeated PDF export");
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static void ValidateProjectNameContract()
    {
        var viewModel = CreateCalculatedViewModel();
        var runA = viewModel.UserEngineeringReport?.Provenance
            ?? throw new InvalidOperationException("BC-AUD-005 project identity: Run A missing.");

        viewModel.ProjectName = "Presentation identity B";
        if (viewModel.IsCalculationCurrent || viewModel.UserEngineeringReport is not null)
            throw new InvalidOperationException("BC-AUD-005 project identity: rename did not invalidate presentation authority.");

        viewModel.CalculateCommand.Execute(null);
        var reportB = viewModel.UserEngineeringReport
            ?? throw new InvalidOperationException("BC-AUD-005 project identity: Run B missing.");
        NotEqual(runA.RunId, reportB.Provenance.RunId, "project rename recalculation RunId");
        Equal(runA.InputHash, reportB.Provenance.InputHash, "ProjectName exclusion from engineering InputHash");
        if (reportB.ProjectName != "Presentation identity B")
            throw new InvalidOperationException("BC-AUD-005 project identity: typed report did not retain new metadata identity.");
    }

    private static void ValidateInputOnlyPersistenceContract()
    {
        var provenanceMembers = typeof(BuoyProjectDto)
            .GetProperties()
            .Where(x => x.Name.Contains("Run", StringComparison.OrdinalIgnoreCase) ||
                        x.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase) ||
                        x.Name.Contains("CalculationTimestamp", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name)
            .ToArray();
        if (provenanceMembers.Length != 0)
        {
            throw new InvalidOperationException(
                "BC-AUD-005: input-only project DTO unexpectedly claims retained calculation authority: " +
                string.Join(", ", provenanceMembers));
        }

        const string oldProject = "{\"ProjectName\":\"Legacy\",\"WaterDensity\":\"1025\",\"Depth\":\"20\"}";
        if (JsonSerializer.Deserialize<BuoyProjectDto>(oldProject) is null)
            throw new InvalidOperationException("BC-AUD-005: backward-compatible input-only project JSON no longer opens.");
    }

    private static MainWindowViewModel CreateCalculatedViewModel()
    {
        var viewModel = new MainWindowViewModel();
        viewModel.AddCurrentProfilePointCommand.Execute(null);
        viewModel.AddCurrentProfilePointCommand.Execute(null);
        viewModel.CurrentProfilePoints[0].DepthM = "0";
        viewModel.CurrentProfilePoints[0].EastCurrentMS = "0.20";
        viewModel.CurrentProfilePoints[1].DepthM = viewModel.Depth;
        viewModel.CurrentProfilePoints[1].EastCurrentMS = "0.20";
        viewModel.CalculateCommand.Execute(null);
        if (!viewModel.IsCalculationCurrent ||
            !viewModel.CanExportPdf ||
            !viewModel.CanExportFullReport)
        {
            throw new InvalidOperationException("BC-AUD-005: UI fixture did not produce current export authority.");
        }

        return viewModel;
    }

    private static Fixture CreateFixture(double lineLengthM)
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
        var anchor = new AnchorInput("Audit block", "Deadweight", "Concrete", 3000, 1.2, 1.0);
        var rope = new RopePreset("audit:rope", "Audit rope", "Synthetic", 14, 70, 0.15, 1.0, "BC-AUD-005");
        var assembly = new[]
        {
            new AssemblyItemInput(AssemblyItemKind.Line, "Audit line", true, rope, null, lineLengthM, 1, 0, 0, 0, 0)
        };
        return new Fixture(environment, buoy, assembly, anchor, 3);
    }

    private static ApplicationCalculationRun Run(Fixture fixture)
    {
        return ApplicationCalculationRunner.Run(
            fixture.Environment,
            fixture.Buoy,
            fixture.Assembly,
            fixture.Anchor,
            fixture.SafetyFactor);
    }

    private static CalculationRunProvenance RequireProvenance(
        ApplicationCalculationRun run,
        string context)
    {
        return run.Snapshot.Provenance
            ?? throw new InvalidOperationException($"BC-AUD-005: {context} has no retained provenance.");
    }

    private static void RequireComplete(
        CalculationRunProvenance provenance,
        DateTimeOffset notBefore,
        string context)
    {
        if (!Guid.TryParseExact(provenance.RunId, "D", out _) ||
            provenance.CalculationTimestampUtc < notBefore ||
            provenance.CalculationTimestampUtc > DateTimeOffset.UtcNow ||
            provenance.CalculationTimestampUtc.Offset != TimeSpan.Zero ||
            provenance.InputHash.Length != 64 ||
            provenance.ResultHash.Length != 64 ||
            string.IsNullOrWhiteSpace(provenance.SourceIdentity))
        {
            throw new InvalidOperationException($"BC-AUD-005: {context} provenance is incomplete or malformed.");
        }
    }

    private static void AssertFullText(CalculationRunProvenance provenance, string fullText)
    {
        string[] expected =
        {
            "## Calculation run provenance",
            $"- Run ID: {provenance.RunId}",
            $"- Calculation timestamp UTC: {provenance.CalculationTimestampUtc.ToUniversalTime():O}",
            $"- Input hash (SHA-256): {provenance.InputHash}",
            $"- Result hash (SHA-256): {provenance.ResultHash}",
            $"- Source identity: {provenance.SourceIdentity}",
            $"- Input schema: {CalculationRunFingerprint.InputSchema}",
            $"- Result schema: {CalculationRunFingerprint.ResultSchema}"
        };
        foreach (var line in expected)
        {
            if (!fullText.Contains(line, StringComparison.Ordinal))
                throw new InvalidOperationException($"BC-AUD-005: Full TXT provenance mismatch: {line}");
        }
    }

    private static void Same(
        CalculationRunProvenance expected,
        CalculationRunProvenance actual,
        string consumer)
    {
        if (!ReferenceEquals(expected, actual) || expected != actual)
            throw new InvalidOperationException($"BC-AUD-005: {consumer} did not retain exact snapshot provenance authority.");
    }

    private static void Same(
        CalculationRunProvenance expected,
        PdfReportProvenanceReadModel actual,
        string consumer)
    {
        if (expected.RunId != actual.RunId ||
            expected.CalculationTimestampUtc != actual.CalculationTimestampUtc ||
            expected.InputHash != actual.InputHash ||
            expected.ResultHash != actual.ResultHash ||
            expected.SourceIdentity != actual.SourceIdentity)
        {
            throw new InvalidOperationException($"BC-AUD-005: {consumer} differs from snapshot provenance.");
        }
    }

    private static void Equal(string expected, string actual, string context)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException($"BC-AUD-005: {context} is not deterministic.");
    }

    private static void NotEqual(string first, string second, string context)
    {
        if (string.Equals(first, second, StringComparison.Ordinal))
            throw new InvalidOperationException($"BC-AUD-005: {context} did not change provenance.");
    }

    private sealed record Fixture(
        EnvironmentInput Environment,
        BuoyInput Buoy,
        IReadOnlyList<AssemblyItemInput> Assembly,
        AnchorInput Anchor,
        double SafetyFactor);
}
