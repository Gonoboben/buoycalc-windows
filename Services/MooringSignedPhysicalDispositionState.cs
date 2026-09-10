namespace BuoyCalc.Windows.Services;

/// <summary>
/// Typed terminal engineering disposition for a signed candidate that has already
/// been classified by the calculation core as physically rejected. This state does
/// not create geometry, tension, anchor reaction, or structural-capacity authority.
/// </summary>
public sealed record MooringSignedPhysicalDispositionState(
    MooringShapeSourceIdentity SourceIdentity,
    MooringSignedCandidateStatus CandidateStatus,
    string Verdict,
    string MainRiskCode,
    string MainRisk,
    bool HasHardFailure,
    bool BlocksEngineeringGeometry,
    string DiagnosticCode,
    string DiagnosticText,
    string MethodNote);

public static class MooringSignedPhysicalDispositionStateProjector
{
    public static MooringSignedPhysicalDispositionState? Project(
        MooringSignedCandidateResult? signedCandidate)
    {
        if (signedCandidate is null ||
            signedCandidate.Status != MooringSignedCandidateStatus.RejectedPhysical)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(signedCandidate.DiagnosticCode))
        {
            throw new InvalidOperationException(
                "RejectedPhysical signed candidate must preserve a diagnostic code before physical disposition projection.");
        }

        if (string.IsNullOrWhiteSpace(signedCandidate.DiagnosticText))
        {
            throw new InvalidOperationException(
                "RejectedPhysical signed candidate must preserve diagnostic text before physical disposition projection.");
        }

        return new MooringSignedPhysicalDispositionState(
            signedCandidate.SourceIdentity,
            signedCandidate.Status,
            "Не подходит",
            signedCandidate.DiagnosticCode,
            signedCandidate.DiagnosticText,
            HasHardFailure: true,
            BlocksEngineeringGeometry: true,
            signedCandidate.DiagnosticCode,
            signedCandidate.DiagnosticText,
            "Physical rejection is terminal engineering evidence from the signed candidate; no fallback X/Z, F1, F2, or F3 authority is fabricated.");
    }
}
