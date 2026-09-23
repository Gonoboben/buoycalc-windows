using System.Globalization;
using System.Reflection;
using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;

internal static class ProjectReplayPersistenceRegression
{
    public static void ProjectReplay_UsesEmbeddedResolvedPresetSnapshotOrFails()
    {
        Console.WriteLine("BC_AUD_003_PROJECT_REPLAY_BEGIN");
        var source = new MainWindowViewModel
        {
            BuoyName = "Audited raw buoy",
            BuoyVolume = "9.99",
            BuoyWeight = "999",
            BuoyArea = "9.99",
            BuoyCd = "9.99"
        };
        source.AddCurrentProfilePointCommand.Execute(null);
        source.AddCurrentProfilePointCommand.Execute(null);
        source.CurrentProfilePoints[0].DepthM = "0";
        source.CurrentProfilePoints[0].EastCurrentMS = "0.20";
        source.CurrentProfilePoints[1].DepthM = source.Depth;
        source.CurrentProfilePoints[1].EastCurrentMS = "0.20";
        source.CalculateCommand.Execute(null);
        if (!source.IsCalculationCurrent)
            throw new InvalidOperationException("BC-AUD-003: fixture did not create a current Run A before project load.");

        var saved = InvokeToDto(source);
        var savedLine = saved.AssemblyItems.First(x => x.Kind == "Line");
        var savedConnector = saved.AssemblyItems.First(x => x.Kind == "Connector");
        if (savedLine.ResolvedRopePreset is null || savedConnector.ResolvedConnectorPreset is null)
            throw new InvalidOperationException("BC-AUD-003: save did not embed resolved line and connector engineering snapshots.");

        savedLine.RopePresetId = "user:deleted-rope";
        savedLine.ResolvedRopePreset = new ResolvedRopePresetDto
        {
            Id = "user:deleted-rope",
            Name = "Deleted snapshot rope",
            Material = "Snapshot material",
            DiameterMm = 17.75,
            BreakingLoadKn = 88.5,
            WeightWaterKgM = 0.321,
            DragCoefficient = 1.17
        };
        savedConnector.ConnectorPresetId = "user:deleted-connector";
        savedConnector.ResolvedConnectorPreset = new ResolvedConnectorPresetDto
        {
            Id = "user:deleted-connector",
            Name = "Deleted snapshot connector",
            Type = "Snapshot shackle",
            WeightAirKg = 4.5,
            VolumeM3 = 0.00123,
            BreakingLoadKn = 77.7,
            ProjectedAreaM2 = 0.009,
            DragCoefficient = 1.31
        };
        var savedPayload = saved.AssemblyItems.First(x => x.Kind == "Payload");
        savedPayload.PayloadPresetId = "user:deleted-payload";
        savedPayload.Title = "Persisted payload title";
        savedPayload.PayloadWeightAirKg = "41.5";
        savedPayload.PayloadVolumeM3 = "0.016";
        savedPayload.PayloadProjectedAreaM2 = "0.051";
        savedPayload.PayloadDragCoefficient = "1.07";

        var json = JsonSerializer.Serialize(saved);
        var roundTripped = JsonSerializer.Deserialize<BuoyProjectDto>(json)
            ?? throw new InvalidOperationException("BC-AUD-003: project JSON did not deserialize.");
        InvokeFromDto(source, roundTripped);

        if (source.IsCalculationCurrent || source.CanExportPdf || source.CanExportFullReport)
            throw new InvalidOperationException("BC-AUD-003/002: project load retained prior calculation/export authority.");
        RequireEqual("9.99", source.BuoyVolume, "buoy volume");
        RequireEqual("999", source.BuoyWeight, "buoy weight");
        RequireEqual("9.99", source.BuoyArea, "buoy projected area");
        RequireEqual("9.99", source.BuoyCd, "buoy drag coefficient");

        var restoredLine = source.AssemblyItems.First(x => x.IsLine).ToInput().RopePreset
            ?? throw new InvalidOperationException("BC-AUD-003: restored line has no rope engineering input.");
        RequireEqual("user:deleted-rope", restoredLine.Id, "embedded rope ID");
        RequireEqual("Deleted snapshot rope", restoredLine.Name, "embedded rope name");
        RequireDouble(17.75, restoredLine.DiameterMm, "embedded rope diameter");
        RequireDouble(88.5, restoredLine.BreakingLoadKn, "embedded rope MBL");
        RequireDouble(0.321, restoredLine.WeightWaterKgM, "embedded rope water weight");
        RequireDouble(1.17, restoredLine.DragCoefficient, "embedded rope Cd");

        var restoredConnector = source.AssemblyItems.First(x => x.IsConnector).ToInput().ConnectorPreset
            ?? throw new InvalidOperationException("BC-AUD-003: restored connector has no engineering input.");
        RequireEqual("user:deleted-connector", restoredConnector.Id, "embedded connector ID");
        RequireDouble(77.7, restoredConnector.BreakingLoadKn, "embedded connector MBL");
        RequireDouble(0.009, restoredConnector.ProjectedAreaM2, "embedded connector area");

        var restoredPayload = source.AssemblyItems.First(x => x.IsPayload);
        RequireEqual("user:deleted-payload", restoredPayload.PayloadPresetStorageId, "payload source ID");
        RequireEqual("Persisted payload title", restoredPayload.Title, "payload title");
        RequireEqual("41.5", restoredPayload.PayloadWeightAirKg, "payload weight");
        RequireEqual("0.016", restoredPayload.PayloadVolumeM3, "payload volume");

        source.CalculateCommand.Execute(null);
        var provenance = source.UserEngineeringReport?.Provenance
            ?? throw new InvalidOperationException("BC-AUD-003/005: replay did not create a new calculation provenance authority.");
        var expectedInputHash = ComputeExpectedInputHash(source);
        RequireEqual(expectedInputHash, provenance.InputHash, "replayed calculation InputHash");

        var replayDto = InvokeToDto(source);
        var replayLine = replayDto.AssemblyItems.First(x => x.Kind == "Line");
        RequireEqual("user:deleted-rope", replayLine.ResolvedRopePreset?.Id ?? string.Empty, "re-saved embedded rope ID");
        RequireDouble(17.75, replayLine.ResolvedRopePreset?.DiameterMm ?? double.NaN, "re-saved embedded rope diameter");

        Console.WriteLine(
            $"BC_AUD_003_PROJECT_REPLAY|BuoyRawExact=True|RopeEmbedded=True|ConnectorEmbedded=True|PayloadRaw=True|PriorRunInvalidated=True|InputHash={provenance.InputHash}");
        Console.WriteLine("BC_AUD_003_PROJECT_REPLAY_END");
    }

