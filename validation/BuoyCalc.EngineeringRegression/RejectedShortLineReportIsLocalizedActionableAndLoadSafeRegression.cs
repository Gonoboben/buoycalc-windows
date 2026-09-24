using System.Reflection;
using System.Text;
using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;

internal static class RejectedShortLineReportIsLocalizedActionableAndLoadSafeRegression
{
    public static void RejectedShortLine_ReportIsLocalizedActionableAndLoadSafe()
    {
        Console.WriteLine("BC_AUD_009_EARLY_SHORT_LINE_BEGIN");
        ValidateCoreIsNotInvoked();
        ValidateCalculatedControls();
        ValidateApplicationPresentationAndExports();
        ValidateProjectReplay();

        if (CalculationRunFingerprint.InputSchema != "buoycalc-engineering-input/v1" ||
            CalculationRunFingerprint.ResultSchema != "buoycalc-engineering-result/v3")
            throw new InvalidOperationException("BC-AUD-009 changed the approved input v1/result v3 fingerprint schemas.");

        Console.WriteLine("BC_AUD_009_EARLY_SHORT_LINE_ROLLUP|DepthM=85|ActiveLineM=60|MinimumM=85|DeficitM=25|CoreCalls=0|SegmentRows=0|Outcome=PreflightPhysicalRejected|LocalizedActionable=True|CalculatedLoadsAbsent=True|SelectedXZAbsent=True|F1F2F3F4Absent=True|FullTxt=True|DedicatedPdf=True|BC_AUD_002=True|ProjectReplay=True|ExactDepthControl=True|GreaterDepthControl=True|ExistingToleranceControl=True|DisabledLineExcluded=True|NonLineExcluded=True|InputSchema=v1|ResultSchema=v3");
        Console.WriteLine("BC_AUD_009_EARLY_SHORT_LINE_END");
    }

    private static void ValidateCoreIsNotInvoked()
    {
        var fixture = CreateFixture(85, 60, includeExcludedItems: true);
        var coreCalls = 0;
        CalculationResult Calculate(EnvironmentInput environment, BuoyInput buoy, IReadOnlyList<AssemblyItemInput> assembly, AnchorInput anchor, double safetyFactor)
        {
            coreCalls++;
            return BuoyCalculator.Calculate(environment, buoy, assembly, anchor, safetyFactor);
        }

        var outcome = ApplicationCalculationRunner.RunOutcome(
            fixture.Environment, fixture.Buoy, fixture.Assembly, fixture.Anchor, fixture.SafetyFactor, Calculate);
        var rejected = outcome as PreflightPhysicalRejectedApplicationRunOutcome
            ?? throw new InvalidOperationException($"BC-AUD-009 85/60: expected PreflightPhysicalRejected, got {outcome.Kind}.");
        if (coreCalls != 0)
            throw new InvalidOperationException($"BC-AUD-009 85/60: calculation core was invoked {coreCalls} time(s).");

        AssertEvidence(rejected, 85, 60, 85, 25, "85/60 early preflight");
        AssertNoCalculatedAuthority(rejected);
        var repeated = ApplicationCalculationRunner.RunOutcome(
            fixture.Environment, fixture.Buoy, fixture.Assembly, fixture.Anchor, fixture.SafetyFactor);
        if (repeated is not PreflightPhysicalRejectedApplicationRunOutcome repeatedRejected ||
            rejected.Provenance.InputHash != repeatedRejected.Provenance.InputHash ||
            rejected.Provenance.ResultHash != repeatedRejected.Provenance.ResultHash ||
            rejected.Provenance.RunId == repeatedRejected.Provenance.RunId)
            throw new InvalidOperationException("BC-AUD-009 85/60: deterministic hashes or explicit-run RunId semantics changed.");

        Console.WriteLine($"BC_AUD_009_CORE_BYPASS|CoreCalls={coreCalls}|SegmentRows=0|Outcome={rejected.Kind}|InputHash={rejected.Provenance.InputHash}|ResultHash={rejected.Provenance.ResultHash}|RunId={rejected.Provenance.RunId}|RepeatedHashes=True|RunIdsDistinct=True");
    }

    private static void ValidateCalculatedControls()
    {
        AssertCalculatedControl(85, 85, "line equals depth");
        AssertCalculatedControl(85, 85.2, "line greater than depth");
        AssertCalculatedControl(85, 85 - 0.5e-9, "existing signed length tolerance");
    }

