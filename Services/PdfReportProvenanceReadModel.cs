using BuoyCalc.Windows.ApplicationModel;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// PDF-specific presentation of retained run provenance plus a separately owned
/// export timestamp. No identity or fingerprint is created by this projection.
/// </summary>
public sealed record PdfReportProvenanceReadModel(
    string RunId,
    DateTimeOffset CalculationTimestampUtc,
    string InputHash,
    string ResultHash,
    string SourceIdentity,
    DateTimeOffset ExportTimestampUtc);

public static class PdfReportProvenanceReadModelProjector
{
    public static PdfReportProvenanceReadModel Project(
        CalculationRunProvenance provenance,
        DateTimeOffset exportTimestampUtc)
    {
        ArgumentNullException.ThrowIfNull(provenance);

        return new PdfReportProvenanceReadModel(
            provenance.RunId,
            provenance.CalculationTimestampUtc,
            provenance.InputHash,
            provenance.ResultHash,
            provenance.SourceIdentity,
            exportTimestampUtc.ToUniversalTime());
    }
}
