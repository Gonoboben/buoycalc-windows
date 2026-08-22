namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v1.0.0";
    public const string VersionNote = "Release Candidate — инженерная модель F1–F4 заморожена";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
