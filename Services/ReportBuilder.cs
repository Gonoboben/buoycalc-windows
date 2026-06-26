using System.Linq;
using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class ReportBuilder
{
    public static string Build(
        string projectName,
        EnvironmentInput environment,
        BuoyInput buoy,
        AnchorInput anchor,
        CalculationResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# BuoyCalc Windows — предварительный отчёт");
        sb.AppendLine();
        sb.AppendLine($"Проект: {projectName}");
        sb.AppendLine($"Вердикт: {result.Verdict}");
        sb.AppendLine($"Главный риск: {result.MainRisk}");
        sb.AppendLine();

        sb.AppendLine("## Условия");
        sb.AppendLine($"- Плотность воды: {environment.WaterDensityKgM3:0} кг/м³");
        sb.AppendLine($"- Глубина: {environment.DepthM:0.##} м");
        sb.AppendLine($"- Течение: {environment.CurrentSpeedMS:0.##} м/с");
        sb.AppendLine($"- Волна: {environment.WaveHeightM:0.##} м / {environment.WavePeriodS:0.##} с");
        sb.AppendLine($"- Грунт: {environment.Seabed.Name}");
        sb.AppendLine();

        sb.AppendLine("## Буй");
        sb.AppendLine($"- Название: {buoy.Name}");
        sb.AppendLine($"- Объём: {buoy.VolumeM3:0.###} м³");
        sb.AppendLine($"- Масса: {buoy.WeightKg:0.##} кг");
        sb.AppendLine($"- Cd: {buoy.DragCoefficient:0.##}");
        sb.AppendLine();

        sb.AppendLine("## Якорь");
        sb.AppendLine($"- Название: {anchor.Name}");
        sb.AppendLine($"- Тип: {anchor.Type}");
        sb.AppendLine($"- Материал: {anchor.Material}");
        sb.AppendLine($"- Масса: {anchor.WeightAirKg:0.##} кг");
        sb.AppendLine();

        sb.AppendLine("## Итоги");
        sb.AppendLine($"- Плавучесть: {result.BuoyancyKg:0.##} кг");
        sb.AppendLine($"- Вес в воде: {result.TotalWeightWaterKg:0.##} кг");
        sb.AppendLine($"- Чистая плавучесть: {result.NetBuoyancyKg:0.##} кг");
        sb.AppendLine($"- Горизонтальная сила: {result.HorizontalForceN:0.##} Н");
        sb.AppendLine($"- Натяжение: {result.TensionKn:0.##} кН");
        sb.AppendLine($"- WLL: {result.WorkingLoadKn:0.##} кН");
        sb.AppendLine($"- Запас натяжения: {result.TensionReserve:0.##}");
        sb.AppendLine($"- Удержание якоря: {result.AnchorHoldingKg:0.##} кг");
        sb.AppendLine($"- Запас якоря: {result.AnchorReserve:0.##}");
        sb.AppendLine($"- Длина линии: {result.LineLengthM:0.##} м");
        sb.AppendLine($"- Оценочный снос: {result.EstimatedOffsetM:0.##} м");
        sb.AppendLine();

        sb.AppendLine("## Проверки");
        foreach (var check in result.Checks)
        {
            sb.AppendLine($"- {check}");
        }

        sb.AppendLine();
        sb.AppendLine("## Ограничения");
        sb.AppendLine("Расчёт является предварительным. Для реального проектирования нужны паспортные данные элементов, проверка грунта, динамика и инженерная верификация.");

        return sb.ToString();
    }
}
