using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

/// <summary>
/// Research-only direct A03/A05 non-interference verification for the INTERNAL observer.
/// Compares complete public property trees with raw IEEE-754 bits, not rounded text,
/// and checks the existing selected F1-F4 authority remains unmodified.
/// </summary>
internal static class ObserverControlAudit
{
    public static string Verify(
        string scenario,
        EnvironmentInput environment,
        BuoyInput buoy,
        ApplicationCalculationRun run,
        MooringSignedCandidateStatus expected)
    {
        var reference = run.Snapshot.SignedCandidate
            ?? throw new InvalidOperationException($"{scenario}: reference candidate missing.");
        if (reference.Status != expected)
            throw new InvalidOperationException($"{scenario}: expected {expected}, got {reference.Status}.");

        var beforeCandidate = Canonical(reference);
        var beforeSelected = SelectedAuthority(run);
        var steps = 0;
        var observed = MooringSignedCandidateEvaluator.BuildWithDiagnosticObserver(
            environment, buoy, run.Result, run.Snapshot.TechnicalReportData.SequencePositions,
            step =>
            {
                if (step.Iteration != steps + 1 || step.Fields.Count == 0)
                    throw new InvalidOperationException($"{scenario}: invalid observer iteration or empty evidence.");
                steps++;
            });
        var observedCanonical = Canonical(observed);
        if (!StringComparer.Ordinal.Equals(beforeCandidate, observedCanonical))
            throw new InvalidOperationException(
                $"{scenario}: observer changed a full candidate property tree (bit-exact).");
        if (!StringComparer.Ordinal.Equals(beforeCandidate, Canonical(reference)) ||
            !StringComparer.Ordinal.Equals(beforeSelected, SelectedAuthority(run)))
            throw new InvalidOperationException(
                $"{scenario}: observer mutated the original application snapshot or F1-F4.");

        if (expected == MooringSignedCandidateStatus.RejectedPhysical)
        {
            if (steps != 0 || observed.ExactFixedPointReached ||
                observed.Shape is not null || observed.FinalTensionTrace is not null ||
                run.Snapshot.SelectedShape is not null)
                throw new InvalidOperationException(
                    $"{scenario}: a rejected boundary ran observer steps or acquired shape authority.");
        }
        else if (expected == MooringSignedCandidateStatus.Accepted)
        {
            if (steps <= 0 || steps != observed.FeedbackIterations ||
                !observed.ExactFixedPointReached || observed.Shape is null ||
                observed.FinalTensionTrace is null ||
                run.Snapshot.SelectedShape is null ||
                run.Snapshot.SelectedDesignTensionDemand is null ||
                run.Snapshot.SelectedAnchorReaction is null ||
                run.Snapshot.SelectedLocalStructuralCapacity is null ||
                run.Snapshot.SelectedEngineeringAssessment is null)
                throw new InvalidOperationException(
                    $"{scenario}: Accepted observer violated selected authority or iteration contract.");
        }
        else
        {
            throw new InvalidOperationException($"{scenario}: unsupported observer-control status {expected}.");
        }

        var referenceSha = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(beforeCandidate)));
        var observedSha = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(observedCanonical)));
        Console.WriteLine($"BC_AUD_006_OBSERVER_CONTROL|Scenario={scenario}|Status={expected}|Steps={steps}|CandidateBitIdentical=True|SelectedF1F2F3F4Unchanged=True|SolverModified=False");
        return string.Join(",",
            scenario, expected.ToString(), steps.ToString(CultureInfo.InvariantCulture),
            "True", "True", referenceSha, observedSha);
    }

    private static string SelectedAuthority(ApplicationCalculationRun run)
    {
        var snapshot = run.Snapshot;
        return Canonical(new object?[]
        {
            snapshot.SelectedShape,
            snapshot.SelectedDesignTensionDemand,
            snapshot.SelectedAnchorReaction,
            snapshot.SelectedLocalStructuralCapacity,
            snapshot.SelectedEngineeringAssessment
        });
    }

    private static string Canonical(object? source)
    {
        var builder = new StringBuilder();
        Visit(source, "$", builder, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return builder.ToString();
    }

    private static void Visit(object? source, string path, StringBuilder builder, HashSet<object> visiting)
    {
        if (source is null)
        {
            Atom(path, "null", builder);
            return;
        }

        var type = source.GetType();
        switch (source)
        {
            case double d:
                Atom(path, "double:" + BitConverter.DoubleToInt64Bits(d).ToString("X16", CultureInfo.InvariantCulture), builder);
                return;
            case float f:
                Atom(path, "float:" + BitConverter.SingleToInt32Bits(f).ToString("X8", CultureInfo.InvariantCulture), builder);
                return;
            case decimal m:
                Atom(path, "decimal:" + string.Join(":", decimal.GetBits(m).Select(x => x.ToString("X8", CultureInfo.InvariantCulture))), builder);
                return;
            case string s:
                Atom(path, "string:" + s, builder);
                return;
            case DateTime t:
                Atom(path, "datetime:" + t.ToBinary().ToString("X16", CultureInfo.InvariantCulture), builder);
                return;
            case DateTimeOffset t:
                Atom(path, "datetimeoffset:" + t.Ticks.ToString("X16", CultureInfo.InvariantCulture) +
                    ":" + t.Offset.Ticks.ToString(CultureInfo.InvariantCulture), builder);
                return;
            case Guid g:
                Atom(path, "guid:" + g.ToString("D"), builder);
                return;
        }

        if (type.IsEnum || type.IsPrimitive)
        {
            Atom(path, type.FullName + ":" + Convert.ToString(source, CultureInfo.InvariantCulture), builder);
            return;
        }

        bool guard = !type.IsValueType;
        if (guard && !visiting.Add(source))
            throw new InvalidOperationException($"Cycle detected in control result property tree at {path}.");
        try
        {
            if (source is IEnumerable sequence)
            {
                var index = 0;
                foreach (var value in sequence)
                    Visit(value, path + "[" + (index++).ToString(CultureInfo.InvariantCulture) + "]", builder, visiting);
                Atom(path + "#count", index.ToString(CultureInfo.InvariantCulture), builder);
                return;
            }

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.GetIndexParameters().Length == 0)
                .OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            if (properties.Length == 0)
            {
                Atom(path, type.FullName + ":" + Convert.ToString(source, CultureInfo.InvariantCulture), builder);
                return;
            }

            Atom(path + "#type", type.FullName ?? type.Name, builder);
            foreach (var property in properties)
                Visit(property.GetValue(source), path + "." + property.Name, builder, visiting);
        }
        finally
        {
            if (guard) visiting.Remove(source);
        }
    }

    private static void Atom(string path, string value, StringBuilder builder)
    {
        builder.Append(path.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(path);
        builder.Append('=').Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('\n');
    }
}
