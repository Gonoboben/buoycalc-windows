using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class UserReportBuilder
{
    public static string Build(EnvironmentInput environment, CalculationSnapshot snapshot)
    {
        var assessment = snapshot.SelectedEngineeringAssessment;
        if (assessment is null)
        {
            return Build(environment, snapshot.Result);
        }

        var governingElement = assessment.GoverningWeakLinkElementNumber.HasValue
            ? $"#{assessment.GoverningWeakLinkElementNumber.Value} {assessment.GoverningWeakLinkTitle} / {assessment.GoverningWeakLinkPresetName}"
            : "не определён среди элементов с доступной локальной capacity-моделью";
        var governingReserve = assessment.GoverningWeakLinkReserve.HasValue
            ? assessment.GoverningWeakLinkReserve.Value.ToString("0.##")
            : "не определён";

        return $"Вердикт: {assessment.Verdict}\n" +
               $"Главный риск: {assessment.MainRisk}\n" +
               $"Грунт: {environment.Seabed.DisplayName}\n" +
               $"Течение расчётное: {environment.EffectiveCurrentSpeedMS:0.###} м/с\n" +
               $"Чистая плавучесть: {snapshot.Result.NetBuoyancyKg:0.##} кг\n" +
               $"Расчётная selected design-нагрузка: {assessment.DesignTensionDemandKn:0.##} кН\n" +
               $"Определяющий локальный несущий элемент: {governingElement}\n" +
               $"Локальный запас определяющего элемента: {governingReserve}\n" +
               $"Контакт якоря: {AnchorContactText(assessment.AnchorContactClassification)}\n" +
               $"Горизонтальная selected-нагрузка на якорь: {assessment.AnchorHorizontalDemandN:0.##} Н\n" +
               "Горизонтальная удерживающая способность якоря: требуется отдельная валидированная модель якорь/грунт";
    }

    public static string Build(EnvironmentInput environment, CalculationResult result)
    {
        var display = VerdictDisplayAdvisor.Build(environment, result);

        return $"Вердикт: {display.Verdict}\n" +
               $"Главный риск: {display.MainRisk}\n" +
               $"Грунт: {environment.Seabed.DisplayName}\n" +
               $"Течение расчётное: {environment.EffectiveCurrentSpeedMS:0.###} м/с\n" +
               $"Чистая плавучесть: {result.NetBuoyancyKg:0.##} кг\n" +
               $"Нагрузка слабого звена: {result.TensionKn:0.##} кН\n" +
               $"Слабое звено: {result.WeakLinkName}\n" +
               $"Запас слабого звена: {result.TensionReserve:0.##}\n" +
               $"Запас якоря: {result.AnchorReserve:0.##}";
    }

    private static string AnchorContactText(MooringAnchorContactClassification classification)
    {
        return classification switch
        {
            MooringAnchorContactClassification.CompressiveContact => "сжимающий контакт",
            MooringAnchorContactClassification.ZeroNormalLimit => "предел нулевой нормальной реакции",
            MooringAnchorContactClassification.UpliftSeparation => "расчётный отрыв rigid-body contact state",
            _ => classification.ToString()
        };
    }
}
