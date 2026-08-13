using System.Text.Json;

internal static class FeedbackGoldenDiffEvidence
{
    private const double AbsoluteTolerance = 1e-8;
    private const double RelativeTolerance = 1e-8;

    public static void Print(string[] args)
    {
        if (args.Length != 2 || args[0] != "--verify")
        {
            return;
        }

        var expectedPath = Path.GetFullPath(args[1]);
        if (!File.Exists(expectedPath))
        {
            return;
        }

        var actualPath = Path.Combine(Path.GetTempPath(), $"buoycalc-feedback-actual-{Guid.NewGuid():N}.json");
        try
        {
            var writeResult = Program.Main(new[] { "--write-baseline", actualPath });
            if (writeResult != 0 || !File.Exists(actualPath))
            {
                Console.Error.WriteLine("FEEDBACK_GOLDEN_DIFF: unable to generate temporary actual baseline.");
                return;
            }

            using var expectedDocument = JsonDocument.Parse(File.ReadAllText(expectedPath));
            using var actualDocument = JsonDocument.Parse(File.ReadAllText(actualPath));
            var differences = new List<string>();
            CollectDifferences(
                expectedDocument.RootElement,
                actualDocument.RootElement,
                "$",
                differences);

            Console.Error.WriteLine("BEGIN_FEEDBACK_GOLDEN_DIFF");
            Console.Error.WriteLine($"DifferenceCount={differences.Count}");
            foreach (var difference in differences)
            {
                Console.Error.WriteLine(difference);
            }
            Console.Error.WriteLine("END_FEEDBACK_GOLDEN_DIFF");
        }
        finally
        {
            try
            {
                if (File.Exists(actualPath))
                {
                    File.Delete(actualPath);
                }
            }
            catch
            {
                // Evidence cleanup must not mask the real regression result.
            }
        }
    }

    private static void CollectDifferences(
        JsonElement expected,
        JsonElement actual,
        string path,
        List<string> differences)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            differences.Add($"{path}: kind {expected.ValueKind} -> {actual.ValueKind}");
            return;
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var expectedProperties = expected.EnumerateObject()
                    .ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
                var actualProperties = actual.EnumerateObject()
                    .ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
                var allNames = expectedProperties.Keys
                    .Union(actualProperties.Keys, StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal);

                foreach (var name in allNames)
                {
                    if (!expectedProperties.TryGetValue(name, out var expectedValue))
                    {
                        differences.Add($"{path}.{name}: property added");
                        continue;
                    }

                    if (!actualProperties.TryGetValue(name, out var actualValue))
                    {
                        differences.Add($"{path}.{name}: property removed");
                        continue;
                    }

                    CollectDifferences(expectedValue, actualValue, path + "." + name, differences);
                }
                break;
            }
            case JsonValueKind.Array:
            {
                var expectedItems = expected.EnumerateArray().ToArray();
                var actualItems = actual.EnumerateArray().ToArray();
                if (expectedItems.Length != actualItems.Length)
                {
                    differences.Add($"{path}: length {expectedItems.Length} -> {actualItems.Length}");
                }

                var count = Math.Min(expectedItems.Length, actualItems.Length);
                for (var index = 0; index < count; index++)
                {
                    CollectDifferences(expectedItems[index], actualItems[index], $"{path}[{index}]", differences);
                }
                break;
            }
            case JsonValueKind.Number:
            {
                if (expected.TryGetInt64(out var expectedInteger) && actual.TryGetInt64(out var actualInteger))
                {
                    if (expectedInteger != actualInteger)
                    {
                        differences.Add($"{path}: {expectedInteger} -> {actualInteger}");
                    }
                    break;
                }

                var expectedNumber = expected.GetDouble();
                var actualNumber = actual.GetDouble();
                if (!NearlyEqual(expectedNumber, actualNumber))
                {
                    differences.Add($"{path}: {expectedNumber:R} -> {actualNumber:R}");
                }
                break;
            }
            case JsonValueKind.String:
            {
                var expectedString = expected.GetString();
                var actualString = actual.GetString();
                if (!string.Equals(expectedString, actualString, StringComparison.Ordinal))
                {
                    differences.Add($"{path}: '{expectedString}' -> '{actualString}'");
                }
                break;
            }
            case JsonValueKind.True:
            case JsonValueKind.False:
            {
                var expectedBoolean = expected.GetBoolean();
                var actualBoolean = actual.GetBoolean();
                if (expectedBoolean != actualBoolean)
                {
                    differences.Add($"{path}: {expectedBoolean} -> {actualBoolean}");
                }
                break;
            }
            case JsonValueKind.Null:
                break;
            default:
                differences.Add($"{path}: unsupported kind {expected.ValueKind}");
                break;
        }
    }

    private static bool NearlyEqual(double expected, double actual)
    {
        if (!double.IsFinite(expected) || !double.IsFinite(actual))
        {
            return false;
        }

        var difference = Math.Abs(expected - actual);
        if (difference <= AbsoluteTolerance)
        {
            return true;
        }

        return difference <= RelativeTolerance * Math.Max(1.0, Math.Max(Math.Abs(expected), Math.Abs(actual)));
    }
}
