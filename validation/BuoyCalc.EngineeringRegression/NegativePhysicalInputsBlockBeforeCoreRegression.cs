using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.ViewModels;

internal static class NegativePhysicalInputsBlockBeforeCoreRegression
{
    private const string BlockingMarker = "BC-AUD-004";

    public static void NegativePhysicalInputs_BlockBeforeCore()
    {
        Console.WriteLine("BC_AUD_004_PRECORE_VALIDATION_BEGIN");

        foreach (var testCase in InvalidTypedCases())
        {
            RequireBlockedBeforeCompletedRun(testCase.Name, testCase.Request);
        }

        foreach (var rawValue in new[] { "abc", "NaN", "Infinity", "-Infinity" })
        {
            RequireRawUiValueBlocked(rawValue);
        }

        ValidateSignedPositiveControls();
        ValidateBcAud002Interaction();
        ValidateBcAud003Interaction();

        Console.WriteLine(
            "BC_AUD_004_PRECORE_VALIDATION_ROLLUP|TypedInvalidCases=10|RawInvalidCases=4|CompletedRuns=0|ProvenanceCreated=False|SignedCurrentsPreserved=True|SignedWeightWaterPreserved=True|BC_AUD_002=True|BC_AUD_003=True|BC_AUD_005=True");
        Console.WriteLine("BC_AUD_004_PRECORE_VALIDATION_END");
    }

    private static IReadOnlyList<InvalidCase> InvalidTypedCases()
    {
        var baseline = ValidRequest();
        var line = baseline.AssemblyItems.Single(x => x.Kind == AssemblyItemKind.Line);
        var payload = baseline.AssemblyItems.Single(x => x.Kind == AssemblyItemKind.Payload);

        return new[]
        {
            Case("payload mass -20 kg", baseline with
            {
                AssemblyItems = Replace(baseline, payload, payload with { PayloadWeightAirKg = -20 })
            }),
            Case("negative rope diameter", baseline with
            {
                AssemblyItems = Replace(baseline, line, line with
                {
                    RopePreset = line.RopePreset! with { DiameterMm = -1 }
                })
            }),
            Case("negative rope Cd", baseline with
            {
                AssemblyItems = Replace(baseline, line, line with
                {
                    RopePreset = line.RopePreset! with { DragCoefficient = -1 }
                })
            }),
            Case("negative payload area", baseline with
            {
                AssemblyItems = Replace(baseline, payload, payload with { PayloadProjectedAreaM2 = -1 })
            }),
            Case("zero rope MBL", baseline with
            {
                AssemblyItems = Replace(baseline, line, line with
                {
                    RopePreset = line.RopePreset! with { BreakingLoadKn = 0 }
                })
            }),
            Case("negative rope MBL", baseline with
            {
                AssemblyItems = Replace(baseline, line, line with
                {
                    RopePreset = line.RopePreset! with { BreakingLoadKn = -1 }
                })
            }),
            Case("typed NaN", baseline with { Buoy = baseline.Buoy with { ProjectedAreaM2 = double.NaN } }),
            Case("typed positive infinity", baseline with { Buoy = baseline.Buoy with { WeightKg = double.PositiveInfinity } }),
            Case("typed negative infinity", baseline with { Anchor = baseline.Anchor with { BaseHoldingCoefficient = double.NegativeInfinity } }),
            Case("negative connector Cd", baseline with
            {
                AssemblyItems = baseline.AssemblyItems.Append(new AssemblyItemInput(
                    AssemblyItemKind.Connector,
                    "Invalid connector",
                    true,
                    null,
                    ValidConnector() with { DragCoefficient = -1 },
                    0,
                    1,
                    0,
                    0,
                    0,
                    0)).ToArray()
            })
        };
    }