    private static void AssertCalculatedControl(double depthM, double lineLengthM, string label)
    {
        var fixture = CreateFixture(depthM, lineLengthM, includeExcludedItems: false);
        var coreCalls = 0;
        var outcome = ApplicationCalculationRunner.RunOutcome(
            fixture.Environment,
            fixture.Buoy,
            fixture.Assembly,
            fixture.Anchor,
            fixture.SafetyFactor,
            (environment, buoy, assembly, anchor, safetyFactor) =>
            {
                coreCalls++;
                return BuoyCalculator.Calculate(environment, buoy, assembly, anchor, safetyFactor);
            });

        if (outcome is not CalculatedApplicationRunOutcome calculated ||
            coreCalls != 1 || calculated.Calculation.Result is null || calculated.Calculation.Snapshot is null)
            throw new InvalidOperationException($"BC-AUD-009 control '{label}': existing calculated path did not execute exactly once.");
    }

    private static void ValidateApplicationPresentationAndExports()
    {
        var viewModel = CreateShortLineViewModel();
        viewModel.CalculateCommand.Execute(null);
        var report = viewModel.PreflightPhysicalRejectionReport
            ?? throw new InvalidOperationException("BC-AUD-009 UI: typed preflight report was not published.");
        AssertEvidence(report, 85, 60, 85, 25, "UI presentation");

        if (!viewModel.IsCalculationCurrent || !viewModel.CanExportPdf || !viewModel.CanExportFullReport ||
            viewModel.ApplicationRunReport is not PreflightPhysicalRejectionReportReadModel ||
            viewModel.UserEngineeringReport is not null || viewModel.SelectedShape is not null || viewModel.ElementRows.Count != 0)
            throw new InvalidOperationException("BC-AUD-009 UI: completed preflight authority is stale, non-exportable, or retains calculated/selected state.");

        foreach (var text in new[]
        {
            "Вердикт: Не подходит", "активная длина линии 60 м", "глубины постановки 85 м",
            "Минимальная активная длина линии для этой постановки: 85 м", "Дефицит длины: 25 м",
            "увеличьте суммарную активную длину линии как минимум на 25 м", "до не менее 85 м",
            "Preflight_LineShorterThanDepth"
        })
        {
            if (!viewModel.ResultText.Contains(text, StringComparison.Ordinal) && !viewModel.ReportText.Contains(text, StringComparison.Ordinal))
                throw new InvalidOperationException($"BC-AUD-009 presentation is missing '{text}'.");
        }

        foreach (var forbidden in new[]
        {
            "Суммарная сила течения", "Волновой horizontal proxy", "Legacy horizontal sum",
            "Чистая плавучесть", "Макс. натяжение", "Таблица сегментов"
        })
        {
            if (viewModel.ResultText.Contains(forbidden, StringComparison.Ordinal) || viewModel.ReportText.Contains(forbidden, StringComparison.Ordinal))
                throw new InvalidOperationException($"BC-AUD-009 preflight presentation published calculated field '{forbidden}'.");
        }

        AssertProvenanceText(report.Provenance, viewModel.ReportText);
        var pdfProvenance = PdfReportProvenanceReadModelProjector.Project(report.Provenance, DateTimeOffset.UtcNow);
        if (pdfProvenance.RunId != report.Provenance.RunId ||
            pdfProvenance.CalculationTimestampUtc != report.Provenance.CalculationTimestampUtc ||
            pdfProvenance.InputHash != report.Provenance.InputHash ||
            pdfProvenance.ResultHash != report.Provenance.ResultHash ||
            pdfProvenance.SourceIdentity != report.Provenance.SourceIdentity)
            throw new InvalidOperationException("BC-AUD-009 PDF projection did not retain the completed outcome provenance.");
        ValidatePdfArtifact(report, viewModel.ReportText);

        viewModel.Depth = "86";
        if (viewModel.IsCalculationCurrent || viewModel.ApplicationRunReport is not null ||
            viewModel.PreflightPhysicalRejectionReport is not null || viewModel.CanExportPdf ||
            viewModel.CanExportFullReport || viewModel.SelectedShape is not null || viewModel.ElementRows.Count != 0)
            throw new InvalidOperationException("BC-AUD-009/002: input mutation retained preflight export authority.");
    }

