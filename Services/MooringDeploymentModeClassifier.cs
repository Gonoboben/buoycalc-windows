using System;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum MooringDeploymentMode
{
    Unknown,
    Surface,
    Submerged,
    ShortLine,
    ExcessLine,
    Overloaded
}

public sealed record MooringDeploymentModeResult(
    MooringDeploymentMode Mode,
    string ModeCode,
    string Title,
    double DepthM,
    double LineLengthM,
    double LineToDepthRatio,
    double ExcessLineM,
    double ShortageM,
    double BuoyDepthM,
    double NetBuoyancyKg,
    bool IsSurfaceMode,
    bool IsSubmergedMode,
    bool IsShortLineMode,
    bool IsExcessLineMode,
    string Status,
    string MethodNote);

public static class MooringDeploymentModeClassifier
{
    private const double DepthToleranceM = 0.01;
    private const double SurfaceDepthToleranceM = 0.05;
    private const double ExcessLineRatio = 1.20;

    public static MooringDeploymentModeResult Build(
        EnvironmentInput environment,
        CalculationResult result,
        MooringShapeResult primaryShape)
    {
        var depthM = Math.Max(0, environment.DepthM > 0 ? environment.DepthM : primaryShape.DepthM);
        var lineLengthM = Math.Max(0, result.LineLengthM > 0 ? result.LineLengthM : primaryShape.LineLengthM);
        var ratio = depthM > DepthToleranceM ? lineLengthM / depthM : 0;
        var shortageM = Math.Max(0, depthM - lineLengthM);
        var excessLineM = Math.Max(0, lineLengthM - depthM);
        var buoyDepthM = primaryShape.BuoyPoint?.ZDepthM ?? 0;
        var netBuoyancyKg = result.NetBuoyancyKg;

        var mode = ResolveMode(primaryShape, depthM, lineLengthM, ratio, buoyDepthM, netBuoyancyKg);
        var title = Title(mode);
        var modeCode = ModeCode(mode);
        var status = Status(mode, shortageM, excessLineM, buoyDepthM, ratio);

        return new MooringDeploymentModeResult(
            mode,
            modeCode,
            title,
            depthM,
            lineLengthM,
            ratio,
            excessLineM,
            shortageM,
            buoyDepthM,
            netBuoyancyKg,
            mode == MooringDeploymentMode.Surface,
            mode == MooringDeploymentMode.Submerged,
            mode == MooringDeploymentMode.ShortLine,
            mode == MooringDeploymentMode.ExcessLine,
            status,
            "v0.41: режим постановки классифицируется отдельно от solver. Классификатор не меняет силы, натяжения или форму; он только помечает расчёт как surface/submerged/short/excess line/overloaded для отчёта и будущих правил solver.");
    }

    private static MooringDeploymentMode ResolveMode(
        MooringShapeResult primaryShape,
        double depthM,
        double lineLengthM,
        double ratio,
        double buoyDepthM,
        double netBuoyancyKg)
    {
        if (depthM <= DepthToleranceM || lineLengthM <= DepthToleranceM)
        {
            return MooringDeploymentMode.Unknown;
        }

        if (netBuoyancyKg <= 0 || primaryShape.BuoyState == BuoyShapeState.Overloaded)
        {
            return MooringDeploymentMode.Overloaded;
        }

        if (lineLengthM + DepthToleranceM < depthM)
        {
            return MooringDeploymentMode.ShortLine;
        }

        if (buoyDepthM > Math.Max(SurfaceDepthToleranceM, depthM * 0.01) || primaryShape.BuoyState == BuoyShapeState.Submerged)
        {
            return MooringDeploymentMode.Submerged;
        }

        if (ratio >= ExcessLineRatio)
        {
            return MooringDeploymentMode.ExcessLine;
        }

        if (primaryShape.BuoyState == BuoyShapeState.Surface || buoyDepthM <= Math.Max(SurfaceDepthToleranceM, depthM * 0.01))
        {
            return MooringDeploymentMode.Surface;
        }

        return MooringDeploymentMode.Unknown;
    }

    private static string ModeCode(MooringDeploymentMode mode)
    {
        return mode switch
        {
            MooringDeploymentMode.Surface => "surface",
            MooringDeploymentMode.Submerged => "submerged",
            MooringDeploymentMode.ShortLine => "short",
            MooringDeploymentMode.ExcessLine => "excess line",
            MooringDeploymentMode.Overloaded => "overloaded",
            _ => "unknown"
        };
    }

    private static string Title(MooringDeploymentMode mode)
    {
        return mode switch
        {
            MooringDeploymentMode.Surface => "Поверхностная постановка",
            MooringDeploymentMode.Submerged => "Погружённая постановка",
            MooringDeploymentMode.ShortLine => "Короткая линия",
            MooringDeploymentMode.ExcessLine => "Избыточная длина линии",
            MooringDeploymentMode.Overloaded => "Перегруженный буй",
            _ => "Режим не определён"
        };
    }

    private static string Status(
        MooringDeploymentMode mode,
        double shortageM,
        double excessLineM,
        double buoyDepthM,
        double ratio)
    {
        return mode switch
        {
            MooringDeploymentMode.Surface => $"OK: буй у поверхности; L/Depth={ratio:0.####}",
            MooringDeploymentMode.Submerged => $"INFO: буй ниже поверхности; zбуя={buoyDepthM:0.####} м",
            MooringDeploymentMode.ShortLine => $"WARNING: линия короче глубины на {shortageM:0.####} м",
            MooringDeploymentMode.ExcessLine => $"INFO: линия длиннее глубины на {excessLineM:0.####} м; L/Depth={ratio:0.####}",
            MooringDeploymentMode.Overloaded => "WARNING: чистая плавучесть неположительная или буй перегружен",
            _ => "WARNING: недостаточно данных для классификации режима постановки"
        };
    }
}
