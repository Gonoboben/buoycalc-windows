using System.Globalization;
using System.Reflection;
using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;

internal static class DuplicateCurrentProfileDepthsBlockBeforeCoreRegression
{
    private const string BlockingMarker = "BC-AUD-007";

    private static readonly CurrentProfilePointInput A = new(0, 0.1, 0, 0, 1025);
    private static readonly CurrentProfilePointInput B = new(0, 0.8, 0, 0, 1025);
    private static readonly CurrentProfilePointInput C = new(20, 0.2, 0, 0, 1025);

    public static void DuplicateCurrentProfileDepths_BlockBeforeCore()
    {
        Console.WriteLine("BC_AUD_007_DUPLICATE_DEPTH_PRECORE_BEGIN");

        var outcomes = DuplicatePermutations()
            .Select(permutation => Execute(permutation.Name, permutation.Points))
            .ToArray();

        foreach (var outcome in outcomes)
        {
            Console.WriteLine(outcome.CompletedRun is null
                ? $"BC_AUD_007_DUPLICATE_BLOCKED|Permutation={outcome.Name}|CompletedRun=False|Provenance=False|Failure={outcome.Failure?.Message}"
                : string.Join(
                    "|",
                    "BC_AUD_007_PRE_FIX_ACCEPTED",
                    $"Permutation={outcome.Name}",
                    $"CurrentForceN={F(outcome.CompletedRun.Result.CurrentForceN)}",
                    $"Verdict={outcome.CompletedRun.Result.Verdict}",
                    $"SignedStatus={outcome.CompletedRun.Snapshot.SignedCandidate?.Status}",
                    $"RunId={outcome.CompletedRun.Snapshot.Provenance?.RunId}"));
        }

        var completed = outcomes.Where(x => x.CompletedRun is not null).ToArray();
        if (completed.Length > 0)
        {
            var forceCount = completed
                .Select(x => x.CompletedRun!.Result.CurrentForceN)
                .Distinct()
                .Count();
            var statusCount = completed
                .Select(x => x.CompletedRun!.Snapshot.SignedCandidate?.Status)
                .Distinct()
                .Count();
            Console.WriteLine(
                $"BC_AUD_007_PRE_FIX_ROLLUP|CompletedRuns={completed.Length}|DistinctForces={forceCount}|DistinctSignedStatuses={statusCount}");
            throw new InvalidOperationException(
                $"BC-AUD-007 failing-first: {completed.Length} duplicate-depth permutations created completed calculation/provenance authority; distinct forces={forceCount}; distinct signed statuses={statusCount}.");
        }

        if (outcomes.Any(x =>
                x.Failure is not CurrentProfileValidationException validationFailure ||
                validationFailure.Code != CurrentProfileRequirement.DuplicateDepthCode ||
                validationFailure.DuplicateDepthM != 0 ||
                !validationFailure.Message.Contains(BlockingMarker, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "BC-AUD-007: duplicate-depth request was not rejected by the stable production validation contract.");
        }

        ValidateUniqueDepthPermutationControls();
        ValidateUiInvalidationAndFailure();
        ValidateProjectReplayInteraction();

        Console.WriteLine(
            "BC_AUD_007_DUPLICATE_DEPTH_PRECORE_ROLLUP|Permutations=6|CompletedRuns=0|ProvenanceCreated=False|UniquePermutationInputHashEqual=True|UniquePermutationResultHashEqual=True|SignedCurrentsPreserved=True|BC_AUD_002=True|BC_AUD_003=True|BC_AUD_004=True|BC_AUD_005=True");
        Console.WriteLine("BC_AUD_007_DUPLICATE_DEPTH_PRECORE_END");
    }

    private static Outcome Execute(string name, IReadOnlyList<CurrentProfilePointInput> points)
    {
        try
        {
            return new Outcome(name, Run(points), null);
        }
        catch (Exception ex)
        {
            return new Outcome(name, null, ex);
        }
    }

    private static ApplicationCalculationRun Run(IReadOnlyList<CurrentProfilePointInput> points)
    {
        var environment = new EnvironmentInput(
            1025,
            20,
            0.8,
            0.2,
            6,
            SeabedCatalog.ById("sand"),
            true,
            points);
        var rope = new RopePreset(
            "audit:rope",
            "Audit rope",
            "Synthetic",
            14,
            70,
            0.15,
            1.0,
            "BC-AUD-007");
        var assembly = new[]
        {
            new AssemblyItemInput(AssemblyItemKind.Line, "Audit line", true, rope, null, 20.2, 1, 0, 0, 0, 0)
        };

        return ApplicationCalculationRunner.Run(
            environment,
            new BuoyInput("Audit buoy", 1.0, 100, 0.10, 0.8),
            assembly,
            new AnchorInput("Audit block", "Deadweight", "Concrete", 3000, 1.2, 1.0),
            3);
    }

    private static void ValidateUniqueDepthPermutationControls()
    {
        var point0 = new CurrentProfilePointInput(0, -0.3, -0.2, -0.04, 1025);
        var point10 = new CurrentProfilePointInput(10, 0.1, -0.1, 0.02, 1025);
        var point20 = new CurrentProfilePointInput(20, 0.2, 0, 0, 1025);
        if (!CurrentProfileRequirement.IsUsable(new[]
            {
                point10,
                point10 with { DepthM = 10.000001 }
            }))
        {
            throw new InvalidOperationException(
                "BC-AUD-007 positive control: distinct near depths were rejected as duplicates.");
        }

        var first = CreateCalculatedViewModel(new[] { point0, point10, point20 });
        var reordered = CreateCalculatedViewModel(new[] { point20, point0, point10 });
        var firstReport = first.UserEngineeringReport
            ?? throw new InvalidOperationException("BC-AUD-007 positive control: first valid profile produced no report authority.");
        var reorderedReport = reordered.UserEngineeringReport
            ?? throw new InvalidOperationException("BC-AUD-007 positive control: reordered valid profile produced no report authority.");

        if (firstReport.Provenance.RunId == reorderedReport.Provenance.RunId ||
            firstReport.Provenance.InputHash != reorderedReport.Provenance.InputHash ||
            firstReport.Provenance.ResultHash != reorderedReport.Provenance.ResultHash ||
            firstReport.Calculation.CurrentForceN != reorderedReport.Calculation.CurrentForceN)
        {
            throw new InvalidOperationException(
                "BC-AUD-007 positive control: a unique-depth row permutation changed canonical input/result identity or reused RunId.");
        }

        var canonical = reorderedReport.Environment.CurrentProfile;
        if (!canonical.Select(x => x.DepthM).SequenceEqual(new[] { 0d, 10d, 20d }) ||
            canonical[0].EastCurrentMS != -0.3 ||
            canonical[0].NorthCurrentMS != -0.2 ||
            canonical[0].VerticalCurrentMS != -0.04)
        {
            throw new InvalidOperationException(
                "BC-AUD-007/004 positive control: unique profile was not canonically sorted or signed currents were changed.");
        }

        Console.WriteLine(
            $"BC_AUD_007_UNIQUE_PERMUTATION|Orders=0,10,20;20,0,10|InputHash={firstReport.Provenance.InputHash}|ResultHash={firstReport.Provenance.ResultHash}|CurrentForceN={F(firstReport.Calculation.CurrentForceN)}|RunIdsDistinct=True|SignedCurrentsPreserved=True");
    }

    private static void ValidateUiInvalidationAndFailure()
    {
        var viewModel = CreateCalculatedViewModel(new[]
        {
            new CurrentProfilePointInput(0, 0.2, 0, 0, 1025),
            new CurrentProfilePointInput(10, 0.2, 0, 0, 1025),
            new CurrentProfilePointInput(20, 0.2, 0, 0, 1025)
        });
        var runA = viewModel.UserEngineeringReport?.Provenance
            ?? throw new InvalidOperationException("BC-AUD-007/002: valid Run A has no provenance.");

        viewModel.CurrentProfilePoints[1].DepthM = "0";
        if (viewModel.IsCalculationCurrent || viewModel.CanExportPdf || viewModel.CanExportFullReport)
            throw new InvalidOperationException("BC-AUD-007/002: duplicate-depth mutation retained Run A/export authority.");

        viewModel.CalculateCommand.Execute(null);
        if (viewModel.IsCalculationCurrent ||
            viewModel.UserEngineeringReport is not null ||
            viewModel.SelectedShape is not null ||
            viewModel.CanExportPdf ||
            viewModel.CanExportFullReport ||
            !viewModel.ResultText.Contains(BlockingMarker, StringComparison.Ordinal) ||
            !viewModel.ResultText.Contains("глубине 0 м", StringComparison.Ordinal) ||
            !viewModel.CurrentProfileSummary.Contains("глубине 0 м", StringComparison.Ordinal) ||
            viewModel.ResultText.Contains(runA.RunId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"BC-AUD-007/002/005: failed Calculate published authority or omitted the duplicate-depth diagnostic. Result='{viewModel.ResultText}'.");
        }

        Console.WriteLine(
            "BC_AUD_007_UI_FAILURE|RunAInvalidated=True|CompletedRun=False|SelectedShape=False|Report=False|PdfBlocked=True|FullTxtBlocked=True|DuplicateDepthIdentified=0");
    }

    private static void ValidateProjectReplayInteraction()
    {
        var source = new MainWindowViewModel();
        ConfigureProfile(source, new[] { A, B, C });
        var json = JsonSerializer.Serialize(InvokeToDto(source));
        var dto = JsonSerializer.Deserialize<BuoyProjectDto>(json)
            ?? throw new InvalidOperationException("BC-AUD-007/003: project JSON did not deserialize.");
        var restored = new MainWindowViewModel();
        InvokeFromDto(restored, dto);

        var actual = restored.CurrentProfilePoints
            .Select(x => (x.DepthM, x.EastCurrentMS))
            .ToArray();
        var expected = new[] { ("0", "0.1"), ("0", "0.8"), ("20", "0.2") };
        if (!actual.SequenceEqual(expected))
            throw new InvalidOperationException("BC-AUD-007/003: persistence deleted, merged, or reordered duplicate profile rows.");

        restored.CalculateCommand.Execute(null);
        if (restored.IsCalculationCurrent ||
            restored.UserEngineeringReport is not null ||
            restored.SelectedShape is not null ||
            restored.CanExportPdf ||
            restored.CanExportFullReport ||
            !restored.ResultText.Contains(BlockingMarker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("BC-AUD-007/003: restored duplicate profile obtained calculation authority.");
        }

        Console.WriteLine(
            "BC_AUD_007_PROJECT_REPLAY|RowsRetained=3|OrderRetained=0:0.1,0:0.8,20:0.2|SilentMerge=False|CalculateBlocked=True");
    }

    private static MainWindowViewModel CreateCalculatedViewModel(IReadOnlyList<CurrentProfilePointInput> points)
    {
        var viewModel = new MainWindowViewModel();
        ConfigureProfile(viewModel, points);
        viewModel.CalculateCommand.Execute(null);
        if (!viewModel.IsCalculationCurrent ||
            !viewModel.CanExportPdf ||
            !viewModel.CanExportFullReport ||
            viewModel.UserEngineeringReport?.Provenance is null)
        {
            throw new InvalidOperationException(
                $"BC-AUD-007 positive control: valid unique profile did not create current authority. Result='{viewModel.ResultText}'.");
        }

        return viewModel;
    }

    private static void ConfigureProfile(
        MainWindowViewModel viewModel,
        IReadOnlyList<CurrentProfilePointInput> points)
    {
        foreach (var point in points)
        {
            viewModel.AddCurrentProfilePointCommand.Execute(null);
            var row = viewModel.CurrentProfilePoints[^1];
            row.DepthM = F(point.DepthM);
            row.EastCurrentMS = F(point.EastCurrentMS);
            row.NorthCurrentMS = F(point.NorthCurrentMS);
            row.VerticalCurrentMS = F(point.VerticalCurrentMS);
            row.WaterDensityKgM3 = F(point.WaterDensityKgM3);
        }
    }

    private static BuoyProjectDto InvokeToDto(MainWindowViewModel viewModel)
    {
        var method = typeof(MainWindowViewModel).GetMethod("ToDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-007/003: MainWindowViewModel.ToDto was not found.");
        return (BuoyProjectDto)(method.Invoke(viewModel, null)
            ?? throw new InvalidOperationException("BC-AUD-007/003: MainWindowViewModel.ToDto returned null."));
    }

    private static void InvokeFromDto(MainWindowViewModel viewModel, BuoyProjectDto dto)
    {
        var method = typeof(MainWindowViewModel).GetMethod("FromDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-007/003: MainWindowViewModel.FromDto was not found.");
        try
        {
            method.Invoke(viewModel, new object[] { dto });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static IReadOnlyList<Permutation> DuplicatePermutations() =>
        new[]
        {
            new Permutation("A,B,C", new[] { A, B, C }),
            new Permutation("B,A,C", new[] { B, A, C }),
            new Permutation("C,A,B", new[] { C, A, B }),
            new Permutation("C,B,A", new[] { C, B, A }),
            new Permutation("A,C,B", new[] { A, C, B }),
            new Permutation("B,C,A", new[] { B, C, A })
        };

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record Permutation(string Name, IReadOnlyList<CurrentProfilePointInput> Points);
    private sealed record Outcome(string Name, ApplicationCalculationRun? CompletedRun, Exception? Failure);
}