    public static void MissingPresetId_MustNotFallbackSilently()
    {
        var unsafeLegacy = new BuoyProjectDto
        {
            AssemblyItems =
            {
                new AssemblyItemDto
                {
                    IsEnabled = true,
                    Kind = "Line",
                    Title = "Deleted rope",
                    RopePresetId = "user:deleted-rope",
                    LengthM = "20.2"
                }
            }
        };
        RequireMissingDependencyFailure(
            () => ProjectReplayDependencyValidator.EnsureSafe(unsafeLegacy),
            "user:deleted-rope");

        var missingRope = new AssemblyItemViewModel
        {
            Kind = "Line",
            Title = "Deleted rope",
            RopePresetStorageId = "user:deleted-rope",
            LengthM = "20.2"
        };
        RequireMissingDependencyFailure(() => _ = missingRope.ToInput(), "user:deleted-rope");

        var unsafeConnector = new BuoyProjectDto
        {
            AssemblyItems =
            {
                new AssemblyItemDto
                {
                    IsEnabled = true,
                    Kind = "Connector",
                    Title = "Deleted connector",
                    ConnectorPresetId = "user:deleted-connector"
                }
            }
        };
        RequireMissingDependencyFailure(
            () => ProjectReplayDependencyValidator.EnsureSafe(unsafeConnector),
            "user:deleted-connector");

        var safeLegacy = new BuoyProjectDto
        {
            AssemblyItems =
            {
                new AssemblyItemDto
                {
                    IsEnabled = true,
                    Kind = "Line",
                    Title = "Existing legacy rope",
                    RopePresetId = "built-in:polyester_20",
                    LengthM = "20.2"
                }
            }
        };
        ProjectReplayDependencyValidator.EnsureSafe(safeLegacy);

        Console.WriteLine(
            "BC_AUD_003_MISSING_PRESET|MissingRopeBlocked=True|FirstBuiltInSubstitution=False|MissingConnectorBlocked=True|ExistingLegacyPresetReadable=True");
    }

