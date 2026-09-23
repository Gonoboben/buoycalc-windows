using BuoyCalc.Windows.ViewModels;

internal static class InputMutationInvalidatesLastRunAndBlocksEveryExportRegression
{
    public static void InputMutation_InvalidatesLastRunAndBlocksEveryExport()
    {
        Console.WriteLine("BC_AUD_002_INPUT_MUTATION_INVALIDATION_BEGIN");
        ValidateMutation("environment depth", vm => vm.Depth = "49");
        ValidateMutation("assembly line length", vm =>
        {
            var line = vm.AssemblyItems.First(x => x.IsLine);
            line.LengthM = "46";
        });
        ValidateMutation("buoy input", vm => vm.BuoyWeight = "81");
        ValidateMutation("anchor input", vm => vm.AnchorWeight = "501");
        ValidateMutation("current profile", vm => vm.CurrentProfilePoints[0].EastCurrentMS = "0.21");
        ValidateMutation("project name", vm => vm.ProjectName = "State B Project");
        Console.WriteLine(
            "BC_AUD_002_INPUT_MUTATION_INVALIDATION_ROLLUP|Scenarios=6|Environment=True|Assembly=True|Buoy=True|Anchor=True|CurrentProfile=True|ProjectIdentity=True|PdfBlockedUntilRecalculate=True|FullTxtBlockedUntilRecalculate=True|SelectedShapeCleared=True");
        Console.WriteLine("BC_AUD_002_INPUT_MUTATION_INVALIDATION_END");
    }

    private static void ValidateMutation(string name, Action<MainWindowViewModel> mutate)
    {
        var viewModel = CreateCalculatedViewModel();
        var priorReport = viewModel.UserEngineeringReport;
        var priorShape = viewModel.SelectedShape;
        var priorReportText = viewModel.ReportText;
        var priorResultText = viewModel.ResultText;

        RequireCurrentAuthority(viewModel, name, "before mutation");
        mutate(viewModel);

        if (viewModel.IsCalculationCurrent ||
            viewModel.UserEngineeringReport is not null ||
            viewModel.SelectedShape is not null ||
            !string.IsNullOrWhiteSpace(viewModel.ReportText))
        {
            throw new InvalidOperationException(
                $"BC-AUD-002 {name}: input mutation retained previous calculation authority. " +
                $"SameReport={ReferenceEquals(priorReport, viewModel.UserEngineeringReport)}, " +
                $"SameShape={ReferenceEquals(priorShape, viewModel.SelectedShape)}, " +
                $"SameFullTxt={priorReportText == viewModel.ReportText}.");
        }

        if (viewModel.ElementRows.Count != 0 ||
            viewModel.ResultText == priorResultText ||
            !viewModel.ResultText.Contains("Выполните расчёт повторно", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"BC-AUD-002 {name}: stale result presentation remains available after mutation.");
        }

        if (viewModel.CanExportPdf || viewModel.CanExportFullReport)
            throw new InvalidOperationException($"BC-AUD-002 {name}: at least one stale export remains available.");

        viewModel.CalculateCommand.Execute(null);
        RequireCurrentAuthority(viewModel, name, "after recalculation");

        if (ReferenceEquals(priorReport, viewModel.UserEngineeringReport) ||
            ReferenceEquals(priorShape, viewModel.SelectedShape))
        {
            throw new InvalidOperationException($"BC-AUD-002 {name}: recalculation did not publish new authority objects.");
        }

        Console.WriteLine(
            $"BC_AUD_002_INPUT_MUTATION_INVALIDATION|Mutation={name}|Invalidated=True|PdfBlocked=True|FullTxtBlocked=True|RecalculateRestored=True");
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
        return viewModel;
    }

    private static void RequireCurrentAuthority(MainWindowViewModel viewModel, string name, string phase)
    {
        if (!viewModel.IsCalculationCurrent ||
            !viewModel.CanExportPdf ||
            !viewModel.CanExportFullReport ||
            viewModel.UserEngineeringReport is null ||
            viewModel.SelectedShape is null ||
            string.IsNullOrWhiteSpace(viewModel.ReportText))
        {
            throw new InvalidOperationException(
                $"BC-AUD-002 {name}: accepted fixture did not expose report, selected shape, and Full TXT {phase}.");
        }
    }
}
