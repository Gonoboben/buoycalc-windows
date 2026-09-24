using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class UserReportBuilder
{
    public static string Build(EnvironmentInput environment, CalculationSnapshot snapshot)
    {
        var physicalDisposition = snapshot.PhysicalDisposition;
        if (physicalDisposition is not null)
        {
            return $"Вердикт: {physicalDisposition.Verdict}\n" +
                   $"Главный риск: {physicalDisposition.MainRisk}\n" +
                   $"Код физического отказа: {physicalDisposition.DiagnosticCode}\n" +
                   $"Грунт: {environment.Seabed.DisplayName}\n" +
                   $"Течение расчётное: {environment.EffectiveCurrentSpeedMS:0.###} м/с\n" +
                   $"Чистая плавучесть: {snapshot.Result.NetBuoyancyKg:0.##} кг\n" +
                   "Расчётная форма X/Z: недоступна — signed candidate физически отклонён\n" +
                   "Selected F1/F2/F3: недоступны, поскольку физически допустимая selected-геометрия не существует";
        }

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
        var designTension = assessment.DesignTensionDemandKn.HasValue
            ? $"{assessment.DesignTensionDemandKn.Value:0.##} кН"
            : "недоступна";
        var anchorContact = assessment.AnchorContactClassification.HasValue
            ? AnchorContactText(assessment.AnchorContactClassification.Value)
            : "недоступен — F2 authority не создаётся при неположительном весе якоря в воде";
        var anchorDemand = assessment.AnchorHorizontalDemandN.HasValue
            ? $"{assessment.AnchorHorizontalDemandN.Value:0.##} Н"
            : "недоступна";
        var anchorCapacity = assessment.AnchorHorizontalCapacityDisposition.HasValue
            ? "требуется отдельная валидированная модель якорь/грунт"
            : "недоступна без F2; capacity authority не синтезирована";

        return $"Вердикт: {assessment.Verdict}\n" +
               $"Главный риск: {assessment.MainRisk}\n" +
               $"Грунт: {environment.Seabed.DisplayName}\n" +
               $"Течение расчётное: {environment.EffectiveCurrentSpeedMS:0.###} м/с\n" +
               $"Чистая плавучесть: {snapshot.Result.NetBuoyancyKg:0.##} кг\n" +
               $"Расчётная selected design-нагрузка: {designTension}\n" +
               $"Определяющий локальный несущий элемент: {governingElement}\n" +
               $"Локальный запас определяющего элемента: {governingReserve}\n" +
               $"Контакт якоря: {anchorContact}\n" +
               $"Горизонтальная selected-нагрузка на якорь: {anchorDemand}\n" +
               $"Горизонтальная удерживающая способность якоря: {anchorCapacity}";
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