    private static void RequireBlockedBeforeCompletedRun(string name, CalculationRequest request)
    {
        ApplicationCalculationRun? completedRun = null;
        Exception? failure = null;

        try
        {
            completedRun = ApplicationCalculationRunner.Run(
                request.Environment,
                request.Buoy,
                request.AssemblyItems,
                request.Anchor,
                request.SafetyFactor);
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        if (completedRun is not null)
        {
            throw new InvalidOperationException(
                $"BC-AUD-004 {name}: invalid input created a completed run and provenance {completedRun.Snapshot.Provenance?.RunId}.");
        }

        if (failure is not EngineeringInputValidationException validationFailure ||
            string.IsNullOrWhiteSpace(validationFailure.Code) ||
            string.IsNullOrWhiteSpace(validationFailure.Field) ||
            !validationFailure.Message.Contains(BlockingMarker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"BC-AUD-004 {name}: expected a stable pre-core validation failure, got {failure?.GetType().Name ?? "no failure"}: {failure?.Message ?? "<none>"}.");
        }

        Console.WriteLine(
            $"BC_AUD_004_BLOCKED|Case={name}|CompletedRun=False|Provenance=False|Code={validationFailure.Code}|Field={validationFailure.Field}");
    }

    private static void RequireRawUiValueBlocked(string rawValue)
    {
        var viewModel = CreateCalculatedViewModel();
        viewModel.BuoyArea = rawValue;

        if (viewModel.IsCalculationCurrent || viewModel.CanExportPdf || viewModel.CanExportFullReport)
            throw new InvalidOperationException($"BC-AUD-004 raw '{rawValue}': BC-AUD-002 did not invalidate Run A.");

        viewModel.CalculateCommand.Execute(null);

        if (viewModel.IsCalculationCurrent ||
            viewModel.UserEngineeringReport is not null ||
            viewModel.SelectedShape is not null ||
            viewModel.CanExportPdf ||
            viewModel.CanExportFullReport ||
            !viewModel.ResultText.Contains(BlockingMarker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"BC-AUD-004 raw '{rawValue}': UI published authority or omitted the blocking diagnostic. Result='{viewModel.ResultText}'.");
        }
    }

    private static void ValidateSignedPositiveControls()
    {
        var request = ValidRequest();
        var signedProfile = request.Environment.EffectiveCurrentProfile
            .Select((point, index) => point with
            {
                EastCurrentMS = index == 0 ? -0.30 : -0.10,
                NorthCurrentMS = index == 0 ? -0.20 : -0.05,
                VerticalCurrentMS = index == 0 ? -0.04 : 0.02,
                WaterDensityKgM3 = index == 0 ? point.WaterDensityKgM3 : 0
            })
            .ToArray();
        var line = request.AssemblyItems.Single(x => x.Kind == AssemblyItemKind.Line);
        var signedLine = line with { RopePreset = line.RopePreset! with { WeightWaterKgM = -0.05 } };
        var payload = request.AssemblyItems.Single(x => x.Kind == AssemblyItemKind.Payload);
        var zeroPayload = payload with
        {
            PayloadWeightAirKg = 0,
            PayloadVolumeM3 = 0,
            PayloadProjectedAreaM2 = 0,
            PayloadDragCoefficient = 0
        };
        request = request with
        {
            Environment = request.Environment with { CurrentProfile = signedProfile },
            AssemblyItems = request.AssemblyItems
                .Select(x => ReferenceEquals(x, line) ? signedLine : ReferenceEquals(x, payload) ? zeroPayload : x)
                .ToArray()
        };

        var run = ApplicationCalculationRunner.Run(
            request.Environment,
            request.Buoy,
            request.AssemblyItems,
            request.Anchor,
            request.SafetyFactor);

        if (run.Snapshot.Provenance is null)
            throw new InvalidOperationException("BC-AUD-004 positive controls: valid signed inputs produced no provenance.");
        if (request.Environment.EffectiveCurrentProfile[0].EastCurrentMS != -0.30 ||
            request.Environment.EffectiveCurrentProfile[0].NorthCurrentMS != -0.20 ||
            request.Environment.EffectiveCurrentProfile[0].VerticalCurrentMS != -0.04 ||
            request.AssemblyItems.Single(x => x.Kind == AssemblyItemKind.Line).RopePreset!.WeightWaterKgM != -0.05 ||
            request.AssemblyItems.Single(x => x.Kind == AssemblyItemKind.Payload).PayloadWeightAirKg != 0)
        {
            throw new InvalidOperationException("BC-AUD-004 positive controls: signed inputs were normalized or changed.");
        }
    }

    private static void ValidateBcAud002Interaction()
    {
        var viewModel = CreateCalculatedViewModel();
        var runA = viewModel.UserEngineeringReport?.Provenance
            ?? throw new InvalidOperationException("BC-AUD-004/002: fixture Run A has no provenance.");
        var payload = viewModel.AssemblyItems.First(x => x.IsPayload);
        payload.PayloadWeightAirKg = "-20";

        if (viewModel.IsCalculationCurrent || viewModel.CanExportPdf || viewModel.CanExportFullReport)
            throw new InvalidOperationException("BC-AUD-004/002: invalid mutation retained Run A/export authority.");

        viewModel.CalculateCommand.Execute(null);

        if (viewModel.IsCalculationCurrent ||
            viewModel.UserEngineeringReport is not null ||
            viewModel.SelectedShape is not null ||
            viewModel.CanExportPdf ||
            viewModel.CanExportFullReport ||
            viewModel.ResultText.Contains(runA.RunId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("BC-AUD-004/002/005: failed validation restored Run A or created current authority.");
        }
    }

    private static void ValidateBcAud003Interaction()
    {
        var viewModel = new MainWindowViewModel();
        var dto = InvokeToDto(viewModel);
        dto.BuoyWeight = "-20";
        InvokeFromDto(viewModel, dto);

        if (viewModel.BuoyWeight != "-20")
            throw new InvalidOperationException("BC-AUD-004/003: persistence did not restore the invalid raw value exactly.");

        viewModel.CalculateCommand.Execute(null);
        if (viewModel.IsCalculationCurrent ||
            viewModel.UserEngineeringReport is not null ||
            viewModel.SelectedShape is not null ||
            viewModel.CanExportPdf ||
            viewModel.CanExportFullReport ||
            !viewModel.ResultText.Contains(BlockingMarker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("BC-AUD-004/003: invalid restored project created calculation authority.");
        }
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
        if (!viewModel.IsCalculationCurrent || viewModel.UserEngineeringReport?.Provenance is null)
            throw new InvalidOperationException("BC-AUD-004: valid UI fixture did not create Run A.");
        return viewModel;
    }

    private static CalculationRequest ValidRequest()
    {
        var environment = new EnvironmentInput(
            1025,
            50,
            0.2,
            0,
            0,
            SeabedCatalog.ById("sand"),
            true,
            new[]
            {
                new CurrentProfilePointInput(0, 0.2, 0, 0, 1025),
                new CurrentProfilePointInput(50, 0.1, 0, 0, 1025)
            });
        var rope = new RopePreset("audit:rope", "Audit rope", "Synthetic", 20, 70, 0.15, 1.2, "BC-AUD-004");
        var assembly = new AssemblyItemInput[]
        {
            new(AssemblyItemKind.Line, "Line", true, rope, null, 55, 1, 0, 0, 0, 0),
            new(AssemblyItemKind.Payload, "Payload", true, null, null, 0, 1, 20, 0.01, 0.03, 1.0)
        };
        return new CalculationRequest(
            environment,
            new BuoyInput("Buoy", 1, 100, 0.8, 0.8),
            assembly,
            new AnchorInput("Anchor", "Deadweight", "Concrete", 1000, 0.4, 1),
            3);
    }

    private static ConnectorPreset ValidConnector() =>
        new("audit:connector", "Audit connector", "Shackle", 2, 0.0002, 55, 0.004, 1.2, "BC-AUD-004");

    private static IReadOnlyList<AssemblyItemInput> Replace(
        CalculationRequest request,
        AssemblyItemInput original,
        AssemblyItemInput replacement) =>
        request.AssemblyItems.Select(x => ReferenceEquals(x, original) ? replacement : x).ToArray();

    private static InvalidCase Case(string name, CalculationRequest request) => new(name, request);

    private static BuoyProjectDto InvokeToDto(MainWindowViewModel viewModel)
    {
        var method = typeof(MainWindowViewModel).GetMethod("ToDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-004/003: MainWindowViewModel.ToDto was not found.");
        return (BuoyProjectDto)(method.Invoke(viewModel, null)
            ?? throw new InvalidOperationException("BC-AUD-004/003: MainWindowViewModel.ToDto returned null."));
    }

    private static void InvokeFromDto(MainWindowViewModel viewModel, BuoyProjectDto dto)
    {
        var method = typeof(MainWindowViewModel).GetMethod("FromDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-004/003: MainWindowViewModel.FromDto was not found.");
        try
        {
            method.Invoke(viewModel, new object[] { dto });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private sealed record InvalidCase(string Name, CalculationRequest Request);

    private sealed record CalculationRequest(
        EnvironmentInput Environment,
        BuoyInput Buoy,
        IReadOnlyList<AssemblyItemInput> AssemblyItems,
        AnchorInput Anchor,
        double SafetyFactor);
}
