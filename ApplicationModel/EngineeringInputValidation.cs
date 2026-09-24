using System;
using System.Collections.Generic;
using System.Globalization;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ApplicationModel;

public sealed class EngineeringInputValidationException : InvalidOperationException
{
    public EngineeringInputValidationException(
        string code,
        string field,
        string suppliedValue,
        string diagnostic)
        : base($"BC-AUD-004 [{code}] {field}: {diagnostic} Получено: {suppliedValue}.")
    {
        Code = code;
        Field = field;
        SuppliedValue = suppliedValue;
        Diagnostic = diagnostic;
    }

    public string Code { get; }
    public string Field { get; }
    public string SuppliedValue { get; }
    public string Diagnostic { get; }
}

public static class EngineeringNumberParser
{
    public static double ParseFinite(string field, string? rawValue)
    {
        var original = rawValue ?? string.Empty;
        var normalized = original.Replace(',', '.');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw Failure(
                "NUMERIC_PARSE",
                field,
                Quote(original),
                "требуется числовое значение в инвариантном формате");
        }

        if (!double.IsFinite(value))
        {
            throw Failure(
                "NON_FINITE",
                field,
                Quote(original),
                "значение должно быть конечным числом; NaN и Infinity недопустимы");
        }

        return value;
    }

    public static int ParseInteger(string field, string? rawValue)
    {
        var original = rawValue ?? string.Empty;
        if (!int.TryParse(original, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw Failure(
                "INTEGER_PARSE",
                field,
                Quote(original),
                "требуется целое числовое значение");
        }

        return value;
    }

    private static EngineeringInputValidationException Failure(
        string code,
        string field,
        string value,
        string diagnostic) => new(code, field, value, diagnostic);

    private static string Quote(string value) => $"'{value}'";
}

/// <summary>
/// Authoritative physical-domain gate for production calculation requests.
/// It must run before input fingerprinting and before the calculation core.
/// Signed current components and RopePreset.WeightWaterKgM intentionally have
/// no sign restriction; they are required only to be finite.
/// </summary>
public static class EngineeringInputValidator
{
    public static void Validate(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(buoy);
        ArgumentNullException.ThrowIfNull(assemblyItems);
        ArgumentNullException.ThrowIfNull(anchor);

        Positive("Environment.WaterDensityKgM3", environment.WaterDensityKgM3);
        Positive("Environment.DepthM", environment.DepthM);
        Nonnegative("Environment.CurrentSpeedMS", environment.CurrentSpeedMS);
        Nonnegative("Environment.WaveHeightM", environment.WaveHeightM);
        Nonnegative("Environment.WavePeriodS", environment.WavePeriodS);
        if (environment.WaveHeightM > 0 && environment.WavePeriodS <= 0)
        {
            throw Failure(
                "WAVE_PERIOD_REQUIRED",
                "Environment.WavePeriodS",
                environment.WavePeriodS,
                "период волны должен быть больше нуля при положительной высоте волны");
        }

        if (environment.Seabed is null)
        {
            throw Failure("REQUIRED", "Environment.Seabed", "<null>", "грунт должен быть задан");
        }
        Nonnegative("Environment.Seabed.HoldingMultiplier", environment.Seabed.HoldingMultiplier);

        for (var index = 0; index < environment.EffectiveCurrentProfile.Count; index++)
        {
            var point = environment.EffectiveCurrentProfile[index];
            var prefix = $"Environment.CurrentProfile[{index}]";
            Nonnegative(prefix + ".DepthM", point.DepthM);
            Finite(prefix + ".EastCurrentMS", point.EastCurrentMS);
            Finite(prefix + ".NorthCurrentMS", point.NorthCurrentMS);
            Finite(prefix + ".VerticalCurrentMS", point.VerticalCurrentMS);
            Nonnegative(prefix + ".WaterDensityKgM3", point.WaterDensityKgM3);
        }

        Positive("Buoy.VolumeM3", buoy.VolumeM3);
        Nonnegative("Buoy.WeightKg", buoy.WeightKg);
        Nonnegative("Buoy.ProjectedAreaM2", buoy.ProjectedAreaM2);
        Nonnegative("Buoy.DragCoefficient", buoy.DragCoefficient);

        Nonnegative("Anchor.WeightAirKg", anchor.WeightAirKg);
        Nonnegative("Anchor.VolumeM3", anchor.VolumeM3);
        Nonnegative("Anchor.BaseHoldingCoefficient", anchor.BaseHoldingCoefficient);
        Positive("SafetyFactor", safetyFactor);

        for (var index = 0; index < assemblyItems.Count; index++)
        {
            var item = assemblyItems[index];
            if (!item.IsEnabled)
            {
                continue;
            }

            var prefix = $"AssemblyItems[{index}]";
            PositiveInteger(prefix + ".Count", item.Count);
            switch (item.Kind)
            {
                case AssemblyItemKind.Line:
                    ValidateLine(prefix, item);
                    break;
                case AssemblyItemKind.Connector:
                    ValidateConnector(prefix, item);
                    break;
                case AssemblyItemKind.Payload:
                    ValidatePayload(prefix, item);
                    break;
                default:
                    throw Failure("UNSUPPORTED_KIND", prefix + ".Kind", item.Kind.ToString(), "неподдерживаемый тип элемента");
            }
        }
    }