    private static string ComputeExpectedInputHash(MainWindowViewModel viewModel)
    {
        var current = viewModel.CurrentProfilePoints
            .Select(x => x.ToInput())
            .OrderBy(x => x.DepthM)
            .ToArray();
        var environment = new EnvironmentInput(
            Parse(viewModel.WaterDensity),
            Parse(viewModel.Depth),
            current.Length == 0 ? 0 : current.Max(x => x.HorizontalSpeedMS),
            Parse(viewModel.WaveHeight),
            Parse(viewModel.WavePeriod),
            viewModel.SelectedSeabedPreset ?? SeabedCatalog.ById("unknown"),
            true,
            current);
        var buoy = new BuoyInput(
            viewModel.BuoyName,
            Parse(viewModel.BuoyVolume),
            Parse(viewModel.BuoyWeight),
            Parse(viewModel.BuoyArea),
            Parse(viewModel.BuoyCd));
        var anchor = new AnchorInput(
            viewModel.AnchorName,
            viewModel.AnchorType,
            viewModel.AnchorMaterial,
            Parse(viewModel.AnchorWeight),
            Parse(viewModel.AnchorVolume),
            Parse(viewModel.AnchorCoefficient));
        return CalculationRunFingerprint.ComputeInputHash(
            environment,
            buoy,
            viewModel.AssemblyItems.Select(x => x.ToInput()).ToArray(),
            anchor,
            Parse(viewModel.SafetyFactor));
    }

    private static BuoyProjectDto InvokeToDto(MainWindowViewModel viewModel)
    {
        var method = typeof(MainWindowViewModel).GetMethod("ToDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-003: MainWindowViewModel.ToDto was not found.");
        return (BuoyProjectDto)(method.Invoke(viewModel, null)
            ?? throw new InvalidOperationException("BC-AUD-003: MainWindowViewModel.ToDto returned null."));
    }

    private static void InvokeFromDto(MainWindowViewModel viewModel, BuoyProjectDto dto)
    {
        var method = typeof(MainWindowViewModel).GetMethod("FromDto", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BC-AUD-003: MainWindowViewModel.FromDto was not found.");
        try
        {
            method.Invoke(viewModel, new object[] { dto });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static void RequireMissingDependencyFailure(Action action, string expectedId)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains(expectedId, StringComparison.Ordinal) &&
            ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"BC-AUD-003: missing preset '{expectedId}' did not produce an explicit dependency failure.");
    }

    private static double Parse(string value)
    {
        value = (value ?? string.Empty).Replace(',', '.');
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static void RequireEqual(string expected, string actual, string field)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException($"BC-AUD-003: saved {field} '{expected}' restored as '{actual}'.");
    }

    private static void RequireDouble(double expected, double actual, string field)
    {
        if (BitConverter.DoubleToInt64Bits(expected) != BitConverter.DoubleToInt64Bits(actual))
            throw new InvalidOperationException($"BC-AUD-003: {field} expected {expected:R}, actual {actual:R}.");
    }
}
