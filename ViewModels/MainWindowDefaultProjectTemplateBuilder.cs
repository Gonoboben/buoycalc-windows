using System.Collections.Generic;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ViewModels;

internal enum MainWindowDefaultProjectValueSource
{
    Literal,
    CurrentSpeed,
    Depth,
    WaterDensity
}

internal sealed record MainWindowDefaultProjectValue(
    string Value,
    MainWindowDefaultProjectValueSource Source = MainWindowDefaultProjectValueSource.Literal);

internal sealed record MainWindowDefaultCurrentProfilePointTemplate(
    MainWindowDefaultProjectValue DepthM,
    MainWindowDefaultProjectValue EastCurrentMS,
    MainWindowDefaultProjectValue NorthCurrentMS,
    MainWindowDefaultProjectValue VerticalCurrentMS,
    MainWindowDefaultProjectValue WaterDensityKgM3);

internal sealed record MainWindowDefaultAssemblyItemTemplate(
    string Kind,
    string Title,
    string? RopePresetStorageId = null,
    string? ConnectorPresetStorageId = null,
    string? PayloadPresetStorageId = null,
    string? LengthM = null,
    string? Count = null);

internal sealed record MainWindowDefaultProjectTemplate(
    string ProjectName,
    string ProjectFilePath,
    string WaterDensity,
    string Depth,
    string CurrentSpeed,
    bool UseCurrentProfile,
    string WaveHeight,
    string WavePeriod,
    string SeabedPresetId,
    string? BuoyPresetId,
    string PreferredAnchorPresetId,
    string SafetyFactor,
    string ResultText,
    string ReportText,
    IReadOnlyList<MainWindowDefaultCurrentProfilePointTemplate> CurrentProfilePoints,
    IReadOnlyList<MainWindowDefaultAssemblyItemTemplate> AssemblyItems);

internal static class MainWindowDefaultProjectTemplateBuilder
{
    internal static MainWindowDefaultProjectTemplate Build()
    {
        return new MainWindowDefaultProjectTemplate(
            "Тестовый проект",
            ProjectJsonStorage.DefaultProjectPath,
            "1025",
            "50",
            "0.5",
            false,
            "1.0",
            "6.0",
            "unknown",
            null,
            "built-in:concrete_500",
            "5",
            "Нажмите «Рассчитать».",
            "",
            new[]
            {
                new MainWindowDefaultCurrentProfilePointTemplate(
                    Literal("0"),
                    CurrentSpeed(),
                    Literal("0"),
                    Literal("0"),
                    WaterDensity()),
                new MainWindowDefaultCurrentProfilePointTemplate(
                    Literal("10"),
                    Literal("0.45"),
                    Literal("0"),
                    Literal("0"),
                    WaterDensity()),
                new MainWindowDefaultCurrentProfilePointTemplate(
                    Literal("25"),
                    Literal("0.3"),
                    Literal("0"),
                    Literal("0"),
                    WaterDensity()),
                new MainWindowDefaultCurrentProfilePointTemplate(
                    Depth(),
                    Literal("0.1"),
                    Literal("0"),
                    Literal("0"),
                    WaterDensity())
            },
            new[]
            {
                new MainWindowDefaultAssemblyItemTemplate(
                    "Connector",
                    "Скоба под буем",
                    ConnectorPresetStorageId: "built-in:shackle_55",
                    Count: "1"),
                new MainWindowDefaultAssemblyItemTemplate(
                    "Line",
                    "Верхний буйреп",
                    RopePresetStorageId: "built-in:polyester_20",
                    LengthM: "45"),
                new MainWindowDefaultAssemblyItemTemplate(
                    "Connector",
                    "Вертлюг",
                    ConnectorPresetStorageId: "built-in:swivel_60",
                    Count: "1"),
                new MainWindowDefaultAssemblyItemTemplate(
                    "Payload",
                    "ADCP",
                    PayloadPresetStorageId: "built-in:adcp_40"),
                new MainWindowDefaultAssemblyItemTemplate(
                    "Line",
                    "Нижняя цепь",
                    RopePresetStorageId: "built-in:chain_10",
                    LengthM: "10")
            });
    }

    private static MainWindowDefaultProjectValue Literal(string value)
    {
        return new MainWindowDefaultProjectValue(value);
    }

    private static MainWindowDefaultProjectValue CurrentSpeed()
    {
        return new MainWindowDefaultProjectValue("", MainWindowDefaultProjectValueSource.CurrentSpeed);
    }

    private static MainWindowDefaultProjectValue Depth()
    {
        return new MainWindowDefaultProjectValue("", MainWindowDefaultProjectValueSource.Depth);
    }

    private static MainWindowDefaultProjectValue WaterDensity()
    {
        return new MainWindowDefaultProjectValue("", MainWindowDefaultProjectValueSource.WaterDensity);
    }
}