    private static void ValidateLine(string prefix, AssemblyItemInput item)
    {
        if (item.RopePreset is null)
        {
            throw Failure("REQUIRED", prefix + ".RopePreset", "<null>", "для активной линии требуется resolved rope preset");
        }

        Positive(prefix + ".LengthM", item.LengthM);
        Positive(prefix + ".RopePreset.DiameterMm", item.RopePreset.DiameterMm);
        Positive(prefix + ".RopePreset.BreakingLoadKn", item.RopePreset.BreakingLoadKn);
        Finite(prefix + ".RopePreset.WeightWaterKgM", item.RopePreset.WeightWaterKgM);
        Nonnegative(prefix + ".RopePreset.DragCoefficient", item.RopePreset.DragCoefficient);
    }

    private static void ValidateConnector(string prefix, AssemblyItemInput item)
    {
        if (item.ConnectorPreset is null)
        {
            throw Failure("REQUIRED", prefix + ".ConnectorPreset", "<null>", "для активного соединителя требуется resolved connector preset");
        }

        Nonnegative(prefix + ".ConnectorPreset.WeightAirKg", item.ConnectorPreset.WeightAirKg);
        Nonnegative(prefix + ".ConnectorPreset.VolumeM3", item.ConnectorPreset.VolumeM3);
        Positive(prefix + ".ConnectorPreset.BreakingLoadKn", item.ConnectorPreset.BreakingLoadKn);
        Nonnegative(prefix + ".ConnectorPreset.ProjectedAreaM2", item.ConnectorPreset.ProjectedAreaM2);
        Nonnegative(prefix + ".ConnectorPreset.DragCoefficient", item.ConnectorPreset.DragCoefficient);
    }

    private static void ValidatePayload(string prefix, AssemblyItemInput item)
    {
        Nonnegative(prefix + ".PayloadWeightAirKg", item.PayloadWeightAirKg);
        Nonnegative(prefix + ".PayloadVolumeM3", item.PayloadVolumeM3);
        Nonnegative(prefix + ".PayloadProjectedAreaM2", item.PayloadProjectedAreaM2);
        Nonnegative(prefix + ".PayloadDragCoefficient", item.PayloadDragCoefficient);
    }

    private static void Finite(string field, double value)
    {
        if (!double.IsFinite(value))
        {
            throw Failure("NON_FINITE", field, value, "значение должно быть конечным числом; NaN и Infinity недопустимы");
        }
    }

    private static void Nonnegative(string field, double value)
    {
        Finite(field, value);
        if (value < 0)
        {
            throw Failure("NONNEGATIVE_REQUIRED", field, value, "значение не должно быть отрицательным");
        }
    }

    private static void Positive(string field, double value)
    {
        Finite(field, value);
        if (value <= 0)
        {
            throw Failure("POSITIVE_REQUIRED", field, value, "значение должно быть больше нуля");
        }
    }

    private static void PositiveInteger(string field, int value)
    {
        if (value <= 0)
        {
            throw Failure("POSITIVE_INTEGER_REQUIRED", field, value.ToString(CultureInfo.InvariantCulture), "количество должно быть положительным целым числом");
        }
    }

    private static EngineeringInputValidationException Failure(
        string code,
        string field,
        double value,
        string diagnostic) =>
        Failure(code, field, value.ToString("R", CultureInfo.InvariantCulture), diagnostic);

    private static EngineeringInputValidationException Failure(
        string code,
        string field,
        string value,
        string diagnostic) => new(code, field, value, diagnostic);
}