    private static void ValidatePdfArtifact(ApplicationRunReportReadModel report, string fullText)
    {
        var retainArtifact = string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);
        var directory = retainArtifact
            ? Path.Combine("artifacts", "bc-aud-009")
            : Path.Combine(Path.GetTempPath(), "BuoyCalc-BC-AUD-009-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var pdfPath = Path.Combine(directory, "bc-aud-009-short-line.pdf");
        var textPath = Path.Combine(directory, "bc-aud-009-short-line-full-report.txt");
        try
        {
            PdfReportBuilder.Build(pdfPath, report);
            File.WriteAllText(textPath, fullText, new UTF8Encoding(false));
            if (!File.Exists(pdfPath) || new FileInfo(pdfPath).Length == 0 || !File.Exists(textPath) || new FileInfo(textPath).Length == 0)
                throw new InvalidOperationException("BC-AUD-009: dedicated PDF/Full TXT artifacts were not generated.");
        }
        finally
        {
            if (!retainArtifact && Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void ValidateProjectReplay()
    {
        var source = CreateShortLineViewModel();
        source.CalculateCommand.Execute(null);
        var first = source.PreflightPhysicalRejectionReport
            ?? throw new InvalidOperationException("BC-AUD-009 replay: source preflight outcome is absent.");
        var json = JsonSerializer.Serialize(InvokeToDto(source));
        var dto = JsonSerializer.Deserialize<BuoyProjectDto>(json)
            ?? throw new InvalidOperationException("BC-AUD-009 replay: project JSON did not deserialize.");
        var replay = new MainWindowViewModel();
        InvokeFromDto(replay, dto);
        if (replay.IsCalculationCurrent || replay.ApplicationRunReport is not null)
            throw new InvalidOperationException("BC-AUD-009/002 replay: project load retained completed authority.");

        replay.CalculateCommand.Execute(null);
        var second = replay.PreflightPhysicalRejectionReport
            ?? throw new InvalidOperationException("BC-AUD-009 replay: restored project did not reproduce preflight rejection.");
        AssertEvidence(second, 85, 60, 85, 25, "project replay");
        if (first.Provenance.InputHash != second.Provenance.InputHash ||
            first.Provenance.ResultHash != second.Provenance.ResultHash ||
            first.Provenance.RunId == second.Provenance.RunId)
            throw new InvalidOperationException("BC-AUD-009 replay: canonical hashes changed or explicit Calculate reused RunId.");
    }

    private static MainWindowViewModel CreateShortLineViewModel()
    {
        var viewModel = new MainWindowViewModel { ProjectName = "BC-AUD-009 short-line project", Depth = "85" };
        viewModel.AddCurrentProfilePointCommand.Execute(null);
        viewModel.AddCurrentProfilePointCommand.Execute(null);
        viewModel.CurrentProfilePoints[0].DepthM = "0";
        viewModel.CurrentProfilePoints[0].EastCurrentMS = "0.20";
        viewModel.CurrentProfilePoints[1].DepthM = "85";
        viewModel.CurrentProfilePoints[1].EastCurrentMS = "0.20";
        var lines = viewModel.AssemblyItems.Where(x => x.IsLine).ToArray();
        lines[0].LengthM = "50";
        lines[1].LengthM = "10";
        return viewModel;
    }

    private static Fixture CreateFixture(double depthM, double activeLineLengthM, bool includeExcludedItems)
    {
        var environment = new EnvironmentInput(1025, depthM, 0.2, 0.5, 6, SeabedCatalog.ById("sand"), true,
            new[] { new CurrentProfilePointInput(0, 0.2, 0, 0, 1025), new CurrentProfilePointInput(depthM, 0.2, 0, 0, 1025) });
        var buoy = new BuoyInput("BC-AUD-009 buoy", 1, 100, 0.1, 0.8);
        var anchor = new AnchorInput("BC-AUD-009 anchor", "Deadweight", "Concrete", 3000, 1.2, 1);
        var rope = new RopePreset("audit:bc-aud-009-rope", "BC-AUD-009 rope", "Synthetic", 14, 70, 0.15, 1, "BC-AUD-009");
        var assembly = new List<AssemblyItemInput>
        {
            new(AssemblyItemKind.Line, "Active line", true, rope, null, activeLineLengthM, 1, 0, 0, 0, 0)
        };
        if (includeExcludedItems)
        {
            assembly.Add(new AssemblyItemInput(AssemblyItemKind.Line, "Disabled line", false, rope, null, 1000, 1, 0, 0, 0, 0));
            assembly.Add(new AssemblyItemInput(AssemblyItemKind.Payload, "Non-line length", true, null, null, 500, 1, 10, 0.01, 0.02, 0.8));
        }
        return new Fixture(environment, buoy, assembly, anchor, 3);
    }

    private static void AssertEvidence(PreflightPhysicalRejectedApplicationRunOutcome outcome, double depth, double activeLine, double minimum, double deficit, string context)
    {
        var evidence = outcome.Rejection.Evidence;
        if (outcome.Kind != ApplicationRunOutcomeKind.PreflightPhysicalRejected ||
            outcome.Rejection.Classification != PreflightPhysicalRejectionKind.LineShorterThanDepth ||
            outcome.Rejection.Verdict != "Не подходит" || !outcome.Rejection.HasHardFailure ||
            !outcome.Rejection.BlocksEngineeringGeometry || evidence.DepthM != depth ||
            evidence.AvailableActiveLineLengthM != activeLine || evidence.MinimumRequiredActiveLineLengthM != minimum || evidence.DeficitM != deficit)
            throw new InvalidOperationException($"BC-AUD-009 {context}: typed rejection evidence changed.");
    }

    private static void AssertEvidence(PreflightPhysicalRejectionReportReadModel report, double depth, double activeLine, double minimum, double deficit, string context)
    {
        var evidence = report.Evidence;
        if (report.Classification != PreflightPhysicalRejectionKind.LineShorterThanDepth || report.Verdict != "Не подходит" ||
            !report.HasHardFailure || !report.BlocksEngineeringGeometry || evidence.DepthM != depth ||
            evidence.AvailableActiveLineLengthM != activeLine || evidence.MinimumRequiredActiveLineLengthM != minimum || evidence.DeficitM != deficit)
            throw new InvalidOperationException($"BC-AUD-009 {context}: presentation evidence changed.");
    }

    private static void AssertNoCalculatedAuthority(PreflightPhysicalRejectedApplicationRunOutcome outcome)
    {
        var forbidden = new HashSet<string>(StringComparer.Ordinal)
        {
            "Result", "CalculationResult", "Snapshot", "CalculationSnapshot", "SelectedShape",
            "SelectedDesignEnvelope", "SelectedDesignTensionDemand", "SelectedAnchorReaction",
            "SelectedLocalStructuralCapacity", "SelectedEngineeringAssessment", "SegmentRows",
            "CurrentForceN", "WaveForceN", "HorizontalForceN", "MaxTensionN"
        };
        var exposed = outcome.GetType().GetProperties().Select(x => x.Name)
            .Concat(outcome.Rejection.GetType().GetProperties().Select(x => x.Name)).Where(forbidden.Contains).ToArray();
        if (exposed.Length != 0)
            throw new InvalidOperationException("BC-AUD-009 preflight outcome exposes calculated authority: " + string.Join(", ", exposed));
    }

    private static void AssertProvenanceText(CalculationRunProvenance provenance, string text)
    {
        foreach (var value in new[]
        {
            provenance.RunId, provenance.InputHash, provenance.ResultHash, provenance.SourceIdentity,
            provenance.CalculationTimestampUtc.ToUniversalTime().ToString("O")
        })
        {
            if (!text.Contains(value, StringComparison.Ordinal))
                throw new InvalidOperationException($"BC-AUD-009 Full TXT lost provenance value '{value}'.");
        }
    }

    private static BuoyProjectDto InvokeToDto(MainWindowViewModel viewModel)
    {
        var method = typeof(MainWindowViewModel).GetMethod("ToDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-009 replay: ToDto boundary not found.");
        return (BuoyProjectDto)(method.Invoke(viewModel, null) ?? throw new InvalidOperationException("BC-AUD-009 replay: ToDto returned null."));
    }

    private static void InvokeFromDto(MainWindowViewModel viewModel, BuoyProjectDto dto)
    {
        var method = typeof(MainWindowViewModel).GetMethod("FromDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-009 replay: FromDto boundary not found.");
        method.Invoke(viewModel, new object[] { dto });
    }

    private sealed record Fixture(EnvironmentInput Environment, BuoyInput Buoy, IReadOnlyList<AssemblyItemInput> Assembly, AnchorInput Anchor, double SafetyFactor);
}
