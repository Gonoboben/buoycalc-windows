using System;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record VerdictDisplayResult(
    string Verdict,
    string MainRisk,
    string Note);

public static class VerdictDisplayAdvisor
{
    private const double HighAnchorReserve = 10.0;

    public static VerdictDisplayResult Build(EnvironmentInput environment, CalculationResult result)
    {
        if (result.AnchorWeightWaterKg <= 0 || result.AnchorHoldingKg <= 0)
        {
            return new VerdictDisplayResult(
                "Не подходит",
                UserStatusPolicy.ToUserRisk("ERROR: якорь имеет нулевой или отрицательный вес в воде; удержание невозможно."),
                "Отрицательный или нулевой вес якоря в воде имеет приоритет над остальными рисками. Такой якорь не должен считаться удерживающим элементом.");
        }

        if (environment.DepthM > 0 && result.LineLengthM < environment.DepthM)
        {
            return new VerdictDisplayResult(
                "Не подходит",
                UserStatusPolicy.ToUserRisk("FAILED: линия короче глубины; поверхностная постановка невозможна, буй будет под водой."),
                "Волновая нагрузка в расчёте не отключается. Предупреждение относится к геометрии постановки: длины линии недостаточно для поверхностного положения буя.");
        }

        if (CanDowngradeRockDeadweightWarning(environment, result))
        {
            return new VerdictDisplayResult(
                "Подходит с примечанием",
                "Критичных рисков не найдено; каменистый грунт снижает предсказуемость контакта грузового якоря, но запас якоря большой.",
                "Предупреждение по каменистому грунту сохранено как инженерное примечание. Оно не должно переводить проект в 'Требуется проверка', если после грунтового коэффициента запас якоря остаётся большим.");
        }

        return new VerdictDisplayResult(
            UserStatusPolicy.ToUserVerdict(result.Verdict),
            UserStatusPolicy.ToUserRisk(result.MainRisk),
            "Отображаемый вердикт совпадает с исходным вердиктом расчётного ядра.");
    }

    private static bool CanDowngradeRockDeadweightWarning(EnvironmentInput environment, CalculationResult result)
    {
        var hasHardFailure = result.Checks.Any(IsHardFailure);
        if (hasHardFailure)
        {
            return false;
        }

        var isRock = ContainsAny(environment.Seabed.Name, "кам", "rock") ||
                     ContainsAny(environment.Seabed.DisplayName, "кам", "rock") ||
                     ContainsAny(environment.Seabed.Note, "кам", "rock");
        if (!isRock)
        {
            return false;
        }

        var looksLikeDeadweight = result.ElementRows.Any(x =>
            x.Kind == "Якорь" &&
            (ContainsAny(x.Title, "concrete", "бетон", "deadweight", "груз") ||
             ContainsAny(x.PresetName, "concrete", "бетон", "deadweight", "груз")));

        return looksLikeDeadweight &&
               result.AnchorReserve >= HighAnchorReserve &&
               result.TensionReserve >= 1.0 &&
               result.AnchorHoldingKg > result.RequiredAnchorHoldingKg;
    }

    private static bool IsHardFailure(string value)
    {
        value ??= string.Empty;
        return value.StartsWith("FAILED", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(string? value, params string[] parts)
    {
        value ??= string.Empty;
        return parts.Any(part => value.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
}
