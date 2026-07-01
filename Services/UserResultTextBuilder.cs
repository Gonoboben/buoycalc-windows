using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class UserResultTextBuilder
{
    public static string Build(EnvironmentInput environment, CalculationResult result)
    {
        return UserReportBuilder.Build(environment, result);
    }
}
