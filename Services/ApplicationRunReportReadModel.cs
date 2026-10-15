using System.Globalization;
using System.Text;
using BuoyCalc.Windows.ApplicationModel;

namespace BuoyCalc.Windows.Services;

public abstract record ApplicationRunReportReadModel(CalculationRunProvenance Provenance);

public sealed record CalculatedApplicationRunReportReadModel(
    UserEngineeringReportReadModel Report)
    : ApplicationRunReportReadModel(Report.Provenance);

public sealed record PreflightPhysicalRejectionReportReadModel : ApplicationRunReportReadModel
{
    public PreflightPhysicalRejectionReportReadModel(
        string projectName,
        CalculationRunProvenance provenance,
        PreflightPhysicalRejectionKind classification,
        string diagnosticCode,
        PreflightPhysicalRejectionVerdict verdictKind,
        string verdict,
        bool hasHardFailure,
        bool blocksEngineeringGeometry,
        ShortLinePreflightPhysicalRejectionEvidence evidence)
        : base(provenance)
    {
        ProjectName = projectName;
        Classification = classification;
        DiagnosticCode = diagnosticCode;
        VerdictKind = verdictKind;
        Verdict = verdict;
        HasHardFailure = hasHardFailure;
        BlocksEngineeringGeometry = blocksEngineeringGeometry;
        Evidence = evidence;
    }

    public string ProjectName { get; }
    public PreflightPhysicalRejectionKind Classification { get; }
    public string DiagnosticCode { get; }
    public PreflightPhysicalRejectionVerdict VerdictKind { get; }
    public string Verdict { get; }
    public bool HasHardFailure { get; }
    public bool BlocksEngineeringGeometry { get; }
    public ShortLinePreflightPhysicalRejectionEvidence Evidence { get; }
}

public static class ApplicationRunReportReadModelProjector
{
    public static CalculatedApplicationRunReportReadModel ProjectCalculated(
        UserEngineeringReportReadModel report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new CalculatedApplicationRunReportReadModel(report);
    }

    public static PreflightPhysicalRejectionReportReadModel ProjectPreflightPhysicalRejected(
        string projectName,
        PreflightPhysicalRejectedApplicationRunOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        var rejection = outcome.Rejection;
        if (rejection.Classification != PreflightPhysicalRejectionKind.LineShorterThanDepth)
        {
            throw new InvalidOperationException(
                $"Unsupported preflight physical-rejection classification: {rejection.Classification}.");
        }

        return new PreflightPhysicalRejectionReportReadModel(
            string.IsNullOrWhiteSpace(projectName) ? "BuoyCalc Project" : projectName.Trim(),
            outcome.Provenance,
            rejection.Classification,
            rejection.DiagnosticCode,
            rejection.VerdictKind,
            rejection.Verdict,
            rejection.HasHardFailure,
            rejection.BlocksEngineeringGeometry,
            rejection.Evidence);
    }
}

public static class PreflightPhysicalRejectionReportBuilder
{
    public static string BuildUserResult(PreflightPhysicalRejectionReportReadModel report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var evidence = report.Evidence;
        return string.Join(
            Environment.NewLine,
            $"Вердикт: {report.Verdict}",
            $"Причина: активная длина линии {F(evidence.AvailableActiveLineLengthM)} м меньше глубины постановки {F(evidence.DepthM)} м.",
            $"Минимальная активная длина линии для этой постановки: {F(evidence.MinimumRequiredActiveLineLengthM)} м.",
            $"Дефицит длины: {F(evidence.DeficitM)} м.",
            $"Что исправить: увеличьте суммарную активную длину линии как минимум на {F(evidence.DeficitM)} м — до не менее {F(evidence.MinimumRequiredActiveLineLengthM)} м.",
            "Расчётная геометрия, нагрузки и проверки F1/F2/F3/F4 не вычислялись, потому что обязательное геометрическое условие не выполнено.",
            $"Код диагностики: {report.DiagnosticCode}");
    }

    public static string BuildTechnicalReport(PreflightPhysicalRejectionReportReadModel report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var evidence = report.Evidence;
        var provenance = report.Provenance;
        var text = new StringBuilder();
        text.AppendLine("# BuoyCalc Windows — завершённый preflight physical-rejection отчёт");
        text.AppendLine();
        text.AppendLine($"- Проект: {report.ProjectName}");
        text.AppendLine("- Outcome kind: PreflightPhysicalRejected");
        text.AppendLine($"- Классификация: {report.Classification}");
        text.AppendLine($"- Код диагностики: {report.DiagnosticCode}");
        text.AppendLine($"- Вердикт: {report.Verdict}");
        text.AppendLine($"- Hard failure: {report.HasHardFailure}");
        text.AppendLine($"- Блокирует инженерную геометрию: {report.BlocksEngineeringGeometry}");
        text.AppendLine();
        text.AppendLine("## Инженерные evidence");
        text.AppendLine();
        text.AppendLine($"- Глубина постановки: {F(evidence.DepthM)} м");
        text.AppendLine($"- Активная длина линии: {F(evidence.AvailableActiveLineLengthM)} м");
        text.AppendLine($"- Минимальная требуемая активная длина линии: {F(evidence.MinimumRequiredActiveLineLengthM)} м");
        text.AppendLine($"- Дефицит длины: {F(evidence.DeficitM)} м");
        text.AppendLine();
        text.AppendLine("## Что исправить");
        text.AppendLine();
        text.AppendLine($"Увеличьте суммарную активную длину линии как минимум на {F(evidence.DeficitM)} м — до не менее {F(evidence.MinimumRequiredActiveLineLengthM)} м.");
        text.AppendLine();
        text.AppendLine("## Граница authority");
        text.AppendLine();
        text.AppendLine("- CalculationResult: отсутствует; calculation core не запускался.");
        text.AppendLine("- CalculationSnapshot: отсутствует.");
        text.AppendLine("- Selected X/Z: отсутствует.");
        text.AppendLine("- F1/F2/F3/F4: отсутствуют.");
        text.AppendLine("- Инженерные нагрузки и таблица сегментов: не вычислялись и не публикуются.");
        text.AppendLine();
        text.AppendLine("## Provenance");
        text.AppendLine();
        text.AppendLine($"- Run ID: {provenance.RunId}");
        text.AppendLine($"- Calculation timestamp UTC: {provenance.CalculationTimestampUtc.ToUniversalTime():O}");
        text.AppendLine($"- Input hash (SHA-256): {provenance.InputHash}");
        text.AppendLine($"- Result hash (SHA-256): {provenance.ResultHash}");
        text.AppendLine($"- Source identity: {provenance.SourceIdentity}");
        return text.ToString().TrimEnd();
    }

    private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
