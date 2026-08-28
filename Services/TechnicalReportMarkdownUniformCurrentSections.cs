namespace BuoyCalc.Windows.Services;

// Compatibility bridge for the frozen v1 technical-report assembly path.
// The historical scalar/uniform-current section is retired and intentionally emits no text.
internal static class TechnicalReportMarkdownUniformCurrentSections
{
    public static bool TryAppend(string methodName, object[] args)
    {
        return methodName == "AppendUniformCurrentNormalVectorRows";
    }
}
