using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class UserResultTextBuilder
{
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
}
