using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class UserReportBuilder
{
    public static string Build(EnvironmentInput environment, CalculationResult result)
    {
        return UserResultTextBuilder.Build(environment, result);
    }
}
