using System.Globalization;

internal static class BoundaryConditionedFeedbackRollupRegression
{
    public static void Validate()
    {
        var original = Console.Out;
        using var capture = new StringWriter(CultureInfo.InvariantCulture);
        string output;

        try
        {
            Console.SetOut(capture);
            BoundaryConditionedFeedbackCouplingRegression.Validate();
            output = capture.ToString();
        }
        catch
        {
            output = capture.ToString();
            Console.SetOut(original);
            original.Write(output);
            throw;
        }
        finally
        {
            Console.SetOut(original);
        }

        original.Write(output);

        var summaries = output
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(IsSummaryLine)
            .ToList();

        original.WriteLine($"BOUNDARY_FEEDBACK_ROLLUP_BEGIN|Count={summaries.Count}");
        foreach (var line in summaries)
            original.WriteLine($"BOUNDARY_FEEDBACK_ROLLUP|{line}");
        original.WriteLine("BOUNDARY_FEEDBACK_ROLLUP_END");
    }

    private static bool IsSummaryLine(string line) =>
        line.StartsWith("BOUNDARY_FEEDBACK_SCENARIO|", StringComparison.Ordinal) ||
        line.StartsWith("BOUNDARY_FEEDBACK_BUDGET|", StringComparison.Ordinal) ||
        line.StartsWith("BOUNDARY_FEEDBACK_TERMINAL|", StringComparison.Ordinal);
}
