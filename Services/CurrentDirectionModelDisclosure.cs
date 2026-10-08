namespace BuoyCalc.Windows.Services;

/// <summary>
/// BC-AUD-008: presentation-only limitation text for the approved
/// magnitude-based v1 current model. This has no solver/input authority.
/// Keep the text consistent across XAML, Full TXT and typed PDF.
/// </summary>
public static class CurrentDirectionModelDisclosure
{
    public const string Notice = "Ограничение v1: азимут оси +X используется только для INFO-проекции; East/North не задают направление расчётной X/Z-формы. Расчёт учитывает |Uгор|.";
    public const string Detail = "Базовые силы течения вычисляются по |Uгор| = sqrt(East² + North²); shape-based совместимая модель использует (|Uгор|, W). Азимут +X и географическое направление East/North не управляют solver, selected X/Z и проверками F1–F4, якоря и слабого звена. Исходные компоненты East/North сохраняются для профиля и диагностической INFO-проекции. Это не географически ориентированная физическая модель течения.";
}
